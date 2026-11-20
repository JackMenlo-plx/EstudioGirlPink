using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudioGirlPink.Models;

namespace StudioGirlPink.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<HairDiagnosis> HairDiagnoses => Set<HairDiagnosis>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<PortfolioWork> PortfolioWorks => Set<PortfolioWork>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Precisiones numéricas
        modelBuilder.Entity<Service>()
            .Property(s => s.BasePrice)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Client>()
            .Property(c => c.TotalSpent)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.QuotedPrice)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.DepositAmount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<InventoryItem>()
            .Property(i => i.CurrentStock)
            .HasPrecision(10, 2);

        modelBuilder.Entity<InventoryItem>()
            .Property(i => i.MinimumStock)
            .HasPrecision(10, 2);

        modelBuilder.Entity<InventoryItem>()
            .Property(i => i.UnitCost)
            .HasPrecision(10, 2);

        modelBuilder.Entity<InventoryItem>()
            .Property(i => i.DamagedOrDiscardedStock)
            .HasPrecision(10, 2);

        modelBuilder.Entity<FinancialTransaction>()
            .Property(t => t.Amount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Promotion>()
            .Property(p => p.DiscountPrice)
            .HasPrecision(10, 2);

        modelBuilder.Entity<PasswordResetToken>()
            .HasIndex(t => t.TokenHash);

        // Relación Cliente - Diagnóstico Capilar (1 a 1)
        modelBuilder.Entity<Client>()
            .HasOne(c => c.HairDiagnosis)
            .WithOne(d => d.Client)
            .HasForeignKey<HairDiagnosis>(d => d.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed data de Servicios según sección 2 de requerimientos
        modelBuilder.Entity<Service>().HasData(
            new Service
            {
                Id = 1,
                Name = "Uñas Acrílicas",
                Category = "Uñas",
                Description = "Extensión con tip. Base $200 (Largo Nivel 1) + $50 por nivel adicional (1 al 10).",
                BasePrice = 200m,
                EstimatedMinutes = 120,
                PricingRuleType = "UñasNivel",
                IsActive = true
            },
            new Service
            {
                Id = 2,
                Name = "Baño de Acrílico",
                Category = "Uñas",
                Description = "Aplicación sobre uña natural para dar fuerza y durabilidad.",
                BasePrice = 200m,
                EstimatedMinutes = 60,
                PricingRuleType = "Fijo",
                IsActive = true
            },
            new Service
            {
                Id = 3,
                Name = "Gel Semipermanente",
                Category = "Uñas",
                Description = "Color y brillo de alta durabilidad en uña natural.",
                BasePrice = 150m,
                EstimatedMinutes = 60,
                PricingRuleType = "Fijo",
                IsActive = true
            },
            new Service
            {
                Id = 4,
                Name = "Ruber (Nivelación)",
                Category = "Uñas",
                Description = "Base niveladora que protege y estructura la uña natural.",
                BasePrice = 220m,
                EstimatedMinutes = 60,
                PricingRuleType = "Fijo",
                IsActive = true
            },
            new Service
            {
                Id = 5,
                Name = "Acripie",
                Category = "Pies",
                Description = "Servicio estético profesional para embellecer y proteger las uñas de los pies.",
                BasePrice = 250m,
                EstimatedMinutes = 60,
                PricingRuleType = "Fijo",
                IsActive = true
            },
            new Service
            {
                Id = 6,
                Name = "Laminado de Ceja",
                Category = "Pestañas y Cejas",
                Description = "Diseño, peinado y fijación semipermanente de cejas.",
                BasePrice = 200m,
                EstimatedMinutes = 30,
                PricingRuleType = "Fijo",
                IsActive = true
            },
            new Service
            {
                Id = 7,
                Name = "Alisado Progresivo",
                Category = "Cabello",
                Description = "Tratamiento termo-activo reductor de frizz y alisado según largo (Hombro $500, Medio $900, Cintura $1350, Extra $1500; +$200 volumen).",
                BasePrice = 500m,
                EstimatedMinutes = 240,
                PricingRuleType = "AlisadoLargo",
                IsActive = true
            },
            new Service
            {
                Id = 8,
                Name = "Efectos de Color con Extensión",
                Category = "Cabello",
                Description = "Efecto de color degradado y aplicación de extensiones.",
                BasePrice = 450m,
                EstimatedMinutes = 60,
                PricingRuleType = "Fijo",
                IsActive = true
            },
            new Service
            {
                Id = 9,
                Name = "Servicios Especiales / Personalizados",
                Category = "Especiales",
                Description = "Diseños exclusivos y combinaciones personalizadas. Requiere subir foto de referencia.",
                BasePrice = 250m,
                EstimatedMinutes = 90,
                PricingRuleType = "Personalizado",
                IsActive = true
            }
        );

        // Fecha estática y fija para el seed data (garantiza un modelo determinista para EF Core)
        var staticSeedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Seed data de Inventario inicial según sección 3.C
        modelBuilder.Entity<InventoryItem>().HasData(
            new InventoryItem { Id = 1, Name = "Acrílico Cristal", Category = "Acrílicos", CurrentStock = 8, MinimumStock = 3, Unit = "Frascos (2 oz)", UnitCost = 180m, LastRestockedAt = staticSeedDate },
            new InventoryItem { Id = 2, Name = "Acrílico Cover Pink", Category = "Acrílicos", CurrentStock = 12, MinimumStock = 4, Unit = "Frascos (2 oz)", UnitCost = 210m, LastRestockedAt = staticSeedDate },
            new InventoryItem { Id = 3, Name = "Monómero Libre de MMA", Category = "Líquidos", CurrentStock = 1500, MinimumStock = 500, Unit = "ml", UnitCost = 0.65m, LastRestockedAt = staticSeedDate },
            new InventoryItem { Id = 4, Name = "Geles Semipermanentes Studio Pink", Category = "Geles", CurrentStock = 35, MinimumStock = 10, Unit = "Esmaltes", UnitCost = 120m, LastRestockedAt = staticSeedDate },
            new InventoryItem { Id = 5, Name = "Gel de Construcción Rubber", Category = "Geles", CurrentStock = 6, MinimumStock = 2, Unit = "Frascos", UnitCost = 250m, LastRestockedAt = staticSeedDate },
            new InventoryItem { Id = 6, Name = "Acetona Pura 100%", Category = "Líquidos", CurrentStock = 2500, MinimumStock = 1000, Unit = "ml", UnitCost = 0.15m, LastRestockedAt = staticSeedDate },
            new InventoryItem { Id = 7, Name = "Limas Profesionales 100/180", Category = "Herramientas", CurrentStock = 25, MinimumStock = 10, Unit = "Piezas", UnitCost = 18m, LastRestockedAt = staticSeedDate },
            new InventoryItem { Id = 8, Name = "Tips Cristal Surtidos", Category = "Herramientas", CurrentStock = 5, MinimumStock = 2, Unit = "Cajas (500 pzs)", UnitCost = 160m, LastRestockedAt = staticSeedDate },
            new InventoryItem { Id = 9, Name = "Tratamiento de Alisado Progresivo", Category = "Químicos", CurrentStock = 1200, MinimumStock = 400, Unit = "ml", UnitCost = 1.20m, LastRestockedAt = staticSeedDate },
            new InventoryItem { Id = 10, Name = "Guantes de Nitrilo Rosa", Category = "Desechables", CurrentStock = 2, MinimumStock = 1, Unit = "Cajas (100 pzs)", UnitCost = 140m, LastRestockedAt = staticSeedDate }
        );
    }
}
