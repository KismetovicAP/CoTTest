# BillingCycle DDD Aggregate Root Implementation

This repository contains implementations of a Domain-Driven Design (DDD) Aggregate Root for managing billing cycles with usage charges.

---

## Prompt 1: NoCoTTest Project - Basic Implementation

**User Request:**
> In NoCoTTest project, write a C# class for a Domain-Driven Design Aggregate Root named BillingCycle. It needs to manage a collection of UsageCharge entities. Implement an AddCharge method. The rules are: cannot add charges if status is closed or under-review. If a new charge pushes the total cycle amount over the customers credit limit, change status to ReviewRequired and raise a CreditLimitExceeded domain event. Give me the full code immediately without explanations.

**Response:**

Created the following files in the `NoCoTTest` project:

### NoCoTTest/Domain/UsageCharge.cs
```csharp
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
```

### NoCoTTest/Domain/BillingCycleStatus.cs
```csharp
namespace NoCoTTest.Domain;

public enum BillingCycleStatus
{
	Open,
	ReviewRequired,
	UnderReview,
	Closed
}
```

### NoCoTTest/Domain/Events/CreditLimitExceededEvent.cs
```csharp
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
```

### NoCoTTest/Domain/BillingCycle.cs
```csharp
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
```

---

## Prompt 2: CoTTest Project - Production-Ready with Chain-of-Thought

**User Request:**
> In CoTTest project, We need to implement a robust, production-ready DDD Aggregate Root in .NET 10 for an enterprise `BillingCycle`.
> Before writing any C# code, I want you to reason through the domain logic step-by-step using Chain-of-Thought. Structure your thoughts into these exact sections:
> 1. Encapsulation Strategy: Explain how to protect the internal `UsageCharge` collection and state properties from external manipulation (avoiding public setters and public mutating lists).
> 2. Invariant & State Validation: Outline the exact logical sequence of checks required when a new charge is introduced.
> 3. Domain Events: Detail when and how the domain events should be dispatched safely without introducing side effects inside the aggregate itself.
> Once you have completed this step-by-step structural analysis, output the complete, clean C# implementation that adheres exactly to your reasoning.

**Response:**

## Chain-of-Thought Analysis for BillingCycle Aggregate Root

### 1. Encapsulation Strategy

The aggregate root must be the sole guardian of its consistency boundary. To achieve this:

- **Private backing fields**: Use `private readonly List<UsageCharge>` for the charges collection and `private readonly List<object>` for domain events. The `readonly` modifier ensures the field reference cannot be reassigned, though contents can be mutated through controlled methods.

- **Read-only public access**: Expose collections through `IReadOnlyCollection<T>` properties, preventing external code from casting and mutating the internal list.

- **Private setters**: All state properties (Id, Status, CreditLimit, etc.) should use `private set` to prevent external modification. Only the aggregate's methods can change state.

- **Constructor-based initialization**: Immutable properties like Id, CustomerId, StartDate, EndDate, and CreditLimit should be set once during construction. The Id should be generated internally (aggregate controls its identity).

- **No public collection mutators**: Never expose `Add()`, `Remove()`, or `Clear()` methods on collections. All mutations must flow through domain-meaningful methods like `AddCharge()`.

### 2. Invariant & State Validation

The `AddCharge` method must enforce business rules in a precise sequence:

**Step 1 - Pre-condition Guard**: Before any state change, validate current status:
- If `Status == Closed`: Throw exception (closed cycles are immutable for audit/compliance)
- If `Status == UnderReview`: Throw exception (cycle is locked pending manual review)
- If `Status == Open` or `ReviewRequired`: Proceed (ReviewRequired means flagged but still accepting charges)

**Step 2 - Entity Creation**: Instantiate `UsageCharge` with provided data. The charge entity must validate its own construction (e.g., amount > 0, description not empty).

**Step 3 - State Mutation**: Add the charge to the internal `_charges` collection.

**Step 4 - Invariant Check**: Recalculate aggregate totals and validate business constraints:
- Compute `TotalAmount = _charges.Sum(c => c.Amount)`
- Compare against `CreditLimit`
- If `TotalAmount > CreditLimit`:
  * Transition `Status` to `ReviewRequired` (if not already Closed/UnderReview)
  * Create `CreditLimitExceededEvent` with aggregate ID, total amount, and credit limit
  * Add event to `_domainEvents` collection

**Step 5 - Maintain Idempotency**: If status is already `ReviewRequired` and limit is still exceeded, we can either:
- Add another event (if tracking each violation matters)
- Skip event creation (if only the first violation matters)
- For production systems, typically raise the event each time to enable proper monitoring

### 3. Domain Events

