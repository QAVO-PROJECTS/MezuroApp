using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MezuroApp.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class EditProductOptionAndAddOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductOption_Products_ProductId",
                table: "ProductOption");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductOptionValues_ProductOption_OptionId",
                table: "ProductOptionValues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductOption",
                table: "ProductOption");

            migrationBuilder.DropIndex(
                name: "IX_ProductOption_ProductId",
                table: "ProductOption");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ProductOption");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "ProductOption");

            migrationBuilder.DropColumn(
                name: "NameRu",
                table: "ProductOption");

            migrationBuilder.DropColumn(
                name: "NameTr",
                table: "ProductOption");

            migrationBuilder.RenameTable(
                name: "ProductOption",
                newName: "ProductOptions");

            migrationBuilder.AddColumn<string>(
                name: "CustomNameAz",
                table: "ProductOptions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomNameEn",
                table: "ProductOptions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomNameRu",
                table: "ProductOptions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomNameTr",
                table: "ProductOptions",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OptionId",
                table: "ProductOptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductOptions",
                table: "ProductOptions",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NameAz = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    NameTr = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Options", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_OptionId",
                table: "ProductOptions",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_ProductId_OptionId",
                table: "ProductOptions",
                columns: new[] { "ProductId", "OptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Options_NameAz",
                table: "Options",
                column: "NameAz",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOptions_Options_OptionId",
                table: "ProductOptions",
                column: "OptionId",
                principalTable: "Options",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOptions_Products_ProductId",
                table: "ProductOptions",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOptionValues_ProductOptions_OptionId",
                table: "ProductOptionValues",
                column: "OptionId",
                principalTable: "ProductOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductOptions_Options_OptionId",
                table: "ProductOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductOptions_Products_ProductId",
                table: "ProductOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductOptionValues_ProductOptions_OptionId",
                table: "ProductOptionValues");

            migrationBuilder.DropTable(
                name: "Options");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductOptions",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_OptionId",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_ProductId_OptionId",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "CustomNameAz",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "CustomNameEn",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "CustomNameRu",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "CustomNameTr",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "OptionId",
                table: "ProductOptions");

            migrationBuilder.RenameTable(
                name: "ProductOptions",
                newName: "ProductOption");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ProductOption",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "ProductOption",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameRu",
                table: "ProductOption",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameTr",
                table: "ProductOption",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductOption",
                table: "ProductOption",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOption_ProductId",
                table: "ProductOption",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOption_Products_ProductId",
                table: "ProductOption",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductOptionValues_ProductOption_OptionId",
                table: "ProductOptionValues",
                column: "OptionId",
                principalTable: "ProductOption",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
