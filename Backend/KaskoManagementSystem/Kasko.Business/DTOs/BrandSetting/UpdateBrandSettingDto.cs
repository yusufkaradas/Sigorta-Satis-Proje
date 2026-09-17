namespace Kasko.Business.DTOs.BrandSetting;

public class UpdateBrandSettingDto
{
    public string CompanyName { get; set; } = string.Empty;

    public string SystemName { get; set; } = string.Empty;

    public string? LogoImage { get; set; }

    public string? LoginImage { get; set; }

    public string? FaviconImage { get; set; }
}