Domain events are facts that have occurred within the aggregate. Proper handling requires:

**Deferred Dispatch Pattern**:
- Events are **collected** in the aggregate's `_domainEvents` list during the transaction
- Events are **not published** by the aggregate itself (no `EventBus.Publish()` calls)
- Infrastructure (repository/DbContext/Unit of Work) retrieves events via `DomainEvents` property after calling `SaveChanges()`
- Infrastructure publishes events **only after successful persistence** (ensuring transactional consistency)

**Event Immutability**:
- Event properties should use `{ get; }` with values set in constructor
- Events represent immutable facts: "At time T, credit limit was exceeded"
- Include rich contextual data: BillingCycleId, TotalAmount, CreditLimit, OccurredAt timestamp

**Aggregate Responsibility**:
- Aggregate decides **when** an event should be raised (based on business rules)
- Aggregate decides **what data** the event contains
- Aggregate does **not** decide **who** handles the event or **how** it's dispatched

**Clearing Events**:
- Provide `ClearDomainEvents()` method for infrastructure to call after publishing
- This prevents events from being published multiple times if the aggregate is retrieved again in the same session

**No Side Effects**:
- The aggregate must remain pure - no external dependencies (no ILogger, IEmailService, etc.)
- Event handlers (separate classes) perform side effects like sending emails, updating read models, etc.

---

## Complete C# Implementation

Created the following files in the `CoTTest` project:

### CoTTest/Domain/UsageCharge.cs
```csharp
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
```

### CoTTest/Domain/BillingCycleStatus.cs
```csharp
namespace CoTTest.Domain;

/// <summary>
/// Represents the lifecycle status of a billing cycle.
/// </summary>
public enum BillingCycleStatus
{
	/// <summary>
	/// Cycle is active and accepting new charges.
	/// </summary>
	Open = 0,

	/// <summary>
	/// Cycle has exceeded credit limit and requires manual review.
	/// Charges can still be added while in this state.
	/// </summary>
	ReviewRequired = 1,

	/// <summary>
	/// Cycle is locked for manual review. No charges can be added.
	/// </summary>
	UnderReview = 2,

	/// <summary>
	/// Cycle is finalized and closed. No further modifications allowed.
	/// </summary>
	Closed = 3
}
```

### CoTTest/Domain/Events/CreditLimitExceededEvent.cs
```csharp
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
```

### CoTTest/Domain/BillingCycle.cs
```csharp
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
```

**Implementation Summary:**

✅ **Encapsulation**: Private backing fields, read-only collections, private setters  
✅ **Invariant Validation**: Sequential guard checks before state mutation  
✅ **Domain Events**: Deferred dispatch pattern with immutable event objects  
✅ **Production-Ready**: Input validation, clear exceptions, XML documentation  
✅ **DDD Principles**: Aggregate controls its consistency boundary, rich domain behavior  

---

## Key Differences Between Implementations

| Aspect | NoCoTTest (Basic) | CoTTest (Production-Ready) |
|--------|-------------------|----------------------------|
| **Validation** | Minimal | Comprehensive with ArgumentException |
| **Documentation** | None | Full XML comments |
| **Entity Design** | Public class | Sealed class |
| **Guard Clauses** | Basic status check | Detailed messages explaining why |
| **Event Properties** | Basic getters | Includes computed ExcessAmount |
| **Additional Methods** | None | MarkAsUnderReview(), Close() |
| **Constructor Validation** | None | All inputs validated |
| **Error Messages** | Generic | Specific with business context |

---

## Usage Example

```csharp
// Create a billing cycle
var billingCycle = new BillingCycle(
	customerId: Guid.NewGuid(),
	startDate: DateTime.UtcNow,
	endDate: DateTime.UtcNow.AddMonths(1),
	creditLimit: 1000m
);

// Add charges
billingCycle.AddCharge(300m, "API Calls", DateTime.UtcNow);
billingCycle.AddCharge(500m, "Storage", DateTime.UtcNow);
billingCycle.AddCharge(400m, "Bandwidth", DateTime.UtcNow); // Exceeds limit!

// Check status
Console.WriteLine(billingCycle.Status); // ReviewRequired
Console.WriteLine(billingCycle.TotalAmount); // 1200

// Check domain events
if (billingCycle.DomainEvents.Any())
{
	var creditEvent = (CreditLimitExceededEvent)billingCycle.DomainEvents.First();
	Console.WriteLine($"Credit limit exceeded by {creditEvent.ExcessAmount}");
}

// After processing events
billingCycle.ClearDomainEvents();
```

---

## License

This is a demonstration project for Domain-Driven Design patterns in .NET 10.
