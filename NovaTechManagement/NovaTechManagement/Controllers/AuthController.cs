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
            return Ok(new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            });
        }
    }
}
