using System.Security.Claims;
using Kasko.Business.DTOs.User;
using Kasko.Business.Exceptions;
using Kasko.Business.Security;
using Kasko.Business.Services;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Kasko.Business.Tests.Services
{
    public class UserServicesTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<IRoleRepository> _roleRepositoryMock = null!;
        private Mock<IPasswordHasher<User>> _passwordHasherMock = null!;
        private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;

        private PasswordHasherService _passwordHasherService = null!;
        private UserService _userService = null!;

        public UserServicesTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _roleRepositoryMock = new Mock<IRoleRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher<User>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            _unitOfWorkMock
                .Setup(x => x.Users)
                .Returns(_userRepositoryMock.Object);

            _unitOfWorkMock
                .Setup(x => x.Roles)
                .Returns(_roleRepositoryMock.Object);

            _passwordHasherService = new PasswordHasherService(
                _passwordHasherMock.Object);

            _userService = new UserService(
                _unitOfWorkMock.Object,
                _passwordHasherService,
                _httpContextAccessorMock.Object);
        }

       

        [Fact]
        public async Task GetAllAsync_ShouldReturnMappedUserListDtos()
        {
            
            var user1 = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Ahmet",
                LastName = "Yılmaz",
                Email = "ahmet@example.com",
                IsActive = true,
                IsDeleted = false
            };

            var user2 = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Mehmet",
                LastName = "Demir",
                Email = "mehmet@example.com",
                IsActive = false,
                IsDeleted = false
            };

            _userRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<User>
                {
                    user1,
                    user2
                });

           
            var result = (await _userService.GetAllAsync()).ToList();

           
            Assert.Equal(2, result.Count);

            Assert.Equal(user1.Id, result[0].Id);
            Assert.Equal(user1.FirstName, result[0].FirstName);
            Assert.Equal(user1.LastName, result[0].LastName);
            Assert.Equal(user1.Email, result[0].Email);
            Assert.Equal(user1.IsActive, result[0].IsActive);

            Assert.Equal(user2.Id, result[1].Id);
            Assert.Equal(user2.FirstName, result[1].FirstName);
            Assert.Equal(user2.LastName, result[1].LastName);
            Assert.Equal(user2.Email, result[1].Email);
            Assert.Equal(user2.IsActive, result[1].IsActive);
        }

       

        [Fact]
        public async Task GetByIdAsync_WhenUserNotFound_ShouldReturnNull()
        {
           
            var userId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

         
            var result = await _userService.GetByIdAsync(userId);

           
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedUserDto()
        {
           
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                FirstName = "Hasan",
                LastName = "Çelik",
                Email = "hasan@example.com",
                IsActive = true,
                IsDeleted = false,
                RoleId = roleId
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

          
            var result = await _userService.GetByIdAsync(userId);

           
            Assert.NotNull(result);

            Assert.Equal(user.Id, result!.Id);
            Assert.Equal(user.FirstName, result.FirstName);
            Assert.Equal(user.LastName, result.LastName);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.IsActive, result.IsActive);
        }

      

        [Fact]
        public async Task CreateAsync_WhenEmailExists_ShouldThrowBadRequestException()
        {
            
            var dto = new CreateUserDto
            {
                FirstName = "Ahmet",
                LastName = "Yılmaz",
                Email = "ahmet@example.com",
                Password = "Password123!",
                PhoneNumber = "5551112233",
                RoleId = Guid.NewGuid()
            };

            _userRepositoryMock
                .Setup(x => x.AnyAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync(true);

            
            var act = async () =>
                await _userService.CreateAsync(dto);

            
            await Assert.ThrowsAsync<BadRequestException>(act);

            _roleRepositoryMock.Verify(
                x => x.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _userRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<User>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenRoleNotFound_ShouldThrowNotFoundException()
        {
           
            var dto = new CreateUserDto
            {
                FirstName = "Mehmet",
                LastName = "Demir",
                Email = "mehmet@example.com",
                Password = "Password123!",
                PhoneNumber = "5552223344",
                RoleId = Guid.NewGuid()
            };

            _userRepositoryMock
                .Setup(x => x.AnyAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync(false);

            _roleRepositoryMock
                .Setup(x => x.GetByIdAsync(dto.RoleId))
                .ReturnsAsync((Role?)null);

          
            var act = async () =>
                await _userService.CreateAsync(dto);

            
            await Assert.ThrowsAsync<NotFoundException>(act);

            _userRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<User>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenValid_ShouldCreateUserAndSave()
        {
            
            var roleId = Guid.NewGuid();
            var generatedHash = "HASHED_PASSWORD_VALUE";

            var dto = new CreateUserDto
            {
                FirstName = "Ali",
                LastName = "Kaya",
                Email = "ali@example.com",
                Password = "Password123!",
                PhoneNumber = "5553334455",
                RoleId = roleId
            };

            var role = new Role
            {
                Id = roleId
            };

            _userRepositoryMock
                .Setup(x => x.AnyAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync(false);

            _roleRepositoryMock
                .Setup(x => x.GetByIdAsync(roleId))
                .ReturnsAsync(role);

            _passwordHasherMock
                .Setup(x => x.HashPassword(
                    It.IsAny<User>(),
                    dto.Password))
                .Returns(generatedHash);

            _userRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            
            await _userService.CreateAsync(dto);

           
            _passwordHasherMock.Verify(
                x => x.HashPassword(
                    It.IsAny<User>(),
                    dto.Password),
                Times.Once);

            _userRepositoryMock.Verify(
                x => x.AddAsync(It.Is<User>(u =>
                    u.FirstName == dto.FirstName &&
                    u.LastName == dto.LastName &&
                    u.Email == dto.Email &&
                    u.PhoneNumber == dto.PhoneNumber &&
                    u.RoleId == dto.RoleId &&
                    u.IsActive &&
                    !u.IsDeleted &&
                    u.CreatedDate != default &&
                    u.PasswordHash == generatedHash)),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldGeneratePasswordHash()
        {
            
            var roleId = Guid.NewGuid();

            var dto = new CreateUserDto
            {
                FirstName = "Veli",
                LastName = "Şahin",
                Email = "veli@example.com",
                Password = "Password123!",
                RoleId = roleId
            };

            var role = new Role
            {
                Id = roleId
            };

            _userRepositoryMock
                .Setup(x => x.AnyAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync(false);

            _roleRepositoryMock
                .Setup(x => x.GetByIdAsync(roleId))
                .ReturnsAsync(role);

            _passwordHasherMock
                .Setup(x => x.HashPassword(
                    It.IsAny<User>(),
                    dto.Password))
                .Returns("HASHED_VALUE");

            _userRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .Callback<User>(user =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
                })
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

         
            await _userService.CreateAsync(dto);

          
            _passwordHasherMock.Verify(
                x => x.HashPassword(
                    It.IsAny<User>(),
                    dto.Password),
                Times.Once);
        }

     

        [Fact]
        public async Task UpdateAsync_WhenUserNotFound_ShouldThrowNotFoundException()
        {
            
            var userId = Guid.NewGuid();

            var dto = new UpdateUserDto
            {
                Id = userId,
                FirstName = "Ahmet",
                LastName = "Yılmaz",
                Email = "ahmet@example.com",
                IsActive = true,
                RoleId = Guid.NewGuid()
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            
            var act = async () =>
                await _userService.UpdateAsync(dto);

         
            await Assert.ThrowsAsync<NotFoundException>(act);

            _userRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<User>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenEmailExistsForAnotherUser_ShouldThrowBadRequestException()
        {
         
            var userId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                FirstName = "Eski",
                LastName = "Kullanıcı",
                Email = "old@example.com"
            };

            var dto = new UpdateUserDto
            {
                Id = userId,
                FirstName = "Yeni",
                LastName = "Kullanıcı",
                Email = "new@example.com",
                IsActive = true,
                RoleId = Guid.NewGuid()
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _userRepositoryMock
                .Setup(x => x.AnyAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync(true);

            
            var act = async () =>
                await _userService.UpdateAsync(dto);

           
            await Assert.ThrowsAsync<BadRequestException>(act);

            _roleRepositoryMock.Verify(
                x => x.GetByIdAsync(It.IsAny<Guid>()),
                Times.Never);

            _userRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<User>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenRoleNotFound_ShouldThrowNotFoundException()
        {
            
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                Email = "old@example.com"
            };

            var dto = new UpdateUserDto
            {
                Id = userId,
                FirstName = "Yeni",
                LastName = "Kullanıcı",
                Email = "new@example.com",
                IsActive = true,
                RoleId = roleId
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _userRepositoryMock
                .Setup(x => x.AnyAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync(false);

            _roleRepositoryMock
                .Setup(x => x.GetByIdAsync(roleId))
                .ReturnsAsync((Role?)null);

         
            var act = async () =>
                await _userService.UpdateAsync(dto);

            
            await Assert.ThrowsAsync<NotFoundException>(act);

            _userRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<User>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenValid_ShouldUpdateUserAndSave()
        {
            
            var userId = Guid.NewGuid();
            var roleId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                FirstName = "Eski",
                LastName = "İsim",
                Email = "old@example.com",
                PhoneNumber = "5551112233",
                RoleId = Guid.NewGuid(),
                IsActive = true
            };

            var role = new Role
            {
                Id = roleId
            };

            var dto = new UpdateUserDto
            {
                Id = userId,
                FirstName = "Yeni",
                LastName = "İsim",
                Email = "new@example.com",
                PhoneNumber = "5559998877",
                IsActive = false,
                RoleId = roleId
            };

            _userRepositoryMock
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _userRepositoryMock
                .Setup(x => x.AnyAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
                .ReturnsAsync(false);

            _roleRepositoryMock
                .Setup(x => x.GetByIdAsync(roleId))
                .ReturnsAsync(role);

            _userRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            
            await _userService.UpdateAsync(dto);

            
            Assert.Equal(dto.FirstName, user.FirstName);
            Assert.Equal(dto.LastName, user.LastName);
            Assert.Equal(dto.Email, user.Email);
            Assert.Equal(dto.PhoneNumber, user.PhoneNumber);
            Assert.Equal(dto.RoleId, user.RoleId);
            Assert.Equal(dto.IsActive, user.IsActive);
            Assert.NotEqual(default, user.UpdatedDate);

            _userRepositoryMock.Verify(
                x => x.UpdateAsync(user),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }

        

        [Fact]
        public async Task DeleteAsync_WhenUserNotFound_ShouldThrowNotFoundException()
        {
           
            var userId = Guid.NewGuid();

            _userRepositoryMock
                .Setup(x => x.DeleteUserAsync(
                    userId,
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            
            var act = async () =>
                await _userService.DeleteAsync(userId);

            
            await Assert.ThrowsAsync<NotFoundException>(act);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenValid_ShouldDeleteUserAndSave()
        {
            
            var userId = Guid.NewGuid();
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

            _userRepositoryMock
                .Setup(x => x.DeleteUserAsync(
                    userId,
                    deletedBy))
                .ReturnsAsync(true);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            
            await _userService.DeleteAsync(userId);

            
            _userRepositoryMock.Verify(
                x => x.DeleteUserAsync(
                    userId,
                    deletedBy),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }
    }
}