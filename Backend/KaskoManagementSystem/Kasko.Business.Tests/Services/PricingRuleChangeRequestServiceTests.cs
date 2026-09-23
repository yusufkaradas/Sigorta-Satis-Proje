using Kasko.Business.DTOs.PricingRuleChangeRequest;
using Kasko.Business.Services;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Moq;

namespace Kasko.Business.Tests.Services;

public class PricingRuleChangeRequestServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPricingRuleRepository> _pricingRuleRepositoryMock;
    private readonly Mock<IPricingRuleChangeRequestRepository> _changeRequestRepositoryMock;

    private readonly PricingRuleChangeRequestService _service;

    public PricingRuleChangeRequestServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _unitOfWorkMock.Setup(x => x.Notifications).Returns(new Mock<INotificationRepository>().Object);

        _unitOfWorkMock.Setup(x => x.Roles).Returns(new Mock<IRoleRepository>().Object);

        _pricingRuleRepositoryMock =
            new Mock<IPricingRuleRepository>();

        _changeRequestRepositoryMock =
            new Mock<IPricingRuleChangeRequestRepository>();

        _unitOfWorkMock
            .Setup(x => x.PricingRules)
            .Returns(_pricingRuleRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.PricingRuleChangeRequests)
            .Returns(_changeRequestRepositoryMock.Object);

        _service =
            new PricingRuleChangeRequestService(
                _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenRuleExists_ShouldCreatePendingRequest()
    {
        var ruleId = Guid.NewGuid();
        var requestedBy = Guid.NewGuid();

        var rule = new PricingRule
        {
            Id = ruleId,
            Code = "BASE_KASKO_RATE",
            Name = "Base Kasko Rate",
            Value = 0.0200m,
            Version = 1,
            EffectiveFrom = new DateTime(2026, 1, 1),
            IsActive = true,
            IsDeleted = false
        };

        var effectiveFrom =
            DateTime.UtcNow.AddDays(10);

        var dto = new CreatePricingRuleChangeRequestDto
        {
            PricingRuleId = ruleId,
            NewValue = 0.0250m,
            Reason = "Yeni fiyatlandırma oranı",
            EffectiveFrom = effectiveFrom
        };

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(ruleId))
            .ReturnsAsync(rule);

        PricingRuleChangeRequest? addedRequest = null;

        _changeRequestRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<PricingRuleChangeRequest>()))
            .Callback<PricingRuleChangeRequest>(
                request => addedRequest = request)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result =
            await _service.CreateAsync(dto, requestedBy);

        Assert.NotNull(addedRequest);

        Assert.Equal(ruleId, addedRequest!.PricingRuleId);
        Assert.Equal(rule.Value, addedRequest.OldValue);
        Assert.Equal(dto.NewValue, addedRequest.NewValue);
        Assert.Equal(dto.Reason, addedRequest.Reason);
        Assert.Equal(requestedBy, addedRequest.RequestedBy);
        Assert.Equal("Pending", addedRequest.Status);
        Assert.Equal(dto.EffectiveFrom, addedRequest.EffectiveFrom);

        Assert.Equal(
            addedRequest.Id,
            result.Id);

        _changeRequestRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<PricingRuleChangeRequest>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task ApproveAsync_WhenPendingRequest_ShouldCreateNewVersionAndApproveRequest()
    {
        var ruleId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var approvedBy = Guid.NewGuid();

        var currentEffectiveFrom =
            new DateTime(2026, 1, 1);

        var newEffectiveFrom =
            new DateTime(2026, 10, 1);

        var currentRule = new PricingRule
        {
            Id = ruleId,
            Code = "BASE_KASKO_RATE",
            Name = "Base Kasko Rate",
            Description = "Temel kasko oranı",
            Value = 0.0200m,
            Version = 2,
            EffectiveFrom = currentEffectiveFrom,
            EffectiveUntil = null,
            IsActive = true,
            IsDeleted = false
        };

        var request = new PricingRuleChangeRequest
        {
            Id = requestId,
            PricingRuleId = ruleId,
            OldValue = 0.0200m,
            NewValue = 0.0250m,
            Reason = "Yeni fiyatlandırma oranı",
            RequestedBy = Guid.NewGuid(),
            RequestedDate = DateTime.UtcNow,
            Status = "Pending",
            EffectiveFrom = newEffectiveFrom,
            IsDeleted = false
        };

        _changeRequestRepositoryMock
            .Setup(x => x.GetByIdAsync(requestId))
            .ReturnsAsync(request);

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(ruleId))
            .ReturnsAsync(currentRule);

        _pricingRuleRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<PricingRule>
            {
            new PricingRule
            {
                Id = Guid.NewGuid(),
                Code = "BASE_KASKO_RATE",
                Version = 1,
                Value = 0.0180m,
                EffectiveFrom = new DateTime(2025, 1, 1),
                IsActive = true,
                IsDeleted = false
            },
            currentRule
            });

        PricingRule? addedRule = null;

        _pricingRuleRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PricingRule>()))
            .Callback<PricingRule>(
                rule => addedRule = rule)
            .Returns(Task.CompletedTask);

        _pricingRuleRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<PricingRule>()))
            .Returns(Task.CompletedTask);

        _changeRequestRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<PricingRuleChangeRequest>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        await _service.ApproveAsync(
            requestId,
            approvedBy);

        Assert.NotNull(addedRule);

        Assert.Equal("BASE_KASKO_RATE", addedRule!.Code);
        Assert.Equal(3, addedRule.Version);
        Assert.Equal(0.0250m, addedRule.Value);
        Assert.Equal(newEffectiveFrom, addedRule.EffectiveFrom);
        Assert.Null(addedRule.EffectiveUntil);
        Assert.True(addedRule.IsActive);
        Assert.False(addedRule.IsDeleted);

        Assert.Equal(
            newEffectiveFrom.AddTicks(-1),
            currentRule.EffectiveUntil);

        Assert.Equal("Approved", request.Status);
        Assert.Equal(approvedBy, request.ApprovedBy);
        Assert.NotNull(request.ApprovedDate);

        _pricingRuleRepositoryMock.Verify(
            x => x.UpdateAsync(currentRule),
            Times.Once);

        _pricingRuleRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<PricingRule>()),
            Times.Once);

        _changeRequestRepositoryMock.Verify(
            x => x.UpdateAsync(request),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task RejectAsync_WhenPendingRequest_ShouldRejectWithoutChangingPricingRule()
    {
        var ruleId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var rejectedBy = Guid.NewGuid();

        var currentRule = new PricingRule
        {
            Id = ruleId,
            Code = "BASE_KASKO_RATE",
            Name = "Base Kasko Rate",
            Description = "Temel kasko oranı",
            Value = 0.0200m,
            Version = 2,
            EffectiveFrom = new DateTime(2026, 1, 1),
            EffectiveUntil = null,
            IsActive = true,
            IsDeleted = false
        };

        var request = new PricingRuleChangeRequest
        {
            Id = requestId,
            PricingRuleId = ruleId,
            OldValue = 0.0200m,
            NewValue = 0.0300m,
            Reason = "Oran artırımı",
            RequestedBy = Guid.NewGuid(),
            RequestedDate = DateTime.UtcNow,
            Status = "Pending",
            EffectiveFrom = new DateTime(2026, 10, 1),
            IsDeleted = false
        };

        _changeRequestRepositoryMock
            .Setup(x => x.GetByIdAsync(requestId))
            .ReturnsAsync(request);

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(ruleId))
            .ReturnsAsync(currentRule);

        _changeRequestRepositoryMock
            .Setup(x => x.UpdateAsync(
                It.IsAny<PricingRuleChangeRequest>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        await _service.RejectAsync(
            requestId,
            rejectedBy);

        Assert.Equal("Rejected", request.Status);
        Assert.Equal(rejectedBy, request.ApprovedBy);
        Assert.NotNull(request.ApprovedDate);

        Assert.Equal(0.0200m, currentRule.Value);
        Assert.Equal(2, currentRule.Version);
        Assert.Null(currentRule.EffectiveUntil);
        Assert.True(currentRule.IsActive);
        Assert.False(currentRule.IsDeleted);

        _pricingRuleRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PricingRule>()),
            Times.Never);

        _pricingRuleRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<PricingRule>()),
            Times.Never);

        _changeRequestRepositoryMock.Verify(
            x => x.UpdateAsync(request),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
}