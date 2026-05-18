using GLMS.Enterprise.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GLMS.Enterprise.Services.Observers;

/// <summary>
/// Observer that logs contract status changes to an audit trail.
/// </summary>
public class AuditLogObserver : IContractStatusObserver
{
    private readonly ILogger<AuditLogObserver> _logger;

    public AuditLogObserver(ILogger<AuditLogObserver> logger)
    {
        _logger = logger;
    }

    public void OnStatusChanged(Guid contractId, string oldStatus, string newStatus)
    {
        _logger.LogInformation("AUDIT LOG: Contract {ContractId} status changed from {OldStatus} to {NewStatus} at {Timestamp}",
            contractId, oldStatus, newStatus, DateTime.UtcNow);

        // In a real implementation, this would write to a dedicated audit table or file
    }
}
