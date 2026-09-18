using System.Security.Cryptography;
using System.Text;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Kasko.Business.Services.Concrete;

public class PolicyPdfService : IPolicyPdfService
{
    private const decimal ExpenseTaxRate = 0.05m;

    private const string Primary = "#374151";

    private const string Muted = "#6B7280";

    private const string Line = "#D1D5DB";

    private const string Soft = "#F3F4F6";

    private readonly IUnitOfWork _unitOfWork;

    private readonly IGenericRepository<BrandSetting> _brandSettings;

    public PolicyPdfService(
        IUnitOfWork unitOfWork,
        IGenericRepository<BrandSetting> brandSettings)
    {
        _unitOfWork = unitOfWork;
        _brandSettings = brandSettings;
    }

    public async Task<byte[]> GenerateAsync(Guid policyId)
    {
        var policy =
            await _unitOfWork.Policies
                .GetByIdIncludingDetailsAsync(policyId);

        if (policy == null || policy.IsDeleted)
        {
            throw new NotFoundException("Poliçe bulunamadı.");
        }

        if (policy.Status != PolicyStatus.Active &&
            policy.Status != PolicyStatus.Expired)
        {
            throw new BadRequestException(
                "Poliçe belgesi ödeme tamamlandıktan sonra oluşturulur.");
        }

        var quote =
            await _unitOfWork.Quotes
                .GetByIdIncludingDetailsAsync(policy.QuoteId);

        if (quote == null || quote.IsDeleted)
        {
            throw new NotFoundException("Teklif bulunamadı.");
        }

        var payments =
            (await _unitOfWork.Payments.FindAsync(x => x.PolicyId == policy.Id && !x.IsDeleted))
                .OrderBy(x => x.PaymentDate ?? x.CreatedDate)
                .ToList();

        var brand =
            (await _brandSettings.GetAllAsync()).FirstOrDefault();

        var companyName = string.IsNullOrWhiteSpace(brand?.CompanyName)
            ? "Kasko Sigorta"
            : brand!.CompanyName;

        var logo = DecodeImage(brand?.LogoImage);

        var customer = quote.Customer;

        var vehicle = quote.Vehicle;

        var total = policy.PremiumAmount;

        var net = Math.Round(total / (1 + ExpenseTaxRate), 2, MidpointRounding.AwayFromZero);

        var tax = total - net;

        var marketValue = quote.PricingSnapshot?.MarketValue ?? vehicle.MarketValue;

        var verificationCode = BuildVerificationCode(policy);

        QuestPDF.Settings.License =
            LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#111827"));

                page.Header().Element(header => ComposeHeader(header, companyName, logo, policy, quote));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(9);

                    column.Item().Row(row =>
                    {
                        row.Spacing(9);

                        row.RelativeItem().Element(box => InfoBox(box, "SİGORTA ŞİRKETİ", new[]
                        {
                            ("Unvanı", companyName),
                            ("Ürün", string.IsNullOrWhiteSpace(quote.PackageName) ? "Kasko Sigorta Poliçesi" : $"Kasko · {quote.PackageName}"),
                            ("Düzenlenme", $"{policy.CreatedDate.ToLocalTime():dd.MM.yyyy HH:mm}")
                        }));

                        row.RelativeItem().Element(box => InfoBox(box, "SİGORTALI / SİGORTA ETTİREN", new[]
                        {
                            ("Adı Soyadı", $"{customer.FirstName} {customer.LastName}"),
                            ("T.C. Kimlik No", MaskIdentity(customer.IdentityNumber)),
                            ("Telefon", MaskPhone(customer.PhoneNumber)),
                            ("E-posta", MaskEmail(customer.Email)),
                            ("Adres", $"{customer.District} / {customer.City}".Trim(' ', '/'))
                        }));
                    });

                    column.Item().Element(box => InfoBox(box, "SİGORTA KONUSU ARAÇ", new[]
                    {
                        ("Marka / Model", $"{vehicle.Brand} {vehicle.Model}"),
                        ("Model Yılı", vehicle.ModelYear.ToString()),
                        ("Plaka", string.IsNullOrWhiteSpace(vehicle.PlateNumber) ? "—" : vehicle.PlateNumber),
                        ("Kasa / Yakıt / Vites", $"{VehicleTypeText(vehicle.VehicleType)} · {FuelText(vehicle.FuelType)} · {TransmissionText(vehicle.TransmissionType)}"),
                        ("Renk", string.IsNullOrWhiteSpace(vehicle.Color) ? "—" : vehicle.Color),
                        ("Sigorta Bedeli (TSB Rayiç)", $"{marketValue:N2} TL")
                    }, 2));

                    column.Item().Element(box => ComposeCoverages(box, quote));

                    column.Item().Row(row =>
                    {
                        row.Spacing(9);

                        row.RelativeItem().Element(box => InfoBox(box, "PRİM BİLGİLERİ", new[]
                        {
                            ("Net Prim", $"{net:N2} TL"),
                            ("Gider Vergisi (%5)", $"{tax:N2} TL"),
                            ("Ödenecek Toplam Prim", $"{total:N2} TL")
                        }));

                        row.RelativeItem().Element(box => ComposePayments(box, payments, total));
                    });
                });

                page.Footer().Element(footer => ComposeFooter(footer, policy, verificationCode));
            });

            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#111827"));

                page.Header().Element(header => ComposeHeader(header, companyName, logo, policy, quote));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(7);

                    column.Item().Text("ÖZEL ŞARTLAR VE BİLGİLENDİRME").Bold().FontSize(11).FontColor(Primary);

                    foreach (var (title, body) in Conditions(policy, quote.PricingSnapshot?.DeductibleFactor ?? 1m))
                    {
                        column.Item().Column(item =>
                        {
                            item.Item().Text(title).Bold().FontSize(9);
                            item.Item().PaddingTop(2).Text(body).FontColor("#374151").LineHeight(1.35f);
                        });
                    }
                });

                page.Footer().Element(footer => ComposeFooter(footer, policy, verificationCode));
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GenerateTermsAsync(Guid policyId)
    {
        var policy =
            await _unitOfWork.Policies
                .GetByIdIncludingDetailsAsync(policyId);

        if (policy == null || policy.IsDeleted)
        {
            throw new NotFoundException("Poliçe bulunamadı.");
        }

        var quote =
            await _unitOfWork.Quotes
                .GetByIdIncludingDetailsAsync(policy.QuoteId);

        if (quote == null)
        {
            throw new NotFoundException("Teklif bulunamadı.");
        }

        var brand =
            (await _brandSettings.GetAllAsync()).FirstOrDefault();

        var companyName = string.IsNullOrWhiteSpace(brand?.CompanyName)
            ? "Kasko Sigorta"
            : brand!.CompanyName;

        var logo = DecodeImage(brand?.LogoImage);

        var verificationCode = BuildVerificationCode(policy);

        var sections = new List<(string Title, string Body)>
        {
            ("1. Sigortacı ve Aracı Bilgisi",
                $"Bu sözleşme {companyName} tarafından düzenlenmiştir. Poliçe ve ödeme işlemleri müşteri portalı üzerinden yürütülür; tüm belgelere Poliçelerim sayfasından ulaşabilirsiniz."),
            ("2. Sigortanın Konusu",
                "Kasko sigortası, poliçede yazılı aracın teminat tablosunda belirtilen risklerden doğan maddi hasarlarını poliçe süresi boyunca güvence altına alır."),
            ("3. Teminat Dışında Kalan Haller",
                "Alkollü veya ehliyetsiz araç kullanımı, kasıtlı hasar, aracın kiraya verilmesi, yarış ve deneme sürüşleri ile poliçede yer almayan riskler teminat dışıdır."),
            ("4. Hasar Anında Yapılması Gerekenler",
                "Hasarı en geç 5 iş günü içinde bildirin, olay yerinde tutanak veya polis raporu alın, aracı onarım için eksper incelemesinden önce değiştirmeyin."),
            ("5. Prim Ödemesi",
                "Prim peşin veya seçilen taksit sayısına göre kartınızdan tahsil edilir. Taksitin ödenmemesi halinde teminat ödeme yapılana kadar askıya alınabilir."),
            ("6. Cayma ve İptal",
                "Poliçe başlangıcından itibaren 30 gün içinde cayma hakkınızı kullanabilirsiniz. İptal talebinde iade, poliçenin kullanılmayan gün sayısına göre hesaplanır."),
            ("7. Kişisel Verilerin Korunması",
                "Kişisel verileriniz 6698 sayılı Kanun kapsamında yalnızca sigorta sözleşmesinin kurulması ve yürütülmesi amacıyla işlenir, yasal zorunluluklar dışında üçüncü kişilerle paylaşılmaz."),
            ("8. Uyuşmazlıkların Çözümü",
                "Sözleşmeden doğan uyuşmazlıklarda öncelikle müşteri hizmetlerine başvurabilir, sonuç alamazsanız Sigorta Tahkim Komisyonu'na veya yetkili mahkemelere başvurabilirsiniz.")
        };

        sections.AddRange(Conditions(policy, quote.PricingSnapshot?.DeductibleFactor ?? 1m)
            .Select((item, index) => ($"{9 + index}. {item.Title.Split(' ', 2).Last()}", item.Body)));

        QuestPDF.Settings.License =
            LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#111827"));

                page.Header().Element(header => ComposeHeader(header, companyName, logo, policy, quote));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(7);

                    column.Item().Text("ÖN BİLGİLENDİRME FORMU VE GENEL ŞARTLAR ÖZETİ").Bold().FontSize(11).FontColor(Primary);

                    foreach (var (title, body) in sections)
                    {
                        column.Item().Column(item =>
                        {
                            item.Item().Text(title).Bold().FontSize(9);
                            item.Item().PaddingTop(2).Text(body).FontColor("#374151").LineHeight(1.35f);
                        });
                    }
                });

                page.Footer().Element(footer => ComposeFooter(footer, policy, verificationCode));
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GenerateQuoteAsync(Guid quoteId)
    {
        var quote =
            await _unitOfWork.Quotes
                .GetByIdIncludingDetailsAsync(quoteId);

        if (quote == null || quote.IsDeleted)
        {
            throw new NotFoundException("Teklif bulunamadı.");
        }

        var brand =
            (await _brandSettings.GetAllAsync()).FirstOrDefault();

        var companyName = string.IsNullOrWhiteSpace(brand?.CompanyName)
            ? "Kasko Sigorta"
            : brand!.CompanyName;

        var logo = DecodeImage(brand?.LogoImage);

        var customer = quote.Customer;

        var vehicle = quote.Vehicle;

        var total = quote.PremiumAmount;

        var net = Math.Round(total / (1 + ExpenseTaxRate), 2, MidpointRounding.AwayFromZero);

        var marketValue = quote.PricingSnapshot?.MarketValue ?? vehicle.MarketValue;

        var deductibleFactor = quote.PricingSnapshot?.DeductibleFactor ?? 1m;

        QuestPDF.Settings.License =
            LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#111827"));

                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        if (logo != null)
                        {
                            row.ConstantItem(90).Height(42).Image(logo).FitArea();
                            row.ConstantItem(10);
                        }

                        row.RelativeItem().Column(title =>
                        {
                            title.Item().Text(companyName).Bold().FontSize(15).FontColor(Primary);
                            title.Item().Text("KASKO SİGORTASI TEKLİF FORMU").SemiBold().FontSize(10).FontColor(Muted);
                        });

                        row.ConstantItem(210).Border(1).BorderColor(Line).Padding(6).Column(meta =>
                        {
                            MetaLine(meta, "Teklif No", quote.QuoteNumber);
                            MetaLine(meta, "Teklif Tarihi", $"{quote.CreatedDate.ToLocalTime():dd.MM.yyyy HH:mm}");
                            MetaLine(meta, "Geçerlilik", $"{quote.ValidUntil.ToLocalTime():dd.MM.yyyy}");
                            MetaLine(meta, "Paket", string.IsNullOrWhiteSpace(quote.PackageName) ? "Kasko" : quote.PackageName);
                        });
                    });

                    column.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Primary);
                });

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(9);

                    column.Item().Row(row =>
                    {
                        row.Spacing(9);

                        row.RelativeItem().Element(box => InfoBox(box, "SİGORTA ETTİREN", new[]
                        {
                            ("Adı Soyadı", $"{customer.FirstName} {customer.LastName}"),
                            ("T.C. Kimlik No", MaskIdentity(customer.IdentityNumber)),
                            ("Telefon", MaskPhone(customer.PhoneNumber)),
                            ("E-posta", MaskEmail(customer.Email))
                        }));

                        row.RelativeItem().Element(box => InfoBox(box, "ARAÇ", new[]
                        {
                            ("Marka / Model", $"{vehicle.Brand} {vehicle.Model}"),
                            ("Model Yılı", vehicle.ModelYear.ToString()),
                            ("Plaka", string.IsNullOrWhiteSpace(vehicle.PlateNumber) ? "—" : vehicle.PlateNumber),
                            ("Sigorta Bedeli (TSB Rayiç)", $"{marketValue:N2} TL")
                        }));
                    });

                    column.Item().Element(box => ComposeCoverages(box, quote));

                    column.Item().Element(box => InfoBox(box, "PRİM BİLGİLERİ", new[]
                    {
                        ("Net Prim", $"{net:N2} TL"),
                        ("Gider Vergisi (%5)", $"{total - net:N2} TL"),
                        ("Toplam Prim (Peşin)", $"{total:N2} TL"),
                        ("Taksit Seçenekleri", $"3 x {total / 3:N2} TL · 6 x {total / 6:N2} TL · 9 x {total / 9:N2} TL"),
                        ("Muafiyet", deductibleFactor < 1m ? $"Hasar başına %{(deductibleFactor <= 0.80m ? 5 : 2)}" : "Muafiyetsiz")
                    }));

                    column.Item().Background(Soft).Padding(8).Text(
                        $"Bu teklif {quote.ValidUntil.ToLocalTime():dd.MM.yyyy} tarihine kadar geçerlidir. Prim, teklif tarihindeki beyanlarınıza ve TSB kasko değer listesine göre hesaplanmıştır. " +
                        "Satın alma işlemini müşteri portalınızdaki Tekliflerim bölümünden tamamlayabilirsiniz.")
                        .FontSize(8).FontColor(Primary);
                });

                page.Footer().Column(column =>
                {
                    column.Item().LineHorizontal(0.5f).LineColor(Line);
                    column.Item().PaddingTop(4).Text($"Teklif No: {quote.QuoteNumber}").FontSize(7).FontColor(Muted);
                    column.Item().PaddingTop(2).AlignCenter()
                        .Text("Bu belge eğitim amaçlı bir staj projesinde üretilmiştir; gerçek bir sigorta teklifi değildir.")
                        .FontSize(7).Italic().FontColor(Muted);
                });
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GenerateEstimateAsync(
        Kasko.Business.DTOs.QuickQuote.QuickQuoteEstimateResultDto estimate,
        Kasko.Business.DTOs.QuickQuote.QuickQuoteEstimatePdfRequestDto request)
    {
        var brand =
            (await _brandSettings.GetAllAsync()).FirstOrDefault();

        var companyName = string.IsNullOrWhiteSpace(brand?.CompanyName)
            ? "Kasko Sigorta"
            : brand!.CompanyName;

        var logo = DecodeImage(brand?.LogoImage);

        var createdAt = DateTime.Now;

        var validUntil = createdAt.Date.AddDays(7);

        var reference = string.IsNullOrWhiteSpace(request.Reference) ? $"HT-{createdAt:yyyyMMdd-HHmm}" : request.Reference.Trim();

        var coverageNames = estimate.Packages
            .SelectMany(x => x.Coverages)
            .Select(x => x.CoverageName)
            .Distinct()
            .ToList();

        var deductibleText = request.Deductible switch
        {
            2m => "Hasar başına %2",
            5m => "Hasar başına %5",
            _ => "Muafiyetsiz"
        };

        QuestPDF.Settings.License =
            LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontColor("#111827"));

                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        if (logo != null)
                        {
                            row.ConstantItem(90).Height(42).Image(logo).FitArea();
                            row.ConstantItem(10);
                        }

                        row.RelativeItem().Column(title =>
                        {
                            title.Item().Text(companyName).Bold().FontSize(15).FontColor(Primary);
                            title.Item().Text("KASKO HIZLI TEKLİF KARŞILAŞTIRMASI").SemiBold().FontSize(10).FontColor(Muted);
                        });

                        row.ConstantItem(210).Border(1).BorderColor(Line).Padding(6).Column(meta =>
                        {
                            MetaLine(meta, "Referans", reference);
                            MetaLine(meta, "Teklif Tarihi", $"{createdAt:dd.MM.yyyy HH:mm}");
                            MetaLine(meta, "Geçerlilik", $"{validUntil:dd.MM.yyyy}");
                        });
                    });

                    column.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Primary);
                });

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(9);

                    column.Item().Row(row =>
                    {
                        row.Spacing(9);

                        row.RelativeItem().Element(box => InfoBox(box, "ARAÇ", new[]
                        {
                            ("Marka / Model", $"{estimate.BrandName} {estimate.TypeName}"),
                            ("Model Yılı", estimate.ModelYear.ToString()),
                            ("Plaka", string.IsNullOrWhiteSpace(request.PlateNumber) ? "—" : request.PlateNumber!),
                            ("Sigorta Bedeli (TSB Rayiç)", $"{estimate.MarketValue:N2} TL")
                        }));

                        row.RelativeItem().Element(box => InfoBox(box, "BEYAN EDİLEN BİLGİLER", new[]
                        {
                            ("Kullanım", string.Equals(request.Usage, "PRIVATE", StringComparison.OrdinalIgnoreCase) ? "Özel" : "Ticari"),
                            ("Hasar Geçmişi", request.ClaimsCount == 0 ? "Hasarsız" : $"{request.ClaimsCount} hasar"),
                            ("Sürücü Doğum Yılı", request.BirthYear > 0 ? request.BirthYear.ToString() : "—"),
                            ("Muafiyet", deductibleText)
                        }));
                    });

                    column.Item().Border(1).BorderColor(Line).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            foreach (var _ in estimate.Packages)
                            {
                                columns.RelativeColumn(1.4f);
                            }
                        });

                        table.Cell().Background(Primary).Padding(6).Text("TEMİNAT").Bold().FontColor("#FFFFFF");

                        foreach (var package in estimate.Packages)
                        {
                            var selected = package.PackageId == request.PackageId;
                            table.Cell().Background(selected ? "#2563EB" : Primary).Padding(6).AlignCenter()
                                .Text(selected ? $"{package.PackageName} (Seçili)" : package.PackageName).Bold().FontColor("#FFFFFF");
                        }

                        foreach (var name in coverageNames)
                        {
                            table.Cell().BorderTop(0.5f).BorderColor(Line).Padding(5).Text(name);

                            foreach (var package in estimate.Packages)
                            {
                                var coverage = package.Coverages.FirstOrDefault(x => x.CoverageName == name);
                                var text = coverage == null
                                    ? "—"
                                    : !string.IsNullOrWhiteSpace(coverage.OptionName)
                                        ? coverage.OptionName!
                                        : coverage.Limit.HasValue ? $"{coverage.Limit:N0} TL" : "Dahil";
                                table.Cell().BorderTop(0.5f).BorderColor(Line).Padding(5).AlignCenter()
                                    .Text(text).FontColor(coverage == null ? Muted : "#111827");
                            }
                        }

                        table.Cell().Background(Soft).Padding(6).Text("YILLIK PRİM (Vergiler dahil)").Bold();

                        foreach (var package in estimate.Packages)
                        {
                            table.Cell().Background(Soft).Padding(6).AlignCenter().Text($"{package.TotalPremium:N2} TL").Bold().FontSize(10);
                        }

                        table.Cell().Padding(5).Text("9 taksitle aylık").FontColor(Muted);

                        foreach (var package in estimate.Packages)
                        {
                            table.Cell().Padding(5).AlignCenter().Text($"{package.TotalPremium / 9:N2} TL").FontColor(Muted);
                        }
                    });

                    column.Item().Background(Soft).Padding(8).Column(note =>
                    {
                        note.Spacing(3);
                        note.Item().Text("Nasıl satın alırım?").Bold().FontColor(Primary);
                        note.Item().Text("1. Müşteri portalına giriş yapın; hesabınız yoksa birkaç dakikada kayıt olun.").FontSize(8);
                        note.Item().Text("2. Aracınızı ekleyip Tekliflerim > Yeni Teklif Al adımlarıyla dilediğiniz paketi seçin.").FontSize(8);
                        note.Item().Text("3. Peşin veya 3/6/9 taksitle ödemenizi yapın; poliçeniz anında düzenlenir.").FontSize(8);
                    });

                    column.Item().Text(
                        $"Bu teklif bilgilendirme amaçlıdır ve {validUntil:dd.MM.yyyy} tarihine kadar geçerlidir. Kesin prim, portalda beyanlarınız ve güncel TSB kasko değeri doğrulandıktan sonra belirlenir.")
                        .FontSize(7.5f).FontColor(Muted);
                });

                page.Footer().Column(column =>
                {
                    column.Item().LineHorizontal(0.5f).LineColor(Line);
                    column.Item().PaddingTop(4).Text($"Referans: {reference}").FontSize(7).FontColor(Muted);
                    column.Item().PaddingTop(2).AlignCenter()
                        .Text("Bu belge eğitim amaçlı bir staj projesinde üretilmiştir; gerçek bir sigorta teklifi değildir.")
                        .FontSize(7).Italic().FontColor(Muted);
                });
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, string companyName, byte[]? logo, Policy policy, Quote quote)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                if (logo != null)
                {
                    row.ConstantItem(90).Height(42).Image(logo).FitArea();
                    row.ConstantItem(10);
                }

                row.RelativeItem().Column(title =>
                {
                    title.Item().Text(companyName).Bold().FontSize(15).FontColor(Primary);
                    title.Item().Text(PackageTitle(quote)).SemiBold().FontSize(10).FontColor(Muted);
                });

                row.ConstantItem(210).Border(1).BorderColor(Line).Padding(6).Column(meta =>
                {
                    MetaLine(meta, "Poliçe No", policy.PolicyNumber);
                    MetaLine(meta, "Teklif No", quote.QuoteNumber);
                    MetaLine(meta, "Başlangıç", $"{policy.StartDate.ToLocalTime():dd.MM.yyyy HH:mm}");
                    MetaLine(meta, "Bitiş", $"{policy.EndDate.ToLocalTime():dd.MM.yyyy HH:mm}");
                });
            });

            column.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Primary);
        });
    }

    private static void MetaLine(ColumnDescriptor column, string label, string value)
    {
        column.Item().Row(row =>
        {
            row.ConstantItem(60).Text(label).FontColor(Muted).FontSize(8);
            row.RelativeItem().AlignRight().Text(value).SemiBold().FontSize(8);
        });
    }

    private static void InfoBox(IContainer container, string title, (string Label, string Value)[] rows, int columns = 1)
    {
        container.Border(1).BorderColor(Line).Column(column =>
        {
            column.Item().Background(Soft).PaddingVertical(4).PaddingHorizontal(7).Text(title).Bold().FontSize(8.5f).FontColor(Primary);

            column.Item().Padding(7).Table(table =>
            {
                table.ColumnsDefinition(definition =>
                {
                    for (var i = 0; i < columns; i++)
                    {
                        definition.RelativeColumn(2);
                        definition.RelativeColumn(3);
                    }
                });

                foreach (var (label, value) in rows)
                {
                    table.Cell().PaddingVertical(2).Text(label).FontColor(Muted);
                    table.Cell().PaddingVertical(2).Text(string.IsNullOrWhiteSpace(value) ? "—" : value).SemiBold();
                }
            });
        });
    }

    private static void ComposeCoverages(IContainer container, Quote quote)
    {
        var coverages = quote.QuoteCoverages
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.CalculatedPrice > 0 ? 1 : 0)
            .ThenBy(x => x.Coverage.Name)
            .ToList();

        container.Border(1).BorderColor(Line).Column(column =>
        {
            column.Item().Background(Soft).PaddingVertical(4).PaddingHorizontal(7).Text("SİGORTA TEMİNATLARI").Bold().FontSize(8.5f).FontColor(Primary);

            column.Item().Padding(7).Table(table =>
            {
                table.ColumnsDefinition(definition =>
                {
                    definition.RelativeColumn(4);
                    definition.RelativeColumn(3);
                    definition.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().BorderBottom(1).BorderColor(Line).PaddingBottom(3).Text("Teminat").Bold().FontColor(Muted);
                    header.Cell().BorderBottom(1).BorderColor(Line).PaddingBottom(3).Text("Sigorta Bedeli / Limit").Bold().FontColor(Muted);
                    header.Cell().BorderBottom(1).BorderColor(Line).PaddingBottom(3).AlignRight().Text("Prim").Bold().FontColor(Muted);
                });

                foreach (var coverage in coverages)
                {
                    table.Cell().BorderBottom(0.5f).BorderColor(Soft).PaddingVertical(3).Text(coverage.Coverage.Name);
                    table.Cell().BorderBottom(0.5f).BorderColor(Soft).PaddingVertical(3).Text(LimitText(coverage));
                    table.Cell().BorderBottom(0.5f).BorderColor(Soft).PaddingVertical(3).AlignRight()
                        .Text(coverage.CalculatedPrice > 0 ? $"{coverage.CalculatedPrice:N2} TL" : "Pakete dahil");
                }
            });
        });
    }

    private static void ComposePayments(IContainer container, IReadOnlyList<Payment> payments, decimal total)
    {
        container.Border(1).BorderColor(Line).Column(column =>
        {
            column.Item().Background(Soft).PaddingVertical(4).PaddingHorizontal(7).Text("PRİM ÖDEME TABLOSU").Bold().FontSize(8.5f).FontColor(Primary);

            column.Item().Padding(7).Table(table =>
            {
                table.ColumnsDefinition(definition =>
                {
                    definition.RelativeColumn(2);
                    definition.RelativeColumn(2);
                    definition.RelativeColumn(2);
                    definition.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Text("Taksit").Bold().FontColor(Muted);
                    header.Cell().Text("Tarih").Bold().FontColor(Muted);
                    header.Cell().AlignRight().Text("Tutar").Bold().FontColor(Muted);
                    header.Cell().AlignRight().Text("Durum").Bold().FontColor(Muted);
                });

                if (payments.Count == 0)
                {
                    table.Cell().Text("Peşin");
                    table.Cell().Text("—");
                    table.Cell().AlignRight().Text($"{total:N2} TL");
                    table.Cell().AlignRight().Text("Bekliyor");
                    return;
                }

                foreach (var payment in payments)
                {
                    var count = payment.InstallmentCount > 1 ? payment.InstallmentCount : 1;
                    var paidAt = (payment.PaymentDate ?? payment.CreatedDate).ToLocalTime();
                    var perInstallment = Math.Round(payment.Amount / count, 2, MidpointRounding.AwayFromZero);

                    for (var index = 0; index < count; index++)
                    {
                        var amount = index == count - 1
                            ? payment.Amount - perInstallment * (count - 1)
                            : perInstallment;

                        var status = payment.Status != PaymentStatus.Successful
                            ? PaymentStatusText(payment.Status)
                            : index == 0 ? "Ödendi" : "Karttan tahsil edilecek";

                        table.Cell().PaddingTop(2).Text(count == 1 ? "Peşin" : $"{index + 1}/{count}. Taksit");
                        table.Cell().PaddingTop(2).Text($"{paidAt.AddMonths(index):dd.MM.yyyy}");
                        table.Cell().PaddingTop(2).AlignRight().Text($"{amount:N2} TL");
                        table.Cell().PaddingTop(2).AlignRight().Text(status);
                    }
                }
            });
        });
    }

    private static void ComposeFooter(IContainer container, Policy policy, string verificationCode)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(0.5f).LineColor(Line);

            column.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text($"Poliçe No: {policy.PolicyNumber} · Doğrulama Kodu: {verificationCode}").FontSize(7).FontColor(Muted);

                row.ConstantItem(70).AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(7).FontColor(Muted));
                    text.Span("Sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });

            column.Item().PaddingTop(2).AlignCenter()
                .Text("Bu belge eğitim amaçlı bir staj projesinde üretilmiştir; gerçek bir sigorta sözleşmesi değildir.")
                .FontSize(7).Italic().FontColor(Muted);
        });
    }

    private static IEnumerable<(string Title, string Body)> Conditions(Policy policy, decimal deductibleFactor)
    {
        yield return ("1. Teminatın Kapsamı",
            "Bu poliçe, üzerinde bilgileri yazılı aracı, teminat tablosunda gösterilen riskler için poliçe başlangıç ve bitiş tarihleri arasında güvence altına alır. " +
            "Araç hasarlarında ödeme, hasar tarihindeki piyasa rayiç değeri ve Türkiye Sigorta Birliği kasko değer listesi esas alınarak belirlenir.");

        yield return ("2. Beyan Yükümlülüğü",
            "Teklif sırasında verilen araç, kullanım şekli ve hasar geçmişi bilgileri prim hesabının dayanağıdır. " +
            "Bu bilgilerin eksik veya yanlış olduğunun anlaşılması halinde tazminattan indirim yapılabilir veya poliçe iptal edilebilir.");

        yield return ("3. Muafiyet",
            deductibleFactor < 1m
                ? $"Bu poliçede her hasarda, hasar tutarının %{(deductibleFactor <= 0.80m ? 5 : 2)}'i oranında muafiyet uygulanır ve bu tutar tazminattan düşülür. Muafiyet seçimi karşılığında primde indirim yapılmıştır."
                : "Bu poliçede hasar tazminatlarından muafiyet kesintisi yapılmaz. Hasarın teminat kapsamındaki tutarı eksper raporuna göre ödenir.");

        yield return ("4. Prim Ödemesi",
            "Sigortacının sorumluluğu primin ödenmesiyle başlar. Poliçede gösterilen toplam prime %5 gider vergisi dahildir. " +
            "Ödemesi tamamlanmamış poliçeler için teminat başlamaz.");

        yield return ("5. Cayma Hakkı",
            "Sigorta ettiren, poliçenin düzenlendiği tarihten itibaren 14 gün içinde gerekçe göstermeden cayma hakkını kullanabilir. " +
            "Bu durumda ödenen prim, teminattan yararlanılan gün sayısı düşülerek iade edilir.");

        yield return ("6. İptal ve Prim İadesi",
            "Poliçe süresi içinde müşteri portalından iptal talebi oluşturulabilir. Talep onaylandığında prim, kullanılmayan gün sayısı oranında hesaplanarak iade edilir " +
            "(iade = prim × kalan gün / toplam gün).");

        yield return ("7. Hasar İhbarı ve İstenen Belgeler",
            "Hasar, öğrenildiği andan itibaren en geç 5 iş günü içinde bildirilmelidir. Başvuruda kaza tespit tutanağı veya ifade tutanağı, sürücü belgesi ve ruhsat fotokopisi, " +
            "hasarlı araç fotoğrafları ve tazminatın ödeneceği IBAN bilgisi istenir. Çalınma hasarlarında emniyet makamından alınan tutanak da gereklidir.");

        yield return ("8. Poliçenin Yenilenmesi",
            $"Poliçe {policy.EndDate.ToLocalTime():dd.MM.yyyy} tarihinde kendiliğinden sona erer. Bitiş tarihinden 60 gün önce müşteri portalından yenileme teklifi alınabilir; " +
            "yenileme yapılmazsa teminat bitiş tarihinde sona erer.");

        yield return ("9. Kişisel Verilerin Korunması",
            "Sigortalıya ait kişisel veriler yalnızca teklif, poliçe ve hasar işlemlerinin yürütülmesi amacıyla işlenir. Bu belgede kimlik ve iletişim bilgileri gizlilik için maskelenmiştir.");
    }

    private static string PackageTitle(Quote quote)
    {
        return string.IsNullOrWhiteSpace(quote.PackageName)
            ? "KASKO SİGORTA POLİÇESİ"
            : $"KASKO SİGORTA POLİÇESİ · {quote.PackageName.ToUpper(new System.Globalization.CultureInfo("tr-TR"))}";
    }

    private static string LimitText(QuoteCoverage coverage)
    {
        if (!string.IsNullOrWhiteSpace(coverage.OptionName))
        {
            return coverage.OptionName;
        }

        return coverage.Limit.HasValue
            ? $"{coverage.Limit.Value:N0} TL"
            : "Rayiç değer";
    }

    private static string BuildVerificationCode(Policy policy)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{policy.Id}|{policy.PolicyNumber}"));
        var code = Convert.ToHexString(hash)[..12];
        return $"{code[..4]}-{code[4..8]}-{code[8..]}";
    }

    private static byte[]? DecodeImage(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl) ||
            !(dataUrl.StartsWith("data:image/png") || dataUrl.StartsWith("data:image/jpeg") || dataUrl.StartsWith("data:image/jpg")))
        {
            return null;
        }

        var comma = dataUrl.IndexOf(',');

        try
        {
            return comma < 0 ? null : Convert.FromBase64String(dataUrl[(comma + 1)..]);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string MaskIdentity(string value)
    {
        return value.Length == 11 ? $"{value[..3]}*****{value[8..]}" : "*****";
    }

    private static string MaskPhone(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length >= 4 ? $"*******{digits[^4..]}" : "—";
    }

    private static string MaskEmail(string value)
    {
        var at = value.IndexOf('@');

        if (at <= 0)
        {
            return "—";
        }

        var name = value[..at];
        var domain = value[(at + 1)..];

        return $"{name[..Math.Min(2, name.Length)]}*****@{domain[..Math.Min(2, domain.Length)]}*****";
    }

    private static string VehicleTypeText(VehicleType value) => value switch
    {
        VehicleType.Sedan => "Sedan",
        VehicleType.Hatchback => "Hatchback",
        VehicleType.SUV => "SUV",
        VehicleType.Pickup => "Pickup",
        VehicleType.Coupe => "Coupe",
        VehicleType.Convertible => "Cabrio",
        VehicleType.Van => "Van",
        _ => "—"
    };

    private static string FuelText(FuelType value) => value switch
    {
        FuelType.Gasoline => "Benzin",
        FuelType.Diesel => "Dizel",
        FuelType.Hybrid => "Hibrit",
        FuelType.Electric => "Elektrik",
        _ => "—"
    };

    private static string TransmissionText(TransmissionType value) => value switch
    {
        TransmissionType.Manual => "Manuel",
        TransmissionType.Automatic => "Otomatik",
        _ => "—"
    };

    private static string PaymentStatusText(PaymentStatus value) => value switch
    {
        PaymentStatus.Successful => "Ödendi",
        PaymentStatus.Failed => "Başarısız",
        PaymentStatus.Refunded => "İade edildi",
        PaymentStatus.Cancelled => "İptal",
        _ => "Bekliyor"
    };
}
