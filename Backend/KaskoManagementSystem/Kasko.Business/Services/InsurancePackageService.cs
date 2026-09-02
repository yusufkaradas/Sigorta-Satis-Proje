using Kasko.Business.DTOs.Package;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Business.Interfaces;

namespace Kasko.Business.Services;

public class InsurancePackageService : IInsurancePackageService
{
    private readonly IUnitOfWork _unitOfWork;

    public InsurancePackageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<InsurancePackageDto>> GetAllAsync()
    {
        var packages = await _unitOfWork.InsurancePackages.GetAllAsync();

        var result = new List<InsurancePackageDto>();

        foreach (var package in packages.Where(x => x.IsActive && !x.IsDeleted))
        {
            var packageCoverages =
                await _unitOfWork.PackageCoverages.FindAsync(
                    x => x.InsurancePackageId == package.Id &&
                         !x.IsDeleted);

            var coverageDtos = new List<PackageCoverageDto>();

            foreach (var packageCoverage in packageCoverages)
            {
                var coverage =
                    await _unitOfWork.Coverages
                        .GetByIdAsync(packageCoverage.CoverageId);

                if (coverage == null ||
                    coverage.IsDeleted ||
                    !coverage.IsActive)
                {
                    continue;
                }

                coverageDtos.Add(
                    new PackageCoverageDto
                    {
                        CoverageId = coverage.Id,
                        CoverageName = coverage.Name,
                        CalculatedPrice = coverage.BasePrice,
                        IsDefault = packageCoverage.IsDefault
                    });
            }

            result.Add(
                new InsurancePackageDto
                {
                    Id = package.Id,
                    Code = package.Code,
                    Name = package.Name,
                    Description = package.Description,
                    Factor = package.Factor,
                    IsActive = package.IsActive,
                    Coverages = coverageDtos
                });
        }

        return result;
    }
}