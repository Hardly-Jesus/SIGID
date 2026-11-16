using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGID.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4DemandForecast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyntheticSalesBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: false),
                    Pattern = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RandomSeed = table.Column<int>(type: "int", nullable: false),
                    ProductIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProductCount = table.Column<int>(type: "int", nullable: false),
                    RecordCount = table.Column<int>(type: "int", nullable: false),
                    ParametersKey = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyntheticSalesBatches", x => x.Id);
                    table.CheckConstraint("CK_SyntheticSalesBatches_Dates", "[StartDate] <= [EndDate]");
                    table.CheckConstraint("CK_SyntheticSalesBatches_RandomSeed", "[RandomSeed] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "SyntheticSalesRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyntheticSalesRecords", x => x.Id);
                    table.CheckConstraint("CK_SyntheticSalesRecords_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_SyntheticSalesRecords_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyntheticSalesRecords_SyntheticSalesBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "SyntheticSalesBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyntheticSalesBatches_ParametersKey",
                table: "SyntheticSalesBatches",
                column: "ParametersKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyntheticSalesRecords_BatchId_ProductId_Date",
                table: "SyntheticSalesRecords",
                columns: new[] { "BatchId", "ProductId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyntheticSalesRecords_ProductId_Date",
                table: "SyntheticSalesRecords",
                columns: new[] { "ProductId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyntheticSalesRecords");

            migrationBuilder.DropTable(
                name: "SyntheticSalesBatches");
        }
    }
}
