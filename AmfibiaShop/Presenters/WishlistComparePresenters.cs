using System;
using System.Collections.Generic;
using System.Linq;
using AmfibiaShop.Models;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>ПРЕДСТАВИТЕЛЬ избранного.</summary>
public class WishlistPresenter : PagePresenter
{
    private readonly IWishlistView view;

    public WishlistPresenter(IWishlistView view, ShopContext shop, INavigator navigator) : base(shop, navigator)
    {
        this.view = view;

        view.ProductOpened += (_, id) => Navigator.GoProduct(id);
        view.FavoriteToggled += (_, id) => ToggleFavorite(id);
        view.ClearRequested += (_, _) =>
        {
            Shop.Wishlist.Clear();
            Navigator.Notify(Shop.Wishlist.Message);
        };
        view.CatalogRequested += (_, _) => Navigator.GoCatalog();
        Shop.Wishlist.Changed += OnModelChanged;
        Shop.Currency.Changed += OnModelChanged;

        Render();
    }

    public override void Dispose()
    {
        Shop.Wishlist.Changed -= OnModelChanged;
        Shop.Currency.Changed -= OnModelChanged;
        base.Dispose();
    }

    private void Render()
    {
        var items = Shop.Wishlist.Items;
        decimal total = items.Sum(p => p.Price);
        view.ShowItems(Cards(items), items.Count == 0
            ? "Здесь пока пусто — нажмите ♥ на понравившейся вещи"
            : $"{Format.Plural(items.Count, "вещь", "вещи", "вещей")} на {Money(total)}");
    }

    private void OnModelChanged(object? sender, EventArgs e) => Render();
}

/// <summary>ПРЕДСТАВИТЕЛЬ сравнения товаров (до 4 вещей рядом).</summary>
public class ComparePresenter : PagePresenter
{
    public static readonly string[] RowTitles =
    {
        "Цена", "Бренд", "Категория", "Цвет", "Материал", "Для кого", "Состояние", "Рейтинг", "Размеры"
    };

    private readonly ICompareView view;

    public ComparePresenter(ICompareView view, ShopContext shop, INavigator navigator) : base(shop, navigator)
    {
        this.view = view;

        view.ProductOpened += (_, id) => Navigator.GoProduct(id);
        view.RemoveRequested += (_, id) => Shop.Compare.Remove(id);
        view.ClearRequested += (_, _) => Shop.Compare.Clear();
        view.CatalogRequested += (_, _) => Navigator.GoCatalog();
        Shop.Compare.Changed += OnModelChanged;
        Shop.Currency.Changed += OnModelChanged;

        Render();
    }

    public override void Dispose()
    {
        Shop.Compare.Changed -= OnModelChanged;
        Shop.Currency.Changed -= OnModelChanged;
        base.Dispose();
    }

    private void Render()
    {
        IReadOnlyList<Product> items = Shop.Compare.Items;
        decimal? minPrice = items.Count > 1 ? items.Min(p => p.Price) : null;

        var columns = items.Select(p =>
        {
            double rating = Shop.Reviews.Average(p.Id);
            return new CompareColumnData
            {
                Id = p.Id,
                Product = p,
                Brand = p.Brand,
                Name = p.Name,
                IsBestPrice = minPrice == p.Price,
                Values = new[]
                {
                    Money(p.Price) + (p.DiscountPercent > 0 ? $"  (−{p.DiscountPercent}%)" : ""),
                    p.Brand,
                    $"{p.Group} · {p.Category}",
                    p.Color,
                    p.Material,
                    p.Gender,
                    p.Condition,
                    rating > 0 ? $"★ {rating.ToString("0.0", Format.Ru)} ({Shop.Reviews.Count(p.Id)})" : "нет отзывов",
                    string.Join(", ", p.Sizes)
                }
            };
        }).ToList();

        view.ShowComparison(RowTitles, columns, items.Count == 0
            ? "Добавьте вещи к сравнению на странице товара — кнопка «Сравнить»"
            : $"{Format.Plural(items.Count, "вещь", "вещи", "вещей")} из {CompareModel.MaxItems} возможных");
    }

    private void OnModelChanged(object? sender, EventArgs e) => Render();
}
