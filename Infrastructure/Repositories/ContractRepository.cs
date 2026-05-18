using Microsoft.EntityFrameworkCore;
using GLMS.Enterprise.Core.Entities;
using GLMS.Enterprise.Core.Enums;
using GLMS.Enterprise.Core.Interfaces;
using GLMS.Enterprise.Core.Models;
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
            .Include(c => c.ServiceRequests)
            .AsNoTracking()
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

    public async Task<IEnumerable<Contract>> GetEligibleForServiceRequestAsync()
    {
        var today = DateTime.Today;
        return await _context.Contracts
            .Include(c => c.Client)
            .Where(c => c.Status == ContractStatus.Active && c.EndDate >= today)
            .OrderBy(c => c.Client!.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PagedResult<Contract>> SearchAsync(
        DateTime? startDateFrom,
        DateTime? startDateTo,
        ContractStatus? status,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        IQueryable<Contract> query = _context.Contracts
            .Include(c => c.Client)
            .Include(c => c.ServiceRequests)
            .AsNoTracking();

        if (startDateFrom.HasValue)
            query = query.Where(c => c.StartDate >= startDateFrom.Value);

        if (startDateTo.HasValue)
            query = query.Where(c => c.StartDate <= startDateTo.Value);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Contract>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
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
