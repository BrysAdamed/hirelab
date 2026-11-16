using System.Text.Json;
using HireLab.Modules.Identity.Domain;
using HireLab.Modules.Identity.Persistence;
using HireLab.SharedKernel.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireLab.Modules.Identity.Seed;

internal sealed class DevelopmentSeed(IdentityModuleDbContext dbContext, UserManager<User> userManager)
{
    private const string ResourceName = "HireLab.Modules.Identity.Seed.development-seed.json";

    public static DevelopmentSeedData Load()
    {
        using var stream = typeof(DevelopmentSeed).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The embedded resource '{ResourceName}' is missing.");

        return JsonSerializer.Deserialize<DevelopmentSeedData>(stream, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException($"The embedded resource '{ResourceName}' is empty.");
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var data = Load();

        if (!await dbContext.Users.AnyAsync(user => user.Role == StaffRole.Admin, cancellationToken))
        {
            await CreateAsync(User.CreateAdmin(data.Admin.Email, data.Admin.DisplayName), data.Password);
        }

        foreach (var seedTeam in data.Teams)
        {
            var team = await dbContext.Teams.SingleOrDefaultAsync(team => team.Name == seedTeam.Name, cancellationToken);
            if (team is null)
            {
                team = Team.Create(seedTeam.Name);
                dbContext.Teams.Add(team);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await CreateIfMissingAsync(User.CreateManager(seedTeam.Manager.Email, seedTeam.Manager.DisplayName, team.Id), data.Password);
            foreach (var interviewer in seedTeam.Interviewers)
            {
                await CreateIfMissingAsync(User.CreateInterviewer(interviewer.Email, interviewer.DisplayName, team.Id), data.Password);
            }
        }
    }

    private async Task CreateIfMissingAsync(User user, string password)
    {
        if (await userManager.FindByEmailAsync(user.Email!) is null)
        {
            await CreateAsync(user, password);
        }
    }

    private async Task CreateAsync(User user, string password)
    {
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var codes = string.Join(", ", result.Errors.Select(error => error.Code));
            throw new InvalidOperationException($"Seeding a {user.Role} failed: {codes}.");
        }
    }
}

internal sealed record DevelopmentSeedData(string Password, SeedUser Admin, IReadOnlyList<SeedTeam> Teams);

internal sealed record SeedTeam(string Name, SeedUser Manager, IReadOnlyList<SeedUser> Interviewers);

internal sealed record SeedUser(string Email, string DisplayName);
