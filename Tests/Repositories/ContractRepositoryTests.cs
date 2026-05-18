using GLMS.Enterprise.Core.Entities;
using GLMS.Enterprise.Core.Enums;
using GLMS.Enterprise.Infrastructure.Data;
using GLMS.Enterprise.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GLMS.Enterprise.Tests.Repositories;

public class ContractRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ContractRepository _repository;

    public ContractRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new ContractRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<Client> CreateTestClientAsync()
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            Name = "Test Client",
            ContactEmail = "test@example.com",
            ContactPhone = "1234567890",
            Region = "North",
            Address = "123 Test St",
            CreatedAt = DateTime.UtcNow
        };
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        return client;
    }

    private async Task<Contract> CreateTestContractAsync(Client client, ContractStatus status = ContractStatus.Active)
    {
        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddYears(1),
            Status = status,
            ServiceLevel = "Premium",
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow
        };
        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();
        return contract;
    }

    [Fact]
    public async Task AddAsync_Contract_ReturnsWithId()
    {
        // Arrange
        var client = await CreateTestClientAsync();
        var contract = new Contract
        {
            Id = Guid.Empty,
            ClientId = client.Id,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddYears(1),
            Status = ContractStatus.Draft,
            ServiceLevel = "Basic"
        };

        // Act
        var result = await _repository.AddAsync(contract);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(client.Id, result.ClientId);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingContract_ReturnsContract()
    {
        // Arrange
        var client = await CreateTestClientAsync();
        var contract = await CreateTestContractAsync(client);

        // Act
        var result = await _repository.GetByIdAsync(contract.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contract.Id, result.Id);
        Assert.Equal(contract.ClientId, result.ClientId);
        Assert.NotNull(result.Client);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentContract_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllContracts()
    {
        // Arrange
        var client = await CreateTestClientAsync();
        await CreateTestContractAsync(client, ContractStatus.Active);
        await CreateTestContractAsync(client, ContractStatus.Draft);
        await CreateTestContractAsync(client, ContractStatus.Expired);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetActiveContractsAsync_ReturnsOnlyActiveContracts()
    {
        // Arrange
        var client = await CreateTestClientAsync();
        await CreateTestContractAsync(client, ContractStatus.Active);
        await CreateTestContractAsync(client, ContractStatus.Draft);
        await CreateTestContractAsync(client, ContractStatus.Expired);

        // Act
        var result = await _repository.GetActiveContractsAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(ContractStatus.Active, result.First().Status);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsContractsInRange()
    {
        // Arrange
        var client = await CreateTestClientAsync();
        var contract1 = await CreateTestContractAsync(client, ContractStatus.Active);
        contract1.StartDate = new DateTime(2024, 1, 1);
        contract1.EndDate = new DateTime(2024, 6, 30);
        await _context.SaveChangesAsync();

        var contract2 = await CreateTestContractAsync(client, ContractStatus.Active);
        contract2.StartDate = new DateTime(2024, 7, 1);
        contract2.EndDate = new DateTime(2024, 12, 31);
        await _context.SaveChangesAsync();

        var contract3 = await CreateTestContractAsync(client, ContractStatus.Active);
        contract3.StartDate = new DateTime(2025, 1, 1);
        contract3.EndDate = new DateTime(2025, 6, 30);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDateRangeAsync(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsContractsWithStatus()
    {
        // Arrange
        var client = await CreateTestClientAsync();
        await CreateTestContractAsync(client, ContractStatus.Active);
        await CreateTestContractAsync(client, ContractStatus.Active);
        await CreateTestContractAsync(client, ContractStatus.Draft);

        // Act
        var result = await _repository.GetByStatusAsync(ContractStatus.Active);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, c => Assert.Equal(ContractStatus.Active, c.Status));
    }

    [Fact]
    public async Task UpdateAsync_ChangesPersisted()
    {
        // Arrange
        var client = await CreateTestClientAsync();
        var contract = await CreateTestContractAsync(client, ContractStatus.Draft);
        contract.Status = ContractStatus.Active;
        contract.ServiceLevel = "Premium";

        // Act
        var result = await _repository.UpdateAsync(contract);
        var updated = await _repository.GetByIdAsync(contract.Id);

        // Assert
        Assert.Equal(ContractStatus.Active, updated.Status);
        Assert.Equal("Premium", updated.ServiceLevel);
    }

    [Fact]
    public async Task DeleteAsync_ExistingContract_ReturnsTrue()
    {
        // Arrange
        var client = await CreateTestClientAsync();
        var contract = await CreateTestContractAsync(client);

        // Act
        var result = await _repository.DeleteAsync(contract.Id);
        var deleted = await _repository.GetByIdAsync(contract.Id);

        // Assert
        Assert.True(result);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentContract_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_ExistingContract_ReturnsTrue()
    {
        // Arrange
        var client = await CreateTestClientAsync();
        var contract = await CreateTestContractAsync(client);

        // Act
        var result = await _repository.ExistsAsync(contract.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_NonExistentContract_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.ExistsAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsContractActiveAsync_ActiveContract_ReturnsTrue()
    {
        // Arrange
        var client = await CreateTestClientAsync();
        var contract = await CreateTestContractAsync(client, ContractStatus.Active);

        // Act
        var result = await _repository.IsContractActiveAsync(contract.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsContractActiveAsync_ExpiredContract_ReturnsFalse()
    {
        // Arrange
        var client = await CreateTestClientAsync();
        var contract = await CreateTestContractAsync(client, ContractStatus.Expired);

        // Act
        var result = await _repository.IsContractActiveAsync(contract.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsContractActiveAsync_NonExistentContract_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.IsContractActiveAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }
}
