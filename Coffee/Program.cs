using Coffee.Data;
using Coffee.Models;
using Coffee.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Chi ep port khi deploy co bien PORT (Render).
        // Local thi de Visual Studio / launchSettings quan ly URL.
        var port = Environment.GetEnvironmentVariable("PORT");
        if (!string.IsNullOrWhiteSpace(port))
        {
            builder.WebHost.UseUrls($"http://+:{port}");
        }

        // =========================
        // ADD SERVICES
        // =========================
        builder.Services.AddControllersWithViews();

        // DB - Hỗ trợ tự động chuyển đổi chuỗi postgres:// từ DATABASE_URL trên Render
        var myConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
            ?? builder.Configuration.GetConnectionString("MyConnectString");

        if (string.IsNullOrWhiteSpace(myConnectionString))
        {
            throw new InvalidOperationException("Connection string 'MyConnectString' or environment variable 'DATABASE_URL' was not found.");
        }

        if (myConnectionString.StartsWith("postgres://") || myConnectionString.StartsWith("postgresql://"))
        {
            myConnectionString = ParsePostgresConnectionString(myConnectionString);
        }

        builder.Services.AddDbContext<CoffeeShopDbContext>(options =>
        {
            if (myConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) || 
                myConnectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(myConnectionString);
            }
            else
            {
                options.UseSqlServer(myConnectionString);
            }
        });

        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
        builder.Services.Configure<MomoPaymentSettings>(builder.Configuration.GetSection("MomoPaymentSettings"));
        builder.Services.Configure<MomoBusinessSettings>(builder.Configuration.GetSection("MomoBusinessSettings"));

        builder.Services.AddTransient<EmailService>();
        builder.Services.AddHttpClient<MomoBusinessService>();
        builder.Services.AddSingleton<CloudinaryService>();

        // =========================
        // COOKIE AUTH
        // =========================
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.AccessDeniedPath = "/NotFound";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = true;

                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
            });

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var app = builder.Build();

        DateTimeOffsetSchemaInitializer.EnsureAsync(app.Services).GetAwaiter().GetResult();
        PasswordResetSchemaInitializer.EnsureAsync(app.Services).GetAwaiter().GetResult();
        TransactionSchemaInitializer.EnsureAsync(app.Services).GetAwaiter().GetResult();
        ProductSchemaInitializer.EnsureAsync(app.Services).GetAwaiter().GetResult();
        OrderStatusDataInitializer.EnsureAsync(app.Services).GetAwaiter().GetResult();
        RoleDataInitializer.EnsureAsync(app.Services).GetAwaiter().GetResult();
        GameItemSchemaInitializer.EnsureAsync(app.Services).GetAwaiter().GetResult();
        CategorySchemaInitializer.EnsureAsync(app.Services).GetAwaiter().GetResult();
        TransferCodeSchemaInitializer.EnsureAsync(app.Services).GetAwaiter().GetResult();

        // =========================
        // PIPELINE
        // =========================
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        // =========================
        // ROUTE
        // =========================
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.MapFallbackToController("NotFound", "Home");
        app.Run();
    }

    private static string ParsePostgresConnectionString(string databaseUrl)
    {
        databaseUrl = databaseUrl.Replace("postgres://", "").Replace("postgresql://", "");
        var userPassSide = databaseUrl.Split('@')[0];
        var hostDbSide = databaseUrl.Split('@')[1];

        var user = userPassSide.Split(':')[0];
        var password = userPassSide.Split(':')[1];

        var hostPort = hostDbSide.Split('/')[0];
        var database = hostDbSide.Split('/')[1].Split('?')[0]; // Loại bỏ query params nếu có

        var host = hostPort.Split(':')[0];
        var port = hostPort.Contains(":") ? hostPort.Split(':')[1] : "5432";

        return $"Host={host};Port={port};Username={user};Password={password};Database={database};SSL Mode=Require;Trust Server Certificate=True;";
    }
}
