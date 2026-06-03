namespace NoCoTTest.Domain.Events;

public class CreditLimitExceededEvent
{
    public Guid BillingCycleId { get; }
    public decimal TotalAmount { get; }
    public decimal CreditLimit { get; }
    public DateTime OccurredAt { get; }

    public CreditLimitExceededEvent(Guid billingCycleId, decimal totalAmount, decimal creditLimit)
    {
        BillingCycleId = billingCycleId;
        TotalAmount = totalAmount;
        CreditLimit = creditLimit;
        OccurredAt = DateTime.UtcNow;
    }
}
