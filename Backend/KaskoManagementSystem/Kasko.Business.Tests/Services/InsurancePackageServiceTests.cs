using Kasko.Business.DTOs.Package;
using Kasko.Business.Services;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Moq;

namespace Kasko.Business.Tests.Services;

public class InsurancePackageServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IInsurancePackageRepository> _insurancePackageRepositoryMock;
    private readonly Mock<IPackageCoverageRepository> _packageCoverageRepositoryMock;
    private readonly Mock<ICoverageRepository> _coverageRepositoryMock;

    private readonly InsurancePackageService _service;

    public InsurancePackageServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _insurancePackageRepositoryMock = new Mock<IInsurancePackageRepository>();
        _packageCoverageRepositoryMock = new Mock<IPackageCoverageRepository>();
        _coverageRepositoryMock = new Mock<ICoverageRepository>();

        _unitOfWorkMock
            .Setup(x => x.InsurancePackages)
            .Returns(_insurancePackageRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.PackageCoverages)
            .Returns(_packageCoverageRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.Coverages)
            .Returns(_coverageRepositoryMock.Object);

        var coverageOptionRepositoryMock =
            new Mock<IGenericRepository<CoverageOption>>();

        coverageOptionRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<CoverageOption, bool>>>()))
            .ReturnsAsync(new List<CoverageOption>());

        _service = new InsurancePackageService(
            _unitOfWorkMock.Object,
            coverageOptionRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyActivePackages()
    {
        var activePackageId = Guid.NewGuid();
        var deletedPackageId = Guid.NewGuid();

        _insurancePackageRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                new List<InsurancePackage>
                {
                    new InsurancePackage
                    {
                        Id = activePackageId,
                        Code = "STANDART",
                        Name = "Standart Paket",
                        Description = "Genişletilmiş kasko paketi",
                        Factor = 1m,
                        IsActive = true,
                        IsDeleted = false
                    },
                    new InsurancePackage
                    {
                        Id = deletedPackageId,
                        Code = "DELETED",
                        Name = "Silinmiş Paket",
                        Factor = 1m,
                        IsActive = true,
                        IsDeleted = true
                    }
                });

        _packageCoverageRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PackageCoverage, bool>>>()))
            .ReturnsAsync(Array.Empty<PackageCoverage>());

        var result =
            await _service.GetAllAsync();

        var packages = result.ToList();

        Assert.Single(packages);
        Assert.Equal("STANDART", packages[0].Code);
    }

    [Fact]
    public async Task GetAllAsync_ShouldMapPackageCoverage()
    {
        var packageId = Guid.NewGuid();
        var coverageId = Guid.NewGuid();

        _insurancePackageRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(
                new List<InsurancePackage>
                {
                    new InsurancePackage
                    {
                        Id = packageId,
                        Code = "STANDART",
                        Name = "Standart Paket",
                        Description = "Genişletilmiş kasko paketi",
                        Factor = 1m,
                        IsActive = true,
                        IsDeleted = false
                    }
                });

        _packageCoverageRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PackageCoverage, bool>>>()))
            .ReturnsAsync(
                new List<PackageCoverage>
                {
                    new PackageCoverage
                    {
                        Id = Guid.NewGuid(),
                        InsurancePackageId = packageId,
                        CoverageId = coverageId,
                        IsDefault = true,
                        IsDeleted = false
                    }
                });

        _coverageRepositoryMock
            .Setup(x => x.GetByIdAsync(coverageId))
            .ReturnsAsync(
                new Coverage
                {
                    Id = coverageId,
                    Name = "Cam Kırılması",
                    BasePrice = 900m,
                    IsActive = true,
                    IsDeleted = false
                });

        var result =
            await _service.GetAllAsync();

        var package = Assert.Single(result);
        var coverage = Assert.Single(package.Coverages);

        Assert.Equal(
            coverageId,
            coverage.CoverageId);

        Assert.Equal(
            "Cam Kırılması",
            coverage.CoverageName);

        Assert.Equal(
            900m,
            coverage.CalculatedPrice);

        Assert.True(
            coverage.IsDefault);
    }
}