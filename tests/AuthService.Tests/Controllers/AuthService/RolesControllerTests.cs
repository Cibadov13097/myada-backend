using AuthService.API.Controllers;
using AuthService.Application.DTOs.Roles;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Identity;
using AuthService.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthService.Tests.Controllers;

public class RolesControllerTests
{
    [Fact]
    public async Task Create_ShouldNormalizeRoleNameAndReturnCreatedRole()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.Create(new CreateRoleDto { Name = "  Club_Moderator  " }, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var payload = created.Value.Should().BeOfType<RoleDto>().Subject;
        payload.Name.Should().Be("club_moderator");
        payload.NormalizedName.Should().Be("CLUB_MODERATOR");
        payload.UserCount.Should().Be(0);

        var persistedRole = await dbContext.Roles.SingleAsync(role => role.Name == "club_moderator");
        persistedRole.NormalizedName.Should().Be("CLUB_MODERATOR");
    }

    [Fact]
    public async Task Delete_ShouldRejectRolesThatAreAssignedToUsers()
    {
        await using var dbContext = CreateDbContext();
        var role = new IdentityRole("helpdesk_lead");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "support.user",
            NormalizedUserName = "SUPPORT.USER",
            Email = "support.user@myada.local",
            NormalizedEmail = "SUPPORT.USER@MYADA.LOCAL",
            FirstName = "Support",
            LastName = "User",
            OrganizationalId = "900000010",
            UserType = UserType.Staff,
            Status = Status.Active
        };

        dbContext.Roles.Add(role);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        dbContext.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var result = await controller.Delete(role.Id, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        (await dbContext.Roles.AnyAsync(existingRole => existingRole.Id == role.Id)).Should().BeTrue();
    }

    private static MyAdaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MyAdaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new MyAdaDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    private static RolesController CreateController(MyAdaDbContext dbContext)
    {
        var store = new RoleStore<IdentityRole, MyAdaDbContext, string>(dbContext);
        var validators = new IRoleValidator<IdentityRole>[]
        {
            new RoleValidator<IdentityRole>()
        };

        var roleManager = new RoleManager<IdentityRole>(
            store,
            validators,
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<ILogger<RoleManager<IdentityRole>>>());

        return new RolesController(roleManager, dbContext);
    }
}
