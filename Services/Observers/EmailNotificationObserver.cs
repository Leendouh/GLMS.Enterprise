using GLMS.Enterprise.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GLMS.Enterprise.Services.Observers;

/// <summary>
/// Observer that sends email notifications when contract status changes.
/// </summary>
public class EmailNotificationObserver : IContractStatusObserver
{
    private readonly ILogger<EmailNotificationObserver> _logger;

    public EmailNotificationObserver(ILogger<EmailNotificationObserver> logger)
    {
        _logger = logger;
    }

    public void OnStatusChanged(Guid contractId, string oldStatus, string newStatus)
    {
        // In a real implementation, this would send an actual email
        _logger.LogInformation("EMAIL NOTIFICATION: Contract {ContractId} status changed from {OldStatus} to {NewStatus}",
            contractId, oldStatus, newStatus);

        // Simulate email sending logic
        // var emailService = ...;
        // await emailService.SendEmailAsync(clientEmail, subject, body);
    }
}
