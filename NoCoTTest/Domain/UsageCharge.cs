namespace NoCoTTest.Domain;

public class UsageCharge
{
    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; }
    public DateTime ChargeDate { get; private set; }

    public UsageCharge(decimal amount, string description, DateTime chargeDate)
    {
        Id = Guid.NewGuid();
        Amount = amount;
        Description = description;
        ChargeDate = chargeDate;
    }

    private UsageCharge() { }
}
