using EduWalletApp.Data;
using EduWalletApp.DTOs;
using EduWalletApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EduWalletApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<User> userManager,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Register([FromBody] RegisterStudentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                StudentIdNumber = dto.StudentIdNumber,
                University = dto.University,
                YearOfStudy = dto.YearOfStudy,
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth,
                City = dto.City
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Registration failed for {Email}: {Errors}",
                    dto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(result.Errors);
            }

            _logger.LogInformation("New user registered: {Email} (Id: {UserId})", dto.Email, user.Id);
            return Ok(new { message = "Registration successful", userId = user.Id });
        }

        [HttpPost("login")]
        [EnableRateLimiting("AuthPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent email: {Email}", dto.Email);
                return Unauthorized("Invalid email or password.");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                _logger.LogWarning("Failed login attempt for {Email}", dto.Email);
                return Unauthorized("Invalid email or password.");
            }

            _logger.LogInformation("Successful login: {Email} (Id: {UserId})", dto.Email, user.Id);

            var accessToken = GenerateAccessToken(user);

            return Ok(new
            {
                accessToken,
                expiresAt = DateTime.UtcNow.AddHours(2),
                onboardingComplete = user.OnboardingComplete
            });
        }

        private string GenerateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}