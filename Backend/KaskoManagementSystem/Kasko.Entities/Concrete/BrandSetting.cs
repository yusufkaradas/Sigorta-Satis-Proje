using Kasko.Entities.Abstract;

namespace Kasko.Entities.Concrete;

public class BrandSetting : BaseEntity
{
    public string CompanyName { get; set; } = "Şirket Adı";

    public string SystemName { get; set; } = "Sigorta Yönetim Sistemi";

    public string? LogoImage { get; set; }

    public string? LoginImage { get; set; }

    public string? FaviconImage { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
