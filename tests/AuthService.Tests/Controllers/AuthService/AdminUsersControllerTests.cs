using System.Security.Claims;
using AuthService.API.Controllers;
using AuthService.API.Integrations;
using AuthService.Application.DTOs.Auth;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Identity;
using AuthService.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace AuthService.Tests.Controllers;

public class AdminUsersControllerTests
{
    [Fact]
    public async Task Create_ShouldPersistUserAndAssignRequestedRole()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.Create(new CreateUserWithRoleDto
        {
            Email = "student.one@ada.edu.az",
            Password = "Student123!",
            Username = "student.one",
            FirstName = "Student",
            LastName = "One",
            OrganizationalId = "123456789",
            Role = "student"
        }, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var payload = created.Value.Should().BeOfType<AdminUserDto>().Subject;
        payload.UserName.Should().Be("student.one");
        payload.Email.Should().Be("student.one@ada.edu.az");
        payload.OrganizationalId.Should().Be("123456789");
        payload.Roles.Should().ContainSingle().Which.Should().Be("student");
        payload.UserType.Should().Be(UserType.Student);
        payload.Status.Should().Be(Status.Active);

        var persisted = await dbContext.Users.SingleAsync(user => user.UserName == "student.one");
        persisted.FirstName.Should().Be("Student");
        persisted.LastName.Should().Be("One");
        persisted.OrganizationalId.Should().Be("123456789");

        var studentRole = await dbContext.Roles.SingleAsync(role => role.Name == "student");
        (await dbContext.UserRoles.AnyAsync(userRole => userRole.UserId == persisted.Id && userRole.RoleId == studentRole.Id))
            .Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_ShouldReturnUsersWithTheirRoles()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);
        var userManager = CreateUserManager(dbContext);

        var student = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "student.user",
            Email = "student.user@ada.edu.az",
            FirstName = "Student",
            LastName = "User",
            OrganizationalId = "123456781",
            UserType = UserType.Student,
            Status = Status.Active,
            EmailConfirmed = true
        };

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "admin.user",
            Email = "admin.user@ada.edu.az",
            FirstName = "Admin",
            LastName = "User",
            OrganizationalId = "123456782",
            UserType = UserType.Staff,
            Status = Status.Active,
            EmailConfirmed = true
        };

        (await userManager.CreateAsync(student, "Student123!")).Succeeded.Should().BeTrue();
        (await userManager.CreateAsync(admin, "Admin123!")).Succeeded.Should().BeTrue();
        (await userManager.AddToRoleAsync(student, "student")).Succeeded.Should().BeTrue();
        (await userManager.AddToRoleAsync(admin, "admin")).Succeeded.Should().BeTrue();

        var result = await controller.GetAll(role: null, search: null, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<AdminUserListResponseDto>().Subject;
        payload.Count.Should().BeGreaterThanOrEqualTo(2);
        payload.Users.Should().Contain(item => item.UserName == "student.user" && item.Roles.Contains("student"));
        payload.Users.Should().Contain(item => item.UserName == "admin.user" && item.Roles.Contains("admin"));
    }

    [Fact]
    public async Task Update_ShouldReplaceRoleAndPersistEditedFields()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);
        var userManager = CreateUserManager(dbContext);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "old.user",
            Email = "old.user@ada.edu.az",
            FirstName = "Old",
            LastName = "User",
            OrganizationalId = "123456783",
            UserType = UserType.Student,
            Status = Status.Active,
            EmailConfirmed = true
        };

        (await userManager.CreateAsync(user, "OldPassword123!")).Succeeded.Should().BeTrue();
        (await userManager.AddToRoleAsync(user, "student")).Succeeded.Should().BeTrue();

        var result = await controller.Update(user.Id, new UpdateUserDto
        {
            Email = "new.user@ada.edu.az",
            Username = "new.user",
            FirstName = "New",
            LastName = "Name",
            OrganizationalId = "123456784",
            Role = "admin",
            Status = Status.Active
        }, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<AdminUserDto>().Subject;
        payload.UserName.Should().Be("new.user");
        payload.Email.Should().Be("new.user@ada.edu.az");
        payload.OrganizationalId.Should().Be("123456784");
        payload.Roles.Should().ContainSingle().Which.Should().Be("admin");
        payload.UserType.Should().Be(UserType.Staff);

        var persisted = await dbContext.Users.SingleAsync(existing => existing.Id == user.Id);
        persisted.UserName.Should().Be("new.user");
        persisted.Email.Should().Be("new.user@ada.edu.az");
        persisted.FirstName.Should().Be("New");
        persisted.LastName.Should().Be("Name");
        persisted.OrganizationalId.Should().Be("123456784");

        var adminRole = await dbContext.Roles.SingleAsync(role => role.Name == "admin");
        var studentRole = await dbContext.Roles.SingleAsync(role => role.Name == "student");
        (await dbContext.UserRoles.AnyAsync(userRole => userRole.UserId == user.Id && userRole.RoleId == adminRole.Id))
            .Should().BeTrue();
        (await dbContext.UserRoles.AnyAsync(userRole => userRole.UserId == user.Id && userRole.RoleId == studentRole.Id))
            .Should().BeFalse();
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

    private static AdminUsersController CreateController(MyAdaDbContext dbContext)
    {
        var controller = new AdminUsersController(
            CreateUserManager(dbContext),
            CreateRoleManager(dbContext),
            dbContext,
            CreateProvisioningClient());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "admin-user-id"),
                    new Claim(ClaimTypes.Role, "admin")
                }, "TestAuth"))
            }
        };

        return controller;
    }

    private static UserManager<ApplicationUser> CreateUserManager(MyAdaDbContext dbContext)
    {
        var store = new UserStore<ApplicationUser, IdentityRole, MyAdaDbContext, string>(dbContext);
        var options = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
        options.Setup(x => x.Value).Returns(new IdentityOptions());

        return new UserManager<ApplicationUser>(
            store,
            options.Object,
            new PasswordHasher<ApplicationUser>(),
            new IUserValidator<ApplicationUser>[] { new UserValidator<ApplicationUser>() },
            new IPasswordValidator<ApplicationUser>[] { new PasswordValidator<ApplicationUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null,
            Mock.Of<ILogger<UserManager<ApplicationUser>>>());
    }

    private static RoleManager<IdentityRole> CreateRoleManager(MyAdaDbContext dbContext)
    {
        var store = new RoleStore<IdentityRole, MyAdaDbContext, string>(dbContext);
        return new RoleManager<IdentityRole>(
            store,
            new IRoleValidator<IdentityRole>[] { new RoleValidator<IdentityRole>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<ILogger<RoleManager<IdentityRole>>>());
    }

    private static SupportStaffProvisioningClient CreateProvisioningClient()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        return new SupportStaffProvisioningClient(
            new HttpClient(new SuccessMessageHandler())
            {
                BaseAddress = new Uri("http://localhost/")
            },
            configuration,
            Mock.Of<ILogger<SupportStaffProvisioningClient>>());
    }

    private sealed class SuccessMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
