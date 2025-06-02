using Xunit;
using Moq;
using NovaTechManagement.Controllers;
using NovaTechManagement.Data;
using NovaTechManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using NovaTechManagement.DTOs.Role; // For RoleDto
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

public class RolesControllerTests
{
    private ApplicationDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var context = new ApplicationDbContext(options);
        if (!context.Roles.Any()) // Seed roles
        {
            context.Roles.AddRange(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "User" }
            );
            // Seed a test user for assignment tests
            if (!context.Users.Any(u => u.Email == "testadmin@example.com")) {
                // Explicitly setting ID for predictability, ensure it doesn't clash if other tests use same DB name and same ID.
                // Using unique DB names per test method is safer.
                context.Users.Add(new User { Id = 100, Email = "testadmin@example.com", PasswordHash="some_secure_hash_placeholder_123"});
            }
             if (!context.Users.Any(u => u.Email == "testuser@example.com")) {
                context.Users.Add(new User { Id = 101, Email = "testuser@example.com", PasswordHash="another_secure_hash_placeholder_456"});
            }
            context.SaveChanges();
        }
        return context;
    }

    private RolesController CreateController(ApplicationDbContext dbContext, string role = "Admin") {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, "Test Admin User"),
            new Claim(ClaimTypes.NameIdentifier, "100"), // Assuming user ID 100 is an admin for these tests
            new Claim(ClaimTypes.Role, role), 
        }, "mock"));

        var controller = new RolesController(dbContext)
        {
            ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetRoles_ShouldReturnListOfRoles()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext($"TestDb_GetRoles_{System.Guid.NewGuid()}");
        var controller = CreateController(dbContext);

        // Act
        var result = await controller.GetRoles();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var roles = Assert.IsAssignableFrom<IEnumerable<RoleDto>>(okResult.Value);
        Assert.Equal(2, roles.Count());
    }

    [Fact]
    public async Task AssignRoleToUser_AdminRole_ShouldSucceed()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext($"TestDb_AssignRole_{System.Guid.NewGuid()}");
        var controller = CreateController(dbContext, "Admin"); 
        int testUserId = 101; // testuser@example.com (ID 101)
        int adminRoleId = 1; // Admin Role ID

        // Act
        var result = await controller.AssignRoleToUser(testUserId, adminRoleId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var userRoleExists = await dbContext.UserRoles.AnyAsync(ur => ur.UserId == testUserId && ur.RoleId == adminRoleId);
        Assert.True(userRoleExists, "UserRole entry was not created.");
    }


    [Fact]
    public async Task RevokeRoleFromUser_AdminRole_ShouldSucceed()
    {
        // Arrange
        var dbContext = GetInMemoryDbContext($"TestDb_RevokeRole_{System.Guid.NewGuid()}");
        var controller = CreateController(dbContext, "Admin");
        int testUserId = 101; // testuser@example.com (ID 101)
        int userRoleId = 2; // User Role ID (seeded as RoleId=2)
        
        // Pre-assign the role
        dbContext.UserRoles.Add(new UserRole { UserId = testUserId, RoleId = userRoleId });
        await dbContext.SaveChangesAsync();
        Assert.True(await dbContext.UserRoles.AnyAsync(ur => ur.UserId == testUserId && ur.RoleId == userRoleId), "Setup: UserRole was not pre-assigned.");


        // Act
        var result = await controller.RevokeRoleFromUser(testUserId, userRoleId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var userRoleExists = await dbContext.UserRoles.AnyAsync(ur => ur.UserId == testUserId && ur.RoleId == userRoleId);
        Assert.False(userRoleExists, "UserRole entry was not revoked.");
    }
}
