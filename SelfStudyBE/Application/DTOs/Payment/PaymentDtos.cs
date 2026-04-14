namespace Application.DTOs.Payment;

public record CreatePaymentRequest(
    int PlanId
);

public record PaymentLinkResponse(
    int PaymentId,
    string CheckoutUrl,
    string OrderCode
);

public record PaymentSuccessDto(
    string OrderCode,
    decimal Amount,
    string Status
);

public record UserSubscriptionDto(
    int PlanId,
    string PlanName,
    DateTime StartDate,
    DateTime EndDate,
    string Status
);