using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MezuroApp.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailCompaingAndEmailCompaingLogAndNewsletterAndAbandonedCartTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewsletterPreferences",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "abandoned_carts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FootprintId = table.Column<string>(type: "text", nullable: true),
                    BasketId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CartItemsJson = table.Column<string>(type: "jsonb", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    RecoveryEmailSent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RecoveryEmailSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConvertedToOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abandoned_carts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_abandoned_carts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_abandoned_carts_Baskets_BasketId",
                        column: x => x.BasketId,
                        principalTable: "Baskets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_abandoned_carts_orders_ConvertedToOrderId",
                        column: x => x.ConvertedToOrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "email_campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SubjectAz = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SubjectRu = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SubjectEn = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SubjectTr = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentAz = table.Column<string>(type: "text", nullable: false),
                    ContentRu = table.Column<string>(type: "text", nullable: false),
                    ContentEn = table.Column<string>(type: "text", nullable: false),
                    ContentTr = table.Column<string>(type: "text", nullable: false),
                    CampaignType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetSegment = table.Column<string>(type: "jsonb", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "draft"),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalRecipients = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalSent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalOpened = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalClicked = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalBounced = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalUnsubscribed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_campaigns_AspNetUsers_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "newsletter_subscribers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SubscriptionSource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Preferences = table.Column<string>(type: "jsonb", nullable: false),
                    Frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "weekly"),
                    PreferredLanguage = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "az"),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationToken = table.Column<string>(type: "text", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UnsubscribeToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    UnsubscribedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UnsubscribeReason = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubscribedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newsletter_subscribers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_newsletter_subscribers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "email_campaign_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriberId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClickedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UnsubscribedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExternalMessageId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    BounceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NewsletterSubscriberId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_campaign_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_campaign_logs_email_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "email_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_email_campaign_logs_newsletter_subscribers_NewsletterSubscr~",
                        column: x => x.NewsletterSubscriberId,
                        principalTable: "newsletter_subscribers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_email_campaign_logs_newsletter_subscribers_SubscriberId",
                        column: x => x.SubscriberId,
                        principalTable: "newsletter_subscribers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_abandoned_carts_BasketId",
                table: "abandoned_carts",
                column: "BasketId");

            migrationBuilder.CreateIndex(
                name: "IX_abandoned_carts_ConvertedToOrderId",
                table: "abandoned_carts",
                column: "ConvertedToOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_abandoned_carts_ExpiresAt",
                table: "abandoned_carts",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_abandoned_carts_FootprintId",
                table: "abandoned_carts",
                column: "FootprintId");

            migrationBuilder.CreateIndex(
                name: "IX_abandoned_carts_UserId",
                table: "abandoned_carts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_email_campaign_logs_CampaignId",
                table: "email_campaign_logs",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_email_campaign_logs_NewsletterSubscriberId",
                table: "email_campaign_logs",
                column: "NewsletterSubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_email_campaign_logs_SubscriberId",
                table: "email_campaign_logs",
                column: "SubscriberId");

            migrationBuilder.CreateIndex(
                name: "IX_email_campaigns_CreatedById",
                table: "email_campaigns",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_newsletter_subscribers_Email",
                table: "newsletter_subscribers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_newsletter_subscribers_UnsubscribeToken",
                table: "newsletter_subscribers",
                column: "UnsubscribeToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_newsletter_subscribers_UserId",
                table: "newsletter_subscribers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abandoned_carts");

            migrationBuilder.DropTable(
                name: "email_campaign_logs");

            migrationBuilder.DropTable(
                name: "email_campaigns");

            migrationBuilder.DropTable(
                name: "newsletter_subscribers");

            migrationBuilder.DropColumn(
                name: "NewsletterPreferences",
                table: "AspNetUsers");
        }
    }
}
