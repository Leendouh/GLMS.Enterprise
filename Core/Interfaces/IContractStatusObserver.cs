namespace GLMS.Enterprise.Core.Interfaces;

/// <summary>
/// Observer interface for contract status changes.
/// </summary>
public interface IContractStatusObserver
{
    /// <summary>
    /// Called when a contract's status changes.
    /// </summary>
    /// <param name="contractId">The ID of the contract.</param>
    /// <param name="oldStatus">The previous status.</param>
    /// <param name="newStatus">The new status.</param>
    void OnStatusChanged(Guid contractId, string oldStatus, string newStatus);
}
