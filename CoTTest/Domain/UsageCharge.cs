namespace CoTTest.Domain;

/// <summary>
/// Entity representing a single charge within a billing cycle.
/// </summary>
public sealed class UsageCharge
{
    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; }
    public DateTime ChargeDate { get; private set; }

    public UsageCharge(decimal amount, string description, DateTime chargeDate)
    {
        if (amount <= 0)
            throw new ArgumentException("Charge amount must be greater than zero.", nameof(amount));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Charge description cannot be empty.", nameof(description));

        Id = Guid.NewGuid();
        Amount = amount;
        Description = description;
        ChargeDate = chargeDate;
    }

    // EF Core constructor
    private UsageCharge() 
    { 
        Description = string.Empty;
    }
}
