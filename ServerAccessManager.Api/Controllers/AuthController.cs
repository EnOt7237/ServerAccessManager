using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ServerAccessManager.Api.Data;
using ServerAccessManager.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace ServerAccessManager.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>();
    private readonly IConfiguration _config;

    public AuthController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(User user)
    {
        if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            return BadRequest("Пользователь уже существует.");

        user.PasswordHash = _passwordHasher.HashPassword(user, user.PasswordHash);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok("Регистрация успешна!");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] User loginData)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == loginData.Username);
        if (user == null) return Unauthorized("Пользователь не найден.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginData.PasswordHash);
        if (result == PasswordVerificationResult.Failed) return Unauthorized("Неверный пароль.");

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("UserId", user.Id.ToString())
    };

        if (user.Role == "Admin")
        {
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
            return Ok(new { message = "Вход выполнен (Cookie установлен)", role = user.Role });
        }
        else
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                role = user.Role
            });
        }
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }
}