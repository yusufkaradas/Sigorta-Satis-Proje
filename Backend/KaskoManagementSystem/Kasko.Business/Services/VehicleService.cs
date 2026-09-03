using Kasko.Business.DTOs.Vehicle;
using Kasko.Business.Exceptions;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Kasko.Business.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VehicleService(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IEnumerable<VehicleListDto>> GetAllAsync()
        {
            var vehicles = await _unitOfWork.Vehicles.GetAllAsync();

            return vehicles.Select(x => new VehicleListDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                PlateNumber = x.PlateNumber,
                VIN = x.VIN,
                Brand = x.Brand,
                Model = x.Model,
                ModelYear = x.ModelYear,
                VehicleType = x.VehicleType,
                CreatedDate = x.CreatedDate,
                MarketValue = x.MarketValue,
                IsActive = x.IsActive,
            });
        }

        public async Task<VehicleDto?> GetByIdAsync(Guid id)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(id);

            if (vehicle == null)
            {
                return null;
            }

            return new VehicleDto
            {
                Id = vehicle.Id,
                CustomerId = vehicle.CustomerId,
                PlateNumber = vehicle.PlateNumber,
                VIN = vehicle.VIN,
                Brand = vehicle.Brand,
                BrandCode = vehicle.BrandCode,
                TypeCode = vehicle.TypeCode,
                Model = vehicle.Model,
                ModelYear = vehicle.ModelYear,
                VehicleType = vehicle.VehicleType,
                FuelType = vehicle.FuelType,
                TransmissionType = vehicle.TransmissionType,
                EngineVolume = vehicle.EngineVolume,
                EnginePower = vehicle.EnginePower,
                Color = vehicle.Color,
                MarketValue = vehicle.MarketValue,
                CreatedDate = vehicle.CreatedDate,
                IsActive = vehicle.IsActive,
            };
        }

        public async Task<VehicleDto> CreateAsync(CreateVehicleDto dto)
        {
            var customer = await _unitOfWork.Customers
                .GetByIdAsync(dto.CustomerId);

            if (customer == null)
            {
                throw new NotFoundException("Müşteri bulunamadı.");
            }

            var plateExists = await _unitOfWork.Vehicles
                .PlateExistsAsync(dto.PlateNumber);

            if (plateExists)
            {
                throw new BadRequestException(
                    "Bu plaka başka bir araç tarafından kullanılmaktadır.");
            }

            var vinExists = await _unitOfWork.Vehicles
                .VinExistsAsync(dto.VIN);

            if (vinExists)
            {
                throw new BadRequestException(
                    "Bu VIN numarası başka bir araç tarafından kullanılmaktadır.");
            }
            var tsbRecord =
        await _unitOfWork.VehicleValueCatalogs
            .GetActiveByKeyAsync(
                dto.BrandCode,
                dto.TypeCode,
                dto.ModelYear);

            if (tsbRecord == null)
            {
                throw new NotFoundException(
                    "Seçilen marka, araç tipi ve model yılı için TSB araç değeri bulunamadı.");
            }

            var vehicle = new Vehicle
            {
                Id = Guid.NewGuid(),

                CustomerId = dto.CustomerId,

                PlateNumber = dto.PlateNumber,
                VIN = dto.VIN,
                Brand = dto.Brand,
                Model = dto.Model,
                ModelYear = dto.ModelYear,
                BrandCode = dto.BrandCode,
                TypeCode = dto.TypeCode,

                VehicleType = dto.VehicleType,
                FuelType = dto.FuelType,
                TransmissionType = dto.TransmissionType,

                EngineVolume = dto.EngineVolume,
                EnginePower = dto.EnginePower,

                Color = dto.Color,

                MarketValue = tsbRecord.Value,

                CreatedDate = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,
            };

            var userIdValue = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (Guid.TryParse(userIdValue, out var userId))
            {
                vehicle.CreatedBy = userId.ToString();
            }

            await _unitOfWork.Vehicles.AddAsync(vehicle);

            await _unitOfWork.SaveChangesAsync();

            return new VehicleDto
            {
                Id = vehicle.Id,
                CustomerId = vehicle.CustomerId,
                PlateNumber = vehicle.PlateNumber,
                VIN = vehicle.VIN,
                Brand = vehicle.Brand,
                BrandCode = vehicle.BrandCode,
                Model = vehicle.Model,
                TypeCode = vehicle.TypeCode,
                ModelYear = vehicle.ModelYear,
                VehicleType = vehicle.VehicleType,
                FuelType = vehicle.FuelType,
                TransmissionType = vehicle.TransmissionType,
                EngineVolume = vehicle.EngineVolume,
                EnginePower = vehicle.EnginePower,
                Color = vehicle.Color,
                MarketValue = vehicle.MarketValue,
                IsActive = vehicle.IsActive,
                CreatedDate=vehicle.CreatedDate
            };
        }

        public async Task UpdateAsync(
            Guid id,
            UpdateVehicleDto dto)
        {
            var vehicle = await _unitOfWork.Vehicles
                .GetByIdAsync(id);

            if (vehicle == null)
            {
                throw new NotFoundException(
                    "Araç bulunamadı.");
            }

            var customer = await _unitOfWork.Customers
                .GetByIdAsync(dto.CustomerId);

            if (customer == null)
            {
                throw new NotFoundException(
                    "Müşteri bulunamadı.");
            }

            var plateExists = await _unitOfWork.Vehicles
                .PlateExistsAsync(
                    dto.PlateNumber,
                    id);

            if (plateExists)
            {
                throw new BadRequestException(
                    "Bu plaka başka bir araç tarafından kullanılmaktadır.");
            }

            var vinExists = await _unitOfWork.Vehicles
                .VinExistsAsync(
                    dto.VIN,
                    id);

            if (vinExists)
            {
                throw new BadRequestException(
                    "Bu VIN numarası başka bir araç tarafından kullanılmaktadır.");
            }
            
            var tsbRecord =
            await _unitOfWork.VehicleValueCatalogs.GetActiveByKeyAsync(
            dto.BrandCode,
            dto.TypeCode,
            dto.ModelYear);

            if (tsbRecord == null)
            {
                throw new NotFoundException(
                    "Seçilen marka, araç tipi ve model yılı için TSB araç değeri bulunamadı.");
            }
            vehicle.CustomerId = dto.CustomerId;

            vehicle.PlateNumber = dto.PlateNumber;
            vehicle.VIN = dto.VIN;
            vehicle.Brand = dto.Brand;
            vehicle.BrandCode = dto.BrandCode;

            vehicle.Model = dto.Model;
            vehicle.ModelYear = dto.ModelYear;

            vehicle.TypeCode = dto.TypeCode;

            vehicle.VehicleType = dto.VehicleType;
            vehicle.FuelType = dto.FuelType;
            vehicle.TransmissionType = dto.TransmissionType;

            vehicle.EngineVolume = dto.EngineVolume;
            vehicle.EnginePower = dto.EnginePower;

            vehicle.Color = dto.Color;

            vehicle.MarketValue = tsbRecord.Value;

            vehicle.UpdatedDate = DateTime.UtcNow;

            await _unitOfWork.Vehicles.UpdateAsync(vehicle);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var deletedByValue = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            Guid? deletedBy = Guid.TryParse(
                deletedByValue,
                out var deletedById)
                ? deletedById
                : null;

            var result = await _unitOfWork.Vehicles
                .DeleteVehicleAsync(id, deletedBy);

            if (!result)
            {
                throw new NotFoundException(
                    "Araç bulunamadı.");
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}