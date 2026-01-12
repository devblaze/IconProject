using System.Security.Claims;
using Shouldly;
using IconProject.Common.Dtos;
using IconProject.Common.Dtos.Requests.Auth;
using IconProject.Common.Dtos.Responses.Auth;
using IconProject.Controllers;
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
        var createdResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.ActionName.ShouldBe(nameof(AuthController.GetCurrentUser));
        var returnedResponse = createdResult.Value.ShouldBeOfType<AuthResponse>();
        returnedResponse.AccessToken.ShouldBe("jwt-token");
        returnedResponse.User.Email.ShouldBe("newuser@example.com");
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
        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(409);
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
        var createdResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        var response = createdResult.Value.ShouldBeOfType<AuthResponse>();
        response.TokenType.ShouldBe("Bearer");
        response.ExpiresIn.ShouldBe(3600);
        response.User.Id.ShouldBe(42);
        response.User.FirstName.ShouldBe("Test");
        response.User.LastName.ShouldBe("User");
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
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedResponse = okResult.Value.ShouldBeOfType<AuthResponse>();
        returnedResponse.AccessToken.ShouldBe("jwt-token");
        returnedResponse.User.Email.ShouldBe("user@example.com");
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
        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(401);
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
        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(401);
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
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedUser = okResult.Value.ShouldBeOfType<UserInfo>();
        returnedUser.Id.ShouldBe(1);
        returnedUser.Email.ShouldBe("test@example.com");
        returnedUser.FirstName.ShouldBe("Test");
        returnedUser.LastName.ShouldBe("User");
    }

    [Fact]
    public async Task GetCurrentUser_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        // Default context has no user claims

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        result.Result.ShouldBeOfType<UnauthorizedResult>();
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
        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(404);
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
        result.Result.ShouldBeOfType<UnauthorizedResult>();
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
        registerResult.Result.ShouldBeOfType<CreatedAtActionResult>();
        var loginOk = loginResult.Result.ShouldBeOfType<OkObjectResult>();
        var loginResponse = loginOk.Value.ShouldBeOfType<AuthResponse>();
        loginResponse.AccessToken.ShouldBe("valid-token");
    }

    #endregion
}
