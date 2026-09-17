using Kasko.Business.DTOs.Policy;
using Kasko.Business.Exceptions;
using Kasko.Business.Services.Abstract;
using Kasko.DataAccess.Repositories;
using Kasko.DataAccess.Repositories.Abstract;

using Kasko.Business.DTOs.Quote;

using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;


namespace Kasko.Business.Services.Concrete
{
    public class PolicyService : IPolicyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPolicyRepository _policyRepository;
        private readonly IQuoteRepository _quoteRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IQuoteService _quoteService;

        public PolicyService(
            IUnitOfWork unitOfWork,
            IPolicyRepository policyRepository,
            IQuoteRepository quoteRepository,
            ICustomerRepository customerRepository,
            IVehicleRepository vehicleRepository, 
            IHttpContextAccessor httpContextAccessor,
            IQuoteService quoteService)
        {
            _unitOfWork = unitOfWork;
            _policyRepository = policyRepository;
            _quoteRepository = quoteRepository;
            _customerRepository = customerRepository;
            _vehicleRepository = vehicleRepository;
            _httpContextAccessor = httpContextAccessor;
            _quoteService = quoteService;
        }

        public async Task<PolicyDto> CreateAsync(PolicyCreateDto dto)
        {
            
            var customer = await _customerRepository.GetByIdAsync(dto.CustomerId);

            if (customer == null || customer.IsDeleted)
            {
                throw new NotFoundException("Müşteri bulunamadı.");
            }

            
            var vehicle = await _vehicleRepository.GetByIdAsync(dto.VehicleId);

            if (vehicle == null || vehicle.IsDeleted)
            {
                throw new NotFoundException("Araç bulunamadı.");
            }

            
            if (vehicle.CustomerId != dto.CustomerId)
            {
                throw new BadRequestException(
                    "Araç belirtilen müşteriye ait değildir.");
            }

            
            var quote = await _quoteRepository
                .GetByIdIncludingDetailsAsync(dto.QuoteId);

            if (quote == null || quote.IsDeleted)
            {
                throw new NotFoundException("Teklif bulunamadı.");
            }

            
            if (quote.CustomerId != dto.CustomerId)
            {
                throw new BadRequestException(
                    "Teklif belirtilen müşteriye ait değildir.");
            }

            
            if (quote.VehicleId != dto.VehicleId)
            {
                throw new BadRequestException(
                    "Teklif belirtilen araca ait değildir.");
            }

            
            if (quote.ValidUntil < DateTime.UtcNow)
            {
                throw new BadRequestException(
                    "Teklifin geçerlilik süresi dolmuştur.");
            }

            
            if (quote.Status != QuoteStatus.Accepted)
            {
                throw new BadRequestException(
                    "Sadece kabul edilmiş teklifler poliçeye dönüştürülebilir.");
            }

            
            if (dto.StartDate >= dto.EndDate)
            {
                throw new BadRequestException(
                    "Poliçe başlangıç tarihi bitiş tarihinden önce olmalıdır.");
            }

            
            var existingPolicies = await _policyRepository.FindAsync(
                x => x.QuoteId == dto.QuoteId &&
                     !x.IsDeleted);

            if (existingPolicies.Any())
            {
                throw new BadRequestException(
                    "Bu teklif için zaten bir poliçe oluşturulmuştur.");
            }

            var coveragePeriod = dto.EndDate - dto.StartDate;

            var currentPolicy = (await _policyRepository.FindAsync(
                    x => x.VehicleId == dto.VehicleId &&
                         !x.IsDeleted &&
                         x.Status == PolicyStatus.Active &&
                         x.EndDate > dto.StartDate))
                .OrderByDescending(x => x.EndDate)
                .FirstOrDefault();

            if (currentPolicy != null)
            {
                dto.StartDate = currentPolicy.EndDate;
                dto.EndDate = currentPolicy.EndDate + coveragePeriod;
            }

            
            string policyNumber;

            do
            {
                policyNumber =
                    $"POL-{DateTime.UtcNow.Year}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            }
            while (await _policyRepository
                .PolicyNumberExistsAsync(policyNumber));

            
            var policy = new Policy
            {
                Id = Guid.NewGuid(),

                CustomerId = dto.CustomerId,
                VehicleId = dto.VehicleId,
                QuoteId = dto.QuoteId,

                PolicyNumber = policyNumber,

                
                PremiumAmount = quote.PremiumAmount,

                StartDate = dto.StartDate,
                EndDate = dto.EndDate,


                Status = PolicyStatus.Draft,

                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.Policies.AddAsync(policy);
            await _unitOfWork.SaveChangesAsync();

            return new PolicyDto
            {
                Id = policy.Id,
                CustomerId = policy.CustomerId,
                VehicleId = policy.VehicleId,
                QuoteId = policy.QuoteId,
                PolicyNumber = policy.PolicyNumber,
                PremiumAmount = policy.PremiumAmount,
                StartDate = policy.StartDate,
                EndDate = policy.EndDate,
                CreatedDate = policy.CreatedDate,
                Status = policy.Status,
                IsActive = !policy.IsDeleted &&
           policy.Status == PolicyStatus.Active
            };
        }

        public async Task<PolicyDto?> GetByIdAsync(Guid id)
        {
            var policy =
                await _unitOfWork.Policies.GetByIdIncludingDetailsAsync(id);

            if (policy == null)
            {
                throw new NotFoundException("Poliçe bulunamadı.");
            }
            if (_httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true)
            {
                var userIdValue =
                    _httpContextAccessor.HttpContext.User
                        .FindFirst(ClaimTypes.NameIdentifier)?
                        .Value;

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    throw new NotFoundException("Poliçe bulunamadı.");
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null ||
                    policy.CustomerId != currentUser.CustomerId.Value)
                {
                    throw new NotFoundException("Poliçe bulunamadı.");
                }
            }
            return new PolicyDto
            {
                Id = policy.Id,
                CustomerId = policy.CustomerId,
                VehicleId = policy.VehicleId,
                QuoteId = policy.QuoteId,
                PolicyNumber = policy.PolicyNumber,
                PremiumAmount = policy.PremiumAmount,
                StartDate = policy.StartDate,
                EndDate = policy.EndDate,
                CreatedDate = policy.CreatedDate,
                Status = policy.Status,
                IsActive = !policy.IsDeleted &&
           policy.Status == PolicyStatus.Active,
           RowVersion = Convert.ToBase64String(policy.RowVersion)
            };
        }

        public async Task<IEnumerable<PolicyListDto>> GetAllAsync()
        {
            var policies =
                await _unitOfWork.Policies.GetAllAsync();

            if (_httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true)
            {
                var userIdValue =
                    _httpContextAccessor.HttpContext.User
                        .FindFirst(ClaimTypes.NameIdentifier)?
                        .Value;

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    return Enumerable.Empty<PolicyListDto>();
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null)
                {
                    return Enumerable.Empty<PolicyListDto>();
                }

                policies = policies
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
                        x.Brand,
                        x.Model
                    });

