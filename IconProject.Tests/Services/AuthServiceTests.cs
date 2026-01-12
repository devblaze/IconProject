using Shouldly;
using IconProject.Common.Dtos.Requests.Auth;
using IconProject.Configuration;
using IconProject.Database.Models;
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.User.Email.ShouldBe("newuser@example.com");
        result.Value.User.FirstName.ShouldBe("John");
        result.Value.User.LastName.ShouldBe("Doe");
        result.Value.ExpiresIn.ShouldBe(3600); // 60 minutes in seconds

        // Verify user was saved
        var savedUser = context.Users.FirstOrDefault(u => u.Email == "newuser@example.com");
        savedUser.ShouldNotBeNull();
        savedUser!.PasswordHash.ShouldNotBe("password123"); // Should be hashed
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
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("EmailAlreadyExists");
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.User.Email.ShouldBe("user@example.com");

        var savedUser = context.Users.FirstOrDefault();
        savedUser!.Email.ShouldBe("user@example.com");
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.User.FirstName.ShouldBeNull();
        result.Value.User.LastName.ShouldBeNull();
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.User.Email.ShouldBe("login@example.com");
        result.Value.User.FirstName.ShouldBe("Test");
        result.Value.User.LastName.ShouldBe("User");
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
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("InvalidCredentials");
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
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("InvalidCredentials");
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
        result.IsSuccess.ShouldBeTrue();
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
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(user.Id);
        result.Value.Email.ShouldBe(user.Email);
        result.Value.FirstName.ShouldBe(user.FirstName);
        result.Value.LastName.ShouldBe(user.LastName);
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
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldContain("NotFound");
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
        result.IsSuccess.ShouldBeTrue();

        // Token should be a valid JWT (3 parts separated by dots)
        var tokenParts = result.Value.AccessToken.Split('.');
        tokenParts.Length.ShouldBe(3);

        // TokenType should be Bearer
        result.Value.TokenType.ShouldBe("Bearer");
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
        result.IsSuccess.ShouldBeTrue();

        // Token should be a valid JWT
        var tokenParts = result.Value.AccessToken.Split('.');
        tokenParts.Length.ShouldBe(3);
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
        user.PasswordHash.ShouldNotBe("password123");
        user.PasswordHash.ShouldNotBeNullOrEmpty();

        // Password hash should be base64 encoded (contains valid base64 characters)
        var isBase64 = IsValidBase64(user.PasswordHash);
        isBase64.ShouldBeTrue();
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
        user1.PasswordHash.ShouldNotBe(user2.PasswordHash);
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
