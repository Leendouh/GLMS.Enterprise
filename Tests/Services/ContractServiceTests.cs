using GLMS.Enterprise.Core.Entities;
using GLMS.Enterprise.Core.Enums;
using GLMS.Enterprise.Core.Interfaces;
using GLMS.Enterprise.Services;
using Moq;
using Xunit;

namespace GLMS.Enterprise.Tests.Services;

public class ContractServiceTests
{
    private readonly Mock<IContractRepository> _mockRepo;
    private readonly ContractService _contractService;

    public ContractServiceTests()
    {
        _mockRepo = new Mock<IContractRepository>();
        _contractService = new ContractService(_mockRepo.Object);
    }

    [Fact]
    public async Task CanCreateServiceRequestAsync_ActiveContract_ReturnsTrue()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var contract = new Contract
        {
            Id = contractId,
            ClientId = Guid.NewGuid(),
            Status = ContractStatus.Active,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddYears(1)
        };
        _mockRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);

        // Act
        var result = await _contractService.CanCreateServiceRequestAsync(contractId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanCreateServiceRequestAsync_ExpiredContract_ReturnsFalse()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var contract = new Contract
        {
            Id = contractId,
            ClientId = Guid.NewGuid(),
            Status = ContractStatus.Expired,
            StartDate = DateTime.Today.AddYears(-2),
            EndDate = DateTime.Today.AddYears(-1)
        };
        _mockRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);

        // Act
        var result = await _contractService.CanCreateServiceRequestAsync(contractId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanCreateServiceRequestAsync_OnHoldContract_ReturnsFalse()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var contract = new Contract
        {
            Id = contractId,
            ClientId = Guid.NewGuid(),
            Status = ContractStatus.OnHold,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddYears(1)
        };
        _mockRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);

        // Act
        var result = await _contractService.CanCreateServiceRequestAsync(contractId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanCreateServiceRequestAsync_DraftContract_ReturnsFalse()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var contract = new Contract
        {
            Id = contractId,
            ClientId = Guid.NewGuid(),
            Status = ContractStatus.Draft,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddYears(1)
        };
        _mockRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);

        // Act
        var result = await _contractService.CanCreateServiceRequestAsync(contractId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanCreateServiceRequestAsync_ContractNotFound_ReturnsFalse()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync((Contract?)null);

        // Act
        var result = await _contractService.CanCreateServiceRequestAsync(contractId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanCreateServiceRequestAsync_EmptyGuid_ReturnsFalse()
    {
        // Arrange
        var contractId = Guid.Empty;

        // Act
        var result = await _contractService.CanCreateServiceRequestAsync(contractId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateContractStatusTransitionAsync_DraftToActive_ReturnsSuccess()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var contract = new Contract
        {
            Id = contractId,
            Status = ContractStatus.Draft
        };
        _mockRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);

        // Act
        var result = await _contractService.ValidateContractStatusTransitionAsync(contractId, ContractStatus.Active);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateContractStatusTransitionAsync_ActiveToDraft_ReturnsFailure()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var contract = new Contract
        {
            Id = contractId,
            Status = ContractStatus.Active
        };
        _mockRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);

        // Act
        var result = await _contractService.ValidateContractStatusTransitionAsync(contractId, ContractStatus.Draft);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Cannot transition", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateContractStatusTransitionAsync_ActiveToExpired_ReturnsSuccess()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var contract = new Contract
        {
            Id = contractId,
            Status = ContractStatus.Active
        };
        _mockRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);

        // Act
        var result = await _contractService.ValidateContractStatusTransitionAsync(contractId, ContractStatus.Expired);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateContractStatusTransitionAsync_ExpiredToActive_ReturnsFailure()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var contract = new Contract
        {
            Id = contractId,
            Status = ContractStatus.Expired
        };
        _mockRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);

        // Act
        var result = await _contractService.ValidateContractStatusTransitionAsync(contractId, ContractStatus.Active);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ValidateContractStatusTransitionAsync_ActiveToOnHold_ReturnsSuccess()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var contract = new Contract
        {
            Id = contractId,
            Status = ContractStatus.Active
        };
        _mockRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);

        // Act
        var result = await _contractService.ValidateContractStatusTransitionAsync(contractId, ContractStatus.OnHold);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateContractStatusTransitionAsync_OnHoldToActive_ReturnsSuccess()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var contract = new Contract
        {
            Id = contractId,
            Status = ContractStatus.OnHold
        };
        _mockRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);

        // Act
        var result = await _contractService.ValidateContractStatusTransitionAsync(contractId, ContractStatus.Active);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateContractStatusTransitionAsync_SameStatus_ReturnsFailure()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        var contract = new Contract
        {
            Id = contractId,
            Status = ContractStatus.Active
        };
        _mockRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync(contract);

        // Act
        var result = await _contractService.ValidateContractStatusTransitionAsync(contractId, ContractStatus.Active);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("already in", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateContractStatusTransitionAsync_ContractNotFound_ReturnsFailure()
    {
        // Arrange
        var contractId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(contractId)).ReturnsAsync((Contract?)null);

        // Act
        var result = await _contractService.ValidateContractStatusTransitionAsync(contractId, ContractStatus.Active);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateContractStatusTransitionAsync_EmptyGuid_ReturnsFailure()
    {
        // Arrange
        var contractId = Guid.Empty;

        // Act
        var result = await _contractService.ValidateContractStatusTransitionAsync(contractId, ContractStatus.Active);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("cannot be empty", result.ErrorMessage);
    }

    [Fact]
    public void GetValidTransitions_ReturnsDictionary()
    {
        // Act
        var transitions = ContractService.GetValidTransitions();

        // Assert
        Assert.NotNull(transitions);
        Assert.Equal(5, transitions.Count);
        Assert.True(transitions.ContainsKey(ContractStatus.Draft));
        Assert.True(transitions.ContainsKey(ContractStatus.Active));
    }
}
