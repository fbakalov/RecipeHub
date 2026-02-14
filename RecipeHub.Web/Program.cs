using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RecipeHub.Data;
using RecipeHub.Data.Data.Seed;
using RecipeHub.Services.Interfaces;
using RecipeHub.Services.Services;

namespace RecipeHub
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services
                .AddDefaultIdentity<IdentityUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                })
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddRazorPages();
            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<IRecipeService, RecipeService>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                await RoleSeeder.SeedAsync(services);

                var context = services.GetRequiredService<ApplicationDbContext>();
                await DataSeeder.SeedAsync(context);

                await UserSeeder.SeedAsync(services);

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            }
            

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Authenticate before Authorize
            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Recipes}/{action=Index}/{id?}");

            app.MapRazorPages();

            app.Run();

        }
    }
}
