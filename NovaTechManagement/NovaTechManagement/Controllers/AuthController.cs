using Microsoft.AspNetCore.Mvc;
using NovaTechManagement.DTOs.Auth;
using NovaTechManagement.Interfaces;
using NovaTechManagement.Data; // For ApplicationDbContext
using NovaTechManagement.Models; // For User model
using Microsoft.EntityFrameworkCore; // For ToListAsync, AnyAsync, FirstOrDefaultAsync etc.
using System.Threading.Tasks; // For Task

namespace NovaTechManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;

        public AuthController(ApplicationDbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto registerRequestDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == registerRequestDto.Email))
            {
                return BadRequest(new { Message = "Email already exists." });
            }

            var user = new User
            {
                Email = registerRequestDto.Email,
                FirstName = registerRequestDto.FirstName,
                LastName = registerRequestDto.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerRequestDto.Password), // Hash password
                RegistrationDate = DateTime.UtcNow
            };

            // Assign default "User" role
            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            if (defaultRole != null)
            {
                // Initialize UserRoles collection if it's null (though it's initialized in the model)
                user.UserRoles ??= new List<UserRole>();
                user.UserRoles.Add(new UserRole { RoleId = defaultRole.Id }); // UserId will be set by EF Core relationship fixup
            }
            else
            {
                // This case should ideally not happen if roles are seeded correctly.
                Console.WriteLine("CRITICAL ERROR: Default 'User' role not found during registration. User will not have a role.");
                // Depending on policy, you might want to prevent registration or log this as a severe issue.
                // For now, we'll proceed with user creation but without the default role.
                // return StatusCode(500, new { Message = "Error assigning default role. Please contact support." });
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Optionally, log the user in directly and return a token, or just confirm registration
            // For now, confirming registration. A separate login call would be typical.
            return Ok(new { Message = "User registered successfully." });
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequestDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginRequestDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequestDto.Password, user.PasswordHash))
            {
                return Unauthorized(new { Message = "Invalid credentials." });
            }

            var token = _tokenService.CreateToken(user);

            // Fetch user roles to include in the response
            var userRoles = await _context.UserRoles
                                          .Where(ur => ur.UserId == user.Id)
                                          .Include(ur => ur.Role) // Ensure Role information is loaded
                                          .Select(ur => ur.Role.Name)
                                          .ToListAsync();

            return Ok(new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = userRoles // Add roles to the DTO
            });
        }

        // POST api/auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            if (forgotPasswordDto == null || string.IsNullOrWhiteSpace(forgotPasswordDto.Email))
            {
                return BadRequest(new { Message = "Email is required." });
            }

            // For now, simulate success. Actual implementation would involve:
            // 1. Check if user with email exists.
            // 2. Generate a password reset token.
            // 3. Save token with expiry to user record or separate table.
            // 4. Send an email with a reset link (out of scope for this agent).
            Console.WriteLine($"Password reset requested for: {forgotPasswordDto.Email}");
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == forgotPasswordDto.Email);
            if (user == null)
            {
                 // To prevent email enumeration, it's common to return a generic success message 
                 // regardless of whether the user exists or not.
                 return Ok(new { Message = "If your email address is registered with us, you will receive instructions to reset your password." });
            }

            // Placeholder for actual reset logic
            return Ok(new { Message = "If your email address is registered with us, you will receive instructions to reset your password." });
        }
    }
}
