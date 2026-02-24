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
        ["EDIT_PROFILE_SUCCESS"]=(
        
         "Profil məlumatları yeniləndi",
        "Profile information has been updated",
        "Profil bilgileri güncellendi",
        "Информация профиля обновлена"
            ),
        ["GET_PROFILE_SUCCESS"]=(
        
        "İstifadəçi məlumatları uğurla qaytarıldı",
        "User information was successfully retrieved",
        "Kullanıcı bilgileri başarıyla alındı",
        "Данные пользователя успешно получены"
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
        // ====== ADDRESS KEYS ======
        ["ADDRESSES_RETURNED"] = (
            "Ünvanlar uğurla qaytarıldı.",
            "Addresses returned successfully.",
            "Adresler başarıyla döndürüldü.",
            "Адреса успешно возвращены."
        ),
        ["ADDRESS_RETURNED"] = (
            "Ünvan uğurla qaytarıldı.",
            "Address returned successfully.",
            "Adres başarıyla döndürüldü.",
            "Адрес успешно возвращён."
        ),
        ["ADDRESS_CREATED"] = (
            "Ünvan uğurla yaradıldı.",
            "Address created successfully.",
            "Adres başarıyla oluşturuldu.",
            "Адрес успешно создан."
        ),
        ["ADDRESS_UPDATED"] = (
            "Ünvan uğurla yeniləndi.",
            "Address updated successfully.",
            "Adres başarıyla güncellendi.",
            "Адрес успешно обновлён."
        ),
        ["ADDRESS_DELETED"] = (
            "Ünvan uğurla silindi.",
            "Address deleted successfully.",
            "Adres başarıyla silindi.",
            "Адрес успешно удалён."
        ),
        ["ADDRESS_NOT_FOUND"] = (
                "Ünvan tapılmadı.",
                "Address not found.",
                "Adres bulunamadı.",
                "Адрес не найден."
            )
,
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
        ["PRODUCT_BESTSELLER_STATUS_UPDATED"] = (
            "Məhsulun “Bestseller” statusu yeniləndi.",
            "Product bestseller status has been updated.",
            "Ürünün “çok satan” durumu güncellendi.",
            "Статус «бестселлер» товара обновлён."
        ),
        // ====== CATEGORY STATUS KEYS (MISSING) ======
        ["CATEGORY_ACTIVE_STATUS_UPDATED"] = (
            "Kateqoriyanın aktivlik statusu yeniləndi.",
            "Category active status has been updated.",
            "Kategori'nin aktiflik durumu güncellendi.",
            "Статус активности категории обновлён."
        ),

        ["CATEGORY_SHOW_MENU_STATUS_UPDATED"] = (
            "Kateqoriyanın menyuda göstərilmə statusu yeniləndi.",
            "Category 'show in menu' status has been updated.",
            "Kategori'nin menüde gösterim durumu güncellendi.",
            "Статус отображения категории в меню обновлён."
        ),
        ["REVIEWS_RETURNED"] = ("Rəylər uğurla qaytarıldı.","Reviews have been returned successfully.","Yorumlar başarıyla döndürüldü.","Отзывы успешно возвращены."),
        ["REVIEWS_ACTIVE_RETURNED"] = ("Aktiv rəylər uğurla qaytarıldı.","Active reviews have been returned successfully.","Aktif yorumlar başarıyla döndürüldü.","Активные отзывы успешно возвращены."),
        ["REVIEW_RETURNED"] = ("Rəy uğurla qaytarıldı.","Review has been returned successfully.","Yorum başarıyla döndürüldü.","Отзыв успешно возвращён."),
        ["REVIEW_CREATED"] = ("Rəy uğurla yaradıldı.","Review has been created successfully.","Yorum başarıyla oluşturuldu.","Отзыв успешно создан."),
        ["REVIEW_REPLIED"] = ("Rəyə cavab uğurla əlavə olundu.","Reply to review has been added successfully.","Yoruma yanıt başarıyla eklendi.","Ответ на отзыв успешно добавлен."),
        ["REVIEW_STATUS_UPDATED"] = ("Rəyin statusu yeniləndi.","Review status has been updated.","Yorum durumu güncellendi.","Статус отзыва обновлён."),
        ["REVIEW_LIKED"] = ("Rəy bəyənildi.","Review liked.","Yorum beğenildi.","Отзыв отмечен как понравившийся."),
        ["REVIEW_DISLIKED"] = ("Rəy bəyənilmədi.","Review disliked.","Yorum beğenilmedi.","Отзыв отмечен как не понравившийся."),
        ["REVIEWS_SORTED"] = ("Rəylər sıralandı.","Reviews have been sorted.","Yorumlar sıralandı.","Отзывы отсортированы."),
        ["REVIEW_DELETED"] = ("Rəy uğurla silindi.","Review has been deleted successfully.","Yorum başarıyla silindi.","Отзыв успешно удалён."),
        ["REVIEW_NOT_FOUND"] = ("Rəy tapılmadı.","Review not found.","Yorum bulunamadı.","Отзыв не найден."),
        
        ["WISHLIST_UPDATED"] = (
            "İstək siyahısı yeniləndi.",
            "Wishlist updated successfully.",
            "İstek listesi güncellendi.",
            "Список желаемого обновлён."
        ),
        ["WISHLIST_RETURNED"] = (
            "İstək siyahısı uğurla qaytarıldı.",
            "Wishlist returned successfully.",
            "İstek listesi başarıyla döndürüldü.",
            "Список желаемого успешно возвращён."
        ),
        ["PRODUCT_ID_NOT_FOUND"] = (
            "Məhsul identifikatoru tapılmadı.",
            "Product id not found.",
            "Ürün kimliği bulunamadı.",
            "Идентификатор товара не найден."
        ),
        ["EMPTY_PRODUCT_LIST"] = (
            "Məhsul siyahısı boşdur!",
            "Product list is empty!",
            "Ürün listesi boş!",
            "Список товаров пуст!"
        ),
        // ====== BASKET KEYS ======
        ["BASKET_RETURNED"] = (
            "Səbət uğurla qaytarıldı.",
            "Basket returned successfully.",
            "Sepet başarıyla döndürüldü.",
            "Корзина успешно возвращена."
        ),

        ["BASKET_UPDATED"] = (
            "Səbət uğurla yeniləndi.",
            "Basket updated successfully.",
            "Sepet başarıyla güncellendi.",
            "Корзина успешно обновлена."
        ),

        ["BASKET_ITEM_DELETED"] = (
            "Səbət elementi uğurla silindi.",
            "Basket item deleted successfully.",
            "Sepet öğesi başarıyla silindi.",
            "Элемент корзины успешно удалён."
        ),

        ["BASKET_CLEARED"] = (
            "Səbət uğurla təmizləndi.",
            "Basket cleared successfully.",
            "Sepet başarıyla temizlendi.",
            "Корзина успешно очищена."
        ),

        ["FOOTPRINT_REQUIRED"] = (
            "Footprint ID tələb olunur.",
            "Footprint ID is required.",
            "Footprint ID gereklidir.",
            "Требуется идентификатор Footprint ID."
        ),

        ["INVALID_FOOTPRINT"] = (
            "Yanlış Footprint ID göndərilib.",
            "Invalid footprint ID.",
            "Geçersiz footprint ID.",
            "Неверный идентификатор footprint."
        ),

        ["BASKET_NOT_FOUND"] = (
            "Səbət tapılmadı.",
            "Basket not found.",
            "Sepet bulunamadı.",
            "Корзина не найдена."
        ),

        // ====== COUPON KEYS ======
        ["COUPONS_RETURNED"] = (
            "Kuponlar uğurla qaytarıldı.",
            "Coupons returned successfully.",
            "Kuponlar başarıyla döndürüldü.",
            "Купоны успешно возвращены."
        ),
        ["COUPON_RETURNED"] = (
            "Kupon uğurla qaytarıldı.",
            "Coupon returned successfully.",
            "Kupon başarıyla döndürüldü.",
            "Купон успешно возвращён."
        ),
        ["COUPON_CREATED"] = (
            "Kupon uğurla yaradıldı.",
            "Coupon created successfully.",
            "Kupon başarıyla oluşturuldu.",
            "Купон успешно создан."
        ),
        ["COUPON_UPDATED"] = (
            "Kupon uğurla yeniləndi.",
            "Coupon updated successfully.",
            "Kupon başarıyla güncellendi.",
            "Купон успешно обновлён."
        ),
        ["COUPON_DELETED"] = (
            "Kupon uğurla silindi.",
            "Coupon deleted successfully.",
            "Kupon başarıyla silindi.",
            "Купон успешно удалён."
        ),
        ["COUPON_ACTIVE_STATUS_UPDATED"] = (
            "Kuponun aktivlik statusu yeniləndi.",
            "Coupon active status has been updated.",
            "Kuponun aktiflik durumu güncellendi.",
            "Статус активности купона обновлён."
        ),


        ["COUPON_INACTIVE"] = (
            "Kupon aktiv deyil.",
            "Coupon is inactive.",
            "Kupon aktif değil.",
            "Купон неактивен."
        ),

        ["COUPON_EXPIRED"] = (
            "Kuponun müddəti bitib.",
            "Coupon has expired.",
            "Kuponun süresi doldu.",
            "Срок действия купона истёк."
        ),

        ["COUPON_NOT_STARTED"] = (
            "Kupon hələ aktiv deyil.",
            "Coupon is not active yet.",
            "Kupon henüz aktif değil.",
            "Купон ещё не активен."
        ),

        ["COUPON_MIN_AMOUNT"] = (
            "Minimum alış məbləği təmin olunmayıb.",
            "Minimum purchase amount not reached.",
            "Minimum alış tutarı sağlanmadı.",
            "Минимальная сумма покупки не достигнута."
        ),

        ["COUPON_USAGE_LIMIT"] = (
            "Kupon istifadə limiti dolub.",
            "Coupon usage limit reached.",
            "Kupon kullanım limiti doldu.",
            "Лимит использования купона достигнут."
        ),

        ["COUPON_USER_LIMIT"] = (
                "Bu kuponu artıq istifadə etmisiniz.",
                "You have already used this coupon.",
                "Bu kuponu zaten kullandınız.",
                "Вы уже использовали этот купон."
            )
,

// ====== COUPON – ERRORS / VALIDATION KEYS (service ilə uyğundur) ======
        ["UNIQE_CUPON"] = ( // service-də bu açarı istifadə etmisən — eynisini saxladım
                "Bu kupon kodu artıq mövcuddur.",
                "This coupon code already exists.",
                "Bu kupon kodu zaten mevcut.",
                "Такой код купона уже существует."
            ),
        ["NOT_FOUND_CUPON"] = (
            "Kupon tapılmadı.",
            "Coupon not found.",
            "Kupon bulunamadı.",
            "Купон не найден."
        ),
        ["INVALID_CUPON_ID"] = (
            "Kupon ID-si yanlışdır!",
            "Invalid coupon ID!",
            "Geçersiz kupon ID!",
            "Неверный идентификатор купона!"
        ),
        ["CUPON_CODE_REQUIRED"] = (
            "Kupon kodu tələb olunur.",
            "Coupon code is required.",
            "Kupon kodu gereklidir.",
            "Требуется код купона."
        ),
        ["BASKET_ITEM_NOT_FOUND"] = (
            "Səbət elementi tapılmadı.",
            "Basket item not found.",
            "Sepet öğesi bulunamadı.",
            "Элемент корзины не найден."
        ),

        ["INVALID_VARIANT_ID"] = (
            "Variant ID formatı yanlışdır!",
            "Invalid variant ID format!",
            "Geçersiz varyant ID formatı!",
            "Неверный формат идентификатора варианта!"
        ),

        ["INVALID_USER_ID"] = (
            "İstifadəçi ID-si yanlışdır!",
            "Invalid user ID!",
            "Geçersiz kullanıcı ID!",
            "Неверный идентификатор пользователя!"
        ),
        // ====== ORDER KEYS ======
        ["ORDER_CREATED"] = (
            "Sifariş uğurla yaradıldı.",
            "Order has been created successfully.",
            "Sipariş başarıyla oluşturuldu.",
            "Заказ успешно создан."
        ),
        ["ORDER_RETURNED"] = (
            "Sifariş uğurla qaytarıldı.",
            "Order returned successfully.",
            "Sipariş başarıyla döndürüldü.",
            "Заказ успешно возвращён."
        ),
        ["ORDERS_RETURNED"] = (
            "Sifarişlər uğurla qaytarıldı.",
            "Orders returned successfully.",
            "Siparişler başarıyla döndürüldü.",
            "Заказы успешно возвращены."
        ),
        ["BASKET_EMPTY"] = (
            "Səbət boşdur.",
            "Basket is empty.",
            "Sepet boş.",
            "Корзина пуста."
        ),
        ["OUT_OF_STOCK"] = (
            "Məhsul stokda yoxdur.",
            "Product is out of stock.",
            "Ürün stokta yok.",
            "Товара нет в наличии."
        ),
        ["EMAIL_REQUIRED"] = (
            "E-poçt tələb olunur.",
            "Email is required.",
            "E-posta gereklidir.",
            "Требуется электронная почта."
        ),
        ["USER_NOT_FOUND"] = (
            "İstifadəçi tapılmadı.",
            "User not found.",
            "Kullanıcı bulunamadı.",
            "Пользователь не найден."
        ),
        ["VARIANT_NOT_FOUND"] = (
            "Variant tapılmadı.",
            "Variant not found.",
            "Varyant bulunamadı.",
            "Вариант не найден."
        )
,// ====== ORDER (EXTRA KEYS) ======
["ORDER_DETAIL_RETURNED"] = (
    "Sifariş detalları uğurla qaytarıldı.",
    "Order details returned successfully.",
    "Sipariş detayları başarıyla döndürüldü.",
    "Детали заказа успешно возвращены."
),

["ORDER_NOT_FOUND"] = (
    "Sifariş tapılmadı.",
    "Order not found.",
    "Sipariş bulunamadı.",
    "Заказ не найден."
),

["INVALID_ORDER_ID"] = (
    "Sifariş ID formatı yanlışdır!",
    "Invalid order ID format!",
    "Geçersiz sipariş ID formatı!",
    "Неверный формат идентификатора заказа!"
),

["INVALID_STATUS"] = (
    "Status yanlışdır. Yalnız: pending, delivered, cancelled.",
    "Invalid status. Allowed: pending, delivered, cancelled.",
    "Geçersiz durum. İzin verilen: pending, delivered, cancelled.",
    "Неверный статус. Допустимые: pending, delivered, cancelled."
),

["INVALID_DATE_FILTER"] = (
    "Tarix filtri yanlışdır. Yalnız: week, month, year.",
    "Invalid date filter. Allowed: week, month, year.",
    "Geçersiz tarih filtresi. İzin verilen: week, month, year.",
    "Неверный фильтр даты. Допустимые: week, month, year."
),

["PAYMENT_METHOD_REQUIRED"] = (
    "Ödəniş metodu tələb olunur.",
    "Payment method is required.",
    "Ödeme yöntemi gereklidir.",
    "Требуется способ оплаты."
),

["INVALID_PAYMENT_METHOD"] = (
    "Ödəniş metodu yanlışdır. Yalnız: card, debit_card, cash.",
    "Invalid payment method. Allowed: card, debit_card, cash.",
    "Geçersiz ödeme yöntemi. İzin verilen: card, debit_card, cash.",
    "Неверный способ оплаты. Допустимые: card, debit_card, cash."
),["PAYMENT_STARTED"] = (
    "Ödəniş prosesi başladıldı.",
    "Payment process started.",
    "Ödeme süreci başlatıldı.",
    "Процесс оплаты запущен."
),
["PAYMENT_CALLBACK_OK"] = (
    "Ödəniş nəticəsi qəbul olundu.",
    "Payment result received.",
    "Ödeme sonucu alındı.",
    "Результат оплаты получен."
),
["PAYMENT_STATUS_RETURNED"] = (
    "Ödəniş statusu qaytarıldı.",
    "Payment status returned.",
    "Ödeme durumu döndürüldü.",
    "Статус оплаты возвращён."
),
["PAYMENT_START_FAILED"] = (
    "Ödənişi başlatmaq mümkün olmadı.",
    "Failed to start payment.",
    "Ödeme başlatılamadı.",
    "Не удалось начать оплату."
),
["ORDER_ALREADY_PAID"] = (
    "Bu sifariş artıq ödənilib.",
    "This order is already paid.",
    "Bu sipariş zaten ödendi.",
    "Этот заказ уже оплачен."
),
["INVALID_ORDER_ID"] = (
    "Sifariş ID formatı yanlışdır!",
    "Invalid order id format!",
    "Geçersiz sipariş id formatı!",
    "Неверный формат id заказа!"
),
["ORDER_NOT_FOUND"] = (
    "Sifariş tapılmadı.",
    "Order not found.",
    "Sipariş bulunamadı.",
    "Заказ не найден."
),
["EPOINT_SIGNATURE_MISMATCH"] = (
    "Epoint imzası uyğunsuzdur.",
    "Epoint signature mismatch.",
    "Epoint imzası uyuşmuyor.",
    "Подпись Epoint не совпадает."
),["NEWSLETTER_SUBSCRIBED"] = (
    "Abunəlik uğurla aktiv edildi.",
    "Subscription activated successfully.",
    "Abonelik başarıyla aktif edildi.",
    "Подписка успешно активирована."
),
["CAMPAIGN_CREATED"] = (
    "Kampaniya uğurla yaradıldı.",
    "Campaign created successfully.",
    "Kampanya başarıyla oluşturuldu.",
    "Кампания успешно создана."
),

["USER_ID_NOT_FOUND"] = (
    "İstifadəçi identifikatoru tapılmadı.",
    "User identifier not found.",
    "Kullanıcı kimliği bulunamadı.",
    "Идентификатор пользователя не найден."
),

["CAMPAIGN_CANCELLED"] = (
    "Kampaniya ləğv edildi.",
    "Campaign has been cancelled.",
    "Kampanya iptal edildi.",
    "Кампания была отменена."
),

["CAMPAIGNS_RETURNED"] = (
    "Kampaniyalar uğurla qaytarıldı.",
    "Campaigns retrieved successfully.",
    "Kampanyalar başarıyla getirildi.",
    "Кампании успешно получены."
),

["CAMPAIGN_RETURNED"] = (
    "Kampaniya məlumatları qaytarıldı.",
    "Campaign details retrieved successfully.",
    "Kampanya detayları başarıyla getirildi.",
    "Данные кампании успешно получены."
),

["CAMPAIGN_NOT_FOUND"] = (
    "Kampaniya tapılmadı.",
    "Campaign not found.",
    "Kampanya bulunamadı.",
    "Кампания не найдена."
),

["CAMPAIGN_ALREADY_SENT"] = (
    "Kampaniya artıq göndərilib.",
    "Campaign has already been sent.",
    "Kampanya zaten gönderildi.",
    "Кампания уже отправлена."
),

["CAMPAIGN_CANNOT_BE_CANCELLED"] = (
    "Göndərilmiş kampaniya ləğv edilə bilməz.",
    "A sent campaign cannot be cancelled.",
    "Gönderilmiş kampanya iptal edilemez.",
    "Отправленную кампанию нельзя отменить."
),

["CAMPAIGN_CANNOT_BE_SCHEDULED"] = (
    "Bu kampaniya planlaşdırıla bilməz.",
    "This campaign cannot be scheduled.",
    "Bu kampanya planlanamaz.",
    "Эту кампанию нельзя запланировать."
),

["SERVER_ERROR"] = (
    "Server xətası baş verdi.",
    "An internal server error occurred.",
    "Sunucu hatası oluştu.",
    "Произошла внутренняя ошибка сервера."
),
["NEWSLETTER_UNSUBSCRIBED"] = (
    "Abunəlik deaktiv edildi.",
    "Subscription has been deactivated.",
    "Abonelik devre dışı bırakıldı.",
    "Подписка отключена."
),
["NEWSLETTER_ME_RETURNED"] = (
    "Abunəlik məlumatları qaytarıldı.",
    "Subscription info returned.",
    "Abonelik bilgisi döndürüldü.",
    "Данные подписки возвращены."
),
["NEWSLETTER_ENSURED"] = (
    "Abunəlik yaradıldı və ya yeniləndi.",
    "Subscription ensured (created or updated).",
    "Abonelik oluşturuldu veya güncellendi.",
    "Подписка создана или обновлена."
),
["SUBSCRIBER_NOT_FOUND"] = (
    "Abunə tapılmadı.",
    "Subscriber not found.",
    "Abone bulunamadı.",
    "Подписчик не найден."
),
["INVALID_EMAIL"] = (
    "E-poçt formatı yanlışdır.",
    "Invalid email format.",
    "Geçersiz e-posta formatı.",
    "Неверный формат электронной почты."
)

            
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
