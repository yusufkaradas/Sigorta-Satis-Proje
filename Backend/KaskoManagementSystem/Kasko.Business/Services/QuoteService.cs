using Kasko.Business.DTOs.Quote;
using Kasko.Business.Exceptions;
using Kasko.Business.Pricing;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Kasko.Business.Services
{
    public class QuoteService : IQuoteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPricingService _pricingService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public QuoteService(
            IUnitOfWork unitOfWork,
            IPricingService pricingService,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _pricingService = pricingService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<QuoteListDto>> GetAllAsync()
        {
            var quotes =
                await _unitOfWork.Quotes.GetAllAsync();

            if (_httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true)
            {
                var userIdValue =
                    _httpContextAccessor.HttpContext.User
                        .FindFirst(ClaimTypes.NameIdentifier)?
                        .Value;

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    return Enumerable.Empty<QuoteListDto>();
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null)
                {
                    return Enumerable.Empty<QuoteListDto>();
                }

                quotes = quotes
                    .Where(x =>
                        x.CustomerId ==
                        currentUser.CustomerId.Value)
                    .ToList();
            }

            var customers =
                await _unitOfWork.Customers.GetAllAsync();

            var vehicles =
                await _unitOfWork.Vehicles.GetAllAsync();

            var customerMap =
                customers.ToDictionary(
                    x => x.Id,
                    x => $"{x.FirstName} {x.LastName}");

            var vehicleMap =
                vehicles.ToDictionary(
                    x => x.Id,
                    x => new
                    {
                        Description = $"{x.Brand} {x.Model}",
                        PlateNumber = x.PlateNumber
                    });

            return quotes
                .Where(x => !x.IsDeleted)
                .Select(x =>
                {
                    customerMap.TryGetValue(
                        x.CustomerId,
                        out var customerName);

                    vehicleMap.TryGetValue(
                        x.VehicleId,
                        out var vehicleInfo);

                    return new QuoteListDto
                    {
                        Id = x.Id,

                        QuoteNumber = x.QuoteNumber,

                        CustomerId = x.CustomerId,

                        CustomerName =
                            customerName ?? "—",

                        VehicleId = x.VehicleId,

                        VehicleDescription =
                            vehicleInfo?.Description ?? "—",

                        PlateNumber =
                            vehicleInfo?.PlateNumber ?? "—",

                        PremiumAmount = x.PremiumAmount,

                        Status = x.Status,

                        ValidUntil = x.ValidUntil,

                        PackageId = x.PackageId,

                        PackageName = x.PackageName,

                        ReviewReason = x.ReviewReason,

                        CreatedDate = x.CreatedDate
                    };
                })
                .ToList();
        }

        public async Task<QuoteDto?> GetByIdAsync(Guid id)
        {
            var quote = await _unitOfWork.Quotes
                .GetByIdIncludingDetailsAsync(id);

            if (quote == null)
            {
                return null;
            }
            if (_httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true)
            {
                var userIdValue =
                    _httpContextAccessor.HttpContext.User
                        .FindFirst(ClaimTypes.NameIdentifier)?
                        .Value;

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    throw new NotFoundException(
                        "Teklif bulunamadı.");
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null ||
                    quote.CustomerId != currentUser.CustomerId.Value)
                {
                    throw new NotFoundException(
                        "Teklif bulunamadı.");
                }
            }
            if (_httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true)
            {
                var userIdValue =
                    _httpContextAccessor.HttpContext.User
                        .FindFirst(ClaimTypes.NameIdentifier)?
                        .Value;

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    return null;
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null ||
                    quote.CustomerId != currentUser.CustomerId.Value)
                {
                    return null;
                }
            }
            return new QuoteDto
            {
                Id = quote.Id,

                CustomerId = quote.CustomerId,

                CustomerName =
        $"{quote.Customer.FirstName} {quote.Customer.LastName}",

                CustomerEmail =
        quote.Customer.Email,

                CustomerPhone =
        quote.Customer.PhoneNumber,

                VehicleId = quote.VehicleId,

                VehicleDescription =
        $"{quote.Vehicle.Brand} {quote.Vehicle.Model}",

                PlateNumber =
        quote.Vehicle.PlateNumber,

                Brand =
        quote.Vehicle.Brand,

                Model =
        quote.Vehicle.Model,

                ModelYear =
        quote.Vehicle.ModelYear,

                MarketValue =
        quote.Vehicle.MarketValue,

                QuoteNumber =
        quote.QuoteNumber,

                PremiumAmount =
        quote.PremiumAmount,

                Status =
        quote.Status,

                PackageId =
        quote.PackageId,

                PackageName =
        quote.PackageName,

                ReviewReason =
        quote.ReviewReason,

                ValidUntil =
        quote.ValidUntil,

                CreatedDate =
        quote.CreatedDate,

                IsActive =
        !quote.IsDeleted,

                Coverages =
        quote.QuoteCoverages
            .Where(x => !x.IsDeleted)
            .Select(x => new Kasko.Business.DTOs.Quote.QuoteCoverageDto
            {
                CoverageId =
                    x.CoverageId,

                CoverageName =
                    x.Coverage.Name,

                CalculatedPrice =
                    x.CalculatedPrice,

                Limit =
                    x.Limit,

                OptionName =
                    x.OptionName
            })
            .ToList(),

                PricingSnapshot =
        quote.PricingSnapshot == null
            ? null
            : new QuotePricingSnapshotDto
            {
                MarketValue =
                    quote.PricingSnapshot.MarketValue,

                BaseRate =
                    quote.PricingSnapshot.BaseRate,

                AgeFactor =
                    quote.PricingSnapshot.AgeFactor,

                UsageFactor =
                    quote.PricingSnapshot.UsageFactor,

                DriverFactor =
                    quote.PricingSnapshot.DriverFactor,

                ClaimsFactor =
                    quote.PricingSnapshot.ClaimsFactor,

                RegionFactor =
                    quote.PricingSnapshot.RegionFactor,

                PackageFactor =
                    quote.PricingSnapshot.PackageFactor,

                DeductibleFactor =
                    quote.PricingSnapshot.DeductibleFactor,

                CoveragePremium =
                    quote.PricingSnapshot.CoveragePremium,

                Discount =
                    quote.PricingSnapshot.Discount,

                FinalPremium =
                    quote.PricingSnapshot.FinalPremium
            }
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

            if (_httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true)
            {
                var userIdValue =
                    _httpContextAccessor.HttpContext.User
                        .FindFirst(ClaimTypes.NameIdentifier)?
                        .Value;

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    throw new NotFoundException(
                        "Müşteri bulunamadı.");
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null ||
                    dto.CustomerId != currentUser.CustomerId.Value)
                {
                    throw new NotFoundException(
                        "Müşteri bulunamadı.");
                }
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

            await EnsureNoActivePolicyAsync(vehicle.Id);

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

                    CoverageIds = await ResolveCoverageIdsAsync(dto.PackageId, dto.CoverageIds),

                    CoverageOptionIds = dto.CoverageOptionIds,

                    EffectiveDate = dto.EffectiveDate
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

            if (_httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true)
            {
                var userIdValue =
                    _httpContextAccessor.HttpContext.User
                        .FindFirst(ClaimTypes.NameIdentifier)?
                        .Value;

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    throw new NotFoundException(
                        "Müşteri bulunamadı.");
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null ||
                    dto.CustomerId != currentUser.CustomerId.Value)
                {
                    throw new NotFoundException(
                        "Müşteri bulunamadı.");
                }
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

            await EnsureNoActivePolicyAsync(vehicle.Id);

            await CancelOpenQuotesAsync(vehicle.Id);

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

         CoverageIds = await ResolveCoverageIdsAsync(
             dto.PackageId,
             dto.CoverageIds),

         CoverageOptionIds =
             dto.CoverageOptionIds,

         EffectiveDate =
             dto.EffectiveDate
     };

            var pricing =
                await _pricingService.CalculateAsync(
                    pricingRequest);

            decimal premiumAmount =
                pricing.TotalPremium;

            var reviewReasons =
                await EvaluateAutoApprovalAsync(
                    premiumAmount,
                    vehicle.MarketValue,
                    previousPolicy?.ClaimsCount ?? dto.ClaimsCount,
                    DateTime.UtcNow.Year - vehicle.ModelYear,
                    dto.Usage);

            var vehicleCategory =
                await ResolveVehicleCategoryAsync(vehicle);

            if (vehicleCategory != null &&
                !string.Equals(vehicleCategory, "Otomobil", StringComparison.OrdinalIgnoreCase))
            {
                reviewReasons.Add($"{vehicleCategory} sınıfı araç (ticari / ağır vasıta ayrıca değerlendirilir)");
            }

            var quote = new Quote
            {
                Id = Guid.NewGuid(),

                CustomerId = dto.CustomerId,
                VehicleId = dto.VehicleId,

                QuoteNumber = quoteNumber,

                PremiumAmount = premiumAmount,

                Status = reviewReasons.Count == 0
                    ? QuoteStatus.Offered
                    : QuoteStatus.Draft,

                ReviewReason = reviewReasons.Count == 0
                    ? null
                    : string.Join(" · ", reviewReasons),

                ValidUntil = dto.ValidUntil,

                PackageId = dto.PackageId,

                PackageName = dto.PackageId.HasValue
                    ? (await _unitOfWork.InsurancePackages.GetByIdAsync(dto.PackageId.Value))?.Name
                    : null,
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
                    CoverageOptionId = coverage.CoverageOptionId,
                    OptionName = coverage.OptionName,
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
                PackageId = quote.PackageId,
                PackageName = quote.PackageName,
                ReviewReason = quote.ReviewReason,
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
            if (_httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true)
            {
                var userIdValue =
                    _httpContextAccessor.HttpContext.User
                        .FindFirst(ClaimTypes.NameIdentifier)?
                        .Value;

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    throw new NotFoundException(
                        "Teklif bulunamadı.");
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null ||
                    quote.CustomerId != currentUser.CustomerId.Value)
                {
                    throw new NotFoundException(
                        "Teklif bulunamadı.");
                }
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
            if (_httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true)
            {
                var userIdValue =
                    _httpContextAccessor.HttpContext.User
                        .FindFirst(ClaimTypes.NameIdentifier)?
                        .Value;

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    throw new NotFoundException(
                        "Teklif bulunamadı.");
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null ||
                    quote.CustomerId != currentUser.CustomerId.Value)
                {
                    throw new NotFoundException(
                        "Teklif bulunamadı.");
                }
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
            if (_httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true)
            {
                var userIdValue =
                    _httpContextAccessor.HttpContext.User
                        .FindFirst(ClaimTypes.NameIdentifier)?
                        .Value;

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    throw new NotFoundException(
                        "Teklif bulunamadı.");
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null ||
                    quote.CustomerId != currentUser.CustomerId.Value)
                {
                    throw new NotFoundException(
                        "Teklif bulunamadı.");
                }
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
        private async Task<string?> ResolveVehicleCategoryAsync(Vehicle vehicle)
        {
            if (string.IsNullOrWhiteSpace(vehicle.BrandCode) || string.IsNullOrWhiteSpace(vehicle.TypeCode))
            {
                return null;
            }

            var catalog = _unitOfWork.VehicleValueCatalogs;

            if (catalog == null)
            {
                return null;
            }

            var entry = await catalog.GetActiveByKeyAsync(vehicle.BrandCode, vehicle.TypeCode, vehicle.ModelYear);

            return string.IsNullOrWhiteSpace(entry?.VehicleCategory) ? null : entry.VehicleCategory;
        }

        private async Task<List<string>> EvaluateAutoApprovalAsync(
            decimal premium,
            decimal marketValue,
            int claimsCount,
            int vehicleAge,
            string usage)
        {
            var reasons = new List<string>();

            var maxPremium = await GetRuleOrDefaultAsync("AUTO_APPROVE_MAX_PREMIUM", 150000m);
            var maxMarketValue = await GetRuleOrDefaultAsync("AUTO_APPROVE_MAX_MARKET_VALUE", 5000000m);
            var maxClaims = await GetRuleOrDefaultAsync("AUTO_APPROVE_MAX_CLAIMS", 2m);
            var maxVehicleAge = await GetRuleOrDefaultAsync("AUTO_APPROVE_MAX_VEHICLE_AGE", 12m);

            if (premium > maxPremium)
            {
                reasons.Add($"Prim {maxPremium:N0} ₺ sınırını aşıyor");
            }

            if (marketValue > maxMarketValue)
            {
                reasons.Add($"Araç değeri {maxMarketValue:N0} ₺ sınırını aşıyor");
            }

            if (claimsCount > maxClaims)
            {
                reasons.Add($"Hasar sayısı {maxClaims:N0} üzerinde");
            }

            if (vehicleAge > maxVehicleAge)
            {
                reasons.Add($"Araç yaşı {maxVehicleAge:N0} yıldan büyük");
            }

            if (!string.Equals(usage?.Trim(), "PRIVATE", StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add("Ticari kullanım");
            }

            return reasons;
        }

        private async Task<decimal> GetRuleOrDefaultAsync(string code, decimal defaultValue)
        {
            if (_unitOfWork.PricingRules == null)
            {
                return defaultValue;
            }

            var rule = await _unitOfWork.PricingRules.GetApplicableRuleAsync(code, DateTime.UtcNow);

            return rule?.Value ?? defaultValue;
        }

        private async Task EnsureNoActivePolicyAsync(Guid vehicleId)
        {
            var renewalWindowStart =
                DateTime.UtcNow.AddDays(60);

            var activePolicy =
                (await _unitOfWork.Policies
                    .FindAsync(x =>
                        x.VehicleId == vehicleId &&
                        !x.IsDeleted &&
                        x.Status == PolicyStatus.Active))
                    .OrderByDescending(x => x.EndDate)
                    .FirstOrDefault();

            if (activePolicy != null &&
                activePolicy.EndDate > renewalWindowStart)
            {
                throw new BadRequestException(
                    $"Bu aracın {activePolicy.EndDate:dd.MM.yyyy} tarihine kadar geçerli aktif poliçesi var. Yeni teklif, poliçe bitimine 60 gün kala yenileme olarak alınabilir.");
            }
        }

        private async Task CancelOpenQuotesAsync(Guid vehicleId)
        {
            var openQuotes =
                (await _unitOfWork.Quotes
                    .FindAsync(x =>
                        x.VehicleId == vehicleId &&
                        !x.IsDeleted &&
                        (x.Status == QuoteStatus.Draft ||
                         x.Status == QuoteStatus.Offered)))
                    .ToList();

            foreach (var openQuote in openQuotes)
            {
                openQuote.Status = QuoteStatus.Cancelled;
                openQuote.UpdatedDate = DateTime.UtcNow;

                await _unitOfWork.Quotes.UpdateAsync(openQuote);
            }
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