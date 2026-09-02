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
        public async Task<PricingCalculation> CalculateAsync(
    CreateQuoteDto dto)
        {
            if (dto.ValidUntil <= DateTime.UtcNow)
            {
                throw new BadRequestException(
                    "Teklif geçerlilik tarihi gelecekte olmalıdır.");
            }
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
                    "Silinmiş bir araç için fiyat hesaplanamaz.");
            }

            if (!vehicle.IsActive)
            {
                throw new BadRequestException(
                    "Pasif bir araç için fiyat hesaplanamaz.");
            }

            if (vehicle.CustomerId != dto.CustomerId)
            {
                throw new BadRequestException(
                    "Seçilen araç bu müşteriye ait değildir.");
            }

            PreviousPolicy? previousPolicy = null;

            if (dto.PreviousPolicyId.HasValue)
            {
                previousPolicy =
                    await _unitOfWork
                        .PreviousPolicies
                        .GetByIdAsync(dto.PreviousPolicyId.Value);

                if (previousPolicy == null ||
                    previousPolicy.IsDeleted)
                {
                    throw new NotFoundException(
                        "Önceki poliçe bulunamadı.");
                }

                if (previousPolicy.CustomerId != dto.CustomerId)
                {
                    throw new BadRequestException(
                        "Önceki poliçe bu müşteriye ait değil.");
                }
            }

            var driverAge =
                CalculateDriverAge(
                    customer.DateOfBirth);

            var region =
                ResolveRegion(
                    customer.City);

            var pricingRequest =
                new PricingRequest
                {
                    MarketValue =
                        vehicle.MarketValue,

                    ModelYear =
                        vehicle.ModelYear,

                    DriverAge =
                        driverAge,

                    Usage =
                        dto.Usage,

                    ClaimsCount =
                        previousPolicy?.ClaimsCount
                        ?? dto.ClaimsCount,

                    Region =
                        region,

                    PackageId =
                        dto.PackageId,

                    Deductible =
                        dto.Deductible,

                    CoverageIds = await ResolveCoverageIdsAsync(dto.PackageId, dto.CoverageIds)
                };

            return await _pricingService.CalculateAsync(
                pricingRequest);
        }

        public async Task<QuoteDto> CreateAsync(CreateQuoteDto dto)
        {
            if (dto.ValidUntil <= DateTime.UtcNow)
            {
                throw new BadRequestException(
                    "Teklif geçerlilik tarihi gelecekte olmalıdır.");
            }
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

            var driverAge = CalculateDriverAge(customer.DateOfBirth);
            var region = ResolveRegion(customer.City);
            PreviousPolicy? previousPolicy = null;

            if (dto.PreviousPolicyId.HasValue)
            {
                previousPolicy =
                    await _unitOfWork
                        .PreviousPolicies
                        .GetByIdAsync(dto.PreviousPolicyId.Value);

                if (previousPolicy == null ||
                    previousPolicy.IsDeleted)
                {
                    throw new NotFoundException(
                        "Önceki poliçe bulunamadı.");
                }

                if (previousPolicy.CustomerId != dto.CustomerId)
                {
                    throw new BadRequestException(
                        "Önceki poliçe bu müşteriye ait değil.");
                }
            }
            var pricingRequest = new PricingRequest
            {
                MarketValue = vehicle.MarketValue,
                ModelYear = vehicle.ModelYear,
                DriverAge = driverAge,
                Usage = dto.Usage,
                ClaimsCount = previousPolicy?.ClaimsCount ?? dto.ClaimsCount,
                Region = region,
                PackageId = dto.PackageId,
                Deductible = dto.Deductible,
                CoverageIds = await ResolveCoverageIdsAsync(dto.PackageId, dto.CoverageIds)
            };

            var pricing =
                await _pricingService.CalculateAsync(
                    pricingRequest);

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

            var pricingSnapshot = new QuotePricingSnapshot
            {
                Id = Guid.NewGuid(),

                QuoteId = quote.Id,

                MarketValue = pricing.MarketValue,

                BaseRate = pricing.BaseRate,

                AgeFactor = pricing.AgeFactor,

                UsageFactor = pricing.UsageFactor,

                DriverFactor = pricing.DriverFactor,

                ClaimsFactor = pricing.ClaimsFactor,

                RegionFactor = pricing.RegionFactor,

                PackageFactor = pricing.PackageFactor,

                DeductibleFactor = pricing.DeductibleFactor,

                CoveragePremium = pricing.CoveragePremium,

                Discount = pricing.Discount,

                FinalPremium = pricing.TotalPremium,

                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork
                .QuotePricingSnapshots
                .AddAsync(pricingSnapshot);

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

        private static int CalculateDriverAge(DateTime dateOfBirth)
        {
            var today = DateTime.UtcNow.Date;

            var age = today.Year - dateOfBirth.Year;

            if (dateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }

            return age < 0 ? 0 : age;
        }
        private async Task<IReadOnlyCollection<Guid>> ResolveCoverageIdsAsync(
    Guid? packageId,
    IReadOnlyCollection<Guid> coverageIds)
        {
            var requestedCoverageIds =
                coverageIds
                    .Distinct()
                    .ToHashSet();

            if (!packageId.HasValue)
            {
                return requestedCoverageIds.ToArray();
            }

            var packageCoverages =
                await _unitOfWork
                    .PackageCoverages
                    .FindAsync(x =>
                        x.InsurancePackageId == packageId.Value);

            var allowedCoverageIds =
                packageCoverages
                    .Select(x => x.CoverageId)
                    .ToHashSet();

            var invalidCoverageIds =
                requestedCoverageIds
                    .Where(x => !allowedCoverageIds.Contains(x))
                    .ToArray();

            if (invalidCoverageIds.Length > 0)
            {
                throw new BadRequestException(
                    "Seçilen teminatlardan biri veya daha fazlası seçilen sigorta paketine ait değil.");
            }

            foreach (var packageCoverage in packageCoverages)
            {
                if (packageCoverage.IsDefault)
                {
                    requestedCoverageIds.Add(
                        packageCoverage.CoverageId);
                }
            }

            return requestedCoverageIds.ToArray();
        }
        private string ResolveRegion(string? city)
        {
            return "NORMAL";
        }
    }
}