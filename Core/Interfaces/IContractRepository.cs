using GLMS.Enterprise.Core.Entities;
using GLMS.Enterprise.Core.Enums;

namespace GLMS.Enterprise.Core.Interfaces;

public interface IContractRepository
{
    Task<Contract?> GetByIdAsync(Guid id);
    Task<IEnumerable<Contract>> GetAllAsync();
    Task<IEnumerable<Contract>> GetActiveContractsAsync();
    Task<IEnumerable<Contract>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task<IEnumerable<Contract>> GetByStatusAsync(ContractStatus status);
    Task<Contract> AddAsync(Contract contract);
    Task<Contract> UpdateAsync(Contract contract);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> IsContractActiveAsync(Guid contractId);
}
