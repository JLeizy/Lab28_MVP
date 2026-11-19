using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace AmfibiaShop.Models;

/// <summary>Порядок сортировки каталога.</summary>
public enum ProductSort
{
    Default,
    PriceAscending,
    PriceDescending,
    Name
}

/// <summary>
/// МОДЕЛЬ каталога.
/// Загружает товары, ищет и сортирует их, знает размерные сетки.
/// </summary>
public class CatalogModel
{
    // Разделы в том же порядке, что в меню сайта
    private static readonly string[] GroupOrder =
    {
        "Одежда", "Обувь", "Сумки", "Аксессуары", "Ювелирные изделия", "Парфюмерия", "Для дома"
    };

    private static readonly StringComparer RussianComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("ru-RU"), ignoreCase: true);

    private readonly List<Product> products;

    /// <summary>Каталог из файла catalog.json, встроенного в программу.</summary>
    public CatalogModel()
        : this(LoadEmbeddedCatalog())
    {
    }

    /// <summary>Каталог из готового списка товаров.</summary>
    public CatalogModel(IEnumerable<Product> products)
    {
        this.products = products.ToList();
        FoundProducts = this.products;
    }

    /// <summary>Все товары.</summary>
    public IReadOnlyList<Product> Products => products;

    /// <summary>Результат последнего поиска.</summary>
    public IReadOnlyList<Product> FoundProducts { get; private set; }

    /// <summary>Бренды по алфавиту.</summary>
    public IReadOnlyList<string> Brands =>
        products.Select(p => p.Brand).Distinct().OrderBy(b => b, RussianComparer).ToList();

    /// <summary>Разделы, в которых есть товары.</summary>
    public IReadOnlyList<string> Groups =>
        GroupOrder.Where(g => products.Any(p => p.Group == g)).ToList();

    /// <summary>
    /// Поиск товаров. null или пустая строка в фильтре означает «все».
    /// Результат сохраняется в свойство FoundProducts.
    /// </summary>
    public void Search(string? brand, string? group, string? text, ProductSort sort)
    {
        IEnumerable<Product> query = products;

        if (!string.IsNullOrEmpty(brand))
        {
            query = query.Where(p => p.Brand == brand);
        }

        if (!string.IsNullOrEmpty(group))
        {
            query = query.Where(p => p.Group == group);
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            string word = text.Trim();
            query = query.Where(p =>
                p.Name.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                p.Brand.Contains(word, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(word, StringComparison.OrdinalIgnoreCase));
        }

        query = sort switch
        {
            ProductSort.PriceAscending => query.OrderBy(p => p.Price),
            ProductSort.PriceDescending => query.OrderByDescending(p => p.Price),
            ProductSort.Name => query.OrderBy(p => p.Name, RussianComparer),
            _ => query
        };

        FoundProducts = query.ToList(); // обновление модели
    }

    /// <summary>
    /// Размеры товара — те же сетки, что на сайте (db/seed-data/sizes.js).
    /// </summary>
    public IReadOnlyList<string> GetSizes(Product product)
    {
        switch (product.SizeType)
        {
            case "clothing":
                if (product.Gender == "Женское") return new[] { "XS", "S", "M", "L", "XL" };
                if (product.Gender == "Мужское") return new[] { "S", "M", "L", "XL", "XXL" };
                return new[] { "XS", "S", "M", "L", "XL", "XXL" };

            case "shoes":
                if (product.Gender == "Женское") return new[] { "35", "36", "37", "38", "39", "40", "41" };
                if (product.Gender == "Мужское") return new[] { "39", "40", "41", "42", "43", "44", "45", "46" };
                return new[] { "36", "37", "38", "39", "40", "41", "42", "43", "44", "45" };

            case "ring":
                return new[] { "16", "16.5", "17", "17.5", "18", "19", "20" };

            case "volume_ml":
                return new[] { "50 ml", "100 ml" };

            default:
                return new[] { "OS" }; // единый размер
        }
    }

    private static List<Product> LoadEmbeddedCatalog()
    {
        Assembly assembly = typeof(CatalogModel).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream("catalog.json");
        if (stream == null)
        {
            throw new InvalidOperationException("В программе нет файла каталога catalog.json.");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<Product>>(stream, options) ?? new List<Product>();
    }
}
