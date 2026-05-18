using GLMS.Enterprise.Core.Enums;
using GLMS.Enterprise.Core.Interfaces;
using GLMS.Enterprise.Core.Models;
using GLMS.Enterprise.Services.Observers;

namespace GLMS.Enterprise.Services;

/// <summary>
/// Business logic layer for contract operations.
/// Implements workflow rules: which status transitions are valid,
/// and whether a ServiceRequest can be created for a given contract.
/// </summary>
public class ContractService : IContractService
{
    private readonly IContractRepository _contractRepository;
    private readonly IContractStatusSubject? _statusNotifier;

    // Valid status transition matrix
    // Key = current status, Value = allowed next statuses
    private static readonly Dictionary<ContractStatus, HashSet<ContractStatus>> _validTransitions = new()
    {
        [ContractStatus.Draft]      = new() { ContractStatus.Active },
        [ContractStatus.Active]     = new() { ContractStatus.OnHold, ContractStatus.Expired, ContractStatus.Terminated },
        [ContractStatus.OnHold]     = new() { ContractStatus.Active, ContractStatus.Expired, ContractStatus.Terminated },
        [ContractStatus.Expired]    = new() { ContractStatus.Terminated },
        [ContractStatus.Terminated] = new() { /* terminal state — no transitions */ }
    };

    public ContractService(IContractRepository contractRepository, IContractStatusSubject? statusNotifier = null)
    {
        _contractRepository = contractRepository;
        _statusNotifier = statusNotifier;
    }

    /// <summary>
    /// Returns true only if the contract is Active and not past its end date (R1).
    /// </summary>
    public async Task<bool> CanCreateServiceRequestAsync(Guid contractId)
        => await GetServiceRequestCreationErrorAsync(contractId) is null;

    /// <summary>
    /// Returns a user-facing error when a service request cannot be created; null if allowed.
    /// </summary>
    public async Task<string?> GetServiceRequestCreationErrorAsync(Guid contractId)
    {
        if (contractId == Guid.Empty)
            return "Please select a contract.";

        var contract = await _contractRepository.GetByIdAsync(contractId);
        if (contract == null)
            return "Contract not found.";

        if (contract.Status == ContractStatus.OnHold)
            return "Cannot create service request. Contract is On Hold.";

        if (contract.Status == ContractStatus.Expired || contract.EndDate.Date < DateTime.Today)
            return "Cannot create service request. Contract is Expired.";

        if (contract.Status != ContractStatus.Active)
            return $"Cannot create service request. Contract is {contract.Status}.";

        return null;
    }

    /// <summary>
    /// Validates whether transitioning a contract to newStatus is permitted.
    /// If valid, notifies all registered observers of the status change.
    /// </summary>
    public async Task<ValidationResult> ValidateContractStatusTransitionAsync(
        Guid contractId, ContractStatus newStatus)
    {
        if (contractId == Guid.Empty)
            return ValidationResult.Failure("Contract ID cannot be empty.");

        var contract = await _contractRepository.GetByIdAsync(contractId);
        if (contract == null)
            return ValidationResult.Failure($"Contract with ID {contractId} was not found.");

        var currentStatus = contract.Status;

        if (currentStatus == newStatus)
            return ValidationResult.Failure($"Contract is already in '{newStatus}' status.");

        if (_validTransitions.TryGetValue(currentStatus, out var allowed) && allowed.Contains(newStatus))
        {
            // Notify observers of the status change
            _statusNotifier?.NotifyObservers(contractId, currentStatus.ToString(), newStatus.ToString());
            return ValidationResult.Success();
        }

        return ValidationResult.Failure(
            $"Cannot transition from '{currentStatus}' to '{newStatus}'. " +
            $"Allowed transitions from '{currentStatus}': {string.Join(", ", _validTransitions[currentStatus].Select(s => s.ToString()).DefaultIfEmpty("none"))}.");
    }

    /// <summary>
    /// Exposes the valid transitions dictionary for UI/documentation purposes.
    /// </summary>
    public static IReadOnlyDictionary<ContractStatus, HashSet<ContractStatus>> GetValidTransitions()
        => _validTransitions;
}
