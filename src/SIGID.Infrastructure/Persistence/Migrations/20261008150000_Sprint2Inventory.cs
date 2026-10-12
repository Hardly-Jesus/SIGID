using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGID.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Sprint 2:
    /// - Products.CurrentStock (existencias actuales, empieza en 0) y su restricción &gt;= 0.
    /// - Suppliers.Identification pasa a ser opcional (único solo cuando tiene valor).
    /// - Nueva tabla InventoryMovements para entradas y salidas.
    /// </summary>
    public partial class Sprint2Inventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Identification",
                table: "Suppliers");

            migrationBuilder.AlterColumn<string>(
                name: "Identification",
                table: "Suppliers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStock",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    MovementType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    StockAfter = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMovements", x => x.Id);
                    table.CheckConstraint("CK_InventoryMovements_Quantity", "[Quantity] > 0");
                    table.CheckConstraint("CK_InventoryMovements_StockAfter", "[StockAfter] >= 0");
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Identification",
                table: "Suppliers",
                column: "Identification",
                unique: true,
                filter: "[Identification] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_CurrentStock",
                table: "Products",
                sql: "[CurrentStock] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_Date",
                table: "InventoryMovements",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ProductId_Date",
                table: "InventoryMovements",
                columns: new[] { "ProductId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_Identification",
                table: "Suppliers");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_CurrentStock",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CurrentStock",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "Identification",
                table: "Suppliers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Identification",
                table: "Suppliers",
                column: "Identification",
                unique: true);
        }
    }
}
