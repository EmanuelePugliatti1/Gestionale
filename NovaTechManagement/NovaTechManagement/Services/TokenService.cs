using NovaTechManagement.Interfaces;
using NovaTechManagement.Models;
using NovaTechManagement.Data; // Added for ApplicationDbContext
using Microsoft.EntityFrameworkCore; // Added for .Include()
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq; // Added for LINQ methods like .Select
using System.Security.Claims;
using System.Text;

namespace NovaTechManagement.Services
{
    public class TokenService : ITokenService
    {
        private readonly SymmetricSecurityKey _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly ApplicationDbContext _context; // Added DbContext

        public TokenService(IConfiguration config, ApplicationDbContext context) // Modified constructor
        {
            _context = context; // Store context
            var keyString = config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key cannot be null.");
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            _issuer = config["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer cannot be null.");
            _audience = config["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience cannot be null.");
        }

        public string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString())
            };

            if (!string.IsNullOrEmpty(user.FirstName))
                claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
            if (!string.IsNullOrEmpty(user.LastName))
                claims.Add(new Claim(ClaimTypes.Surname, user.LastName));

            // Fetch user roles
            // Note: User object passed in might not have UserRoles loaded if not explicitly included.
            // It's safer to query the context directly.
            // Fetch user roles more explicitly to work around potential In-Memory Include issues
            var userRoleIds = _context.UserRoles
                                     .Where(ur => ur.UserId == user.Id)
                                     .Select(ur => ur.RoleId)
                                     .ToList();
            
            var roleNames = new List<string>();
            if (userRoleIds.Any())
            {
                // Fetch role names based on the collected RoleIds
                roleNames = _context.Roles
                                   .Where(r => userRoleIds.Contains(r.Id))
                                   .Select(r => r.Name) // Role.Name is not nullable string.Empty by default
                                   .ToList();
            }

            foreach (var roleName in roleNames) // roleNames should now be correctly populated
            {
                if (!string.IsNullOrEmpty(roleName))
                {
                    claims.Add(new Claim(ClaimTypes.Role, roleName));
                }
            }

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7), 
                SigningCredentials = creds,
                Issuer = _issuer,
                Audience = _audience
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
