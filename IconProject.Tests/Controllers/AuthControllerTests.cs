using System.Security.Claims;
using FluentAssertions;
using IconProject.Controllers;
using IconProject.Dtos;
using IconProject.Dtos.Auth;
using IconProject.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IconProject.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
        SetupHttpContext();
    }

    private void SetupHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/auth";
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private void SetupAuthenticatedContext(int userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        httpContext.Request.Path = "/api/auth";

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidRequest_ReturnsCreatedWithAuthResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "password123",
            FirstName = "John",
            LastName = "Doe"
        };
        var authResponse = new AuthResponse
        {
            AccessToken = "jwt-token",
            TokenType = "Bearer",
            ExpiresIn = 3600,
            User = new UserInfo
            {
                Id = 1,
                Email = "newuser@example.com",
                FirstName = "John",
                LastName = "Doe"
            }
        };
        _authServiceMock
            .Setup(x => x.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Success(authResponse));

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(AuthController.GetCurrentUser));
        var returnedResponse = createdResult.Value.Should().BeOfType<AuthResponse>().Subject;
        returnedResponse.AccessToken.Should().Be("jwt-token");
        returnedResponse.User.Email.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "password123"
        };
        _authServiceMock
            .Setup(x => x.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Failure(DomainErrors.User.EmailAlreadyExists));

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Register_ServiceReturnsAuthResponse_IncludesAllFields()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            FirstName = "Test",
            LastName = "User"
        };
        var authResponse = new AuthResponse
        {
            AccessToken = "test-token",
            TokenType = "Bearer",
            ExpiresIn = 3600,
            User = new UserInfo
            {
                Id = 42,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            }
        };
        _authServiceMock
            .Setup(x => x.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Success(authResponse));

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<AuthResponse>().Subject;
        response.TokenType.Should().Be("Bearer");
        response.ExpiresIn.Should().Be(3600);
        response.User.Id.Should().Be(42);
        response.User.FirstName.Should().Be("Test");
        response.User.LastName.Should().Be("User");
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithAuthResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "password123"
        };
        var authResponse = new AuthResponse
        {
            AccessToken = "jwt-token",
            TokenType = "Bearer",
            ExpiresIn = 3600,
            User = new UserInfo
            {
                Id = 1,
                Email = "user@example.com"
            }
        };
        _authServiceMock
            .Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Success(authResponse));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        returnedResponse.AccessToken.Should().Be("jwt-token");
        returnedResponse.User.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "user@example.com",
            Password = "wrongpassword"
        };
        _authServiceMock
            .Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Failure(DomainErrors.User.InvalidCredentials));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };
        _authServiceMock
            .Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Failure(DomainErrors.User.InvalidCredentials));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public async Task GetCurrentUser_WithAuthenticatedUser_ReturnsOkWithUserInfo()
    {
        // Arrange
        SetupAuthenticatedContext(1);
        var userInfo = new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        _authServiceMock
            .Setup(x => x.GetCurrentUserAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserInfo>.Success(userInfo));

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUser = okResult.Value.Should().BeOfType<UserInfo>().Subject;
        returnedUser.Id.Should().Be(1);
        returnedUser.Email.Should().Be("test@example.com");
        returnedUser.FirstName.Should().Be("Test");
        returnedUser.LastName.Should().Be("User");
    }

    [Fact]
    public async Task GetCurrentUser_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        // Default context has no user claims

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetCurrentUser_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        SetupAuthenticatedContext(999);
        _authServiceMock
            .Setup(x => x.GetCurrentUserAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserInfo>.Failure(Error.NotFound("User", 999)));

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidClaimFormat_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "not-a-number")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    #endregion

    #region Integration-like Tests

    [Fact]
    public async Task Register_ThenLogin_BothReturnValidTokens()
    {
        // This test verifies the flow of register -> login

        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "flow@example.com",
            Password = "password123"
        };
        var loginRequest = new LoginRequest
        {
            Email = "flow@example.com",
            Password = "password123"
        };

        var authResponse = new AuthResponse
        {
            AccessToken = "valid-token",
            TokenType = "Bearer",
            ExpiresIn = 3600,
            User = new UserInfo { Id = 1, Email = "flow@example.com" }
        };

        _authServiceMock
            .Setup(x => x.RegisterAsync(registerRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Success(authResponse));

        _authServiceMock
            .Setup(x => x.LoginAsync(loginRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Success(authResponse));

        // Act
        var registerResult = await _controller.Register(registerRequest, CancellationToken.None);
        var loginResult = await _controller.Login(loginRequest, CancellationToken.None);

        // Assert
        registerResult.Result.Should().BeOfType<CreatedAtActionResult>();
        var loginOk = loginResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var loginResponse = loginOk.Value.Should().BeOfType<AuthResponse>().Subject;
        loginResponse.AccessToken.Should().Be("valid-token");
    }

    #endregion
}
