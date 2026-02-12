using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MezuroApp.Persistance.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryAndProductsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    NameAz = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NameTr = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DescriptionAz = table.Column<string>(type: "text", nullable: true),
                    DescriptionRu = table.Column<string>(type: "text", nullable: true),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    DescriptionTr = table.Column<string>(type: "text", nullable: true),
                    Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    ImageAltText = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ShowInMenu = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true),
                    MetaTitleAz = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MetaTitleRu = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MetaTitleEn = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MetaTitleTr = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MetaDescriptionAz = table.Column<string>(type: "text", nullable: true),
                    MetaDescriptionRu = table.Column<string>(type: "text", nullable: true),
                    MetaDescriptionEn = table.Column<string>(type: "text", nullable: true),
                    MetaDescriptionTr = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NameAz = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NameTr = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DescriptionAz = table.Column<string>(type: "text", nullable: true),
                    DescriptionRu = table.Column<string>(type: "text", nullable: true),
                    DescriptionEn = table.Column<string>(type: "text", nullable: true),
                    DescriptionTr = table.Column<string>(type: "text", nullable: true),
                    ShortDescriptionAz = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ShortDescriptionRu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ShortDescriptionEn = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ShortDescriptionTr = table.Column<string>(type: "text", nullable: true),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CompareAtPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    CostPrice = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    StockQuantity = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    LowStockThreshold = table.Column<int>(type: "integer", nullable: true, defaultValue: 10),
                    TrackInventory = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    AllowBackorder = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    IsNew = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    IsOnSale = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    Weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Length = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Width = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Height = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    RatingAverage = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    RatingCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    WishlistCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    OrderCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MetaTitleAz = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MetaTitleRu = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MetaTitleEn = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MetaTitleTr = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MetaDescriptionAz = table.Column<string>(type: "text", nullable: true),
                    MetaDescriptionRu = table.Column<string>(type: "text", nullable: true),
                    MetaDescriptionEn = table.Column<string>(type: "text", nullable: true),
                    MetaDescriptionTr = table.Column<string>(type: "text", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_categories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_categories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductColors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ColorNameAz = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColorNameRu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColorNameEn = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColorNameTr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ColorCode = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductColors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductColors_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AltText = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ColorImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductColorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AltText = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColorImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ColorImages_ProductColors_ProductColorId",
                        column: x => x.ProductColorId,
                        principalTable: "ProductColors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductColorId = table.Column<Guid>(type: "uuid", nullable: true),
                    SizeNameAz = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SizeNameRu = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SizeNameEn = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SizeNameTr = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PriceModifier = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    StockQuantity = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastUpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_ProductColors_ProductColorId",
                        column: x => x.ProductColorId,
                        principalTable: "ProductColors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ColorImages_ProductColorId_IsPrimary",
                table: "ColorImages",
                columns: new[] { "ProductColorId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_product_categories_CategoryId",
                table: "product_categories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_product_categories_ProductId_CategoryId",
                table: "product_categories",
                columns: new[] { "ProductId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductColors_ProductId",
                table: "ProductColors",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_IsPrimary",
                table: "ProductImages",
                columns: new[] { "ProductId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_PublishedAt",
                table: "Products",
                columns: new[] { "IsActive", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsFeatured_IsActive",
                table: "Products",
                columns: new[] { "IsFeatured", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsNew_CreatedDate",
                table: "Products",
                columns: new[] { "IsNew", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsOnSale_Price",
                table: "Products",
                columns: new[] { "IsOnSale", "Price" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Price",
                table: "Products",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_Products_RatingAverage_ReviewCount",
                table: "Products",
                columns: new[] { "RatingAverage", "ReviewCount" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductColorId",
                table: "ProductVariants",
                column: "ProductColorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Sku",
                table: "ProductVariants",
                column: "Sku",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ColorImages");

            migrationBuilder.DropTable(
                name: "product_categories");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "ProductColors");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
