using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.DataAccess.Repositories.Abstract;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Kasko.Business.Services.Concrete;

public class PolicyPdfService : IPolicyPdfService
{
    private readonly IUnitOfWork _unitOfWork;

    public PolicyPdfService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        if (policy.Status != Kasko.Entities.Enums.PolicyStatus.Active)
        {
            throw new BadRequestException(
                "Yalnızca aktif poliçeler için PDF oluşturulabilir.");
        }

        var quote =
            await _unitOfWork.Quotes
                .GetByIdIncludingDetailsAsync(policy.QuoteId);

        if (quote == null || quote.IsDeleted)
        {
            throw new NotFoundException("Teklif bulunamadı.");
        }

        QuestPDF.Settings.License =
            LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);

                page.Header()
                    .Column(column =>
                    {
                        column.Item()
                            .Text("KASKO SİGORTA")
                            .FontSize(22)
                            .Bold();

                        column.Item()
                            .Text("Poliçe Belgesi")
                            .FontSize(14);
                    });

                page.Content()
                    .PaddingVertical(20)
                    .Column(column =>
                    {
                        column.Spacing(8);

                        column.Item().Text(
                            $"Poliçe No: {policy.PolicyNumber}");

                        column.Item().Text(
                            $"Müşteri: {quote.Customer.FirstName} {quote.Customer.LastName}");

                        column.Item().Text(
                            $"Araç: {quote.Vehicle.Brand} {quote.Vehicle.Model}");

                        column.Item().Text(
                            $"Plaka: {quote.Vehicle.PlateNumber}");

                        column.Item().Text(
                            $"Başlangıç: {policy.StartDate:dd.MM.yyyy}");

                        column.Item().Text(
                            $"Bitiş: {policy.EndDate:dd.MM.yyyy}");

                        column.Item().Text(
                            $"Toplam Prim: {policy.PremiumAmount:N2} ₺");

                        column.Item()
                            .PaddingTop(15)
                            .Text("Teminatlar")
                            .Bold();

                        foreach (var coverage in quote.QuoteCoverages)
                        {
                            column.Item().Text(
                                $"{coverage.Coverage.Name} - {coverage.CalculatedPrice:N2} ₺");
                        }

                        column.Item()
                            .PaddingTop(20)
                            .Text(
                                "Bu belge demo amaçlı oluşturulmuştur.");
                    });

                page.Footer()
                    .AlignCenter()
                    .Text("Kasko Satış Yönetim Sistemi");
            });
        }).GeneratePdf();
    }
}