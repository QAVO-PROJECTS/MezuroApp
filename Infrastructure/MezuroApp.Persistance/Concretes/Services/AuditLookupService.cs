using MezuroApp.Application.Abstracts.Repositories.Categories;
using MezuroApp.Application.Abstracts.Repositories.Products;
using MezuroApp.Application.Abstracts.Services;

namespace MezuroApp.Persistance.Concretes.Services;
public class AuditLookupService : IAuditLookupService
{
    private readonly ICategoryReadRepository _categoryRead;
    private readonly IProductReadRepository _productRead;
    // ... digər lazım olan repo-lar

    public AuditLookupService(ICategoryReadRepository categoryRead, IProductReadRepository productRead)
    {
        _categoryRead = categoryRead;
        _productRead = productRead;
    }

    public async Task<Dictionary<string, object>?> GetOldValuesAsync(string controller, string action, string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        // controller lower-case normalize et
        controller = (controller ?? "").Trim().ToLowerInvariant();

        if (controller == "category")
        {
            var entity = await _categoryRead.GetAsync(x => x.Id.ToString() == id);
            return entity is null ? null : ToDict(entity);
        }
        if (controller == "product")
        {
            var entity = await _productRead.GetAsync(x => x.Id.ToString() == id);
            return entity is null ? null : ToDict(entity);
        }

        return null;
    }

    private static Dictionary<string, object> ToDict(object obj)
    {
        // Sadə JSON-serialize/de-serialize ilə dict-ə çevirir
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json)
               ?? new Dictionary<string, object>();
    }
}