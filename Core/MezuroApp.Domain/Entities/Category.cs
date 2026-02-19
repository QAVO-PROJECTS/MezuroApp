using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class Category:BaseEntity
{
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category>? Children { get; set; } = new List<Category>();

    // i18n
    public string NameAz { get; set; }
    public string NameRu { get; set; }
    public string NameEn { get; set; }
    public string NameTr { get; set; }

    public string? DescriptionAz { get; set; }
    public string? DescriptionRu { get; set; }
    public string? DescriptionEn { get; set; }
    public string? DescriptionTr { get; set; }

    public string? SubTitleAz { get; set; }
    public string? SubTitleEn { get; set; }
    public string? SubTitleRu{ get; set; }
    public string? SubTitleTr { get; set; }
    public string Slug { get; set; }

    // Image
    public string? ImageUrl { get; set; }
    public string? ImageAltText { get; set; }

    // Hierarchy
    public int Level { get; set; } = 1;
    public int SortOrder { get; set; } = 0;

    // Navigation
    public bool? ShowInMenu { get; set; }
    public bool? IsActive { get; set; }

    // SEO
    public string? MetaTitleAz { get; set; }
    public string? MetaTitleRu { get; set; }
    public string? MetaTitleEn { get; set; }
    public string? MetaTitleTr { get; set; }

    public string? MetaDescriptionAz { get; set; }
    public string? MetaDescriptionRu { get; set; }
    public string? MetaDescriptionEn { get; set; }
    public string? MetaDescriptionTr { get; set; }

    // Relations
    public List<ProductCategory>? ProductCategories { get; set; }
}
