using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JwtProject.Data;
using JwtProject.Entities;
using JwtProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace JwtProject.Services;

public class AuthService : IAuthService
{
    private readonly UserDbContext _userDbContext;
    private readonly IConfiguration _configuration;
    
    public AuthService(
        UserDbContext userDbContext, 
        IConfiguration configuration)
    {
        _userDbContext = userDbContext;
        _configuration = configuration;
    }
    public async Task<User?> RegisterAsync(UserRegisterDto userRegisterDto)
    {
        if (await _userDbContext.Users.AnyAsync(u => u.Username == userRegisterDto.Username))
        {
            return null;
        }
        
        var user = new User();
        
        var hashedPassword = new PasswordHasher<User>()
            .HashPassword(user, userRegisterDto.Password);
        user.Username = userRegisterDto.Username;
        user.Role = userRegisterDto.Role;
        user.PasswordHash = hashedPassword;
        
        _userDbContext.Users.Add(user);
        await _userDbContext.SaveChangesAsync(); 
        
        return user;
    }

    public async Task<TokenResposeDto?> LogicAsync(UserLoginDto userLoginDto)
    {
        var user = await _userDbContext.Users.FirstOrDefaultAsync(u => u.Username == userLoginDto.Username);
        if (user == null)
            return null;

        if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, userLoginDto.Password)
            == PasswordVerificationResult.Failed)
        {
            return null;
        }
        
        return await CreateTokenRespose(user);
    }

    private async Task<TokenResposeDto> CreateTokenRespose(User? user)
    {
        var response = new TokenResposeDto(
            CreateToken(user), 
            await GenerateAndSaveRefreshTokenAsync(user));
        return response;
    }

    public async Task<TokenResposeDto?> RefreshTokensAsync(RefreshTokenRequestDto request)
    {
        var user = await ValidateRefreshTokenAsync(request.UserId, request.RefreshToken);

        return await CreateTokenRespose(user);
    }

    private async Task<User?> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
    {
        var user = await _userDbContext.Users.FindAsync(userId);
        if (user is null
            || user.RefreshToken != refreshToken
            || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return null;
        
        return user;
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
    {
        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(30);
        await _userDbContext.SaveChangesAsync();
        return refreshToken;
    }
    
    private string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new (ClaimTypes.Name, user.Username),
            new (ClaimTypes.NameIdentifier, user.Id.ToString()),
            new (ClaimTypes.Role, user.Role)
        };
        
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration.GetValue<string>("AppSettings:Token")!));
        
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _configuration.GetValue<string>("AppSettings:Issuer"),
            audience: _configuration.GetValue<string>("AppSettings:Audience"),
            claims: claims,
            expires: DateTime.Now.AddMinutes(3),
            signingCredentials: creds);
        
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}