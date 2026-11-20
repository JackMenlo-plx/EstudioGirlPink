/*
using Microsoft.AspNetCore.Identity;

namespace StudioGirlPink.Security;

/// <summary>
/// Provisiona los roles y la cuenta administrativa inicial.
/// </summary>
public static class IdentityBootstrapper
{
    public const string AdministratorRole = "Administrator";
    public const string ClientRole = "Client";

    public static async Task InitializeAsync(IServiceProvider services, ILogger logger)
    {
        var email = Environment.GetEnvironmentVariable("STUDIO_GIRL_PINK_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("STUDIO_GIRL_PINK_ADMIN_PASSWORD");

        // Valores por defecto seguros para desarrollo local si no se definen variables de entorno
        if (string.IsNullOrWhiteSpace(email))
        {
            email = "admin@studiogirlpink.com";
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            password = "StudioAdmin2026!#";
        }

        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // Garantizar existencia del rol Administrator
        if (!await roleManager.RoleExistsAsync(AdministratorRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(AdministratorRole));
            if (!roleResult.Succeeded)
            {
                logger.LogError("No fue posible crear el rol administrativo inicial.");
                throw new InvalidOperationException("No se pudo inicializar la seguridad de la aplicación.");
            }
        }

        // Garantizar existencia del rol Client (Fidelización)
        if (!await roleManager.RoleExistsAsync(ClientRole))
        {
            await roleManager.CreateAsync(new IdentityRole(ClientRole));
        }

        // Crear el usuario administrador inicial si no existe
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                logger.LogError("No fue posible crear la cuenta administrativa inicial: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                throw new InvalidOperationException("No se pudo inicializar la cuenta administrativa. Revisa la política de contraseña.");
            }

            logger.LogInformation("Cuenta administrativa inicial creada exitosamente: {Email}", email);
        }

        // Asegurar que el usuario tenga el rol de administrador
        if (!await userManager.IsInRoleAsync(user, AdministratorRole))
        {
            var addRoleResult = await userManager.AddToRoleAsync(user, AdministratorRole);
            if (!addRoleResult.Succeeded)
            {
                logger.LogError("No fue posible asignar el rol administrativo a la cuenta inicial.");
                throw new InvalidOperationException("No se pudo asignar el rol administrativo.");
            }
        }
    }
}
*/
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

namespace StudioGirlPink.Security;

/// <summary>
/// Provisiona los roles y la cuenta administrativa inicial con validaciones de entorno.
/// </summary>
public static class IdentityBootstrapper
{
    public const string AdministratorRole = "Administrator";
    public const string ClientRole = "Client";

    public static async Task InitializeAsync(IServiceProvider services, IHostEnvironment env, ILogger logger)
    {
        var email = Environment.GetEnvironmentVariable("STUDIO_GIRL_PINK_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("STUDIO_GIRL_PINK_ADMIN_PASSWORD");

        // Protección en Producción: Obligar el uso de variables de entorno explícitas
        if (env.IsProduction())
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogCritical("Error de Seguridad: En entorno de producción deben configurarse las variables STUDIO_GIRL_PINK_ADMIN_EMAIL y STUDIO_GIRL_PINK_ADMIN_PASSWORD.");
                throw new InvalidOperationException("Faltan las credenciales administrativas de producción en las variables de entorno.");
            }
        }
        else
        {
            // Valores por defecto seguros únicamente para desarrollo local
            if (string.IsNullOrWhiteSpace(email))
            {
                email = "admin@studiogirlpink.com";
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                password = "StudioAdmin2026!#";
            }
        }

        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        // 1. Garantizar existencia del rol Administrator
        if (!await roleManager.RoleExistsAsync(AdministratorRole))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(AdministratorRole));
            if (!roleResult.Succeeded)
            {
                logger.LogError("No fue posible crear el rol administrativo inicial.");
                throw new InvalidOperationException("No se pudo inicializar la seguridad de la aplicación.");
            }
        }

        // 2. Garantizar existencia del rol Client (Fidelización)
        if (!await roleManager.RoleExistsAsync(ClientRole))
        {
            await roleManager.CreateAsync(new IdentityRole(ClientRole));
        }

        // 3. Crear el usuario administrador inicial si no existe
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                logger.LogError("No fue posible crear la cuenta administrativa inicial: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                throw new InvalidOperationException("No se pudo inicializar la cuenta administrativa. Revisa la política de contraseña.");
            }

            logger.LogInformation("Cuenta administrativa inicial creada exitosamente para: {Email}", email);
        }

        // 4. Asegurar que el usuario tenga el rol de administrador
        if (!await userManager.IsInRoleAsync(user, AdministratorRole))
        {
            var addRoleResult = await userManager.AddToRoleAsync(user, AdministratorRole);
            if (!addRoleResult.Succeeded)
            {
                logger.LogError("No fue posible asignar el rol administrativo a la cuenta inicial.");
                throw new InvalidOperationException("No se pudo asignar el rol administrativo.");
            }
        }
    }
}