using Cargobell.Data;
using Cargobell.Data.Data;
using Cargobell.Data.Data.Seed;
using Cargobell.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
Console.WriteLine($"\n========================================\nACTIVE CONNECTION STRING: {connectionString}\n========================================\n");

// =======================================================================
// UPDATED: Added compatibility levels specifically matching your SSMS 20 context 
// =======================================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, x =>
    {
        x.MigrationsAssembly("Cargobell.Data");
        x.UseCompatibilityLevel(160); // Forces structural compatibility across environments
    }));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;

    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Error/403";

    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/Dashboard") ||
            context.Request.Path.StartsWithSegments("/Shipments") ||
            context.Request.Path.StartsWithSegments("/Couriers") ||
            context.Request.Path.StartsWithSegments("/Courier/Portal"))
        {
            context.Response.Redirect(context.RedirectUri + "&message=expired");
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// =======================================================================
// PROFESSIONAL UPDATES: Database Migrations & Dynamic Configurations Seeding
// =======================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var configuration = services.GetRequiredService<IConfiguration>();

    try
    {
        // Keep Migrate inside the try block for cloud host target handshakes
        context.Database.Migrate();

        // Run your seeding logic cleanly using async tasks safely processed at startup
        Task.Run(async () =>
        {
            // Seed your static items first
            await SeedData.SeedOffices(context);

            // Seed your courier/admin system roles & users dynamically from configuration
            await SeedData.SeedAdminUserAsync(services, configuration);

        }).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database setup/seeding.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error/500");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();