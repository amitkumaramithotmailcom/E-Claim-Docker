using EClaim.Domain.DTOs;
using EClaim.Domain.Entities;
using EClaim.Domain.Enums;
using EClaim.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace EClaim.Tests
{
  

    public class AuthServiceTests
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;
        private readonly IConfiguration _config;

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);

            // Mock IConfiguration
            var inMemorySettings = new Dictionary<string, string>{
                { "Jwt:Key", "super_secure_test_key_1234567890123456" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" }
            };
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _authService = new AuthService(_context, _config);
        }

        [Fact]
        public async Task RegisterAsync_ShouldCreateUserAndReturnToken()
        {
            var dto = new RegisterRequestDto
            {
                FullName = "Amit Kumar",
                Email = "Amit@gmail.com",
                Phone = "9876543210",
                Address = "Delhi",
                Password = "test123",
                Role = Role.Claimant
            };

            var result = await _authService.RegisterAsync(dto);

            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.Email.Should().Be(dto.Email);

            var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            savedUser.Should().NotBeNull();
            savedUser.FullName.Should().Be("Amit Kumar");
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowException_WhenUserExists()
        {
            _context.Users.Add(new User
            {
                FullName = "Amit Kumar",
                Email = "Amit@gmail.com",
                Phone = "9876543210",
                Address = "Delhi",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password")
            });
            _context.SaveChanges();

            var dto = new RegisterRequestDto
            {
                Email = "Amit@gmail.com",
                Password = "test123",
                FullName = "Amit Kumar",
                Role = Role.Approver
            };

            Func<Task> act = async () => await _authService.RegisterAsync(dto);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("User already exists.");
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnToken_WhenCredentialsAreValid()
        {
            var user = new User
            {
                Email = "Sumit@gmail.com",
                FullName = "Sumit Kumar",
                Phone = "9876543210",
                Address = "Delhi",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("test123"),
                Role = Role.Adjuster
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var dto = new LoginRequestDto
            {
                Email = "Sumit@gmail.com",
                Password = "test123"
            };

            var result = await _authService.LoginAsync(dto);

            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.Email.Should().Be("Sumit@gmail.com");
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowException_WhenUserNotFound()
        {
            var dto = new LoginRequestDto
            {
                Email = "notfound@gmail.com",
                Password = "test123"
            };

            Func<Task> act = async () => await _authService.LoginAsync(dto);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Invalid credentials.");
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowException_WhenPasswordIsInvalid()
        {
            _context.Users.Add(new User
            {
                Email = "Sumit@gmail.com",
                FullName = "Sumit Kumar",
                Phone = "9876543210",
                Address = "Delhi",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("test123"),
                Role = Role.Adjuster
            });
            await _context.SaveChangesAsync();

            var dto = new LoginRequestDto
            {
                Email = "Sumit@gmail.com",
                Password = "wrong123"
            };

            Func<Task> act = async () => await _authService.LoginAsync(dto);

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Invalid credentials.");
        }

        [Fact]
        public async Task ConfirmEmail_ShouldSetIsEmailVerifiedTrue()
        {
            var user = new User
            {
                Email = "Sumit@gmail.com",
                FullName = "Sumit Kumar",
                Phone = "9876543210",
                Address = "Delhi",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("test123"),
                Role = Role.Adjuster,
                IsEmailVerified = false
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _authService.ConfirmEmail("Sumit@gmail.com", "test-token");

            result.Should().BeTrue();

            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "Sumit@gmail.com");
            updatedUser.IsEmailVerified.Should().BeTrue();
        }

        [Fact]
        public async Task ConfirmEmail_ShouldThrowException_WhenUserNotFound()
        {
            Func<Task> act = async () => await _authService.ConfirmEmail("unknown@gmail.com", "fake-token");

            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Invalid credentials.");
        }
    }

}
