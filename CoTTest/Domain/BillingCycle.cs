using CoTTest.Domain.Events;

namespace CoTTest.Domain;

/// <summary>
/// Aggregate Root representing a customer's billing cycle with usage charges.
/// Enforces business rules around charge addition and credit limit monitoring.
/// </summary>
public sealed class BillingCycle
{
    private readonly List<UsageCharge> _charges = new();
    private readonly List<object> _domainEvents = new();

    /// <summary>
    /// Unique identifier for this billing cycle.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The customer to whom this billing cycle belongs.
    /// </summary>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// The start date of the billing cycle.
    /// </summary>
    public DateTime StartDate { get; private set; }

    /// <summary>
    /// The end date of the billing cycle.
    /// </summary>
    public DateTime EndDate { get; private set; }

    /// <summary>
    /// The maximum credit limit allowed for this billing cycle.
    /// </summary>
    public decimal CreditLimit { get; private set; }

    /// <summary>
    /// Current status of the billing cycle.
    /// </summary>
    public BillingCycleStatus Status { get; private set; }

    /// <summary>
    /// Read-only collection of usage charges in this billing cycle.
    /// </summary>
    public IReadOnlyCollection<UsageCharge> Charges => _charges.AsReadOnly();

    /// <summary>
    /// Read-only collection of domain events raised by this aggregate.
    /// </summary>
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Computed total amount of all charges in this billing cycle.
    /// </summary>
    public decimal TotalAmount => _charges.Sum(c => c.Amount);

    /// <summary>
    /// Creates a new billing cycle for a customer.
    /// </summary>
    public BillingCycle(Guid customerId, DateTime startDate, DateTime endDate, decimal creditLimit)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty.", nameof(customerId));

        if (endDate <= startDate)
            throw new ArgumentException("EndDate must be after StartDate.", nameof(endDate));

        if (creditLimit < 0)
            throw new ArgumentException("CreditLimit cannot be negative.", nameof(creditLimit));

        Id = Guid.NewGuid();
        CustomerId = customerId;
        StartDate = startDate;
        EndDate = endDate;
        CreditLimit = creditLimit;
        Status = BillingCycleStatus.Open;
    }

    // EF Core constructor
    private BillingCycle() { }

    /// <summary>
    /// Adds a new usage charge to this billing cycle.
    /// </summary>
    /// <param name="amount">The charge amount.</param>
    /// <param name="description">Description of what the charge is for.</param>
    /// <param name="chargeDate">The date the charge occurred.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to add charges to a closed or under-review billing cycle.
    /// </exception>
    public void AddCharge(decimal amount, string description, DateTime chargeDate)
    {
        // Step 1: Pre-condition guard - validate current status
        if (Status == BillingCycleStatus.Closed)
        {
            throw new InvalidOperationException(
                "Cannot add charges to a closed billing cycle. Closed cycles are immutable for audit and compliance purposes.");
        }

        if (Status == BillingCycleStatus.UnderReview)
        {
            throw new InvalidOperationException(
                "Cannot add charges to a billing cycle under review. The cycle is locked pending manual review.");
        }

        // Step 2: Entity creation with validation
        var charge = new UsageCharge(amount, description, chargeDate);

        // Step 3: State mutation
        _charges.Add(charge);

        // Step 4: Invariant check and domain event raising
        if (TotalAmount > CreditLimit)
        {
            // Transition status if not already in ReviewRequired
            if (Status != BillingCycleStatus.ReviewRequired)
            {
                Status = BillingCycleStatus.ReviewRequired;
            }

            // Raise domain event for credit limit exceeded
            var creditLimitExceededEvent = new CreditLimitExceededEvent(
                billingCycleId: Id,
                totalAmount: TotalAmount,
                creditLimit: CreditLimit
            );

            _domainEvents.Add(creditLimitExceededEvent);
        }
    }

    /// <summary>
    /// Transitions the billing cycle to UnderReview status.
    /// </summary>
    public void MarkAsUnderReview()
    {
        if (Status == BillingCycleStatus.Closed)
        {
            throw new InvalidOperationException("Cannot review a closed billing cycle.");
        }

        Status = BillingCycleStatus.UnderReview;
    }

    /// <summary>
    /// Closes the billing cycle, preventing any further modifications.
    /// </summary>
    public void Close()
    {
        Status = BillingCycleStatus.Closed;
    }

    /// <summary>
    /// Clears the domain events collection after they have been published.
    /// Should be called by infrastructure after successful event dispatch.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
