using FluentAssertions;
using IconProject.Configuration;
using IconProject.Database.Models;
using IconProject.Dtos.Auth;
using IconProject.Services;
using IconProject.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IconProject.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly IOptions<JwtSettings> _jwtSettings;

    public AuthServiceTests()
    {
        _loggerMock = new Mock<ILogger<AuthService>>();
        _jwtSettings = Options.Create(new JwtSettings
        {
            SecretKey = "ThisIsAVeryLongSecretKeyForTestingPurposes123!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationInMinutes = 60
        });
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_WithValidRequest_CreatesUserAndReturnsToken()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.User.Email.Should().Be("newuser@example.com");
        result.Value.User.FirstName.Should().Be("John");
        result.Value.User.LastName.Should().Be("Doe");
        result.Value.ExpiresIn.Should().Be(3600); // 60 minutes in seconds

        // Verify user was saved
        var savedUser = context.Users.FirstOrDefault(u => u.Email == "newuser@example.com");
        savedUser.Should().NotBeNull();
        savedUser!.PasswordHash.Should().NotBe("password123"); // Should be hashed
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsEmailAlreadyExistsError()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        await MockUnitOfWorkFactory.SeedTestUserAsync(context, "existing@example.com");
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "password123"
        };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("EmailAlreadyExists");
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmailToLowercase()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);
        var request = new RegisterRequest
        {
            Email = "USER@EXAMPLE.COM",
            Password = "password123"
        };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.User.Email.Should().Be("user@example.com");

        var savedUser = context.Users.FirstOrDefault();
        savedUser!.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task RegisterAsync_WithoutOptionalFields_CreatesUserSuccessfully()
    {
        // Arrange
        var (unitOfWork, _) = MockUnitOfWorkFactory.Create();
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);
        var request = new RegisterRequest
        {
            Email = "minimal@example.com",
            Password = "password123"
        };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.User.FirstName.Should().BeNull();
        result.Value.User.LastName.Should().BeNull();
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);

        // First register a user
        var registerRequest = new RegisterRequest
        {
            Email = "login@example.com",
            Password = "password123",
            FirstName = "Test",
            LastName = "User"
        };
        await service.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "login@example.com",
            Password = "password123"
        };

        // Act
        var result = await service.LoginAsync(loginRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.User.Email.Should().Be("login@example.com");
        result.Value.User.FirstName.Should().Be("Test");
        result.Value.User.LastName.Should().Be("User");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ReturnsInvalidCredentialsError()
    {
        // Arrange
        var (unitOfWork, _) = MockUnitOfWorkFactory.Create();
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidCredentials");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsInvalidCredentialsError()
    {
        // Arrange
        var (unitOfWork, _) = MockUnitOfWorkFactory.Create();
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);

        // First register a user
        await service.RegisterAsync(new RegisterRequest
        {
            Email = "user@example.com",
            Password = "correctpassword"
        });

        var loginRequest = new LoginRequest
        {
            Email = "user@example.com",
            Password = "wrongpassword"
        };

        // Act
        var result = await service.LoginAsync(loginRequest);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("InvalidCredentials");
    }

    [Fact]
    public async Task LoginAsync_IsCaseInsensitiveForEmail()
    {
        // Arrange
        var (unitOfWork, _) = MockUnitOfWorkFactory.Create();
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);

        // Register with lowercase
        await service.RegisterAsync(new RegisterRequest
        {
            Email = "user@example.com",
            Password = "password123"
        });

        // Login with uppercase
        var loginRequest = new LoginRequest
        {
            Email = "USER@EXAMPLE.COM",
            Password = "password123"
        };

        // Act
        var result = await service.LoginAsync(loginRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region GetCurrentUserAsync Tests

    [Fact]
    public async Task GetCurrentUserAsync_WithExistingUser_ReturnsUserInfo()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var user = await MockUnitOfWorkFactory.SeedTestUserAsync(context);
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);

        // Act
        var result = await service.GetCurrentUserAsync(user.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
        result.Value.FirstName.Should().Be(user.FirstName);
        result.Value.LastName.Should().Be(user.LastName);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithNonExistentUser_ReturnsNotFoundError()
    {
        // Arrange
        var (unitOfWork, _) = MockUnitOfWorkFactory.Create();
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);

        // Act
        var result = await service.GetCurrentUserAsync(999);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region JWT Token Tests

    [Fact]
    public async Task RegisterAsync_GeneratesValidJwtToken()
    {
        // Arrange
        var (unitOfWork, _) = MockUnitOfWorkFactory.Create();
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);
        var request = new RegisterRequest
        {
            Email = "jwt@example.com",
            Password = "password123"
        };

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Token should be a valid JWT (3 parts separated by dots)
        var tokenParts = result.Value.AccessToken.Split('.');
        tokenParts.Should().HaveCount(3);

        // TokenType should be Bearer
        result.Value.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task LoginAsync_GeneratesValidJwtToken()
    {
        // Arrange
        var (unitOfWork, _) = MockUnitOfWorkFactory.Create();
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "jwt@example.com",
            Password = "password123"
        });

        var loginRequest = new LoginRequest
        {
            Email = "jwt@example.com",
            Password = "password123"
        };

        // Act
        var result = await service.LoginAsync(loginRequest);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Token should be a valid JWT
        var tokenParts = result.Value.AccessToken.Split('.');
        tokenParts.Should().HaveCount(3);
    }

    #endregion

    #region Password Hashing Tests

    [Fact]
    public async Task RegisterAsync_HashesPasswordCorrectly()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);
        var request = new RegisterRequest
        {
            Email = "hash@example.com",
            Password = "password123"
        };

        // Act
        await service.RegisterAsync(request);

        // Assert
        var user = context.Users.First(u => u.Email == "hash@example.com");
        user.PasswordHash.Should().NotBe("password123");
        user.PasswordHash.Should().NotBeNullOrEmpty();

        // Password hash should be base64 encoded (contains valid base64 characters)
        var isBase64 = IsValidBase64(user.PasswordHash);
        isBase64.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_SamePasswordProducesDifferentHashes()
    {
        // Arrange
        var (unitOfWork, context) = MockUnitOfWorkFactory.Create();
        var service = new AuthService(unitOfWork, _jwtSettings, _loggerMock.Object);

        // Act
        await service.RegisterAsync(new RegisterRequest
        {
            Email = "user1@example.com",
            Password = "samepassword"
        });

        await service.RegisterAsync(new RegisterRequest
        {
            Email = "user2@example.com",
            Password = "samepassword"
        });

        // Assert
        var user1 = context.Users.First(u => u.Email == "user1@example.com");
        var user2 = context.Users.First(u => u.Email == "user2@example.com");

        // Due to random salt, same password should produce different hashes
        user1.PasswordHash.Should().NotBe(user2.PasswordHash);
    }

    private static bool IsValidBase64(string value)
    {
        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
