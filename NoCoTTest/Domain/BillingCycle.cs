using NoCoTTest.Domain.Events;

namespace NoCoTTest.Domain;

public class BillingCycle
{
    private readonly List<UsageCharge> _charges = new();
    private readonly List<object> _domainEvents = new();

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal CreditLimit { get; private set; }
    public BillingCycleStatus Status { get; private set; }
    public IReadOnlyCollection<UsageCharge> Charges => _charges.AsReadOnly();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    public decimal TotalAmount => _charges.Sum(c => c.Amount);

    public BillingCycle(Guid customerId, DateTime startDate, DateTime endDate, decimal creditLimit)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        StartDate = startDate;
        EndDate = endDate;
        CreditLimit = creditLimit;
        Status = BillingCycleStatus.Open;
    }

    private BillingCycle() { }

    public void AddCharge(decimal amount, string description, DateTime chargeDate)
    {
        if (Status == BillingCycleStatus.Closed || Status == BillingCycleStatus.UnderReview)
        {
            throw new InvalidOperationException($"Cannot add charges when billing cycle is {Status}");
        }

        var charge = new UsageCharge(amount, description, chargeDate);
        _charges.Add(charge);

        if (TotalAmount > CreditLimit)
        {
            Status = BillingCycleStatus.ReviewRequired;
            var domainEvent = new CreditLimitExceededEvent(Id, TotalAmount, CreditLimit);
            _domainEvents.Add(domainEvent);
        }
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
