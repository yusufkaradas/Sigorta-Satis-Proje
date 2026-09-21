using Kasko.Entities.Enums;

namespace Kasko.Business.DTOs.Tariff;

public class TariffDto
{
    public List<TariffPackageDto> Packages { get; set; } = new();

    public List<TariffCoverageDto> Coverages { get; set; } = new();
}

public class TariffPackageDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Factor { get; set; }

    public List<string> CoverageNames { get; set; } = new();

    public List<Guid> CoverageIds { get; set; } = new();
}

public class SetPackageCoverageDto
{
    public bool Included { get; set; }
}

public class UpdatePackageInfoDto
{
    public string? Description { get; set; }
}

public class TariffCoverageDto
{
    public Guid Id { get; set; }

    public bool IsRequired { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public CoveragePricingType PricingType { get; set; }

    public decimal BasePrice { get; set; }

    public decimal? Rate { get; set; }

    public List<TariffOptionDto> Options { get; set; } = new();
}

public class TariffOptionDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal ExtraPrice { get; set; }

    public bool IsDefault { get; set; }
}

public class TariffChangeRequestDto
{
    public Guid Id { get; set; }

    public string TargetType { get; set; } = string.Empty;

    public Guid TargetId { get; set; }

    public string TargetName { get; set; } = string.Empty;

    public string Field { get; set; } = string.Empty;

    public decimal OldValue { get; set; }

    public decimal NewValue { get; set; }

    public string Reason { get; set; } = string.Empty;

    public TariffRequestStatus Status { get; set; }

    public string RequestedByName { get; set; } = string.Empty;

    public DateTime RequestedDate { get; set; }

    public DateTime? DecidedDate { get; set; }

    public string? DecisionNote { get; set; }
}

public class CreateTariffChangeRequestDto
{
    public string TargetType { get; set; } = string.Empty;

    public Guid TargetId { get; set; }

    public string Field { get; set; } = string.Empty;

    public decimal NewValue { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class DecideTariffChangeRequestDto
{
    public string? Note { get; set; }
}
