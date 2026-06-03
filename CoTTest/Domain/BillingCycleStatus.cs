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
