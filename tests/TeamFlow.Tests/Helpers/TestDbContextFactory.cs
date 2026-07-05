using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Moq;
using TeamFlow.Core.Entities;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;

namespace TeamFlow.Tests.Helpers;

public static class TestDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static UserManager<ApplicationUser> CreateUserManager(ApplicationDbContext context)
    {
        var store = new UserStore<ApplicationUser, IdentityRole<Guid>, ApplicationDbContext, Guid>(context);

        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions
        {
            Password = {
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
                RequireNonAlphanumeric = false,
                RequiredLength = 6
            }
        });

        return new UserManager<ApplicationUser>(
            store, options,
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<UserManager<ApplicationUser>>()
        );
    }

    public static INotificationService CreateMockNotificationService()
    {
        var mock = new Mock<INotificationService>();
        mock.Setup(x => x.CreateAsync(
            It.IsAny<Guid>(),
            It.IsAny<NotificationType>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Guid?>()))
            .Returns(System.Threading.Tasks.Task.CompletedTask);
        return mock.Object;
    }

    public static ApplicationUser MakeUser(string firstName = "Test", string lastName = "User", string email = "test@test.com")
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpper(),
            NormalizedUserName = email.ToUpper(),
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }
}