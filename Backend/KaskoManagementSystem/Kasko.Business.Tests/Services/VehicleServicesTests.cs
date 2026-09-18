using Kasko.Business.DTOs.Vehicle;
using Kasko.Business.Exceptions;
using Kasko.Business.Services;
using Kasko.DataAccess.Repositories;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Kasko.Business.Tests.Services
{
    public class VehicleServicesTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IVehicleRepository> _vehicleRepositoryMock = null!;
        private Mock<ICustomerRepository> _customerRepositoryMock = null!;
        private Mock<IVehicleValueCatalogRepository> _vehicleValueCatalogRepositoryMock = null!;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
        private VehicleService _vehicleService = null!;

        public VehicleServicesTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _vehicleRepositoryMock = new Mock<IVehicleRepository>();
            _customerRepositoryMock = new Mock<ICustomerRepository>();
            _vehicleValueCatalogRepositoryMock = new Mock<IVehicleValueCatalogRepository>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            _unitOfWorkMock
                .Setup(x => x.Vehicles)
                .Returns(_vehicleRepositoryMock.Object);

            _unitOfWorkMock
                .Setup(x => x.Customers)
                .Returns(_customerRepositoryMock.Object);
            _unitOfWorkMock
                .Setup(x => x.VehicleValueCatalogs)
                .Returns(_vehicleValueCatalogRepositoryMock.Object);

            _vehicleService = new VehicleService(
                _unitOfWorkMock.Object,
                _httpContextAccessorMock.Object);
        }

    
        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedVehicleListDtos()
        {
           
            var vehicle1 = new Vehicle
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                PlateNumber = "34ABC123",
                VIN = "VIN123456789",
                Brand = "TOYOTA",
                Model = "COROLLA",
                ModelYear = 2024,
                VehicleType = VehicleType.Sedan,
                IsActive = true,
                IsDeleted = false
            };

            var vehicle2 = new Vehicle
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                PlateNumber = "06XYZ456",
                VIN = "VIN987654321",
                Brand = "HONDA",
                Model = "CIVIC",
                ModelYear = 2023,
                VehicleType = VehicleType.Sedan,
                IsActive = false,
                IsDeleted = false
            };

            _vehicleRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Vehicle>
                {
                    vehicle1,
                    vehicle2
                });

           
            var result = (await _vehicleService.GetAllAsync()).ToList();

            
            Assert.Equal(2, result.Count);

            Assert.Equal(vehicle1.Id, result[0].Id);
            Assert.Equal(vehicle1.CustomerId, result[0].CustomerId);
            Assert.Equal(vehicle1.PlateNumber, result[0].PlateNumber);
            Assert.Equal(vehicle1.VIN, result[0].VIN);
            Assert.Equal(vehicle1.Brand, result[0].Brand);
            Assert.Equal(vehicle1.Model, result[0].Model);
            Assert.Equal(vehicle1.ModelYear, result[0].ModelYear);
            Assert.Equal(vehicle1.VehicleType, result[0].VehicleType);
            Assert.Equal(vehicle1.IsActive, result[0].IsActive);

            Assert.Equal(vehicle2.Id, result[1].Id);
            Assert.Equal(vehicle2.CustomerId, result[1].CustomerId);
            Assert.Equal(vehicle2.PlateNumber, result[1].PlateNumber);
            Assert.Equal(vehicle2.VIN, result[1].VIN);
            Assert.Equal(vehicle2.Brand, result[1].Brand);
            Assert.Equal(vehicle2.Model, result[1].Model);
            Assert.Equal(vehicle2.ModelYear, result[1].ModelYear);
            Assert.Equal(vehicle2.VehicleType, result[1].VehicleType);
            Assert.Equal(vehicle2.IsActive, result[1].IsActive);
        }

        
        [Fact]
        public async Task GetByIdAsync_WhenVehicleNotFound_ShouldReturnNull()
        {
            
            var vehicleId = Guid.NewGuid();

            _vehicleRepositoryMock
                .Setup(x => x.GetByIdAsync(vehicleId))
                .ReturnsAsync((Vehicle?)null);

            
            var result = await _vehicleService.GetByIdAsync(vehicleId);

           
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedVehicleDto()
        {
            
            var vehicleId = Guid.NewGuid();
            var customerId = Guid.NewGuid();

            var vehicle = new Vehicle
            {
                Id = vehicleId,
                CustomerId = customerId,
                PlateNumber = "34ABC123",
                VIN = "VIN123456789",
                Brand = "TOYOTA",
                Model = "COROLLA",
                ModelYear = 2024,
                VehicleType = VehicleType.Sedan,
                FuelType = FuelType.Gasoline,
                TransmissionType = TransmissionType.Automatic,
                EngineVolume = 1.6m,
                EnginePower = 130,
                Color = "Beyaz",
                IsActive = true,
                IsDeleted = false
            };

            _vehicleRepositoryMock
                .Setup(x => x.GetByIdAsync(vehicleId))
                .ReturnsAsync(vehicle);

            
            var result = await _vehicleService.GetByIdAsync(vehicleId);

            
            Assert.NotNull(result);

            Assert.Equal(vehicle.Id, result!.Id);
            Assert.Equal(vehicle.CustomerId, result.CustomerId);
            Assert.Equal(vehicle.PlateNumber, result.PlateNumber);
            Assert.Equal(vehicle.VIN, result.VIN);
            Assert.Equal(vehicle.Brand, result.Brand);
            Assert.Equal(vehicle.Model, result.Model);
            Assert.Equal(vehicle.ModelYear, result.ModelYear);
            Assert.Equal(vehicle.VehicleType, result.VehicleType);
            Assert.Equal(vehicle.FuelType, result.FuelType);
            Assert.Equal(vehicle.TransmissionType, result.TransmissionType);
            Assert.Equal(vehicle.EngineVolume, result.EngineVolume);
            Assert.Equal(vehicle.EnginePower, result.EnginePower);
            Assert.Equal(vehicle.Color, result.Color);
            Assert.Equal(vehicle.IsActive, result.IsActive);
        }

        

        [Fact]
        public async Task CreateAsync_WhenCustomerNotFound_ShouldThrowNotFoundException()
        {
            
            var customerId = Guid.NewGuid();

            var dto = new CreateVehicleDto
            {
                CustomerId = customerId,
                PlateNumber = "34ABC123",
                VIN = "VIN123456789",
                Brand = "TOYOTA",
                Model = "COROLLA",
                ModelYear = 2024
            };

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync((Customer?)null);

         
            var act = async () =>
                await _vehicleService.CreateAsync(dto);

         
            await Assert.ThrowsAsync<NotFoundException>(act);

            _vehicleRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Vehicle>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenPlateExists_ShouldThrowBadRequestException()
        {
            
            var customerId = Guid.NewGuid();
            var customer = new Customer
            {
                Id = customerId,
                IsDeleted = false
            };

            var dto = new CreateVehicleDto
            {
                CustomerId = customerId,
                PlateNumber = "34ABC123",
                VIN = "VIN123456789",
                Brand = "TOYOTA",
                Model = "COROLLA",
                ModelYear = 2024
            };

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _vehicleRepositoryMock
                .Setup(x => x.PlateExistsAsync(
                    dto.PlateNumber,
                    It.IsAny<Guid?>()))
                .ReturnsAsync(true);

         
            var act = async () =>
                await _vehicleService.CreateAsync(dto);

            
            await Assert.ThrowsAsync<BadRequestException>(act);

            _vehicleRepositoryMock.Verify(
                x => x.VinExistsAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid?>()),
                Times.Never);

            _vehicleRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Vehicle>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenVinExists_ShouldThrowBadRequestException()
        {
            
            var customerId = Guid.NewGuid();
            var customer = new Customer
            {
                Id = customerId,
                IsDeleted = false
            };

            var dto = new CreateVehicleDto
            {
                CustomerId = customerId,
                PlateNumber = "34ABC123",
                VIN = "VIN123456789",
                Brand = "TOYOTA",
                Model = "COROLLA",
                ModelYear = 2024
            };

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _vehicleRepositoryMock
                .Setup(x => x.PlateExistsAsync(
                    dto.PlateNumber,
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            _vehicleRepositoryMock
                .Setup(x => x.VinExistsAsync(
                    dto.VIN,
                    It.IsAny<Guid?>()))
                .ReturnsAsync(true);

           
            var act = async () =>
                await _vehicleService.CreateAsync(dto);

         
            await Assert.ThrowsAsync<BadRequestException>(act);

            _vehicleRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Vehicle>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenValid_ShouldCreateVehicleAndReturnDto()
        {
         
            var customerId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var customer = new Customer
            {
                Id = customerId,
                IsDeleted = false
            };

            var dto = new CreateVehicleDto
            {
                CustomerId = customerId,
                PlateNumber = "34ABC123",
                VIN = "VIN123456789",
                Brand = "TOYOTA",
                BrandCode = "TOYOTA",
                Model = "COROLLA",
                TypeCode = "COROLLA-2024",
                ModelYear = 2024,
                VehicleType = VehicleType.Sedan,
                FuelType = FuelType.Gasoline,
                TransmissionType = TransmissionType.Automatic,
                EngineVolume = 1.6m,
                EnginePower = 130,
                Color = "Beyaz"
            };
            var tsbRecord = new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = "TOYOTA",
                TypeCode = "COROLLA-2024",

                BrandName = "TOYOTA",
                TypeName = "COROLLA",

                ModelYear = 2024,

                Value = 1_250_000m,

                Source = "TSB",

                EffectiveDate = new DateTime(2026, 8, 1),

                ImportedAt = new DateTime(2026, 8, 1),

                IsActive = true,
                IsDeleted = false
            };
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userId.ToString())
            };

            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            _httpContextAccessorMock
                .Setup(x => x.HttpContext)
                .Returns(httpContext);

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _vehicleRepositoryMock
                .Setup(x => x.PlateExistsAsync(
                    dto.PlateNumber,
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            _vehicleRepositoryMock
                .Setup(x => x.VinExistsAsync(
                    dto.VIN,
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            _vehicleRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Vehicle>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);
            _vehicleValueCatalogRepositoryMock
                .Setup(x => x.GetActiveByKeyAsync(
                 dto.BrandCode,
                 dto.TypeCode,
                 dto.ModelYear))
                .ReturnsAsync(tsbRecord);

            var result = await _vehicleService.CreateAsync(dto);

            
            Assert.NotNull(result);

            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(dto.CustomerId, result.CustomerId);
            Assert.Equal(dto.PlateNumber, result.PlateNumber);
            Assert.Equal(dto.VIN, result.VIN);
            Assert.Equal(dto.Brand, result.Brand);
            Assert.Equal(tsbRecord.BrandCode, result.BrandCode);
            Assert.Equal(dto.Model, result.Model);
            Assert.Equal(dto.ModelYear, result.ModelYear);
            Assert.Equal(tsbRecord.TypeCode, result.TypeCode);
            Assert.Equal(dto.VehicleType, result.VehicleType);
            Assert.Equal(dto.FuelType, result.FuelType);
            Assert.Equal(dto.TransmissionType, result.TransmissionType);
            Assert.Equal(dto.EngineVolume, result.EngineVolume);
            Assert.Equal(dto.EnginePower, result.EnginePower);
            Assert.Equal(dto.Color, result.Color);
            Assert.Equal(tsbRecord.Value, result.MarketValue);
            Assert.True(result.IsActive);

            _vehicleRepositoryMock.Verify(
                x => x.AddAsync(It.Is<Vehicle>(v =>
                    v.CustomerId == dto.CustomerId &&
                    v.PlateNumber == dto.PlateNumber &&
                    v.VIN == dto.VIN &&
                    v.Brand == dto.Brand &&
                    v.Model == dto.Model &&
                    v.ModelYear == dto.ModelYear &&
                    v.VehicleType == dto.VehicleType &&
                    v.FuelType == dto.FuelType &&
                    v.TransmissionType == dto.TransmissionType &&
                    v.EngineVolume == dto.EngineVolume &&
                    v.EnginePower == dto.EnginePower &&
                    v.Color == dto.Color &&
                    v.IsActive &&
                    !v.IsDeleted &&
                    v.CreatedDate != default &&
                    v.CreatedBy == userId.ToString())),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenTsbRecordNotFound_ShouldThrowNotFoundException()
        {
            var customerId = Guid.NewGuid();

            var customer = new Customer
            {
                Id = customerId,
                IsDeleted = false
            };

            var dto = new CreateVehicleDto
            {
                CustomerId = customerId,

                PlateNumber = "34TSB001",
                VIN = "TSBVIN123456",

                Brand = "TOYOTA",
                BrandCode = "TOYOTA",

                Model = "COROLLA",
                TypeCode = "COROLLA-2024",

                ModelYear = 2024,

                VehicleType = VehicleType.Sedan,
                FuelType = FuelType.Gasoline,
                TransmissionType = TransmissionType.Automatic,

                EngineVolume = 1.6m,
                EnginePower = 130,
                Color = "Beyaz",

                MarketValue = 1m
            };

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _vehicleRepositoryMock
                .Setup(x => x.PlateExistsAsync(
                    dto.PlateNumber,
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            _vehicleRepositoryMock
                .Setup(x => x.VinExistsAsync(
                    dto.VIN,
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            _vehicleValueCatalogRepositoryMock
                .Setup(x => x.GetActiveByKeyAsync(
                    dto.BrandCode,
                    dto.TypeCode,
                    dto.ModelYear))
                .ReturnsAsync((VehicleValueCatalog?)null);

            var act = async () =>
                await _vehicleService.CreateAsync(dto);

            await Assert.ThrowsAsync<NotFoundException>(act);

            _vehicleRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Vehicle>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }
        [Fact]
        public async Task CreateAsync_WhenTsbRecordExists_ShouldUseTsbMarketValue()
        {
            var customerId = Guid.NewGuid();

            var customer = new Customer
            {
                Id = customerId,
                IsDeleted = false
            };

            var dto = new CreateVehicleDto
            {
                CustomerId = customerId,

                PlateNumber = "34TSB002",
                VIN = "TSBVIN654321",

                Brand = "TOYOTA",
                BrandCode = "TOYOTA",

                Model = "COROLLA",
                TypeCode = "COROLLA-2024",

                ModelYear = 2024,

                VehicleType = VehicleType.Sedan,
                FuelType = FuelType.Gasoline,
                TransmissionType = TransmissionType.Automatic,

                EngineVolume = 1.6m,
                EnginePower = 130,
                Color = "Beyaz",

                // Kasıtlı olarak yanlış.
                MarketValue = 1m
            };

            var tsbValue = 1_250_000m;

            var tsbRecord = new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = dto.BrandCode,
                TypeCode = dto.TypeCode,

                BrandName = "TOYOTA",
                TypeName = "COROLLA",

                ModelYear = dto.ModelYear,

                Value = tsbValue,

                Source = "TSB",

                EffectiveDate = new DateTime(2026, 8, 1),

                ImportedAt = new DateTime(2026, 8, 1),

                IsActive = true,
                IsDeleted = false
            };

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _vehicleRepositoryMock
                .Setup(x => x.PlateExistsAsync(
                    dto.PlateNumber,
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            _vehicleRepositoryMock
                .Setup(x => x.VinExistsAsync(
                    dto.VIN,
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            _vehicleValueCatalogRepositoryMock
                .Setup(x => x.GetActiveByKeyAsync(
                    dto.BrandCode,
                    dto.TypeCode,
                    dto.ModelYear))
                .ReturnsAsync(tsbRecord);

            _vehicleRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Vehicle>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            var result = await _vehicleService.CreateAsync(dto);

            Assert.NotNull(result);

            Assert.Equal(tsbValue, result.MarketValue);

            _vehicleRepositoryMock.Verify(
                x => x.AddAsync(It.Is<Vehicle>(v =>
                    v.MarketValue == tsbValue &&
                    v.BrandCode == dto.BrandCode &&
                    v.TypeCode == dto.TypeCode)),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }
        [Fact]
        public async Task UpdateAsync_WhenVehicleNotFound_ShouldThrowNotFoundException()
        {
            var vehicleId = Guid.NewGuid();
            var customerId = Guid.NewGuid();

            var dto = new UpdateVehicleDto
            {
                CustomerId = customerId,
                PlateNumber = "34ABC123",
                VIN = "VIN123456789",
                Brand = "TOYOTA",
                Model = "COROLLA",
                ModelYear = 2024
            };

            _vehicleRepositoryMock
                .Setup(x => x.GetByIdAsync(vehicleId))
                .ReturnsAsync((Vehicle?)null);

            
            var act = async () =>
                await _vehicleService.UpdateAsync(vehicleId, dto);

           
            await Assert.ThrowsAsync<NotFoundException>(act);

            _customerRepositoryMock.Verify(
                x => x.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _vehicleRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Vehicle>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenCustomerNotFound_ShouldThrowNotFoundException()
        {
            
            var vehicleId = Guid.NewGuid();
            var customerId = Guid.NewGuid();

            var vehicle = new Vehicle
            {
                Id = vehicleId,
                CustomerId = customerId
            };

            var dto = new UpdateVehicleDto
            {
                CustomerId = customerId,
                PlateNumber = "34ABC123",
                VIN = "VIN123456789",
                Brand = "TOYOTA",
                Model = "COROLLA",
                ModelYear = 2024
            };

            _vehicleRepositoryMock
                .Setup(x => x.GetByIdAsync(vehicleId))
                .ReturnsAsync(vehicle);

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync((Customer?)null);

           
            var act = async () =>
                await _vehicleService.UpdateAsync(vehicleId, dto);

            
            await Assert.ThrowsAsync<NotFoundException>(act);

            _vehicleRepositoryMock.Verify(
                x => x.PlateExistsAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid?>()),
                Times.Never);

            _vehicleRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Vehicle>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenPlateExists_ShouldThrowBadRequestException()
        {
           
            var vehicleId = Guid.NewGuid();
            var customerId = Guid.NewGuid();

            var vehicle = new Vehicle
            {
                Id = vehicleId,
                CustomerId = customerId
            };

            var customer = new Customer
            {
                Id = customerId
            };

            var dto = new UpdateVehicleDto
            {
                CustomerId = customerId,
                PlateNumber = "34ABC123",
                VIN = "VIN123456789",
                Brand = "TOYOTA",
                Model = "COROLLA",
                ModelYear = 2024
            };

            _vehicleRepositoryMock
                .Setup(x => x.GetByIdAsync(vehicleId))
                .ReturnsAsync(vehicle);

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _vehicleRepositoryMock
                .Setup(x => x.PlateExistsAsync(
                    dto.PlateNumber,
                    vehicleId))
                .ReturnsAsync(true);

            
            var act = async () =>
                await _vehicleService.UpdateAsync(vehicleId, dto);

            
            await Assert.ThrowsAsync<BadRequestException>(act);

            _vehicleRepositoryMock.Verify(
                x => x.VinExistsAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid?>()),
                Times.Never);

            _vehicleRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Vehicle>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenVinExists_ShouldThrowBadRequestException()
        {
            
            var vehicleId = Guid.NewGuid();
            var customerId = Guid.NewGuid();

            var vehicle = new Vehicle
            {
                Id = vehicleId,
                CustomerId = customerId
            };

            var customer = new Customer
            {
                Id = customerId
            };

            var dto = new UpdateVehicleDto
            {
                CustomerId = customerId,
                PlateNumber = "34ABC123",
                VIN = "VIN123456789",
                Brand = "TOYOTA",
                Model = "COROLLA",
                ModelYear = 2024
            };

            _vehicleRepositoryMock
                .Setup(x => x.GetByIdAsync(vehicleId))
                .ReturnsAsync(vehicle);

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _vehicleRepositoryMock
                .Setup(x => x.PlateExistsAsync(
                    dto.PlateNumber,
                    vehicleId))
                .ReturnsAsync(false);

            _vehicleRepositoryMock
                .Setup(x => x.VinExistsAsync(
                    dto.VIN,
                    vehicleId))
                .ReturnsAsync(true);

            
            var act = async () =>
                await _vehicleService.UpdateAsync(vehicleId, dto);

            
            await Assert.ThrowsAsync<BadRequestException>(act);

            _vehicleRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Vehicle>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenValid_ShouldUpdateVehicleAndSave()
        {
            
            var vehicleId = Guid.NewGuid();
            var customerId = Guid.NewGuid();

            var vehicle = new Vehicle
            {
                Id = vehicleId,
                CustomerId = Guid.NewGuid(),
                PlateNumber = "34OLD111",
                VIN = "OLDVIN123",
                Brand = "TOYOTA",
                Model = "COROLLA",
                ModelYear = 2020,
                VehicleType = VehicleType.Sedan,
                FuelType = FuelType.Gasoline,
                TransmissionType = TransmissionType.Manual,
                EngineVolume = 1.4m,
                EnginePower = 100,
                Color = "Siyah",
                IsActive = true
            };

            var customer = new Customer
            {
                Id = customerId
            };

            var dto = new UpdateVehicleDto
            {
                CustomerId = customerId,

                PlateNumber = "34NEW222",
                VIN = "NEWVIN456",

                Brand = "HONDA",
                BrandCode = "HONDA",

                Model = "CIVIC",
                TypeCode = "CIVIC-2024",

                ModelYear = 2024,

                VehicleType = VehicleType.Sedan,
                FuelType = FuelType.Hybrid,
                TransmissionType = TransmissionType.Automatic,

                EngineVolume = 1.5m,
                EnginePower = 130,

                Color = "Beyaz"
            };
            var tsbRecord = new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = dto.BrandCode,
                TypeCode = dto.TypeCode,

                BrandName = "HONDA",
                TypeName = "CIVIC",

                ModelYear = dto.ModelYear,

                Value = 1_300_000m,

                Source = "TSB",

                EffectiveDate = new DateTime(2026, 9, 1),
                ImportedAt = new DateTime(2026, 9, 1),

                IsActive = true,
                IsDeleted = false
            };

            _vehicleRepositoryMock
                .Setup(x => x.GetByIdAsync(vehicleId))
                .ReturnsAsync(vehicle);

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _vehicleRepositoryMock
                .Setup(x => x.PlateExistsAsync(
                    dto.PlateNumber,
                    vehicleId))
                .ReturnsAsync(false);

            _vehicleRepositoryMock
                .Setup(x => x.VinExistsAsync(
                    dto.VIN,
                    vehicleId))
                .ReturnsAsync(false);

            _vehicleRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Vehicle>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            _vehicleValueCatalogRepositoryMock
                .Setup(x => x.GetActiveByKeyAsync(
                dto.BrandCode,
                dto.TypeCode,
                dto.ModelYear))
               .ReturnsAsync(tsbRecord);


            await _vehicleService.UpdateAsync(vehicleId, dto);

            
            Assert.Equal(dto.CustomerId, vehicle.CustomerId);
            Assert.Equal(dto.PlateNumber, vehicle.PlateNumber);
            Assert.Equal(dto.VIN, vehicle.VIN);
            Assert.Equal(dto.Brand, vehicle.Brand);
            Assert.Equal(dto.BrandCode, vehicle.BrandCode);
            Assert.Equal(dto.TypeCode, vehicle.TypeCode);
            Assert.Equal(tsbRecord.Value, vehicle.MarketValue);
            Assert.Equal(dto.Model, vehicle.Model);
            Assert.Equal(dto.ModelYear, vehicle.ModelYear);
            Assert.Equal(dto.VehicleType, vehicle.VehicleType);
            Assert.Equal(dto.FuelType, vehicle.FuelType);
            Assert.Equal(dto.TransmissionType, vehicle.TransmissionType);
            Assert.Equal(dto.EngineVolume, vehicle.EngineVolume);
            Assert.Equal(dto.EnginePower, vehicle.EnginePower);
            Assert.Equal(dto.Color, vehicle.Color);
            Assert.NotEqual(default, vehicle.UpdatedDate);

            _vehicleRepositoryMock.Verify(
                x => x.UpdateAsync(vehicle),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenTsbRecordNotFound_ShouldThrowNotFoundException()
        {
            var vehicleId = Guid.NewGuid();
            var customerId = Guid.NewGuid();

            var vehicle = new Vehicle
            {
                Id = vehicleId,
                CustomerId = customerId,
                PlateNumber = "34OLD111",
                VIN = "OLDVIN123"
            };

            var customer = new Customer
            {
                Id = customerId,
                IsDeleted = false
            };

            var dto = new UpdateVehicleDto
            {
                CustomerId = customerId,
                PlateNumber = "34NEW222",
                VIN = "NEWVIN456",

                Brand = "HONDA",
                BrandCode = "HONDA",

                Model = "CIVIC",
                TypeCode = "CIVIC-2024",

                ModelYear = 2024,

                VehicleType = VehicleType.Sedan,
                FuelType = FuelType.Hybrid,
                TransmissionType = TransmissionType.Automatic,

                EngineVolume = 1.5m,
                EnginePower = 130,
                Color = "Beyaz"
            };

            _vehicleRepositoryMock
                .Setup(x => x.GetByIdAsync(vehicleId))
                .ReturnsAsync(vehicle);

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _vehicleRepositoryMock
                .Setup(x => x.PlateExistsAsync(
                    dto.PlateNumber,
                    vehicleId))
                .ReturnsAsync(false);

            _vehicleRepositoryMock
                .Setup(x => x.VinExistsAsync(
                    dto.VIN,
                    vehicleId))
                .ReturnsAsync(false);

            _vehicleValueCatalogRepositoryMock
                .Setup(x => x.GetActiveByKeyAsync(
                    dto.BrandCode,
                    dto.TypeCode,
                    dto.ModelYear))
                .ReturnsAsync((VehicleValueCatalog?)null);

            var act = async () =>
                await _vehicleService.UpdateAsync(vehicleId, dto);

            await Assert.ThrowsAsync<NotFoundException>(act);

            _vehicleRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Vehicle>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenVehicleNotFound_ShouldThrowNotFoundException()
        {
            
            var vehicleId = Guid.NewGuid();

            _vehicleRepositoryMock
                .Setup(x => x.DeleteVehicleAsync(
                    vehicleId,
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            
            var act = async () =>
                await _vehicleService.DeleteAsync(vehicleId);

            
            await Assert.ThrowsAsync<NotFoundException>(act);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenValid_ShouldDeleteVehicleAndSave()
        {
           
            var vehicleId = Guid.NewGuid();
            var deletedBy = Guid.NewGuid();

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    deletedBy.ToString())
            };

            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            _httpContextAccessorMock
                .Setup(x => x.HttpContext)
                .Returns(httpContext);

            _vehicleRepositoryMock
                .Setup(x => x.DeleteVehicleAsync(
                    vehicleId,
                    deletedBy))
                .ReturnsAsync(true);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            
            await _vehicleService.DeleteAsync(vehicleId);

            
            _vehicleRepositoryMock.Verify(
                x => x.DeleteVehicleAsync(
                    vehicleId,
                    deletedBy),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }
    }
}