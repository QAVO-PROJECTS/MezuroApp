using MezuroApp.Domain.Entities.Common;

namespace MezuroApp.Domain.Entities;

public class Option : BaseEntity
{
    // Sistem üzrə ümumi adlar (istəsən tək Name də saxlaya bilərsən)
    public string NameAz { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
    public string NameTr { get; set; }

    // Relations
    public List<ProductOption>? ProductOptions { get; set; }
}