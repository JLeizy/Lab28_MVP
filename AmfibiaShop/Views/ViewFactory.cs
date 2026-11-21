using AmfibiaShop.Presenters;
using AmfibiaShop.Views.Pages;

namespace AmfibiaShop.Views;

/// <summary>Создаёт WPF-страницы для Представителей (они знают только интерфейсы видов).</summary>
public class ViewFactory : IViewFactory
{
    public IHomeView CreateHome() => new HomeView();
    public ICatalogView CreateCatalog() => new CatalogView();
    public IProductView CreateProduct() => new ProductView();
    public IBrandsView CreateBrands() => new BrandsView();
    public IBrandView CreateBrand() => new BrandView();
    public IWishlistView CreateWishlist() => new WishlistView();
    public ICompareView CreateCompare() => new CompareView();
    public ICartView CreateCart() => new CartView();
    public ICheckoutView CreateCheckout() => new CheckoutView();
    public IOrdersView CreateOrders() => new OrdersView();
    public IProfileView CreateProfile() => new ProfileView();
}
