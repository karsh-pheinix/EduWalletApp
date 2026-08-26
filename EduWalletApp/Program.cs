using EduWalletApp.Data;
using EduWalletApp.Models;
using EduWalletApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

namespace EduWalletApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // --- Database setup ---
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // --- Identity setup ---
            builder.Services.AddIdentity<User, IdentityRole<int>>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // --- Controllers / API setup ---
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // --- JWT Authentication setup ---
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                };
            });

            // --- CORS setup — for the frontend, once built ---
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5175", "https://soit-iis.mandela.ac.za")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // --- Rate limiting ---
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        }));

                options.AddFixedWindowLimiter("AuthPolicy", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            // --- Application Services (DI Registration) ---
            builder.Services.AddScoped<FinanceCalculator>();
            builder.Services.AddScoped<ArchetypeCalculator>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
            builder.Services.AddScoped<ITipMatchingService, TipMatchingService>();
            builder.Services.AddScoped<IFinancialAlertService, FinancialAlertService>();
            builder.Services.AddScoped<ISpendingInsightService, SpendingInsightService>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync();

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                foreach (var role in new[] { "Admin", "User" })
                    if (!await roleManager.RoleExistsAsync(role))
                        await roleManager.CreateAsync(new IdentityRole<int>(role));

                var adminEmail = app.Configuration["Admin:Email"];
                var adminPassword = app.Configuration["Admin:Password"];
                if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword) &&
                    await userManager.FindByEmailAsync(adminEmail) is null)
                {
                    var admin = new User
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        FirstName = "EduWallet",
                        LastName = "Admin",
                        StudentIdNumber = "ADMIN",
                        University = "EduWallet",
                        YearOfStudy = 0,
                        City = ""
                    };
                    var result = await userManager.CreateAsync(admin, adminPassword);
                    if (result.Succeeded) await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // --- Global exception handling ---
            app.UseMiddleware<EduWalletApp.Middleware.ExceptionHandlingMiddleware>();

            // --- Middleware pipeline ---
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            

            // Comment out HTTPS Redirection during pure HTTP local testing to fix:
            // "Failed to determine the https port for redirect."
            // app.UseHttpsRedirection();

            app.UseCors("AllowFrontend"); // Ensure CORS runs BEFORE authentication & static files

            //app.UseCors("AllowReact");

            app.UseDefaultFiles();
            app.UseHttpsRedirection();
            // Create wwwroot if missing to suppress StaticFileMiddleware warning
            var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
            if (!Directory.Exists(wwwroot))
            {
                Directory.CreateDirectory(wwwroot);
            }
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<EduWalletApp.Middleware.ActivityTrackingMiddleware>();
            app.UseRateLimiter();

            app.MapControllers();
            app.MapFallbackToFile("index.html"); // For SPA routing, if needed
            app.Run();
        }
    }
}