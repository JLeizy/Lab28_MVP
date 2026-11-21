using AmfibiaShop.Models;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>
/// Переходы между страницами. Реализует ShellPresenter —
/// страничные Представители вызывают эти методы и не знают, как устроено окно.
/// </summary>
public interface INavigator
{
    void GoHome();
    void GoCatalog(CatalogFilter? preset = null);
    void GoProduct(string productId);
    void GoBrands();
    void GoBrand(string slug);
    void GoWishlist();
    void GoCompare();
    void GoCart();
    void GoCheckout();
    void GoOrders(string? orderNumber = null);
    void GoProfile();
    void GoBack();

    /// <summary>Выход из аккаунта.</summary>
    void Logout();

    /// <summary>Короткое всплывающее уведомление.</summary>
    void Notify(string message, bool isError = false);
}

/// <summary>
/// Создание страниц-Видов. Реализуется в слое Видов (WPF),
/// чтобы Представители не зависели от конкретных окон.
/// </summary>
public interface IViewFactory
{
    IHomeView CreateHome();
    ICatalogView CreateCatalog();
    IProductView CreateProduct();
    IBrandsView CreateBrands();
    IBrandView CreateBrand();
    IWishlistView CreateWishlist();
    ICompareView CreateCompare();
    ICartView CreateCart();
    ICheckoutView CreateCheckout();
    IOrdersView CreateOrders();
    IProfileView CreateProfile();
}
