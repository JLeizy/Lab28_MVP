using System;
using System.Linq;
using AmfibiaShop.Models;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>ПРЕДСТАВИТЕЛЬ главной страницы: подборки новинок, культовых моделей, скидок и модных домов.</summary>
public class HomePresenter : PagePresenter
{
    private readonly IHomeView view;

    public HomePresenter(IHomeView view, ShopContext shop, INavigator navigator) : base(shop, navigator)
    {
        this.view = view;

        view.ProductOpened += OnProductOpened;
        view.FavoriteToggled += OnFavoriteToggled;
        view.BrandOpened += OnBrandOpened;
        view.CollectionRequested += OnCollectionRequested;
        Shop.Currency.Changed += OnModelChanged;
        Shop.Wishlist.Changed += OnModelChanged;

        var products = Shop.Catalog.Products;
        view.ShowHero(
            $"{Shop.User.FirstName}, добро пожаловать в AMFIBIA",
            "Маркетплейс 15 модных домов: от Balenciaga до Chrome Hearts. Новые вещи и подлинный ресейл с экспертизой.",
            new[]
            {
                new SpecRow(products.Count.ToString(), "вещей в каталоге"),
                new SpecRow(Shop.Catalog.Brands.Count.ToString(), "модных домов"),
                new SpecRow(products.Count(p => p.Preowned).ToString(), "вещей с ресейла"),
                new SpecRow(products.Count(p => p.IsNew).ToString(), "новинок сезона")
            });
        view.ShowBrands(Shop.Catalog.Brands
            .Select(b => BrandCard(b, products.Count(p => p.BrandSlug == b.Slug)))
            .ToList());
        Render();
    }

    public override void Dispose()
    {
        Shop.Currency.Changed -= OnModelChanged;
        Shop.Wishlist.Changed -= OnModelChanged;
        base.Dispose();
    }

    private void Render()
    {
        view.ShowNew(Cards(Shop.Catalog.NewArrivals(8)));
        view.ShowIconic(Cards(Shop.Catalog.IconicPieces(8)));
        view.ShowSale(Cards(Shop.Catalog.OnSale(8)));
    }

    private void OnModelChanged(object? sender, EventArgs e) => Render();

    private void OnProductOpened(object? sender, string id) => Navigator.GoProduct(id);

    private void OnFavoriteToggled(object? sender, string id) => ToggleFavorite(id);

    private void OnBrandOpened(object? sender, string slug) => Navigator.GoBrand(slug);

    private void OnCollectionRequested(object? sender, string key)
    {
        var filter = key switch
        {
            "new" => new CatalogFilter { OnlyNew = true, Sort = ProductSort.New },
            "iconic" => new CatalogFilter { OnlyIconic = true },
            "sale" => new CatalogFilter { OnlySale = true, Sort = ProductSort.Discount },
            "resale" => new CatalogFilter { OnlyPreowned = true },
            _ => new CatalogFilter()
        };
        Navigator.GoCatalog(filter);
    }
}
