using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendingStock.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailySales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_sales",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    machine_id = table.Column<long>(type: "bigint", nullable: false),
                    sales_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_sales", x => x.id);
                    table.ForeignKey(
                        name: "fk_daily_sales_vending_machines_machine_id",
                        column: x => x.machine_id,
                        principalTable: "vending_machine",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_daily_sales_machine_id_sales_date",
                table: "daily_sales",
                columns: new[] { "machine_id", "sales_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_daily_sales_sales_date",
                table: "daily_sales",
                column: "sales_date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_sales");
        }
    }
}
