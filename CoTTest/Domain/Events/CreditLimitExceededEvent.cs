namespace CoTTest.Domain.Events;

/// <summary>
/// Domain event raised when a billing cycle's total charges exceed the customer's credit limit.
/// </summary>
public sealed class CreditLimitExceededEvent
{
    /// <summary>
    /// The ID of the billing cycle that exceeded the credit limit.
    /// </summary>
    public Guid BillingCycleId { get; }

    /// <summary>
    /// The total amount charged in the billing cycle at the time of the event.
    /// </summary>
    public decimal TotalAmount { get; }

    /// <summary>
    /// The customer's credit limit that was exceeded.
    /// </summary>
    public decimal CreditLimit { get; }

    /// <summary>
    /// The timestamp when the credit limit was exceeded.
    /// </summary>
    public DateTime OccurredAt { get; }

    /// <summary>
    /// The amount by which the credit limit was exceeded.
    /// </summary>
    public decimal ExcessAmount => TotalAmount - CreditLimit;

    public CreditLimitExceededEvent(Guid billingCycleId, decimal totalAmount, decimal creditLimit)
    {
        if (billingCycleId == Guid.Empty)
            throw new ArgumentException("BillingCycleId cannot be empty.", nameof(billingCycleId));

        if (totalAmount < 0)
            throw new ArgumentException("TotalAmount cannot be negative.", nameof(totalAmount));

        if (creditLimit < 0)
            throw new ArgumentException("CreditLimit cannot be negative.", nameof(creditLimit));

        BillingCycleId = billingCycleId;
        TotalAmount = totalAmount;
        CreditLimit = creditLimit;
        OccurredAt = DateTime.UtcNow;
    }
}
