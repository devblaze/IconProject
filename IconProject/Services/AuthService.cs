using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IconProject.Configuration;
using IconProject.Database.Models;
using IconProject.Database.UnitOfWork;
using IconProject.Common.Dtos;
using IconProject.Common.Dtos.Requests.Auth;
using IconProject.Common.Dtos.Responses.Auth;
using IconProject.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IconProject.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork unitOfWork,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }
    
    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var emailExists = await _unitOfWork.Users.ExistsAsync(u => u.Email == request.Email.ToLowerInvariant());
        if (emailExists)
        {
            _logger.LogWarning("Registration attempted with existing email: {Email}", request.Email);
            return DomainErrors.User.EmailAlreadyExists;
        }
        
        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New user registered: {Email} (ID: {UserId})", user.Email, user.Id);
        
        var token = GenerateJwtToken(user);
        var response = CreateAuthResponse(user, token);

        return Result<AuthResponse>.Success(response);
    }
    
    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.Users.FindAsync(u => u.Email == request.Email.ToLowerInvariant());
        var user = users.FirstOrDefault();

        if (user is null)
        {
            _logger.LogWarning("Login attempted for non-existent email: {Email}", request.Email);
            return DomainErrors.User.InvalidCredentials;
        }
        
        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password attempt for user: {Email}", request.Email);
            return DomainErrors.User.InvalidCredentials;
        }

        _logger.LogInformation("User logged in: {Email} (ID: {UserId})", user.Email, user.Id);
        
        var token = GenerateJwtToken(user);
        var response = CreateAuthResponse(user, token);

        return Result<AuthResponse>.Success(response);
    }
    
    public async Task<Result<UserInfo>> GetCurrentUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        if (user is null)
        {
            _logger.LogWarning("GetCurrentUser called for non-existent user ID: {UserId}", userId);
            return Error.NotFound("User", userId);
        }

        return Result<UserInfo>.Success(new UserInfo
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        });
    }

    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private AuthResponse CreateAuthResponse(User user, string token)
    {
        return new AuthResponse
        {
            AccessToken = token,
            ExpiresIn = _jwtSettings.ExpirationInMinutes * 60,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            }
        };
    }

    private static string HashPassword(string password)
    {
        const int iterations = 100000;
        const int saltSize = 16;
        const int hashSize = 32;

        byte[] salt = RandomNumberGenerator.GetBytes(saltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            hashSize);

        byte[] hashBytes = new byte[saltSize + hashSize];
        Array.Copy(salt, 0, hashBytes, 0, saltSize);
        Array.Copy(hash, 0, hashBytes, saltSize, hashSize);

        return Convert.ToBase64String(hashBytes);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        const int iterations = 100000;
        const int saltSize = 16;
        const int hashSize = 32;

        byte[] hashBytes = Convert.FromBase64String(storedHash);

        byte[] salt = new byte[saltSize];
        Array.Copy(hashBytes, 0, salt, 0, saltSize);

        byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            hashSize);

        for (int i = 0; i < hashSize; i++)
        {
            if (hashBytes[saltSize + i] != computedHash[i])
            {
                return false;
            }
        }

        return true;
    }
}
