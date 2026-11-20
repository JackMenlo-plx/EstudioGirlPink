using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StudioGirlPink.Data;
using StudioGirlPink.Hubs;
using StudioGirlPink.Security;
using StudioGirlPink.Services;
using StudioGirlPink.Services.Auth;
using StudioGirlPink.Services.Email;

// 1. Cargar variables de entorno desde archivo .env (si existe)
DotEnvLoader.Load(".env");

var builder = WebApplication.CreateBuilder(args);

// 2. Validar esquema de variables de entorno al arranque (Fail-Fast)
DotEnvLoader.ValidateSchema(builder.Configuration, builder.Environment);

// 3. Controladores y Vistas
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 4. Rate Limiting Estricto y General (Dev-to-Prod)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Política Estricta: 5 peticiones por ventana de 15 minutos (para /auth/login y /auth/forgot-password)
    options.AddFixedWindowLimiter("strict-auth", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(15);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    // Política General: 100 peticiones por minuto para endpoints de API
    options.AddFixedWindowLimiter("general-api", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

// 5. Configuración de CORS dinámico restringido a FRONTEND_URL en producción
var frontendUrl = builder.Configuration["FRONTEND_URL"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("DynamicCorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment() || string.IsNullOrWhiteSpace(frontendUrl))
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(frontendUrl)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// 6. Servicios de Dominio (Lógica de Negocio)
builder.Services.AddScoped<PricingCalculatorService>();
builder.Services.AddScoped<AppointmentScheduleService>();
builder.Services.AddScoped<LoyaltyService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// 7. Servicios de Autenticación, JWT y Recuperación de Contraseñas
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

// 7.1 SignalR y Notificador en Tiempo Real
builder.Services.AddSignalR();
builder.Services.AddScoped<IRealtimeNotifier, RealtimeNotifier>();

// Strategy Mailer Service: Consola/Log en Development vs SMTP/SendGrid/Resend en Production
if (builder.Environment.IsProduction())
{
    builder.Services.AddScoped<IEmailProvider, SmtpEmailProvider>();
}
else
{
    builder.Services.AddScoped<IEmailProvider, DevConsoleEmailProvider>();
}

// 8. Background Hosted Service para Expiración y Limpieza de Slots Express (TTL)
builder.Services.AddHostedService<ExpressSlotCleanupService>();

// 9. Configuración de Base de Datos PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration["DATABASE_URL"] 
        ?? builder.Configuration.GetConnectionString("DefaultConnection");

    options.UseNpgsql(connectionString);
    options.ConfigureWarnings(warnings =>
        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

// 10. Identity Core y RBAC (ADMIN y CLIENTE)
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// 11. Configuración de Cookies de Sesión
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin/login";
    options.AccessDeniedPath = "/admin/login";
    options.Cookie.Name = "StudioGirlPink.Auth";
    options.Cookie.HttpOnly = true;
    
    // Garantiza cookies seguras (HTTPS) en producción
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;

    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
});

var app = builder.Build();

// 12. Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

// CORS y Rate Limiter
app.UseCors("DynamicCorsPolicy");
app.UseRateLimiter();

// Middlewares de Autenticación, JWT Protect y Autorización
app.UseAuthentication();
app.UseMiddleware<ProtectMiddleware>();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHub<StudioHub>("/hubs/studio");

// 13. Inicialización de Base de Datos, Roles y Usuario Admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var env = services.GetRequiredService<IHostEnvironment>();
    var dbContext = services.GetRequiredService<AppDbContext>();

    try
    {
        // Aplica migraciones pendientes automáticamente
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync();
        }

        // Inicializa roles y cuenta administrativa
        await IdentityBootstrapper.InitializeAsync(services, env, logger);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Ocurrió un error durante la migración de Base de Datos o la inicialización de Identity.");
        // En desarrollo permitimos que arranque para no bloquear si no hay postgres local
        if (env.IsProduction())
        {
            throw;
        }
    }
}

app.Run();
