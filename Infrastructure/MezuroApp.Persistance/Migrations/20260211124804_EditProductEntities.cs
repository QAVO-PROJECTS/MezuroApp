using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MezuroApp.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class EditProductEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_ProductOption_OptionId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_OptionId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "OptionId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VariantCode",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VariantNameAz",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VariantNameEn",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VariantNameRu",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VariantNameTr",
                table: "ProductVariants");

            migrationBuilder.AlterColumn<int>(
                name: "StockQuantity",
                table: "ProductVariants",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "ProductVariants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "ProductVariants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsAvailable",
                table: "ProductVariants",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true,
                oldDefaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Products",
                type: "text",
                nullable: true,
                defaultValue: "AZN");

            migrationBuilder.CreateTable(
                name: "ProductOptionValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValueAz = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ValueRu = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ValueEn = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ValueTr = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOptionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOptionValues_ProductOption_OptionId",
                        column: x => x.OptionId,
                        principalTable: "ProductOption",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantOptionValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionValueId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantOptionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantOptionValues_ProductOptionValues_OptionValueId",
                        column: x => x.OptionValueId,
                        principalTable: "ProductOptionValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVariantOptionValues_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionValues_OptionId",
                table: "ProductOptionValues",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantOptionValues_OptionValueId",
                table: "ProductVariantOptionValues",
                column: "OptionValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantOptionValues_VariantId",
                table: "ProductVariantOptionValues",
                column: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductVariantOptionValues");

            migrationBuilder.DropTable(
                name: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Products");

            migrationBuilder.AlterColumn<int>(
                name: "StockQuantity",
                table: "ProductVariants",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "ProductVariants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "ProductVariants",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<bool>(
                name: "IsAvailable",
                table: "ProductVariants",
                type: "boolean",
                nullable: true,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OptionId",
                table: "ProductVariants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantCode",
                table: "ProductVariants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantNameAz",
                table: "ProductVariants",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantNameEn",
                table: "ProductVariants",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantNameRu",
                table: "ProductVariants",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantNameTr",
                table: "ProductVariants",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_OptionId",
                table: "ProductVariants",
                column: "OptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_ProductOption_OptionId",
                table: "ProductVariants",
                column: "OptionId",
                principalTable: "ProductOption",
                principalColumn: "Id");
        }
    }
}
