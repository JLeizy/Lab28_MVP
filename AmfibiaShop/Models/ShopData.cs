using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace AmfibiaShop.Models;

/// <summary>
/// Данные сайта AMFIBIA: каталог, бренды, пользователи, справочники и демо-заказы.
/// Загружаются из файла Data/amfibia.json, встроенного в программу.
/// </summary>
public class ShopData
{
    private Dictionary<string, Product> productById = new();
    private Dictionary<string, Brand> brandBySlug = new();
    private Dictionary<string, OrderStatusInfo> statusByCode = new();

    public List<Brand> Brands { get; set; } = new();
    public List<Product> Products { get; set; } = new();
    public List<List<SilhouetteShape>> Silhouettes { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public List<DeliveryMethod> Delivery { get; set; } = new();
    public List<PaymentMethod> Payments { get; set; } = new();
    public List<PromoCode> Promos { get; set; } = new();
    public List<OrderStatusInfo> Statuses { get; set; } = new();

    /// <summary>Порядок статусов заказа от оформления до вручения.</summary>
    public List<string> Flow { get; set; } = new();

    public List<Warehouse> Warehouses { get; set; } = new();
    public CurrencyRates Rates { get; set; } = new();
    public List<StockRow> Stock { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();

    /// <summary>С какой суммы доставка бесплатная (BYN).</summary>
    public decimal FreeDeliveryFrom { get; set; } = 3000;

    public static ShopData LoadEmbedded()
    {
        Assembly assembly = typeof(ShopData).Assembly;
        using Stream stream = assembly.GetManifestResourceStream("amfibia.json")
            ?? throw new InvalidOperationException("В программе нет файла данных amfibia.json.");
        return Load(stream);
    }

    public static ShopData Load(Stream stream)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        ShopData data = JsonSerializer.Deserialize<ShopData>(stream, options)
            ?? throw new InvalidOperationException("Файл данных пуст.");
        data.Link();
        return data;
    }

    /// <summary>Связывает товары с брендами и силуэтами, строит словари для быстрого поиска.</summary>
    private void Link()
    {
        brandBySlug = Brands.ToDictionary(b => b.Slug);
        productById = Products.ToDictionary(p => p.Id);
        statusByCode = Statuses.ToDictionary(s => s.Code);
        foreach (Product product in Products)
        {
            product.BrandInfo = brandBySlug.GetValueOrDefault(product.BrandSlug);
            if (product.Silhouette >= 0 && product.Silhouette < Silhouettes.Count)
            {
                product.Shapes = Silhouettes[product.Silhouette];
            }
        }
    }

    public Product? FindProduct(string id) => productById.GetValueOrDefault(id);

    public Brand? FindBrand(string slug) => brandBySlug.GetValueOrDefault(slug);

    public string StatusName(string code) => statusByCode.TryGetValue(code, out var s) ? s.Name : code;

    public string StatusHelp(string code) => statusByCode.TryGetValue(code, out var s) ? s.Help : string.Empty;

    public DeliveryMethod? FindDelivery(string code) => Delivery.FirstOrDefault(d => d.Code == code);

    public PaymentMethod? FindPayment(string code) => Payments.FirstOrDefault(p => p.Code == code);
}
