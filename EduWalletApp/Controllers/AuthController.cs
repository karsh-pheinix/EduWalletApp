using EduWalletApp.Data;
using EduWalletApp.DTOs;
using EduWalletApp.Models;
using EduWalletApp.Services;
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
        private readonly IActivityLogger _activityLogger;
        private readonly IEmailNotificationService _emailNotificationService;

        public AuthController(
            UserManager<User> userManager,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AuthController> logger,
            IActivityLogger activityLogger,
            IEmailNotificationService emailNotificationService)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _activityLogger = activityLogger;
            _emailNotificationService = emailNotificationService;
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

            await _userManager.AddToRoleAsync(user, "User");
            await _activityLogger.LogAsync(user.Id, "Registered");
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

            if (user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
                return StatusCode(StatusCodes.Status403Forbidden, "This account has been disabled. Please contact support.");

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                _logger.LogWarning("Failed login attempt for {Email}", dto.Email);
                return Unauthorized("Invalid email or password.");
            }

            _logger.LogInformation("Successful login: {Email} (Id: {UserId})", dto.Email, user.Id);

            var roles = await _userManager.GetRolesAsync(user);
            await _activityLogger.LogAsync(user.Id, "LoggedIn");

            var accessToken = await GenerateAccessToken(user);

            return Ok(new
            {
                accessToken,
                expiresAt = DateTime.UtcNow.AddHours(2),
                onboardingComplete = user.OnboardingComplete,
                isAdmin = roles.Contains("Admin")
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
            {
                _logger.LogWarning("Password reset request for unknown email: {Email}", dto.Email);
                return Ok(new { message = "If an account exists for that email, a reset link has been sent." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var frontendBaseUrl = _configuration["Frontend:ResetPasswordUrl"] ?? "http://localhost:5173/reset-password";
            var resetLink = $"{frontendBaseUrl}?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

            await _emailNotificationService.SendPasswordResetAsync(user.Email!, resetLink);

            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Type = NotificationType.General,
                Message = "A password reset link was sent to your email.",
                AlertKey = $"password-reset:{user.Id}:{DateTimeOffset.UtcNow:O}"
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password reset email queued for {Email}", dto.Email);
            return Ok(new { message = "If an account exists for that email, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return BadRequest(new { message = "Invalid password reset request." });

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Password reset failed for {Email}: {Errors}", dto.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(new { message = "The reset link is invalid or expired." });
            }

            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Type = NotificationType.General,
                Message = "Your password was reset successfully.",
                AlertKey = $"password-reset-success:{user.Id}:{DateTimeOffset.UtcNow:O}"
            });
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successful." });
        }

        private async Task<string> GenerateAccessToken(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

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