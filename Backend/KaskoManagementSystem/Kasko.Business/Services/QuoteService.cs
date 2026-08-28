using Kasko.Business.DTOs.Quote;
using Kasko.Business.Exceptions;
using Kasko.Business.Pricing;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;

namespace Kasko.Business.Services
{
    public class QuoteService : IQuoteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPricingService _pricingService;

        public QuoteService(
            IUnitOfWork unitOfWork,
            IPricingService pricingService)
        {
            _unitOfWork = unitOfWork;
            _pricingService = pricingService;
        }

        public async Task<IEnumerable<QuoteListDto>> GetAllAsync()
        {
            var quotes = await _unitOfWork.Quotes.GetAllAsync();

            return quotes
                .Where(x => !x.IsDeleted)
                .Select(x => new QuoteListDto
                {
                    Id = x.Id,
                    QuoteNumber = x.QuoteNumber,
                    CustomerId = x.CustomerId,
                    VehicleId = x.VehicleId,
                    PremiumAmount = x.PremiumAmount,
                    Status = x.Status,
                    ValidUntil = x.ValidUntil,
                    CreatedDate = x.CreatedDate
                });
        }

        public async Task<QuoteDto?> GetByIdAsync(Guid id)
        {
            var quote = await _unitOfWork.Quotes
                .GetByIdIncludingDetailsAsync(id);

            if (quote == null)
            {
                return null;
            }

            return new QuoteDto
            {
                Id = quote.Id,
                CustomerId = quote.CustomerId,
                VehicleId = quote.VehicleId,
                QuoteNumber = quote.QuoteNumber,
                PremiumAmount = quote.PremiumAmount,
                Status = quote.Status,
                ValidUntil = quote.ValidUntil,
                CreatedDate = quote.CreatedDate
            };
        }

        public async Task<QuoteDto> CreateAsync(CreateQuoteDto dto)
        {
            
            var customer = await _unitOfWork.Customers
                .GetByIdAsync(dto.CustomerId);

            if (customer == null)
            {
                throw new NotFoundException(
                    "Müşteri bulunamadı.");
            }

            
            var vehicle = await _unitOfWork.Vehicles
                .GetByIdAsync(dto.VehicleId);

            if (vehicle == null)
            {
                throw new NotFoundException(
                    "Araç bulunamadı.");
            }

            
            if (vehicle.IsDeleted)
            {
                throw new BadRequestException(
                    "Silinmiş bir araç için teklif oluşturulamaz.");
            }

            if (!vehicle.IsActive)
            {
                throw new BadRequestException(
                    "Pasif bir araç için teklif oluşturulamaz.");
            }

            
            if (vehicle.CustomerId != dto.CustomerId)
            {
                throw new BadRequestException(
                    "Seçilen araç bu müşteriye ait değildir.");
            }

            
            var quoteNumber =
                $"KLF-{DateTime.UtcNow.Year}-{Guid.NewGuid():N}"
                .Substring(0, 21)
                .ToUpper();


            var pricing =
      await _pricingService.CalculateAsync(
          vehicle.MarketValue,
          vehicle.ModelYear,
          dto.CoverageIds);

            decimal premiumAmount =
                pricing.TotalPremium;

            var quote = new Quote
            {
                Id = Guid.NewGuid(),

                CustomerId = dto.CustomerId,
                VehicleId = dto.VehicleId,

                QuoteNumber = quoteNumber,

                PremiumAmount = premiumAmount,

                Status = QuoteStatus.Draft,

                ValidUntil = dto.ValidUntil,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };

            foreach (var coverage in pricing.Coverages)
            {
                var quoteCoverage = new QuoteCoverage
                {
                    Id = Guid.NewGuid(),
                    QuoteId = quote.Id,
                    CoverageId = coverage.CoverageId,
                    CalculatedPrice = coverage.CalculatedPrice,
                    Limit = coverage.Limit,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow
                };

                await _unitOfWork
                    .QuoteCoverages
                    .AddAsync(quoteCoverage);
            }

            await _unitOfWork.Quotes.AddAsync(quote);

            await _unitOfWork.SaveChangesAsync();


            return new QuoteDto
            {
                Id = quote.Id,
                CustomerId = quote.CustomerId,
                VehicleId = quote.VehicleId,
                QuoteNumber = quote.QuoteNumber,
                PremiumAmount = quote.PremiumAmount,
                Status = quote.Status,
                ValidUntil = quote.ValidUntil,
                CreatedDate = quote.CreatedDate
            };
        }

