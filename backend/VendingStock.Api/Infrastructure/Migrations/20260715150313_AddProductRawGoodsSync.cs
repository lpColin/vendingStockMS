using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VendingStock.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductRawGoodsSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_raw",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    sku_id = table.Column<int>(type: "int", nullable: false),
                    sku_name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    base_unit = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    box_unit = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    box_quantity = table.Column<int>(type: "int", nullable: true),
                    spec_str = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    image_url = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    box_price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    unit_price = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    hot = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    package_num = table.Column<int>(type: "int", nullable: true),
                    is_recently_purchased = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    stock = table.Column<int>(type: "int", nullable: false),
                    stock_r = table.Column<int>(type: "int", nullable: false),
                    stock_p = table.Column<int>(type: "int", nullable: false),
                    stock_a = table.Column<int>(type: "int", nullable: false),
                    stock_b = table.Column<int>(type: "int", nullable: false),
                    stock_w = table.Column<int>(type: "int", nullable: false),
                    brand_id_raw = table.Column<int>(type: "int", nullable: true),
                    brand_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category_code1 = table.Column<int>(type: "int", nullable: true),
                    category_name1 = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category_code2 = table.Column<int>(type: "int", nullable: true),
                    category_name2 = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    height = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    length = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    width = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_raw", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sync_task_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    task_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    finished_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    total_count = table.Column<int>(type: "int", nullable: false),
                    success_count = table.Column<int>(type: "int", nullable: false),
                    failed_count = table.Column<int>(type: "int", nullable: false),
                    error_message = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_task_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_product_raw_brand_id_raw",
                table: "product_raw",
                column: "brand_id_raw");

            migrationBuilder.CreateIndex(
                name: "ix_product_raw_category_code1",
                table: "product_raw",
                column: "category_code1");

            migrationBuilder.CreateIndex(
                name: "ix_product_raw_category_code2",
                table: "product_raw",
                column: "category_code2");

            migrationBuilder.CreateIndex(
                name: "ix_product_raw_sku_id",
                table: "product_raw",
                column: "sku_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sync_task_log_task_type_status_started_at",
                table: "sync_task_log",
                columns: new[] { "task_type", "status", "started_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_raw");

            migrationBuilder.DropTable(
                name: "sync_task_log");
        }
    }
}
