using System.Linq.Expressions;
using System.Security.Claims;
using Kasko.Business.DTOs.Customer;
using Kasko.Business.Exceptions;
using Kasko.Business.Services;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Kasko.Business.Tests.Services
{
    public class CustomerServiceTest
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<ICustomerRepository> _customerRepositoryMock = null!;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
        private CustomerService _customerService = null!;

        public CustomerServiceTest()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _customerRepositoryMock = new Mock<ICustomerRepository>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            _unitOfWorkMock
                .Setup(x => x.Customers)
                .Returns(_customerRepositoryMock.Object);

            _customerService = new CustomerService(
                _unitOfWorkMock.Object,
                _httpContextAccessorMock.Object);
        }

        
        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedCustomerListDtos()
        {
            
            var customer1 = new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = "Ahmet",
                LastName = "Yılmaz",
                IdentityNumber = "11111111111",
                Email = "ahmet@example.com",
                PhoneNumber = "5551112233",
                City = "İstanbul",
                District = "Kadıköy",
                IsActive = true,
                IsDeleted = false
            };

            var customer2 = new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = "Mehmet",
                LastName = "Demir",
                IdentityNumber = "22222222222",
                Email = "mehmet@example.com",
                PhoneNumber = "5554445566",
                City = "Ankara",
                District = "Çankaya",
                IsActive = false,
                IsDeleted = false
            };

            _customerRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Customer>
                {
                    customer1,
                    customer2
                });

            
            var result = (await _customerService.GetAllAsync()).ToList();

            
            Assert.Equal(2, result.Count);

            Assert.Equal(customer1.Id, result[0].Id);
            Assert.Equal(customer1.FirstName, result[0].FirstName);
            Assert.Equal(customer1.LastName, result[0].LastName);
            Assert.Equal(customer1.IdentityNumber, result[0].IdentityNumber);
            Assert.Equal(customer1.Email, result[0].Email);
            Assert.Equal(customer1.PhoneNumber, result[0].PhoneNumber);
            Assert.Equal(customer1.City, result[0].City);
            Assert.Equal(customer1.District, result[0].District);
            Assert.Equal(customer1.IsActive, result[0].IsActive);

            Assert.Equal(customer2.Id, result[1].Id);
            Assert.Equal(customer2.FirstName, result[1].FirstName);
            Assert.Equal(customer2.LastName, result[1].LastName);
            Assert.Equal(customer2.IdentityNumber, result[1].IdentityNumber);
            Assert.Equal(customer2.Email, result[1].Email);
            Assert.Equal(customer2.PhoneNumber, result[1].PhoneNumber);
            Assert.Equal(customer2.City, result[1].City);
            Assert.Equal(customer2.District, result[1].District);
            Assert.Equal(customer2.IsActive, result[1].IsActive);
        }

        [Fact]
        public async Task GetAllAsync_WhenDistrictIsNull_ShouldReturnEmptyString()
        {
            
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = "Ali",
                LastName = "Kaya",
                IdentityNumber = "33333333333",
                Email = "ali@example.com",
                PhoneNumber = "5551234567",
                City = "İzmir",
                District = null,
                IsActive = true,
                IsDeleted = false
            };

            _customerRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Customer>
                {
                    customer
                });

           
            var result = (await _customerService.GetAllAsync()).ToList();

            
            Assert.Single(result);
            Assert.Equal(string.Empty, result[0].District);
        }


        [Fact]
        public async Task GetByIdAsync_WhenCustomerNotFound_ShouldReturnNull()
        {
            
            var customerId = Guid.NewGuid();

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync((Customer?)null);

            
            var result = await _customerService.GetByIdAsync(customerId);

          
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedCustomerDto()
        {
           
            var customerId = Guid.NewGuid();

            var customer = new Customer
            {
                Id = customerId,
                FirstName = "Hasan",
                LastName = "Çelik",
                IdentityNumber = "44444444444",
                DateOfBirth = new DateTime(1995, 5, 10),
                Email = "hasan@example.com",
                PhoneNumber = "5558889900",
                Address = "Atatürk Mahallesi",
                City = "Bursa",
                District = "Nilüfer",
                IsActive = true,
                IsDeleted = false
            };

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

          
            var result = await _customerService.GetByIdAsync(customerId);

           
            Assert.NotNull(result);

            Assert.Equal(customer.Id, result!.Id);
            Assert.Equal(customer.FirstName, result.FirstName);
            Assert.Equal(customer.LastName, result.LastName);
            Assert.Equal(customer.IdentityNumber, result.IdentityNumber);
            Assert.Equal(customer.DateOfBirth, result.DateOfBirth);
            Assert.Equal(customer.Email, result.Email);
            Assert.Equal(customer.PhoneNumber, result.PhoneNumber);
            Assert.Equal(customer.Address, result.Address);
            Assert.Equal(customer.City, result.City);
            Assert.Equal(customer.District, result.District);
            Assert.Equal(customer.IsActive, result.IsActive);
        }

        [Fact]
        public async Task GetByIdAsync_WhenDistrictIsNull_ShouldReturnEmptyString()
        {
            
            var customerId = Guid.NewGuid();

            var customer = new Customer
            {
                Id = customerId,
                FirstName = "Veli",
                LastName = "Şahin",
                IdentityNumber = "55555555555",
                Email = "veli@example.com",
                City = "Adana",
                District = null,
                IsActive = true
            };

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            
            var result = await _customerService.GetByIdAsync(customerId);

            
            Assert.NotNull(result);
            Assert.Equal(string.Empty, result!.District);
        }

        

        [Fact]
        public async Task CreateAsync_WhenIdentityNumberExists_ShouldThrowBadRequestException()
        {
           
            var dto = new CreateCustomerDto
            {
                FirstName = "Ahmet",
                LastName = "Yılmaz",
                IdentityNumber = "11111111111",
                DateOfBirth = new DateTime(1990, 1, 1),
                Email = "ahmet@example.com",
                PhoneNumber = "5551112233",
                Address = "İstanbul",
                City = "İstanbul",
                District = "Kadıköy"
            };

            _customerRepositoryMock
                .Setup(x => x.AnyAsync(
                    It.IsAny<Expression<Func<Customer, bool>>>()))
                .ReturnsAsync(true);

            
            var act = async () =>
                await _customerService.CreateAsync(dto);

            
            await Assert.ThrowsAsync<BadRequestException>(act);

            _customerRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Customer>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenEmailExists_ShouldThrowBadRequestException()
        {
           
            var dto = new CreateCustomerDto
            {
                FirstName = "Mehmet",
                LastName = "Demir",
                IdentityNumber = "22222222222",
                DateOfBirth = new DateTime(1991, 2, 2),
                Email = "mehmet@example.com",
                PhoneNumber = "5552223344",
                Address = "Ankara",
                City = "Ankara",
                District = "Çankaya"
            };

            _customerRepositoryMock
                .SetupSequence(x => x.AnyAsync(
                    It.IsAny<Expression<Func<Customer, bool>>>()))
                .ReturnsAsync(false)
                .ReturnsAsync(true);

           
            var act = async () =>
                await _customerService.CreateAsync(dto);

            
            await Assert.ThrowsAsync<BadRequestException>(act);

            _customerRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Customer>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenValid_ShouldCreateCustomerAndSave()
        {
            
            var dto = new CreateCustomerDto
            {
                FirstName = "Ali",
                LastName = "Kaya",
                IdentityNumber = "33333333333",
                DateOfBirth = new DateTime(1993, 3, 3),
                Email = "ali@example.com",
                PhoneNumber = null,
                Address = "İzmir",
                City = "İzmir",
                District = "Bornova"
            };

            _customerRepositoryMock
                .SetupSequence(x => x.AnyAsync(
                    It.IsAny<Expression<Func<Customer, bool>>>()))
                .ReturnsAsync(false)
                .ReturnsAsync(false);

            _customerRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Customer>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            
            await _customerService.CreateAsync(dto);

            
            _customerRepositoryMock.Verify(
                x => x.AddAsync(It.Is<Customer>(c =>
                    c.FirstName == dto.FirstName &&
                    c.LastName == dto.LastName &&
                    c.IdentityNumber == dto.IdentityNumber &&
                    c.DateOfBirth == dto.DateOfBirth &&
                    c.Email == dto.Email &&
                    c.PhoneNumber == string.Empty &&
                    c.Address == dto.Address &&
                    c.City == dto.City &&
                    c.District == dto.District &&
                    c.IsActive &&
                    !c.IsDeleted &&
                    c.CreatedDate != default)),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }

       

        [Fact]
        public async Task UpdateAsync_WhenCustomerNotFound_ShouldThrowNotFoundException()
        {
            
            var customerId = Guid.NewGuid();

            var dto = new UpdateCustomerDto
            {
                Id = customerId,
                FirstName = "Ahmet",
                LastName = "Yılmaz",
                IdentityNumber = "11111111111",
                DateOfBirth = new DateTime(1990, 1, 1),
                Email = "ahmet@example.com",
                PhoneNumber = "5551112233",
                Address = "İstanbul",
                City = "İstanbul",
                District = "Kadıköy",
                IsActive = true
            };

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync((Customer?)null);

           
            var act = async () =>
                await _customerService.UpdateAsync(dto);

            
            await Assert.ThrowsAsync<NotFoundException>(act);

            _customerRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Customer>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenIdentityNumberBelongsToAnotherCustomer_ShouldThrowBadRequestException()
        {
            
            var customerId = Guid.NewGuid();

            var customer = new Customer
            {
                Id = customerId,
                IdentityNumber = "11111111111",
                Email = "old@example.com"
            };

            var dto = new UpdateCustomerDto
            {
                Id = customerId,
                FirstName = "Ahmet",
                LastName = "Yılmaz",
                IdentityNumber = "22222222222",
                Email = "new@example.com",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "5551112233",
                Address = "İstanbul",
                City = "İstanbul",
                District = "Kadıköy",
                IsActive = true
            };

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _customerRepositoryMock
                .Setup(x => x.AnyAsync(
                    It.IsAny<Expression<Func<Customer, bool>>>()))
                .ReturnsAsync(true);

            
            var act = async () =>
                await _customerService.UpdateAsync(dto);

            await Assert.ThrowsAsync<BadRequestException>(act);

            _customerRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Customer>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenEmailBelongsToAnotherCustomer_ShouldThrowBadRequestException()
        {
            
            var customerId = Guid.NewGuid();

            var customer = new Customer
            {
                Id = customerId,
                IdentityNumber = "11111111111",
                Email = "old@example.com"
            };

            var dto = new UpdateCustomerDto
            {
                Id = customerId,
                FirstName = "Ahmet",
                LastName = "Yılmaz",
                IdentityNumber = "11111111111",
                Email = "new@example.com",
                DateOfBirth = new DateTime(1990, 1, 1),
                PhoneNumber = "5551112233",
                Address = "İstanbul",
                City = "İstanbul",
                District = "Kadıköy",
                IsActive = true
            };

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _customerRepositoryMock
                .SetupSequence(x => x.AnyAsync(
                    It.IsAny<Expression<Func<Customer, bool>>>()))
                .ReturnsAsync(false)
                .ReturnsAsync(true);

          
            var act = async () =>
                await _customerService.UpdateAsync(dto);

           
            await Assert.ThrowsAsync<BadRequestException>(act);

            _customerRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Customer>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenValid_ShouldUpdateCustomerAndSave()
        {
          
            var customerId = Guid.NewGuid();

            var customer = new Customer
            {
                Id = customerId,
                FirstName = "Eski",
                LastName = "İsim",
                IdentityNumber = "11111111111",
                DateOfBirth = new DateTime(1990, 1, 1),
                Email = "old@example.com",
                PhoneNumber = "5551112233",
                Address = "Eski adres",
                City = "İstanbul",
                District = "Kadıköy",
                IsActive = true
            };

            var dto = new UpdateCustomerDto
            {
                Id = customerId,
                FirstName = "Yeni",
                LastName = "İsim",
                IdentityNumber = "22222222222",
                DateOfBirth = new DateTime(1992, 2, 2),
                Email = "new@example.com",
                PhoneNumber = null,
                Address = "Yeni adres",
                City = "Ankara",
                District = "Çankaya",
                IsActive = false
            };

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _customerRepositoryMock
                .SetupSequence(x => x.AnyAsync(
                    It.IsAny<Expression<Func<Customer, bool>>>()))
                .ReturnsAsync(false)
                .ReturnsAsync(false);

            _customerRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Customer>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

           
            await _customerService.UpdateAsync(dto);

            
            Assert.Equal(dto.FirstName, customer.FirstName);
            Assert.Equal(dto.LastName, customer.LastName);
            Assert.Equal(dto.IdentityNumber, customer.IdentityNumber);
            Assert.Equal(dto.DateOfBirth, customer.DateOfBirth);
            Assert.Equal(dto.Email, customer.Email);
            Assert.Equal(string.Empty, customer.PhoneNumber);
            Assert.Equal(dto.Address, customer.Address);
            Assert.Equal(dto.City, customer.City);
            Assert.Equal(dto.District, customer.District);
            Assert.Equal(dto.IsActive, customer.IsActive);
            Assert.NotEqual(default, customer.UpdatedDate);

            _customerRepositoryMock.Verify(
                x => x.UpdateAsync(customer),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }

        

        [Fact]
        public async Task DeleteAsync_WhenCustomerNotFound_ShouldThrowNotFoundException()
        {
            
            var customerId = Guid.NewGuid();

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync((Customer?)null);

           
            var act = async () =>
                await _customerService.DeleteAsync(customerId);

            
            await Assert.ThrowsAsync<NotFoundException>(act);

            _customerRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Customer>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenValid_ShouldSoftDeleteCustomer()
        {
            
            var customerId = Guid.NewGuid();
            var deletedBy = Guid.NewGuid();

            var customer = new Customer
            {
                Id = customerId,
                FirstName = "Ali",
                LastName = "Kaya",
                IdentityNumber = "33333333333",
                Email = "ali@example.com",
                IsDeleted = false
            };

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

            _customerRepositoryMock
                .Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync(customer);

            _customerRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Customer>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

           
            await _customerService.DeleteAsync(customerId);

            
            Assert.True(customer.IsDeleted);
            Assert.NotNull(customer.DeletedDate);
            Assert.Equal(deletedBy, customer.DeletedBy);

            _customerRepositoryMock.Verify(
                x => x.UpdateAsync(It.Is<Customer>(c =>
                    c.Id == customerId &&
                    c.IsDeleted &&
                    c.DeletedBy == deletedBy &&
                    c.DeletedDate != null)),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }
    }
}