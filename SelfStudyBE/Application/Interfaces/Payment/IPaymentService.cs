using Application.DTOs.Payment;

using PayOS.Models.Webhooks;

namespace Application.Interfaces.Payment;

public interface IPaymentService
{
    Task<PaymentLinkResponse> CreatePaymentLinkAsync(CreatePaymentRequest request, string userId);
    Task HandlePaymentWebhookAsync(Webhook webhookData);
    Task<UserSubscriptionDto> GetCurrentSubscriptionAsync(string userId);
}