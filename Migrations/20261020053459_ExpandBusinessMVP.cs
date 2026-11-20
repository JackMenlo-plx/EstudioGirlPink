using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudioGirlPink.Migrations
{
    /// <inheritdoc />
    public partial class ExpandBusinessMVP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Appointments",
                newName: "ClientNotes");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Services",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PricingRuleType",
                table: "Services",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyCount",
                table: "Clients",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Clients",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSpent",
                table: "Clients",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Appointments",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "Appointments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AppliedLoyaltyReward",
                table: "Appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DepositReceiptPath",
                table: "Appointments",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepositStatus",
                table: "Appointments",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HairLength",
                table: "Appointments",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasExtraVolume",
                table: "Appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LoyaltyRewardDescription",
                table: "Appointments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NailLengthLevel",
                table: "Appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferencePhotoPath",
                table: "Appointments",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AppointmentId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialTransactions_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HairDiagnoses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    HistoryNatural = table.Column<bool>(type: "boolean", nullable: false),
                    HistoryDyed = table.Column<bool>(type: "boolean", nullable: false),
                    HistoryBleached = table.Column<bool>(type: "boolean", nullable: false),
                    HistoryKeratin = table.Column<bool>(type: "boolean", nullable: false),
                    HistoryBotox = table.Column<bool>(type: "boolean", nullable: false),
                    HistoryNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HasSplitEnds = table.Column<bool>(type: "boolean", nullable: false),
                    IsDry = table.Column<bool>(type: "boolean", nullable: false),
                    HasHairLoss = table.Column<bool>(type: "boolean", nullable: false),
                    Thickness = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Texture = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    HasAllergies = table.Column<bool>(type: "boolean", nullable: false),
                    HasSensitiveSkin = table.Column<bool>(type: "boolean", nullable: false),
                    IsPregnantOrNursing = table.Column<bool>(type: "boolean", nullable: false),
                    UsesDermatitisMedication = table.Column<bool>(type: "boolean", nullable: false),
                    SpecialConditionsNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DesiredGoal = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HairDiagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HairDiagnoses_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CurrentStock = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    MinimumStock = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DamagedOrDiscardedStock = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    LastRestockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "InventoryItems",
                columns: new[] { "Id", "Category", "CurrentStock", "DamagedOrDiscardedStock", "LastRestockedAt", "MinimumStock", "Name", "Unit", "UnitCost" },
                values: new object[,]
                {
                    { 1, "Acrílicos", 8m, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 3m, "Acrílico Cristal", "Frascos (2 oz)", 180m },
                    { 2, "Acrílicos", 12m, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 4m, "Acrílico Cover Pink", "Frascos (2 oz)", 210m },
                    { 3, "Líquidos", 1500m, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 500m, "Monómero Libre de MMA", "ml", 0.65m },
                    { 4, "Geles", 35m, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10m, "Geles Semipermanentes Studio Pink", "Esmaltes", 120m },
                    { 5, "Geles", 6m, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2m, "Gel de Construcción Rubber", "Frascos", 250m },
                    { 6, "Líquidos", 2500m, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1000m, "Acetona Pura 100%", "ml", 0.15m },
                    { 7, "Herramientas", 25m, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 10m, "Limas Profesionales 100/180", "Piezas", 18m },
                    { 8, "Herramientas", 5m, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2m, "Tips Cristal Surtidos", "Cajas (500 pzs)", 160m },
                    { 9, "Químicos", 1200m, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 400m, "Tratamiento de Alisado Progresivo", "ml", 1.20m },
                    { 10, "Desechables", 2m, 0m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1m, "Guantes de Nitrilo Rosa", "Cajas (100 pzs)", 140m }
                });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Category", "Description", "Name", "PricingRuleType" },
                values: new object[] { "Uñas", "Extensión con tip. Base $200 (Largo Nivel 1) + $50 por nivel adicional (1 al 10).", "Uñas Acrílicas", "UñasNivel" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Category", "Description", "Name", "PricingRuleType" },
                values: new object[] { "Uñas", "Aplicación sobre uña natural para dar fuerza y durabilidad.", "Baño de Acrílico", "Fijo" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Category", "Description", "Name", "PricingRuleType" },
                values: new object[] { "Uñas", "Color y brillo de alta durabilidad en uña natural.", "Gel Semipermanente", "Fijo" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Category", "Description", "Name", "PricingRuleType" },
                values: new object[] { "Uñas", "Base niveladora que protege y estructura la uña natural.", "Ruber (Nivelación)", "Fijo" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Category", "Description", "PricingRuleType" },
                values: new object[] { "Pies", "Servicio estético profesional para embellecer y proteger las uñas de los pies.", "Fijo" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Category", "Description", "Name", "PricingRuleType" },
                values: new object[] { "Pestañas y Cejas", "Diseño, peinado y fijación semipermanente de cejas.", "Laminado de Ceja", "Fijo" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Category", "Description", "Name", "PricingRuleType" },
                values: new object[] { "Cabello", "Tratamiento termo-activo reductor de frizz y alisado según largo (Hombro $500, Medio $900, Cintura $1350, Extra $1500; +$200 volumen).", "Alisado Progresivo", "AlisadoLargo" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Category", "Description", "Name", "PricingRuleType" },
                values: new object[] { "Cabello", "Efecto de color degradado y aplicación de extensiones.", "Efectos de Color con Extensión", "Fijo" });

            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "Id", "BasePrice", "Category", "Description", "EstimatedMinutes", "IsActive", "Name", "PricingRuleType" },
                values: new object[] { 9, 250m, "Especiales", "Diseños exclusivos y combinaciones personalizadas. Requiere subir foto de referencia.", 90, true, "Servicios Especiales / Personalizados", "Personalizado" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_AppointmentId",
                table: "FinancialTransactions",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_HairDiagnoses_ClientId",
                table: "HairDiagnoses",
                column: "ClientId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "FinancialTransactions");

            migrationBuilder.DropTable(
                name: "HairDiagnoses");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "PricingRuleType",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "LoyaltyCount",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "TotalSpent",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "AppliedLoyaltyReward",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "DepositReceiptPath",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "DepositStatus",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "HairLength",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "HasExtraVolume",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "LoyaltyRewardDescription",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "NailLengthLevel",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ReferencePhotoPath",
                table: "Appointments");

            migrationBuilder.RenameColumn(
                name: "ClientNotes",
                table: "Appointments",
                newName: "Notes");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Appointments",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40);

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Extensión de uñas con tip.", "Uñas acrílicas" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Aplicación sobre la uña natural.", "Baño de acrílico" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Color y brillo de larga duración.", "Gel semipermanente" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Nivelación y protección de la uña natural.", "Rubber" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 5,
                column: "Description",
                value: "Servicio estético para uñas de los pies.");

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Moldeado semipermanente de cejas.", "Laminado de ceja" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Tratamiento para reducir frizz y alisar progresivamente.", "Alisado progresivo" });

            migrationBuilder.UpdateData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Description", "Name" },
                values: new object[] { "Efecto de color con extensión.", "Efectos de color con extensión" });
        }
    }
}
