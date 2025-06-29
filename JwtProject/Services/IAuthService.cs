using JwtProject.Entities;
using JwtProject.Models;

namespace JwtProject.Services;

public interface IAuthService
{
    Task<User?> RegisterAsync(UserRegisterDto userRegisterDto);
    Task<TokenResposeDto?> LogicAsync(UserLoginDto userLoginDto);
    Task<TokenResposeDto?> RefreshTokensAsync(RefreshTokenRequestDto request);
}