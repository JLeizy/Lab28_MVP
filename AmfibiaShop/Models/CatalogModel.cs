using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AmfibiaShop.Models;

/// <summary>Сортировка каталога.</summary>
public enum ProductSort
{
    Popular,
    New,
    PriceAscending,
    PriceDescending,
    Discount,
    Name
}

/// <summary>Условия поиска товаров.</summary>
public class CatalogFilter
{
    public string Search { get; set; } = string.Empty;
    public string? Group { get; set; }
    public string? Category { get; set; }
    public HashSet<string> Brands { get; set; } = new();
    public string? Gender { get; set; }
    public HashSet<string> Colors { get; set; } = new();
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool OnlyNew { get; set; }
    public bool OnlySale { get; set; }
    public bool OnlyPreowned { get; set; }
    public bool OnlyIconic { get; set; }
    public ProductSort Sort { get; set; } = ProductSort.Popular;
}

/// <summary>Пара «код — название» для справочников каталога.</summary>
public record CatalogOption(string Key, string Title, int Count, string? Extra = null);

/// <summary>
/// МОДЕЛЬ каталога: поиск, фильтры, сортировка, страницы, подборки.
/// </summary>
public class CatalogModel
{
    private static readonly StringComparer Russian = StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), true);
    private static readonly string[] GroupOrder = { "clothing", "shoes", "bags", "accessories", "jewelry", "fragrance", "home" };

    private readonly ShopData data;

    public CatalogModel(ShopData data)
    {
        this.data = data;
        Found = data.Products;
    }

    public IReadOnlyList<Product> Products => data.Products;

    public IReadOnlyList<Brand> Brands => data.Brands;

    /// <summary>Результат последнего поиска.</summary>
    public IReadOnlyList<Product> Found { get; private set; }

    public Product? Find(string id) => data.FindProduct(id);

    public Brand? FindBrand(string slug) => data.FindBrand(slug);

    // ----- справочники для фильтров -----

    public IReadOnlyList<CatalogOption> Groups() =>
        GroupOrder
            .Select(slug => (slug, items: data.Products.Where(p => p.GroupSlug == slug).ToList()))
            .Where(x => x.items.Count > 0)
            .Select(x => new CatalogOption(x.slug, x.items[0].Group, x.items.Count))
            .ToList();

    public IReadOnlyList<CatalogOption> Categories(string? group) =>
        data.Products
            .Where(p => group == null || p.GroupSlug == group)
            .GroupBy(p => (p.CategorySlug, p.Category))
            .Select(g => new CatalogOption(g.Key.CategorySlug, g.Key.Category, g.Count()))
            .OrderByDescending(o => o.Count)
            .ToList();

    public IReadOnlyList<CatalogOption> BrandOptions() =>
        data.Brands
            .Select(b => new CatalogOption(b.Slug, b.Name, data.Products.Count(p => p.BrandSlug == b.Slug)))
            .OrderBy(o => o.Title, Russian)
            .ToList();

    public IReadOnlyList<CatalogOption> Genders() => new[]
    {
        new CatalogOption("women", "Женское", data.Products.Count(p => p.GenderCode == "women")),
        new CatalogOption("men", "Мужское", data.Products.Count(p => p.GenderCode == "men")),
        new CatalogOption("unisex", "Унисекс", data.Products.Count(p => p.GenderCode == "unisex"))
    };

    public IReadOnlyList<CatalogOption> Colors() =>
        data.Products
            .GroupBy(p => (p.ColorCode, p.Color, p.ColorHex))
            .Select(g => new CatalogOption(g.Key.ColorCode, g.Key.Color, g.Count(), g.Key.ColorHex))
            .OrderByDescending(o => o.Count)
            .ToList();

    public decimal MinPrice => data.Products.Min(p => p.Price);

    public decimal MaxPrice => data.Products.Max(p => p.Price);

    // ----- поиск -----

    public IReadOnlyList<Product> Search(CatalogFilter filter)
    {
        IEnumerable<Product> query = data.Products;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string[] words = filter.Search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            query = query.Where(p => words.All(w =>
                p.Name.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                p.NameEn.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                p.Brand.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Contains(w, StringComparison.OrdinalIgnoreCase)));
        }

        if (filter.Group != null) query = query.Where(p => p.GroupSlug == filter.Group);
        if (filter.Category != null) query = query.Where(p => p.CategorySlug == filter.Category);
        if (filter.Brands.Count > 0) query = query.Where(p => filter.Brands.Contains(p.BrandSlug));
        if (filter.Gender != null) query = query.Where(p => p.GenderCode == filter.Gender);
        if (filter.Colors.Count > 0) query = query.Where(p => filter.Colors.Contains(p.ColorCode));
        if (filter.MinPrice != null) query = query.Where(p => p.Price >= filter.MinPrice);
        if (filter.MaxPrice != null) query = query.Where(p => p.Price <= filter.MaxPrice);
        if (filter.OnlyNew) query = query.Where(p => p.IsNew);
        if (filter.OnlySale) query = query.Where(p => p.DiscountPercent > 0);
        if (filter.OnlyPreowned) query = query.Where(p => p.Preowned);
        if (filter.OnlyIconic) query = query.Where(p => p.Iconic);

        query = filter.Sort switch
        {
            ProductSort.New => query.OrderByDescending(p => p.IsNew).ThenByDescending(p => p.Views),
            ProductSort.PriceAscending => query.OrderBy(p => p.Price),
            ProductSort.PriceDescending => query.OrderByDescending(p => p.Price),
            ProductSort.Discount => query.OrderByDescending(p => p.DiscountPercent).ThenByDescending(p => p.Views),
            ProductSort.Name => query.OrderBy(p => p.Name, Russian),
            _ => query.OrderByDescending(p => p.Iconic).ThenByDescending(p => p.Views)
        };

        Found = query.ToList();
        return Found;
    }

    /// <summary>Номер страницы приводится к допустимому диапазону.</summary>
    public static IReadOnlyList<Product> Page(IReadOnlyList<Product> items, int page, int pageSize, out int pageCount, out int actualPage)
    {
        pageCount = Math.Max(1, (int)Math.Ceiling(items.Count / (double)pageSize));
        actualPage = Math.Clamp(page, 1, pageCount);
        return items.Skip((actualPage - 1) * pageSize).Take(pageSize).ToList();
    }

    // ----- подборки -----

    public IReadOnlyList<Product> NewArrivals(int count) =>
        data.Products.Where(p => p.IsNew).OrderByDescending(p => p.Views).Take(count).ToList();

    public IReadOnlyList<Product> IconicPieces(int count) =>
        data.Products.Where(p => p.Iconic && !p.Preowned).OrderByDescending(p => p.Views).Take(count).ToList();

    public IReadOnlyList<Product> OnSale(int count) =>
        data.Products.Where(p => p.DiscountPercent > 0).OrderByDescending(p => p.DiscountPercent).Take(count).ToList();

    public IReadOnlyList<Product> ByBrand(string slug, int count) =>
        data.Products.Where(p => p.BrandSlug == slug).OrderByDescending(p => p.Iconic).ThenByDescending(p => p.Views).Take(count).ToList();

    /// <summary>Похожие: та же подкатегория, сначала тот же бренд.</summary>
    public IReadOnlyList<Product> Similar(Product product, int count) =>
        data.Products
            .Where(p => p.Id != product.Id && p.Model != product.Model && p.CategorySlug == product.CategorySlug)
            .OrderByDescending(p => p.BrandSlug == product.BrandSlug)
            .ThenByDescending(p => p.Views)
            .Take(count)
            .ToList();

    /// <summary>Другие цвета той же модели (общий код модели, как на сайте).</summary>
    public IReadOnlyList<Product> Variants(Product product) =>
        data.Products.Where(p => p.Model == product.Model && p.BrandSlug == product.BrandSlug).ToList();
}
