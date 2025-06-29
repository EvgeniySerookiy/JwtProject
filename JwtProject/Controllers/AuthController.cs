using JwtProject.Entities;
using JwtProject.Models.Token; 
using JwtProject.Models.User; 
using JwtProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtProject.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("register")]
    public async Task<ActionResult<User>> Register(UserRegisterDto userRegisterDto)
    {
        var user = await _authService.RegisterAsync(userRegisterDto);
        if(user is null)
            return BadRequest("Username already exists");
        
        return Ok(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResposeDto>> Login(UserLoginDto userLoginDto)
    {
        var result = await _authService.LogicAsync(userLoginDto);
        if(result is null)
            return BadRequest("Username or password is incorrect");
        
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResposeDto>> RefreshToken(
        RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshTokensAsync(request);
        if(result is null || result.AccessToken is null || result.RefreshToken is null)
            return Unauthorized("Invalid refresh token");
        
        return Ok(result);
    }

    [Authorize]
    [HttpGet]
    public IActionResult AuthenticatedOnlyEndpoint()
    {
        return Ok("You are authenticated");
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnlyEndpoint()
    {
        return Ok("You are and admin");
    }
}