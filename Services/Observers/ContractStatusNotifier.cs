using GLMS.Enterprise.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GLMS.Enterprise.Services.Observers;

/// <summary>
/// Subject implementation that maintains a list of observers and notifies them of contract status changes.
/// </summary>
public class ContractStatusNotifier : IContractStatusSubject
{
    private readonly List<IContractStatusObserver> _observers = new();
    private readonly ILogger<ContractStatusNotifier> _logger;

    public ContractStatusNotifier(
        ILogger<ContractStatusNotifier> logger,
        IEnumerable<IContractStatusObserver> observers)
    {
        _logger = logger;
        foreach (var observer in observers)
        {
            RegisterObserver(observer);
        }
    }

    public void RegisterObserver(IContractStatusObserver observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
            _logger.LogInformation("Observer registered for contract status changes");
        }
    }

    public void RemoveObserver(IContractStatusObserver observer)
    {
        if (_observers.Remove(observer))
        {
            _logger.LogInformation("Observer removed from contract status changes");
        }
    }

    public void NotifyObservers(Guid contractId, string oldStatus, string newStatus)
    {
        _logger.LogInformation("Notifying {Count} observers of contract {ContractId} status change: {OldStatus} -> {NewStatus}",
            _observers.Count, contractId, oldStatus, newStatus);

        foreach (var observer in _observers)
        {
            try
            {
                observer.OnStatusChanged(contractId, oldStatus, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying observer of contract status change");
            }
        }
    }
}
