namespace Domain.Entities;

public class SubscriptionPlan
{
    public int Id { get; set; }
    // Free | VIP Monthly | VIP Yearly
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationDays { get; set; }
 
    public int MaxFlashcardsPerDay { get; set; }
    public int MaxQuizPerDay { get; set; }
    public int MaxAIUsagePerDay { get; set; }
 
    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class UserSubscription
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int PlanId { get; set; }
 
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    // Active | Expired | Cancelled
    public string Status { get; set; } = string.Empty;
 
    public SubscriptionPlan Plan { get; set; } = null!;
}