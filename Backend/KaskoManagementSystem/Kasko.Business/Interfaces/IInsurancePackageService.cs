using Kasko.Business.DTOs.Package;

namespace Kasko.Business.Interfaces;

public interface IInsurancePackageService
{
    Task<IEnumerable<InsurancePackageDto>> GetAllAsync();
}