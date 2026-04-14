using Application.DTOs.Payment;
using Application.Interfaces.Payment;
using Domain.Entities;
using Infrastructure.Configurations;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly PayOSClient _payOS;
    private readonly PayOSSettings _payOSSettings;

    public PaymentService(AppDbContext context, IOptions<PayOSSettings> payOSSettings)
    {
        _context = context;
        _payOSSettings = payOSSettings.Value;

        _payOS = new PayOSClient(
            _payOSSettings.ClientId,
            _payOSSettings.ApiKey,
            _payOSSettings.ChecksumKey
        );
    }

    public async Task<PaymentLinkResponse> CreatePaymentLinkAsync(CreatePaymentRequest request, string userId)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId);
        if (plan == null)
            throw new InvalidOperationException("Gói VIP không tồn tại.");

        
        var hasActiveSubscription = await _context.UserSubscriptions
            .AnyAsync(us => us.UserId == userId 
                          && us.Status == "Active" 
                          && us.EndDate > DateTime.UtcNow);

        if (hasActiveSubscription)
            throw new InvalidOperationException("Bạn đang có gói VIP hoạt động. Không thể nâng cấp thêm lúc này.");

        
        var orderCode = long.Parse(DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")) % 9000000000 + 1000000000;

        var payment = new Payment
        {
            UserId = userId,
            PlanId = plan.Id,
            Amount = plan.Price,
            Currency = "VND",
            Provider = "PayOS",
            TransactionCode = orderCode.ToString(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        
        var paymentRequest = new CreatePaymentLinkRequest
        {
            OrderCode = (int)orderCode,
            Amount = (int)plan.Price,
            Description = $"Nâng cấp{plan.Name}",
            ReturnUrl = _payOSSettings.ReturnUrl,
            CancelUrl = _payOSSettings.CancelUrl,
            
        };

        var createPaymentResponse = await _payOS.PaymentRequests.CreateAsync(paymentRequest);

        return new PaymentLinkResponse(
            PaymentId: payment.Id,
            CheckoutUrl: createPaymentResponse.CheckoutUrl ?? string.Empty,
            OrderCode: orderCode.ToString()
        );
    }

    public async Task HandlePaymentWebhookAsync(Webhook webhook)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var verifiedData = await _payOS.Webhooks.VerifyAsync(webhook);

            // 1. Verify request hợp lệ
            if (verifiedData.Code != "00")
            {
                Console.WriteLine($"Invalid webhook: {verifiedData.Description2}");
                return;
            }

            var orderCode = verifiedData.OrderCode.ToString();

            // 2. Lấy payment
            var payment = await _context.Payments
                .Include(p => p.Plan)
                .FirstOrDefaultAsync(p => p.TransactionCode == orderCode);

            if (payment == null)
            {
                Console.WriteLine($"Payment not found: {orderCode}");
                return;
            }

            //  3. IDEMPOTENT (quan trọng nhất)
            if (payment.Status == "Success")
            {
                Console.WriteLine($"Webhook duplicate ignored: {orderCode}");
                return;
            }

            // 4. Check trạng thái thanh toán
            if (verifiedData.Description == "PAID")
            {
                payment.Status = "Success";
                payment.PaidAt = DateTime.UtcNow;

                
                var existingSub = await _context.UserSubscriptions
                    .AnyAsync(x => x.UserId == payment.UserId 
                                && x.PlanId == payment.PlanId
                                && x.StartDate >= payment.CreatedAt);

                if (!existingSub)
                {
                    var subscription = new UserSubscription
                    {
                        UserId = payment.UserId,
                        PlanId = payment.PlanId,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddDays(payment.Plan.DurationDays),
                        Status = "Active"
                    };

                    _context.UserSubscriptions.Add(subscription);
                }

                Console.WriteLine($" Payment success: {orderCode}");
            }
            else
            {
                payment.Status = "Failed";
                Console.WriteLine($" Payment failed: {orderCode}");
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($" Webhook error: {ex.Message}");
        }
    }

    public async Task<UserSubscriptionDto> GetCurrentSubscriptionAsync(string userId)
    {
        var activeSub = await _context.UserSubscriptions
            .Include(us => us.Plan)
            .Where(us => us.UserId == userId 
                      && us.Status == "Active" 
                      && us.EndDate > DateTime.UtcNow)
            .OrderByDescending(us => us.EndDate)
            .FirstOrDefaultAsync();

        if (activeSub == null)
        {
            return new UserSubscriptionDto(0, "Free", DateTime.UtcNow, DateTime.UtcNow, "Expired");
        }

        return new UserSubscriptionDto(
            activeSub.PlanId,
            activeSub.Plan.Name,
            activeSub.StartDate,
            activeSub.EndDate,
            activeSub.Status
        );
    }
}