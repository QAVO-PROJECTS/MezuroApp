using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MezuroApp.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCardAndEditPaymentTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_orders_OrderId",
                table: "payment_transactions");

            migrationBuilder.AddColumn<Guid>(
                name: "UserCardId",
                table: "payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserCard",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardUid = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CardName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CardMask = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CardExpiry = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BankTransaction = table.Column<string>(type: "text", nullable: true),
                    BankResponse = table.Column<string>(type: "text", nullable: true),
                    OperationCode = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Rrn = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCard", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCard_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_UserCardId",
                table: "payment_transactions",
                column: "UserCardId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCard_UserId_IsDefault",
                table: "UserCard",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_UserCard_UserCardId",
                table: "payment_transactions",
                column: "UserCardId",
                principalTable: "UserCard",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_orders_OrderId",
                table: "payment_transactions",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_UserCard_UserCardId",
                table: "payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_orders_OrderId",
                table: "payment_transactions");

            migrationBuilder.DropTable(
                name: "UserCard");

            migrationBuilder.DropIndex(
                name: "IX_payment_transactions_UserCardId",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "UserCardId",
                table: "payment_transactions");

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_orders_OrderId",
                table: "payment_transactions",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
