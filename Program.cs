using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

// Connection info stored in appsettings.json
IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
// Register the DataContext service
builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(configuration["Data:Blog:ConnectionString"]));
builder.Services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlServer(configuration["Data:AppIdentity:ConnectionString"]));
builder.Services.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<AppIdentityDbContext>().AddDefaultTokenProviders();
var app = builder.Build();

await SeedUsersAndRolesAsync(app.Services);

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static async Task SeedUsersAndRolesAsync(IServiceProvider services)
{
    using IServiceScope scope = services.CreateScope();
    UserManager<AppUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = ["admin", "blogs-moderate", "northwind-customer"];

    foreach (string role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    await EnsureUserInRoleAsync(userManager, "gina", "gina@mail.com", "B@nanas1", string.Empty);
    await EnsureUserInRoleAsync(userManager, "mark", "mark@mail.com", "B@nanas1", "blogs-moderate");
    await EnsureUserInRoleAsync(userManager, "alice", "alice@mail.com", "B@nanas1", "admin");
}

static async Task EnsureUserInRoleAsync(
    UserManager<AppUser> userManager,
    string userName,
    string email,
    string password,
    string roleName)
{
    AppUser user = await userManager.FindByEmailAsync(email);

    if (user == null)
    {
        user = new AppUser
        {
            UserName = userName,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = userName.ToUpperInvariant(),
            EmailConfirmed = true
        };

        IdentityResult createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            string errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed creating user '{email}': {errors}");
        }
    }

    if (!string.IsNullOrWhiteSpace(roleName) && !await userManager.IsInRoleAsync(user, roleName))
    {
        IdentityResult roleResult = await userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            string errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed adding '{email}' to role '{roleName}': {errors}");
        }
    }
}