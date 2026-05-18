using Microsoft.EntityFrameworkCore;
using GLMS.Enterprise.Core.Entities;
using GLMS.Enterprise.Core.Enums;
using GLMS.Enterprise.Core.Interfaces;
using GLMS.Enterprise.Infrastructure.Data;

namespace GLMS.Enterprise.Infrastructure.Repositories;

/// <summary>
/// Concrete implementation of IContractRepository using Entity Framework Core.
/// Registered as Scoped in DI.
/// </summary>
public class ContractRepository : IContractRepository
{
    private readonly ApplicationDbContext _context;

    public ContractRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Contract?> GetByIdAsync(Guid id)
    {
        return await _context.Contracts
            .Include(c => c.Client)
            .Include(c => c.ServiceRequests)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Contract>> GetAllAsync()
    {
        return await _context.Contracts
            .Include(c => c.Client)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Contract>> GetActiveContractsAsync()
    {
        return await _context.Contracts
            .Include(c => c.Client)
            .Where(c => c.Status == ContractStatus.Active)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Contract>> GetByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _context.Contracts
            .Include(c => c.Client)
            .Where(c => c.StartDate >= start && c.EndDate <= end)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Contract>> GetByStatusAsync(ContractStatus status)
    {
        return await _context.Contracts
            .Include(c => c.Client)
            .Where(c => c.Status == status)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Contract> AddAsync(Contract contract)
    {
        if (contract.Id == Guid.Empty)
            contract.Id = Guid.NewGuid();

        contract.CreatedAt = DateTime.UtcNow;
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();
        return contract;
    }

    public async Task<Contract> UpdateAsync(Contract contract)
    {
        _context.Entry(contract).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return contract;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var contract = await _context.Contracts.FindAsync(id);
        if (contract == null) return false;

        _context.Contracts.Remove(contract);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Contracts.AnyAsync(c => c.Id == id);
    }

    public async Task<bool> IsContractActiveAsync(Guid contractId)
    {
        return await _context.Contracts
            .AnyAsync(c => c.Id == contractId && c.Status == ContractStatus.Active);
    }
}
