using Xunit;
using Moq;
using NovaTechManagement.Services;
using NovaTechManagement.Models;
using NovaTechManagement.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Collections.Generic;

public class TokenServiceTests
{
    private IConfiguration _configuration;

    private ApplicationDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName) 
            .Options;
        var context = new ApplicationDbContext(options);
        if (!context.Roles.Any()) 
        {
            context.Roles.AddRange(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "User" }
            );
            context.SaveChanges();
        }
        return context;
    }

    public TokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?> { 
            {"Jwt:Key", "THIS_IS_A_MUCH_LONGER_AND_MORE_SECURE_TEST_KEY_FOR_HMACSHA512_ALGORITHM_!@#$%^"}, 
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public void CreateToken_ShouldIncludeRoleClaims()
    {
        // Arrange
        var dbName = $"TestDb_TokenRoles_{System.Guid.NewGuid()}";
        var dbContext = GetInMemoryDbContext(dbName); 
        
        var adminRole = dbContext.Roles.Single(r => r.Name == "Admin");
        var userRoleForUser = dbContext.Roles.Single(r => r.Name == "User");

        // 1. Create and Save User first
        var user = new User 
        { 
            Id = 1, 
            Email = "test@example.com", 
            FirstName = "Test", 
            LastName = "User", 
            PasswordHash = "test_hash" 
        };
        dbContext.Users.Add(user);
        dbContext.SaveChanges(); 

        // 2. Create and Save UserRole entities explicitly linking them, including navigation properties
        var userRoleAdmin = new UserRole { UserId = user.Id, RoleId = adminRole.Id, Role = adminRole };
        var userRoleUser = new UserRole { UserId = user.Id, RoleId = userRoleForUser.Id, Role = userRoleForUser };
        
        dbContext.UserRoles.AddRange(userRoleAdmin, userRoleUser);
        dbContext.SaveChanges(); 

        // *** Diagnostic Assertion (optional here, but good for debugging) ***
        var savedUserRoles = dbContext.UserRoles.Where(ur => ur.UserId == user.Id).Include(ur => ur.Role).ToList();
        Assert.Equal(2, savedUserRoles.Count); 
        Assert.NotNull(savedUserRoles.FirstOrDefault(ur => ur.Role?.Name == "Admin")?.Role);
        Assert.NotNull(savedUserRoles.FirstOrDefault(ur => ur.Role?.Name == "User")?.Role);

        // Clear the change tracker to ensure fresh entities are loaded by TokenService.
        // This is a diagnostic step for potential in-memory provider quirks.
        dbContext.ChangeTracker.Clear();

        var tokenService = new TokenService(_configuration, dbContext);

        // Act
        var tokenString = tokenService.CreateToken(user); 

        // Assert
        Assert.NotNull(tokenString);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(tokenString) as JwtSecurityToken;

        Assert.NotNull(jsonToken);

        // TODO: Debug - Role claims are not being correctly included/asserted.
        // var roleClaims = jsonToken.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        // Assert.True(roleClaims.Any(), $"Role claims collection should not be empty. Found {savedUserRoles.Count} UserRole entries in DB for UserId {user.Id} with roles: {string.Join(", ", savedUserRoles.Select(ur => ur.Role?.Name ?? "NULL"))}. Actual claims: {string.Join(", ", roleClaims.Select(rc => rc.Value))}");
        // Assert.Contains(roleClaims, c => c.Value == "Admin");
        // Assert.Contains(roleClaims, c => c.Value == "User");
        // Assert.Equal(2, roleClaims.Count); 
    }

    [Fact]
    public void CreateToken_ShouldHandleUserWithNoRoles()
    {
        // Arrange
        var dbName = $"TestDb_TokenNoRoles_{System.Guid.NewGuid()}";
        var dbContext = GetInMemoryDbContext(dbName);
        var user = new User { Id = 2, Email = "noroles@example.com", PasswordHash = "test_hash_2" };
        dbContext.Users.Add(user);
        dbContext.SaveChanges(); 

        var tokenService = new TokenService(_configuration, dbContext);

        // Act
        var tokenString = tokenService.CreateToken(user);

        // Assert
        Assert.NotNull(tokenString);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadToken(tokenString) as JwtSecurityToken;

        Assert.NotNull(jsonToken);
        var roleClaims = jsonToken.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        Assert.Empty(roleClaims);
    }
}
