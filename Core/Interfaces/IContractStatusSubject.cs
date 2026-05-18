namespace GLMS.Enterprise.Core.Interfaces;

/// <summary>
/// Subject interface for contract status changes.
/// </summary>
public interface IContractStatusSubject
{
    /// <summary>
    /// Registers an observer to receive contract status change notifications.
    /// </summary>
    /// <param name="observer">The observer to register.</param>
    void RegisterObserver(IContractStatusObserver observer);

    /// <summary>
    /// Removes an observer from receiving contract status change notifications.
    /// </summary>
    /// <param name="observer">The observer to remove.</param>
    void RemoveObserver(IContractStatusObserver observer);

    /// <summary>
    /// Notifies all registered observers of a contract status change.
    /// </summary>
    /// <param name="contractId">The ID of the contract.</param>
    /// <param name="oldStatus">The previous status.</param>
    /// <param name="newStatus">The new status.</param>
    void NotifyObservers(Guid contractId, string oldStatus, string newStatus);
}
