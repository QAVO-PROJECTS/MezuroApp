using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MezuroApp.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class EDitCoupon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_ProductId_OptionId",
                table: "ProductOptions");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_ProductId",
                table: "ProductOptions",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_ProductId",
                table: "ProductOptions");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_ProductId_OptionId",
                table: "ProductOptions",
                columns: new[] { "ProductId", "OptionId" },
                unique: true);
        }
    }
}