        public async Task UpdateAsync(
            Guid id,
            UpdateQuoteDto dto)
        {
            var quote = await _unitOfWork.Quotes
                .GetByIdAsync(id);

            if (quote == null || quote.IsDeleted)
            {
                throw new NotFoundException(
                    "Teklif bulunamadı.");
            }

            if (quote.Status == QuoteStatus.Accepted)
            {
                throw new BadRequestException(
                    "Kabul edilmiş teklif güncellenemez.");
            }

            if (quote.Status == QuoteStatus.Cancelled)
            {
                throw new BadRequestException(
                    "İptal edilmiş teklif güncellenemez.");
            }

            quote.ValidUntil = dto.ValidUntil;
            quote.UpdatedDate = DateTime.UtcNow;

            await _unitOfWork.Quotes.UpdateAsync(quote);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var quote = await _unitOfWork.Quotes
                .GetByIdAsync(id);

            if (quote == null || quote.IsDeleted)
            {
                throw new NotFoundException(
                    "Teklif bulunamadı.");
            }

            quote.IsDeleted = true;
            quote.DeletedDate = DateTime.UtcNow;

            await _unitOfWork.Quotes.UpdateAsync(quote);

            await _unitOfWork.SaveChangesAsync();
        }

            public async Task ChangeStatusAsync(
    Guid id,
    QuoteStatus newStatus)
        {
            var quote = await _unitOfWork.Quotes.GetByIdAsync(id);

            if (quote == null || quote.IsDeleted)
            {
                throw new NotFoundException(
                    "Teklif bulunamadı.");
            }

            if (quote.Status == newStatus)
            {
                throw new BadRequestException(
                    "Teklif zaten bu durumdadır.");
            }

            
            if (quote.Status is QuoteStatus.Accepted
                or QuoteStatus.Rejected
                or QuoteStatus.Expired
                or QuoteStatus.Cancelled)
            {
                throw new BadRequestException(
                    "Bu durumdaki teklifin durumu değiştirilemez.");
            }

            
            if (quote.ValidUntil < DateTime.UtcNow
                && newStatus != QuoteStatus.Expired)
            {
                quote.Status = QuoteStatus.Expired;
                quote.UpdatedDate = DateTime.UtcNow;

                await _unitOfWork.Quotes.UpdateAsync(quote);
                await _unitOfWork.SaveChangesAsync();

                throw new BadRequestException(
                    "Teklifin geçerlilik süresi dolmuştur.");
            }

            var isValidTransition =
                quote.Status switch
                {
                    QuoteStatus.Draft =>
                        newStatus == QuoteStatus.Offered,

                    QuoteStatus.Offered =>
                        newStatus == QuoteStatus.Accepted ||
                        newStatus == QuoteStatus.Rejected ||
                        newStatus == QuoteStatus.Cancelled ||
                        newStatus == QuoteStatus.Expired,

                    _ => false
                };

            if (!isValidTransition)
            {
                throw new BadRequestException(
                    $"'{quote.Status}' durumundan '{newStatus}' durumuna geçiş yapılamaz.");
            }

            if (newStatus == QuoteStatus.Expired
                && quote.ValidUntil >= DateTime.UtcNow)
            {
                throw new BadRequestException(
                    "Geçerlilik süresi dolmamış bir teklif Expired yapılamaz.");
            }

            quote.Status = newStatus;
            quote.UpdatedDate = DateTime.UtcNow;

            await _unitOfWork.Quotes.UpdateAsync(quote);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
