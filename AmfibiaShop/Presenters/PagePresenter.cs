using System;
using System.Collections.Generic;
using System.Linq;
using AmfibiaShop.Models;
using AmfibiaShop.Presenters.Display;

namespace AmfibiaShop.Presenters;

/// <summary>
/// Общая часть страничных Представителей: доступ к моделям, навигация,
/// подготовка карточек товаров. При уходе со страницы Представитель
/// освобождается (Dispose) и отписывается от событий моделей.
/// </summary>
public abstract class PagePresenter : IDisposable
{
    protected PagePresenter(ShopContext shop, INavigator navigator)
    {
        Shop = shop;
        Navigator = navigator;
    }

    protected ShopContext Shop { get; }

    protected INavigator Navigator { get; }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    protected string Money(decimal amountByn) => Format.Money(amountByn, Shop.Currency);

    /// <summary>Карточка товара для сетки: цена в текущей валюте, значок, избранное, рейтинг.</summary>
    protected ProductCardData Card(Product p)
    {
        (string? badge, string kind) = p switch
        {
            { Preowned: true } => ("Ресейл", "resale"),
            { DiscountPercent: > 0 } => ($"−{p.DiscountPercent}%", "sale"),
            { IsNew: true } => ("Новинка", "new"),
            { Iconic: true } => ("Культовая модель", "iconic"),
            _ => ((string?)null, "")
        };

        double rating = Shop.Reviews.Average(p.Id);
        return new ProductCardData
        {
            Id = p.Id,
            Product = p,
            Brand = p.Brand,
            Name = p.Name,
            Category = p.Category,
            Price = Money(p.Price),
            OldPrice = p.DiscountPercent > 0 ? Money(p.OldPrice!.Value) : null,
            Badge = badge,
            BadgeKind = kind,
            IsFavorite = Shop.Wishlist.Contains(p.Id),
            Rating = rating > 0 ? $"★ {rating.ToString("0.0", Format.Ru)}" : string.Empty
        };
    }

    protected IReadOnlyList<ProductCardData> Cards(IEnumerable<Product> products) => products.Select(Card).ToList();

    protected static BrandCardData BrandCard(Brand b, int productCount) => new()
    {
        Slug = b.Slug,
        Name = b.Name,
        Subtitle = $"{b.CountryName} · с {b.Founded} года",
        ProductsText = Format.Plural(productCount, "вещь", "вещи", "вещей"),
        Bg = b.Palette.Bg,
        Bg2 = b.Palette.Bg2,
        Fg = b.Palette.Fg,
        Accent = b.Palette.Accent,
        FontKey = b.Palette.Font
    };

    /// <summary>Избранное: переключает и показывает уведомление.</summary>
    protected void ToggleFavorite(string productId)
    {
        Shop.Wishlist.Toggle(productId);
        Navigator.Notify(Shop.Wishlist.Message);
    }
}
