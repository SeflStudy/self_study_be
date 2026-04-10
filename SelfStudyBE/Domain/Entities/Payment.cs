namespace Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int PlanId { get; set; }
 
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
 
    public string Provider { get; set; } = "PayOS";
    public string TransactionCode { get; set; } = string.Empty;
    // Pending | Success | Failed
    public string Status { get; set; } = string.Empty;
 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
 
    public SubscriptionPlan Plan { get; set; } = null!;
}