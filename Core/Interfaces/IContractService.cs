using GLMS.Enterprise.Core.Enums;
using GLMS.Enterprise.Core.Models;

namespace GLMS.Enterprise.Core.Interfaces;

public interface IContractService
{
    Task<bool> CanCreateServiceRequestAsync(Guid contractId);
    Task<ValidationResult> ValidateContractStatusTransitionAsync(Guid contractId, ContractStatus newStatus);
}
