using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaTechManagement.Data;
using NovaTechManagement.Models; // For Role model
using NovaTechManagement.DTOs.Role; // For RoleDto
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NovaTechManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // This entire controller is Admin-only
    public class RolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RolesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/roles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            var roles = await _context.Roles
                                      .Select(r => new RoleDto { Id = r.Id, Name = r.Name })
                                      .ToListAsync();
            return Ok(roles);
        }

        // POST: api/users/{userId}/roles/{roleId} - Assign role to user
        [HttpPost("/api/users/{userId}/roles/{roleId}")]
        public async Task<IActionResult> AssignRoleToUser(int userId, int roleId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId);

            if (!userExists || !roleExists)
            {
                return NotFound(new { Message = "User or Role not found." });
            }

            var userHasRole = await _context.UserRoles
                                            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (userHasRole)
            {
                return BadRequest(new { Message = "User already has this role." });
            }

            // Ensure the UserRoles collection on the user is loaded or initialized if needed,
            // though EF Core should handle the join table directly.
            // var user = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == userId);
            // if (user == null) return NotFound(new { Message = "User not found." });
            // user.UserRoles.Add(new UserRole { RoleId = roleId });
            // await _context.SaveChangesAsync();
            // The above is an alternative way; direct manipulation of UserRoles join table is often cleaner for this.

            _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Role assigned successfully." });
        }

        // DELETE: api/users/{userId}/roles/{roleId} - Revoke role from user
        [HttpDelete("/api/users/{userId}/roles/{roleId}")]
        public async Task<IActionResult> RevokeRoleFromUser(int userId, int roleId)
        {
            var userRole = await _context.UserRoles
                                         .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            if (userRole == null)
            {
                return NotFound(new { Message = "User does not have this role or invalid user/role." });
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Role revoked successfully." });
        }
    }
}
