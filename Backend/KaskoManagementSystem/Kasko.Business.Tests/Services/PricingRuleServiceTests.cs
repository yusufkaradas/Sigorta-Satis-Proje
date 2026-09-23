using Kasko.Business.DTOs.PricingRule;
using Kasko.Business.Exceptions;
using Kasko.Business.Services;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Moq;

namespace Kasko.Business.Tests.Services;

public class PricingRuleServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPricingRuleRepository> _pricingRuleRepositoryMock;
    private readonly PricingRuleService _pricingRuleService;

    public PricingRuleServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _pricingRuleRepositoryMock = new Mock<IPricingRuleRepository>();

        _unitOfWorkMock
            .Setup(x => x.PricingRules)
            .Returns(_pricingRuleRepositoryMock.Object);

        _pricingRuleService =
            new PricingRuleService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenNoExistingRule_ShouldCreateVersionOne()
    {
        var dto = new CreatePricingRuleDto
        {
            Code = "TEST_RULE",
            Name = "Test Rule",
            Description = "Test",
            Value = 0.025m,
            IsActive = true,
            EffectiveFrom = new DateTime(2026, 10, 1)
        };

        _pricingRuleRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PricingRule, bool>>>()))
            .ReturnsAsync(new List<PricingRule>());

        PricingRule? addedRule = null;

        _pricingRuleRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PricingRule>()))
            .Callback<PricingRule>(rule => addedRule = rule)
            .Returns(Task.CompletedTask);

        var result = await _pricingRuleService.CreateAsync(dto);

        Assert.NotNull(addedRule);

        Assert.Equal("TEST_RULE", addedRule!.Code);
        Assert.Equal(0.025m, addedRule.Value);
        Assert.Equal(1, addedRule.Version);
        Assert.Equal(new DateTime(2026, 10, 1), addedRule.EffectiveFrom);
        Assert.Null(addedRule.EffectiveUntil);

        Assert.Equal(addedRule.Id, result.Id);
        Assert.Equal(1, result.Version);
        Assert.Equal("TEST_RULE", result.Code);

        _pricingRuleRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PricingRule>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenExistingVersionsExist_ShouldCreateNextVersion()
    {
        var dto = new CreatePricingRuleDto
        {
            Code = "TEST_RULE",
            Name = "Test Rule V2",
            Description = "Test V2",
            Value = 0.030m,
            IsActive = true,
            EffectiveFrom = new DateTime(2026, 9, 1)
        };

        var existingRules = new List<PricingRule>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Code = "TEST_RULE",
                Value = 0.020m,
                Version = 1,
                EffectiveFrom = new DateTime(2026, 1, 1),
                EffectiveUntil = new DateTime(2026, 8, 31),
                IsActive = true,
                IsDeleted = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "TEST_RULE",
                Value = 0.025m,
                Version = 2,
                EffectiveFrom = new DateTime(2026, 9, 1),
                EffectiveUntil = null,
                IsActive = true,
                IsDeleted = false
            }
        };

        _pricingRuleRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PricingRule, bool>>>()))
            .ReturnsAsync(existingRules);

        PricingRule? addedRule = null;

        _pricingRuleRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PricingRule>()))
            .Callback<PricingRule>(rule => addedRule = rule)
            .Returns(Task.CompletedTask);

        var result = await _pricingRuleService.CreateAsync(dto);

        Assert.NotNull(addedRule);

        Assert.Equal(3, addedRule!.Version);
        Assert.Equal("TEST_RULE", addedRule.Code);
        Assert.Equal(0.030m, addedRule.Value);
        Assert.Equal(new DateTime(2026, 9, 1), addedRule.EffectiveFrom);

        Assert.Equal(3, result.Version);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task CreateAsync_WhenExistingActiveVersionExists_ShouldClosePreviousVersion()
    {
        var effectiveFrom = new DateTime(2026, 9, 1);

        var dto = new CreatePricingRuleDto
        {
            Code = "TEST_RULE",
            Name = "Test Rule V2",
            Value = 0.030m,
            IsActive = true,
            EffectiveFrom = effectiveFrom
        };

        var existingRule = new PricingRule
        {
            Id = Guid.NewGuid(),
            Code = "TEST_RULE",
            Value = 0.020m,
            Version = 1,
            EffectiveFrom = new DateTime(2026, 1, 1),
            EffectiveUntil = null,
            IsActive = true,
            IsDeleted = false
        };

        _pricingRuleRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PricingRule, bool>>>()))
            .ReturnsAsync(new List<PricingRule> { existingRule });

        _pricingRuleRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PricingRule>()))
            .Returns(Task.CompletedTask);

        _pricingRuleRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<PricingRule>()))
            .Returns(Task.CompletedTask);

        var result =
            await _pricingRuleService.CreateAsync(dto);

        Assert.Equal(2, result.Version);

        Assert.Equal(
            new DateTime(2026, 9, 1).AddTicks(-1),
            existingRule.EffectiveUntil);

        _pricingRuleRepositoryMock.Verify(
            x => x.UpdateAsync(existingRule),
            Times.Once);
    }
    [Fact]
    public async Task UpdateAsync_WhenRuleExists_ShouldUpdateRule()
    {
        var rule = new PricingRule
        {
            Id = Guid.NewGuid(),
            Code = "TEST_RULE",
            Name = "Old Name",
            Description = "Old Description",
            Value = 0.020m,
            IsActive = true,
            Version = 2,
            EffectiveFrom = new DateTime(2026, 9, 1)
        };

        var dto = new UpdatePricingRuleDto
        {
            Id = rule.Id,
            Name = "New Name",
            Description = "New Description",
            Value = 0.025m,
            IsActive = false
        };

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(rule.Id))
            .ReturnsAsync(rule);

        _pricingRuleRepositoryMock
            .Setup(x => x.UpdateAsync(rule))
            .Returns(Task.CompletedTask);

        await _pricingRuleService.UpdateAsync(dto);

        Assert.Equal("New Name", rule.Name);
        Assert.Equal("New Description", rule.Description);
        Assert.Equal(0.025m, rule.Value);
        Assert.False(rule.IsActive);

        // Version bilgileri değişmemeli
        Assert.Equal(2, rule.Version);
        Assert.Equal(
            new DateTime(2026, 9, 1),
            rule.EffectiveFrom);

        _pricingRuleRepositoryMock.Verify(
            x => x.UpdateAsync(rule),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenRuleDoesNotExist_ShouldThrowNotFoundException()
    {
        var id = Guid.NewGuid();

        var dto = new UpdatePricingRuleDto
        {
            Id = id,
            Name = "Test",
            Value = 0.025m,
            IsActive = true
        };

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync((PricingRule?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _pricingRuleService.UpdateAsync(dto));
    }
    [Fact]
    public async Task DeleteAsync_WhenRuleExists_ShouldDeleteRule()
    {
        var rule = new PricingRule
        {
            Id = Guid.NewGuid(),
            Code = "TEST_RULE",
            Name = "Test Rule",
            Value = 0.020m,
            Version = 1,
            EffectiveFrom = new DateTime(2026, 1, 1),
            IsActive = true,
            IsDeleted = false
        };

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(rule.Id))
            .ReturnsAsync(rule);

        _pricingRuleRepositoryMock
            .Setup(x => x.DeleteAsync(rule))
            .Returns(Task.CompletedTask);

        await _pricingRuleService.DeleteAsync(rule.Id);

        _pricingRuleRepositoryMock.Verify(
            x => x.DeleteAsync(rule),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenMiddleVersionDeleted_ShouldExtendPreviousVersion()
    {
        var previous = new PricingRule
        {
            Id = Guid.NewGuid(),
            Code = "BASE_KASKO_RATE",
            Value = 0.0215m,
            Version = 2,
            EffectiveFrom = new DateTime(2026, 9, 1),
            EffectiveUntil = new DateTime(2026, 11, 1).AddTicks(-1),
            IsActive = true
        };

        var middle = new PricingRule
        {
            Id = Guid.NewGuid(),
            Code = "BASE_KASKO_RATE",
            Value = 0.0230m,
            Version = 3,
            EffectiveFrom = new DateTime(2026, 11, 1),
            EffectiveUntil = new DateTime(2027, 1, 1).AddTicks(-1),
            IsActive = true
        };

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(middle.Id))
            .ReturnsAsync(middle);

        _pricingRuleRepositoryMock
            .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PricingRule, bool>>>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<PricingRule, bool>> predicate) =>
                new[] { previous, middle }.Where(predicate.Compile()).ToList());

        await _pricingRuleService.DeleteAsync(middle.Id);

        Assert.Equal(middle.EffectiveUntil, previous.EffectiveUntil);

        _pricingRuleRepositoryMock.Verify(
            x => x.UpdateAsync(previous),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenLastEngineRuleVersion_ShouldThrowBadRequestException()
    {
        var rule = new PricingRule
        {
            Id = Guid.NewGuid(),
            Code = "CLAIMS_2",
            Name = "2 Hasar",
            Value = 1.15m,
            Version = 1,
            EffectiveFrom = new DateTime(2026, 1, 1),
            IsActive = true,
            IsDeleted = false
        };

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(rule.Id))
            .ReturnsAsync(rule);

        _pricingRuleRepositoryMock
            .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PricingRule, bool>>>()))
            .ReturnsAsync(new List<PricingRule>());

        await Assert.ThrowsAsync<BadRequestException>(
            () => _pricingRuleService.DeleteAsync(rule.Id));

        _pricingRuleRepositoryMock.Verify(
            x => x.DeleteAsync(It.IsAny<PricingRule>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenEngineRuleHasAnotherActiveVersion_ShouldDeleteRule()
    {
        var rule = new PricingRule
        {
            Id = Guid.NewGuid(),
            Code = "BASE_KASKO_RATE",
            Name = "Temel Oran",
            Value = 0.0215m,
            Version = 5,
            EffectiveFrom = new DateTime(2026, 1, 1),
            IsActive = true,
            IsDeleted = false
        };

        var previous = new PricingRule
        {
            Id = Guid.NewGuid(),
            Code = "BASE_KASKO_RATE",
            Name = "Temel Oran",
            Value = 0.02m,
            Version = 4,
            EffectiveFrom = new DateTime(2025, 1, 1),
            IsActive = true,
            IsDeleted = false
        };

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(rule.Id))
            .ReturnsAsync(rule);

        _pricingRuleRepositoryMock
            .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PricingRule, bool>>>()))
            .ReturnsAsync(new List<PricingRule> { previous });

        _pricingRuleRepositoryMock
            .Setup(x => x.DeleteAsync(rule))
            .Returns(Task.CompletedTask);

        await _pricingRuleService.DeleteAsync(rule.Id);

        _pricingRuleRepositoryMock.Verify(
            x => x.DeleteAsync(rule),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenDeactivatingLastEngineRuleVersion_ShouldThrowBadRequestException()
    {
        var rule = new PricingRule
        {
            Id = Guid.NewGuid(),
            Code = "REGION_HIGH",
            Name = "Yüksek Risk",
            Value = 1.2m,
            Version = 1,
            EffectiveFrom = new DateTime(2026, 1, 1),
            IsActive = true,
            IsDeleted = false
        };

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(rule.Id))
            .ReturnsAsync(rule);

        _pricingRuleRepositoryMock
            .Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<PricingRule, bool>>>()))
            .ReturnsAsync(new List<PricingRule>());

        var dto = new UpdatePricingRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            Value = rule.Value,
            IsActive = false
        };

        await Assert.ThrowsAsync<BadRequestException>(
            () => _pricingRuleService.UpdateAsync(dto));

        Assert.True(rule.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_WhenRuleDoesNotExist_ShouldThrowNotFoundException()
    {
        var id = Guid.NewGuid();

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync((PricingRule?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _pricingRuleService.DeleteAsync(id));
    }
}