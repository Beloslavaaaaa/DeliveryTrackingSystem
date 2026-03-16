using Cargobell.Data;
using Cargobell.Data.Data;
using Cargobell.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString,
    x => x.MigrationsAssembly("Cargobell.Data")));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddRoles<IdentityRole>() 
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Increased for testing
    options.SlidingExpiration = true;

    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Courier"))
    {
        await roleManager.CreateAsync(new IdentityRole("Courier"));
    }

    string courierEmail = "courier@cargobell.com";
    string courierPass = "Courier123!";

    var user = await userManager.FindByEmailAsync(courierEmail);
    if (user == null)
    {
        var newCourier = new ApplicationUser
        {
            UserName = courierEmail,
            Email = courierEmail,
            FirstName = "Bella",
            LastName = "Belova",
            DateOfBirth = new DateTime(1990, 1, 1),
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(newCourier, courierPass);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(newCourier, "Courier");
        }
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