            return policies
                .Where(x => !x.IsDeleted)
                .Select(x =>
                {
                    customerMap.TryGetValue(
                        x.CustomerId,
                        out var customerName);

                    vehicleMap.TryGetValue(
                        x.VehicleId,
                        out var vehicle);

                    return new PolicyListDto
                    {
                        Id = x.Id,

                        CustomerId = x.CustomerId,

                        CustomerName =
                            customerName ?? "—",

                        VehicleId = x.VehicleId,

                        Brand =
                            vehicle?.Brand ?? "—",

                        Model =
                            vehicle?.Model ?? "—",

                        PolicyNumber =
                            x.PolicyNumber,

                        PremiumAmount =
                            x.PremiumAmount,

                        StartDate =
                            x.StartDate,

                        EndDate =
                            x.EndDate,

                        Status =
                            x.Status
                    };
                })
                .ToList();
        }
        public async Task<IEnumerable<PolicyListDto>> GetUpcomingRenewalsAsync(
    int daysAhead = 30)
        {
            if (daysAhead < 1)
            {
                throw new BadRequestException(
                    "Yenileme kontrol süresi en az 1 gün olmalıdır.");
            }

            var today = DateTime.UtcNow.Date;
            var limitDate = today.AddDays(daysAhead);

            var policies = await _policyRepository.FindAsync(
                x =>
                    !x.IsDeleted &&
                    x.Status == PolicyStatus.Active &&
                    x.EndDate.Date >= today &&
                    x.EndDate.Date <= limitDate);

            return policies
                .OrderBy(x => x.EndDate)
                .Select(x => new PolicyListDto
                {
                    Id = x.Id,
                    CustomerId = x.CustomerId,
                    VehicleId = x.VehicleId,
                    PolicyNumber = x.PolicyNumber,
                    PremiumAmount = x.PremiumAmount,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Status = x.Status
                })
                .ToList();
        }
        public async Task UpdateAsync(
            Guid id,
            PolicyUpdateDto dto)
        {
            var policy =
                await _unitOfWork.Policies.GetByIdAsync(id);

            if (policy == null || policy.IsDeleted)
            {
                throw new NotFoundException("Poliçe bulunamadı.");
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
                        "Poliçe bulunamadı.");
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null ||
                    policy.CustomerId != currentUser.CustomerId.Value)
                {
                    throw new NotFoundException(
                        "Poliçe bulunamadı.");
                }
            }
            if (dto.EndDate <= policy.StartDate)
            {
                throw new BadRequestException(
                    "Poliçe bitiş tarihi başlangıç tarihinden sonra olmalıdır.");
            }
            if (policy.Status is PolicyStatus.Expired
    or PolicyStatus.Cancelled)
            {
                throw new BadRequestException(
                    "Süresi dolmuş veya iptal edilmiş poliçe güncellenemez.");
            }

            byte[] rowVersion;

            try
            {
                rowVersion = Convert.FromBase64String(dto.RowVersion);
            }
            catch (FormatException)
            {
                throw new BadRequestException(
                    "Geçersiz RowVersion değeri.");
            }

            _unitOfWork.Policies.SetOriginalRowVersion(
              policy,
              rowVersion);

            policy.EndDate = dto.EndDate;
            policy.UpdatedDate = DateTime.UtcNow;

            await _unitOfWork.Policies.UpdateAsync(policy);
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "Poliçe başka bir kullanıcı tarafından güncellenmiş. Lütfen güncel veriyi tekrar alın.");
            }
        }
        public async Task<QuoteDto> RenewAsync(
    PolicyRenewalDto dto)
        {
            var policy = await _policyRepository
                .GetByIdAsync(dto.PolicyId);

            if (policy == null || policy.IsDeleted)
            {
                throw new NotFoundException(
                    "Poliçe bulunamadı.");
            }

            if (policy.Status != PolicyStatus.Active)
            {
                throw new BadRequestException(
                    "Sadece aktif poliçe yenilenebilir.");
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
                        "Poliçe bulunamadı.");
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null ||
                    policy.CustomerId != currentUser.CustomerId.Value)
                {
                    throw new NotFoundException(
                        "Poliçe bulunamadı.");
                }
            }

            if (dto.StartDate >= dto.EndDate)
            {
                throw new BadRequestException(
                    "Yenileme başlangıç tarihi bitiş tarihinden önce olmalıdır.");
            }

            if (dto.StartDate < policy.EndDate.Date)
            {
                throw new BadRequestException(
                    "Yenileme başlangıç tarihi mevcut poliçenin bitiş tarihinden önce olamaz.");
            }

            var quote = await _quoteService.CreateAsync(
                new CreateQuoteDto
                {
                    CustomerId = policy.CustomerId,
                    VehicleId = policy.VehicleId,

                    ValidUntil = dto.StartDate,

                    CoverageIds = dto.CoverageIds,
                    Usage = dto.Usage,
                    ClaimsCount = dto.ClaimsCount,
                    PackageId = dto.PackageId,
                    Deductible = dto.Deductible
                });

            return quote;
        }
        public async Task DeleteAsync(
            Guid id,
            Guid? deletedBy)
        {
            var policy =
                await _unitOfWork.Policies.GetByIdAsync(id);

            if (policy == null || policy.IsDeleted)
            {
                throw new NotFoundException("Poliçe bulunamadı.");
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
                        "Poliçe bulunamadı.");
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null ||
                    policy.CustomerId != currentUser.CustomerId.Value)
                {
                    throw new NotFoundException(
                        "Poliçe bulunamadı.");
                }
            }
            policy.IsDeleted = true;
            policy.DeletedDate = DateTime.UtcNow;
            policy.DeletedBy = deletedBy;

            await _unitOfWork.Policies.UpdateAsync(policy);
            await _unitOfWork.SaveChangesAsync();
        }

            public async Task CancelAsync(
    Guid id,
    Guid? cancelledBy)
        {
            var policy = await _unitOfWork.Policies
                .GetByIdAsync(id);

            if (policy == null || policy.IsDeleted)
            {
                throw new NotFoundException(
                    "Poliçe bulunamadı.");
            }

            if (policy.Status != PolicyStatus.Active)
            {
                throw new BadRequestException(
                    "Sadece aktif poliçe iptal edilebilir.");
            }

            policy.Status = PolicyStatus.Cancelled;
            policy.UpdatedDate = DateTime.UtcNow;

            await _unitOfWork.Policies.UpdateAsync(policy);
            await _unitOfWork.SaveChangesAsync();

       }
        public async Task ExpireAsync(Guid id)
        {
            var policy = await _unitOfWork.Policies
                .GetByIdAsync(id);

            if (policy == null || policy.IsDeleted)
            {
                throw new NotFoundException(
                    "Poliçe bulunamadı.");
            }

            if (policy.Status != PolicyStatus.Active)
            {
                throw new BadRequestException(
                    "Sadece aktif poliçe süresi dolabilir.");
            }

            if (policy.EndDate > DateTime.UtcNow)
            {
                throw new BadRequestException(
                    "Poliçenin süresi henüz dolmamıştır.");
            }

            policy.Status = PolicyStatus.Expired;
            policy.UpdatedDate = DateTime.UtcNow;

            await _unitOfWork.Policies.UpdateAsync(policy);
            await _unitOfWork.SaveChangesAsync();
        }
    }
    }
