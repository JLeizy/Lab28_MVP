using System.Collections.Generic;
using System.Linq;
using AmfibiaShop.Models;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>ПРЕДСТАВИТЕЛЬ списка модных домов.</summary>
public class BrandsPresenter : PagePresenter
{
    public BrandsPresenter(IBrandsView view, ShopContext shop, INavigator navigator) : base(shop, navigator)
    {
        view.BrandOpened += (_, slug) => Navigator.GoBrand(slug);

        var products = Shop.Catalog.Products;
        var cards = Shop.Catalog.Brands
            .OrderBy(b => b.Name)
            .Select(b => BrandCard(b, products.Count(p => p.BrandSlug == b.Slug)))
            .ToList();
        view.ShowBrands(cards, $"{Format.Plural(cards.Count, "модный дом", "модных дома", "модных домов")} — история, дизайнеры и коллекции");
    }
}

/// <summary>ПРЕДСТАВИТЕЛЬ страницы бренда: история, креативные директора, коллекции и вещи бренда.</summary>
public class BrandPresenter : PagePresenter
{
    private readonly IBrandView view;
    private readonly Brand? brand;

    public BrandPresenter(IBrandView view, ShopContext shop, INavigator navigator, string slug) : base(shop, navigator)
    {
        this.view = view;
        brand = Shop.Catalog.FindBrand(slug);

        view.ProductOpened += (_, id) => Navigator.GoProduct(id);
        view.FavoriteToggled += (_, id) => ToggleFavorite(id);
        view.AllProductsRequested += (_, _) =>
        {
            if (brand != null) Navigator.GoCatalog(new CatalogFilter { Brands = new HashSet<string> { brand.Slug } });
        };
        Shop.Currency.Changed += OnModelChanged;
        Shop.Wishlist.Changed += OnModelChanged;

        if (brand == null)
        {
            Navigator.Notify("Бренд не найден.", isError: true);
            return;
        }

        view.ShowBrand(Details(brand));
        RenderProducts();
    }

    public override void Dispose()
    {
        Shop.Currency.Changed -= OnModelChanged;
        Shop.Wishlist.Changed -= OnModelChanged;
        base.Dispose();
    }

    private BrandDetailsData Details(Brand b)
    {
        int count = Shop.Catalog.Products.Count(p => p.BrandSlug == b.Slug);
        string origin = new Brand { Country = b.Origin }.CountryName;
        return new BrandDetailsData
        {
            Slug = b.Slug,
            Name = b.Name,
            Tagline = $"{b.CountryName} · основан в {b.Founded} году",
            Description = b.Description,
            Facts = new[]
            {
                new SpecRow("Основан", b.Founded.ToString()),
                new SpecRow("Основатель", b.Founder),
                new SpecRow("Страна дома", b.CountryName),
                new SpecRow("Производство", origin),
                new SpecRow("Креативных директоров", b.Directors.Count.ToString()),
                new SpecRow("Вещей в каталоге", count.ToString())
            },
            Directors = b.Directors.Select(d => new DirectorData(d.Name, d.Period, d.Description, d.KeyWorks)).ToList(),
            Timeline = b.Timeline.Select(t => new TimelineData(t.Year.ToString(), t.Title, t.Description)).ToList(),
            Collections = b.Collections
                .OrderByDescending(c => c.Year).ThenByDescending(c => c.Season)
                .Select(c => new CollectionData(c.Name, $"{c.SeasonName} {c.Year} · {c.Description}", c.IsNew))
                .ToList(),
            Bg = b.Palette.Bg,
            Bg2 = b.Palette.Bg2,
            Fg = b.Palette.Fg,
            Accent = b.Palette.Accent,
            FontKey = b.Palette.Font,
            ProductsText = $"Все вещи {b.Name} — {Format.Plural(count, "вещь", "вещи", "вещей")}"
        };
    }

    private void RenderProducts()
    {
        if (brand != null) view.ShowProducts(Cards(Shop.Catalog.ByBrand(brand.Slug, 8)));
    }

    private void OnModelChanged(object? sender, System.EventArgs e) => RenderProducts();
}
