using EClaim.Domain.DTOs;
using EClaim.Domain.Entities;
using EClaim.Domain.Enums;
using EClaim.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EClaim.Tests
{

    public class UserServiceTests
    {
        private readonly AppDbContext _context;
        private readonly UserService _service;

        public UserServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            var mockConfig = new Mock<IConfiguration>();
            _service = new UserService(_context, mockConfig.Object);

            SeedUsers();
        }

        private void SeedUsers()
        {
            _context.Users.AddRange(
                new User
                {
                    Id = 1,
                    FullName = "Amit Kumar",
                    Email = "amit@gmail.com",
                    Phone = "9876543210",
                    Address = "Delhi",
                    Role = Role.Claimant,
                    PasswordHash = "",
                    IsEmailVerified = true
                },
                new User
                {
                    Id = 2,
                    FullName = "Sumit Kumar",
                    Email = "Sumit@gmail.com",
                    Phone = "1234567890",
                    Address = "Mumbai",
                    Role = Role.Approver,
                    PasswordHash = "",
                    IsEmailVerified = false
                });

            _context.SaveChanges();
        }

        [Fact]
        public async Task GetUser_ShouldReturnUser_WhenUserExists()
        {
            var result = await _service.GetUser(1);

            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.FullName.Should().Be("Amit Kumar");
        }

        [Fact]
        public async Task GetUser_ShouldReturnNull_WhenUserDoesNotExist()
        {
            var result = await _service.GetUser(99);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllUser_ShouldFilterByFullName()
        {
            var dto = new UserSearchDto { FullName = "Sumit" };

            var result = await _service.GetAllUser(dto);

            result.Should().HaveCount(1);
            result.First().FullName.Should().Be("Sumit Kumar");
        }

        [Fact]
        public async Task GetAllUser_ShouldFilterByRole()
        {
            var dto = new UserSearchDto { Role = Role.Claimant };

            var result = await _service.GetAllUser(dto);

            result.Should().OnlyContain(u => u.Role == Role.Claimant);
        }

        [Fact]
        public async Task GetAllUser_ShouldFilterByEmailVerification()
        {
            var dto = new UserSearchDto { IsEmailVerified = true };

            var result = await _service.GetAllUser(dto);

            result.Should().OnlyContain(u => u.IsEmailVerified);
        }

        [Fact]
        public async Task EditAsync_ShouldUpdateUser_WhenUserExists()
        {
            var dto = new UserSearchDto
            {
                Id = 1,
                FullName = "Amit Kumar Singh",
                Email = "amit.kumar@gmail.com",
                Phone = "9999999999",
                Address = "Noida",
                Role = Role.Adjuster,
                IsEmailVerified = false
            };

            var updatedUser = await _service.EditAsync(dto);

            updatedUser.FullName.Should().Be("Amit Kumar Singh");
            updatedUser.Email.Should().Be("amit.kumar@gmail.com");
            updatedUser.Role.Should().Be(Role.Adjuster);
            updatedUser.IsEmailVerified.Should().BeFalse();
        }

        [Fact]
        public async Task EditAsync_ShouldThrowException_WhenUserNotFound()
        {
            var dto = new UserSearchDto { Id = 999 };

            Func<Task> act = async () => await _service.EditAsync(dto);

            await act.Should().ThrowAsync<ApplicationException>()
                .WithMessage("User not exists");
        }
    }


}
