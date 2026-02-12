using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using MezuroApp.Domain.HelperEntities; // LocalizedMessage burada yerləşir

namespace MezuroApp.WebApi.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    // 4 dil üçün mesaj lüğəti
    protected static readonly Dictionary<string, (string az, string en, string tr, string ru)> Messages = new()
    {
        // ====== CATEGORY KEYS ======
        ["CATEGORIES_RETURNED"] = (
            "Kateqoriyalar uğurla qaytarıldı.",
            "Categories have been returned successfully.",
            "Kategoriler başarıyla döndürüldü.",
            "Категории успешно возвращены."
        ),
        ["CATEGORY_RETURNED"] = (
            "Kateqoriya uğurla qaytarıldı.",
            "Category has been returned successfully.",
            "Kategori başarıyla döndürüldü.",
            "Категория успешно возвращена."
        ),
        ["CATEGORY_CREATED"] = (
            "Kateqoriya uğurla yaradıldı.",
            "Category has been created successfully.",
            "Kategori başarıyla oluşturuldu.",
            "Категория успешно создана."
        ),
        ["CATEGORY_UPDATED"] = (
            "Kateqoriya uğurla yeniləndi.",
            "Category has been updated successfully.",
            "Kategori başarıyla güncellendi.",
            "Категория успешно обновлена."
        ),
        ["CATEGORY_DELETED"] = (
            "Kateqoriya uğurla silindi.",
            "Category has been deleted successfully.",
            "Kategori başarıyla silindi.",
            "Категория успешно удалена."
        ),
        ["CATEGORIES_DELETED_BY_PARENT"] = (
            "Parent-ə bağlı bütün kateqoriyalar uğurla silindi.",
            "All categories related to the parent have been deleted successfully.",
            "Parent'e bağlı tüm kategoriler başarıyla silindi.",
            "Все категории, связанные с родителем, успешно удалены."
        ),
        ["CATEGORY_NOT_FOUND"] = (
            "Kateqoriya tapılmadı.",
            "Category not found.",
            "Kategori bulunamadı.",
            "Категория не найдена."
        ),

        // ====== COMMON / VALIDATION / SERVER ======
        ["INVALID_INPUT"] = (
            "Yanlış daxiletmə məlumatı!",
            "Invalid input!",
            "Geçersiz girdi!",
            "Неверный ввод!"
        ),
        ["SERVER_ERROR"] = (
            "Daxili server xətası baş verdi.",
            "An internal server error occurred.",
            "Dahili sunucu hatası oluştu.",
            "Произошла внутренняя ошибка сервера."
        ),

        // ====== AUTH KEYS ======
        ["REGISTER_SUCCESS"] = (
            "Qeydiyyat prosesi uğurlu oldu.",
            "Registration completed successfully.",
            "Kayıt işlemi başarıyla tamamlandı.",
            "Регистрация прошла успешно."
        ),
        ["LOGIN_SUCCESS"] = (
            "Giriş uğurludur.",
            "Login successful.",
            "Giriş başarılı.",
            "Вход выполнен успешно."
        ),
        ["GOOGLE_LOGIN_SUCCESS"] = (
            "Google ilə giriş uğurludur.",
            "Google login successful.",
            "Google ile giriş başarılı.",
            "Вход через Google выполнен успешно."
        ),
        ["GOOGLE_ID_TOKEN_REQUIRED"] = (
            "Google ID token tələb olunur.",
            "Google ID token is required.",
            "Google ID jetonu gereklidir.",
            "Требуется Google ID токен."
        ),
        ["INVALID_TOKEN_OR_USERID"] = (
            "Yanlış token və ya user ID.",
            "Invalid token or user ID.",
            "Geçersiz token veya kullanıcı kimliği.",
            "Неверный токен или идентификатор пользователя."
        ),
        ["EMAIL_CONFIRMED"] = (
            "E-poçt uğurla təsdiqləndi.",
            "Email confirmed successfully.",
            "E-posta başarıyla doğrulandı.",
            "Электронная почта успешно подтверждена."
        ),
        ["EMAIL_CONFIRM_FAILED"] = (
            "E-poçtun təsdiqi uğursuz oldu",
            "Email confirmation failed",
            "E-posta doğrulaması başarısız oldu",
            "Подтверждение электронной почты не удалось"
        ),
        ["RESET_LINK_SENT"] = (
            "Sıfırlama linki e-poçtunuza göndərildi.",
            "Password reset link has been sent to your email.",
            "Şifre sıfırlama bağlantısı e-postanıza gönderildi.",
            "Ссылка для сброса пароля отправлена на вашу почту."
        ),
        ["PASSWORD_RESET_SUCCESS"] = (
            "Şifrə uğurla yeniləndi.",
            "Password has been reset successfully.",
            "Şifre başarıyla sıfırlandı.",
            "Пароль успешно сброшен."
        ),
        ["PASSWORD_CHANGE_SUCCESS"] = (
            "Şifrə uğurla dəyişdirildi.",
            "Password changed successfully.",
            "Şifre başarıyla değiştirildi.",
            "Пароль успешно изменен."
        ),
        ["USER_ID_NOT_FOUND"] = (
            "İstifadəçi identifikatoru tapılmadı.",
            "User identifier not found.",
            "Kullanıcı kimliği bulunamadı.",
            "Идентификатор пользователя не найден."
        ),
        // ====== PRODUCT KEYS ======
        ["PRODUCTS_RETURNED"] = (
            "Məhsullar uğurla qaytarıldı.",
            "Products have been returned successfully.",
            "Ürünler başarıyla döndürüldü.",
            "Товары успешно возвращены."
        ),
        ["PRODUCT_RETURNED"] = (
            "Məhsul uğurla qaytarıldı.",
            "Product has been returned successfully.",
            "Ürün başarıyla döndürüldü.",
            "Товар успешно возвращён."
        ),
        ["PRODUCT_CREATED"] = (
            "Məhsul uğurla yaradıldı.",
            "Product has been created successfully.",
            "Ürün başarıyla oluşturuldu.",
            "Товар успешно создан."
        ),
        ["PRODUCT_UPDATED"] = (
            "Məhsul uğurla yeniləndi.",
            "Product has been updated successfully.",
            "Ürün başarıyla güncellendi.",
            "Товар успешно обновлён."
        ),
        ["PRODUCT_DELETED"] = (
            "Məhsul uğurla silindi.",
            "Product has been deleted successfully.",
            "Ürün başarıyla silindi.",
            "Товар успешно удалён."
        ),
        ["PRODUCT_NOT_FOUND"] = (
            "Məhsul tapılmadı.",
            "Product not found.",
            "Ürün bulunamadı.",
            "Товар не найден."
        ),
        ["SKU_ALREADY_EXISTS"] = (
            "Bu SKU artıq mövcuddur.",
            "This SKU already exists.",
            "Bu SKU zaten mevcut.",
            "Такой SKU уже существует."
        ),
        ["SLUG_ALREADY_EXISTS"] = (
            "Bu Slug artıq mövcuddur.",
            "This slug already exists.",
            "Bu slug zaten mevcut.",
            "Такой slug уже существует."
        ),
// ====== PRODUCT COLOR KEYS ======
        ["PRODUCT_COLORS_RETURNED"] = (
            "Rənglər uğurla qaytarıldı.",
            "Product colors have been returned successfully.",
            "Ürün renkleri başarıyla döndürüldü.",
            "Варианты цвета успешно возвращены."
        ),
        ["PRODUCT_COLOR_RETURNED"] = (
            "Rəng uğurla qaytarıldı.",
            "Product color has been returned successfully.",
            "Ürün rengi başarıyla döndürüldü.",
            "Вариант цвета успешно возвращён."
        ),
        ["PRODUCT_COLOR_CREATED"] = (
            "Rəng uğurla yaradıldı.",
            "Product color has been created successfully.",
            "Ürün rengi başarıyla oluşturuldu.",
            "Вариант цвета успешно создан."
        ),
        ["PRODUCT_COLOR_UPDATED"] = (
            "Rəng uğurla yeniləndi.",
            "Product color has been updated successfully.",
            "Ürün rengi başarıyla güncellendi.",
            "Вариант цвета успешно обновлён."
        ),
        ["PRODUCT_COLOR_DELETED"] = (
            "Rəng uğurla silindi.",
            "Product color has been deleted successfully.",
            "Ürün rengi başarıyla silindi.",
            "Вариант цвета успешно удалён."
        ),
        ["PRODUCT_COLOR_NOT_FOUND"] = (
            "Rəng tapılmadı.",
            "Product color not found.",
            "Ürün rengi bulunamadı.",
            "Вариант цвета не найден."
        ),
        ["INVALID_ID_FORMAT"] = (
            "Id formatı yanlışdır!",
            "Invalid Id format!",
            "Geçersiz Id formatı!",
            "Неверный формат Id!"
        ),
        ["PRODUCT_NOT_FOUND"] = (
            "Məhsul tapılmadı.",
            "Product not found.",
            "Ürün bulunamadı.",
            "Товар не найден."
        ),
        ["PRODUCT_VARIANT_RETURNED"] = (
            "Variant uğurla qaytarıldı.",
            "Product variant returned successfully.",
            "Ürün varyantı başarıyla döndürüldü.",
            "Вариант товара успешно возвращён."
        ),

        ["PRODUCT_VARIANTS_RETURNED"] = (
            "Variantlar uğurla qaytarıldı.",
            "Product variants returned successfully.",
            "Ürün varyantları başarıyla döndürüldü.",
            "Варианты товара успешно возвращены."
        ),

        ["PRODUCT_VARIANT_CREATED"] = (
            "Variant uğurla yaradıldı.",
            "Product variant created successfully.",
            "Ürün varyantı başarıyla oluşturuldu.",
            "Вариант товара успешно создан."
        ),

        ["PRODUCT_VARIANT_UPDATED"] = (
            "Variant uğurla yeniləndi.",
            "Product variant updated successfully.",
            "Ürün varyantı başarıyla güncellendi.",
            "Вариант товара успешно обновлён."
        ),

        ["PRODUCT_VARIANT_DELETED"] = (
            "Variant uğurla silindi.",
            "Product variant deleted successfully.",
            "Ürün varyantı başarıyla silindi.",
            "Вариант товара успешно удалён."
        ),

        ["PRODUCT_VARIANT_NOT_FOUND"] = (
            "Variant tapılmadı.",
            "Product variant not found.",
            "Ürün varyantı bulunamadı.",
            "Вариант товара не найден."
        ),

        ["VARIANT_SKU_EXISTS"] = (
            "Bu SKU artıq mövcuddur.",
            "This SKU already exists.",
            "Bu SKU zaten mevcut.",
            "Такой SKU уже существует."
        ),
        ["PRODUCT_OR_COLOR_NOT_FOUND"] = (
            "Məhsul və ya rəng tapılmadı.",
            "Product or color not found.",
            "Ürün veya renk bulunamadı.",
            "Товар или цвет не найден."
        ),
// ====== PRODUCT STATUS KEYS ======
        ["PRODUCT_ACTIVE_STATUS_UPDATED"] = (
            "Məhsulun aktivlik statusu yeniləndi.",
            "Product active status has been updated.",
            "Ürünün aktiflik durumu güncellendi.",
            "Статус активности товара обновлён."
        ),
        ["PRODUCT_FEATURED_STATUS_UPDATED"] = (
            "Məhsulun xüsusi qeyd statusu yeniləndi.",
            "Product featured status has been updated.",
            "Ürünün öne çıkarılma durumu güncellendi.",
            "Статус избранного товара обновлён."
        ),
        ["PRODUCT_NEW_STATUS_UPDATED"] = (
            "Məhsulun 'Yeni' statusu yeniləndi.",
            "Product 'new' status has been updated.",
            "Ürünün 'yeni' durumu güncellendi.",
            "Статус 'новинка' товара обновлён."
        ),
        ["PRODUCT_SALE_STATUS_UPDATED"] = (
            "Məhsulun endirimdə olma statusu yeniləndi.",
            "Product sale status has been updated.",
            "Ürünün indirim durumu güncellendi.",
            "Статус распродажи товара обновлён."
        ),
        // ====== ADMIN KEYS ======
        ["ADMIN_LOGIN_SUCCESS"] = (
            "Admin olaraq giriş uğurludur.",
            "Admin login successful.",
            "Admin girişi başarılı.",
            "Вход администратора выполнен успешно."
        ),
        ["ADMIN_CREATED"] = (
            "Admin uğurla yaradıldı.",
            "Admin has been created successfully.",
            "Admin başarıyla oluşturuldu.",
            "Администратор успешно создан."
        ),
        ["ADMIN_PERMISSIONS_SET"] = (
            "Admin icazələri uğurla yeniləndi.",
            "Admin permissions have been set successfully.",
            "Admin izinleri başarıyla güncellendi.",
            "Разрешения администратора успешно установлены."
        ),
        ["ADMIN_PERMISSIONS_UPDATED"] = (
            "Admin icazələri uğurla yeniləndi.",
            "Admin permissions updated successfully.",
            "Admin izinleri başarıyla güncellendi.",
            "Разрешения администратора успешно обновлены."
        ),
        ["ADMIN_ME_SUCCESS"] = (
            "Admin profili uğurla qaytarıldı.",
            "Admin profile returned successfully.",
            "Admin profili başarıyla döndürüldü.",
            "Профиль администратора успешно возвращён."
        ),
        ["ADMINS_RETURNED"] = (
            "Adminlər uğurla qaytarıldı.",
            "Admins returned successfully.",
            "Adminler başarıyla döndürüldü.",
            "Администраторы успешно возвращены."
        ),
        
        ["ADMIN_RESPONSE"] = (
            "Admin məlumatı uğurla qaytarıldı.",
            "Admin info returned successfully.",
            "Admin bilgisi başarıyla döndürüldü.",
            "Информация администратора успешно возвращена."
        ),
        ["ADMIN_PASSWORD_CHANGE_SUCCESS"] = (
            "Admin şifrəsi uğurla dəyişdirildi.",
            "Admin password changed successfully.",
            "Admin şifresi başarıyla değiştirildi.",
            "Пароль администратора успешно изменён."
        ),
        ["ADMIN_RESET_LINK_SENT"] = (
            "Admin üçün şifrə sıfırlama linki göndərildi.",
            "Password reset link has been sent to the admin.",
            "Admin için şifre sıfırlama bağlantısı gönderildi.",
            "Ссылка для сброса пароля администратора отправлена."
        ),
        ["ADMIN_PASSWORD_RESET_SUCCESS"] = (
            "Admin şifrəsi uğurla sıfırlandı.",
            "Admin password reset successfully.",
            "Admin şifresi başarıyla sıfırlandı.",
            "Пароль администратора успешно сброшен."
        ),// ====== PRODUCT CATEGORY FILTER ======
        ["PRODUCTS_BY_CATEGORY_RETURNED"] = (
            "Kateqoriyaya aid məhsullar uğurla qaytarıldı.",
            "Products by category have been returned successfully.",
            "Kategoriye ait ürünler başarıyla döndürüldü.",
            "Товары по категории успешно возвращены."
        ),

// ====== OPTIONS ======
        ["OPTIONS_RETURNED"] = (
            "Option-lar uğurla qaytarıldı.",
            "Options have been returned successfully.",
            "Option'lar başarıyla döndürüldü.",
            "Опции успешно возвращены."
        ),
        ["OPTION_RETURNED"] = (
            "Option uğurla qaytarıldı.",
            "Option has been returned successfully.",
            "Option başarıyla döndürüldü.",
            "Опция успешно возвращена."
        ),
        ["OPTION_CREATED"] = (
            "Option uğurla yaradıldı.",
            "Option has been created successfully.",
            "Option başarıyla oluşturuldu.",
            "Опция успешно создана."
        ),
        ["OPTION_UPDATED"] = (
            "Option uğurla yeniləndi.",
            "Option has been updated successfully.",
            "Option başarıyla güncellendi.",
            "Опция успешно обновлена."
        ),
        ["OPTION_DELETED"] = (
            "Option uğurla silindi.",
            "Option has been deleted successfully.",
            "Option başarıyla silindi.",
            "Опция успешно удалена."
        ),
        ["OPTION_NOT_FOUND"] = (
            "Option tapılmadı.",
            "Option not found.",
            "Option bulunamadı.",
            "Опция не найдена."
        ),
        ["COLOR_SKU_ALREADY_EXISTS"] = (
            "Bu rəng SKU-su artıq mövcuddur.",
            "This color SKU already exists.",
            "Bu renk SKU'su zaten mevcut.",
            "Такой SKU цвета уже существует."
        ),
        ["COLOR_CODE_ALREADY_EXISTS"] = (
                "Bu rəng kodu artıq mövcuddur.",
                "This color code already exists.",
                "Bu renk kodu zaten mevcut.",
                "Такой код цвета уже существует."
            ),
// ====== OPTION – VALIDATION & ERRORS ======
        ["OPTION_NAME_ALREADY_EXISTS"] = (
            "Bu option adı bu məhsul üçün artıq mövcuddur.",
            "This option name already exists for this product.",
            "Bu seçenek adı bu ürün için zaten mevcut.",
            "Такое название опции уже существует для этого товара."
        ),
        ["OPTION_VALUE_ALREADY_EXISTS"] = (
            "Bu option dəyəri artıq mövcuddur.",
            "This option value already exists.",
            "Bu seçenek değeri zaten mevcut.",
            "Такое значение опции уже существует."
        ),
        ["OPTION_VALUE_NOT_FOUND"] = (
            "Option dəyəri tapılmadı.",
            "Option value not found.",
            "Seçenek değeri bulunamadı.",
            "Значение опции не найдено."
        ),
        ["INVALID_OPTION_VALUE_ID"] = (
            "Option dəyərinin Id formatı yanlışdır!",
            "Invalid option value Id format!",
            "Seçenek değeri Id formatı geçersiz!",
            "Неверный формат идентификатора значения опции!"
        ),
        ["VALUE_AZ_REQUIRED"] = (
            "ValueAz boş ola bilməz.",
            "ValueAz cannot be empty.",
            "ValueAz boş olamaz.",
            "Поле ValueAz не может быть пустым."
        ),

// ====== OPTION – BUSINESS RULES (optional) ======
        ["MAX_OPTION_LIMIT_REACHED"] = (
            "Bu məhsul üçün icazə verilən maksimum option sayına çatılıb.",
            "Maximum number of options allowed for this product has been reached.",
            "Bu ürün için izin verilen maksimum seçenek sayısına ulaşıldı.",
            "Достигнуто максимальное число опций для данного товара."
        ),
        ["OPTION_VALUE_IN_USE"] = (
                "Bu option dəyəri aktiv variantlarda istifadə olunur və silinə bilməz.",
                "This option value is used by active variants and cannot be deleted.",
                "Bu seçenek değeri aktif varyantlarda kullanılıyor ve silinemez.",
                "Это значение опции используется активными вариантами и не может быть удалено."
            ),
// ====== OPTIONS BY PRODUCT ======
        ["OPTIONS_BY_PRODUCT_RETURNED"] = (
            "Məhsula aid option-lar uğurla qaytarıldı.",
            "Options for this product have been returned successfully.",
            "Bu ürüne ait option'lar başarıyla döndürüldü.",
            "Опции для данного товара успешно возвращены."
        )
        ,// ====== PRODUCT OPTION (PRODUCT-LEVEL OPTION) ======
        ["PRODUCT_OPTION_NOT_FOUND"] = (
            "Product option tapılmadı.",
            "Product option not found.",
            "Ürün seçeneği bulunamadı.",
            "Вариант опции продукта не найден."
        ),

        ["PRODUCT_OPTION_ALREADY_EXISTS"] = (
            "Bu product option artıq mövcuddur.",
            "This product option already exists.",
            "Bu ürün seçeneği zaten mevcut.",
            "Эта опция продукта уже существует."
        ),
            
    };

    /// <summary>
    /// Mesaj açarı üçün 4 dilli obyekt qaytarır; tapılmazsa bütün dillərə eyni mətni qoyur.
    /// </summary>
    protected LocalizedMessage LocalizeAll(string messageKeyOrPlain)
    {
        if (Messages.TryGetValue(messageKeyOrPlain, out var v))
        {
            return new LocalizedMessage
            {
                az = v.az,
                en = v.en,
                tr = v.tr,
                ru = v.ru
            };
        }

        return new LocalizedMessage
        {
            az = messageKeyOrPlain,
            en = messageKeyOrPlain,
            tr = messageKeyOrPlain,
            ru = messageKeyOrPlain
        };
    }

    // ===== Unified response helpers (4 dilli) =====

    protected IActionResult OkResponse<T>(T data, string messageKey)
        => StatusCode(StatusCodes.Status200OK,
            new ApiResponse<T>(StatusCodes.Status200OK, data, LocalizeAll(messageKey)));

    protected IActionResult CreatedResponse<T>(string? location, T data, string messageKey)
    {
        var response = new ApiResponse<T>(StatusCodes.Status201Created, data, LocalizeAll(messageKey));
        if (!string.IsNullOrWhiteSpace(location))
            Response.Headers.Location = location;
        return StatusCode(StatusCodes.Status201Created, response);
    }

    protected IActionResult BadRequestResponse(string messageKeyOrPlain)
        => StatusCode(StatusCodes.Status400BadRequest,
            new ApiError(StatusCodes.Status400BadRequest, LocalizeAll(messageKeyOrPlain)));

    protected IActionResult NotFoundResponse(string messageKeyOrPlain)
        => StatusCode(StatusCodes.Status404NotFound,
            new ApiError(StatusCodes.Status404NotFound, LocalizeAll(messageKeyOrPlain)));

    protected IActionResult ServerErrorResponse(string? messageKey = "SERVER_ERROR")
        => StatusCode(StatusCodes.Status500InternalServerError,
            new ApiError(StatusCodes.Status500InternalServerError, LocalizeAll(messageKey ?? "SERVER_ERROR")));
}
