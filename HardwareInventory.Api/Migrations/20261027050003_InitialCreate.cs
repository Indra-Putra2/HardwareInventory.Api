using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HardwareInventory.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    BrandId = table.Column<int>(type: "INTEGER", nullable: false),
                    MemoryType = table.Column<string>(type: "TEXT", nullable: false),
                    MemorySpeed = table.Column<int>(type: "INTEGER", nullable: true),
                    Module = table.Column<string>(type: "TEXT", nullable: true),
                    TotalCapacity = table.Column<double>(type: "REAL", nullable: false),
                    FirstWordLatency = table.Column<double>(type: "REAL", nullable: true),
                    CASLatency = table.Column<double>(type: "REAL", nullable: true),
                    Rating = table.Column<double>(type: "REAL", nullable: true),
                    ReviewCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    Price = table.Column<decimal>(type: "REAL", nullable: true),
                    PricePerGB = table.Column<decimal>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rams_Brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                table: "Brands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rams_BrandId",
                table: "Rams",
                column: "BrandId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rams");

            migrationBuilder.DropTable(
                name: "Brands");
        }
    }
}
