using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AmfibiaShop.Models;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>
/// ПРЕДСТАВИТЕЛЬ окна магазина.
/// Связывает вид (окно) с моделями каталога и корзины.
/// </summary>
public class ShopPresenter
{
    public const string AllBrands = "Все бренды";
    public const string AllGroups = "Все разделы";

    /// <summary>Названия сортировок в том же порядке, что значения ProductSort.</summary>
    public static readonly string[] SortNames =
    {
        "По умолчанию", "Сначала дешевле", "Сначала дороже", "По названию"
    };

    private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

    private readonly IShopView view;
    private readonly CatalogModel catalog;
    private readonly CartModel cart;

    public ShopPresenter(IShopView view, CatalogModel catalog, CartModel cart, User user)
    {
        this.view = view;
        this.catalog = catalog;
        this.cart = cart;

        // Подписываемся на события вида
        view.FilterChanged += OnFilterChanged;
        view.ProductSelected += OnProductSelected;
        view.AddToCartRequested += OnAddToCartRequested;
        view.RemoveFromCartRequested += OnRemoveFromCartRequested;
        view.ApplyPromoRequested += OnApplyPromoRequested;

        // Первоначальное заполнение окна данными моделей
        view.ShowUser($"{user.FullName} · {user.Role}");
        view.SetFilters(
            new[] { AllBrands }.Concat(catalog.Brands),
            new[] { AllGroups }.Concat(catalog.Groups),
            SortNames);
        ShowCatalog();
        ShowCart();
    }

    /// <summary>Цена в формате «3 550 BYN» или «1 234,50 BYN».</summary>
    public static string FormatPrice(decimal value)
    {
        string format = value == Math.Truncate(value) ? "N0" : "N2";
        return value.ToString(format, RuCulture) + " BYN";
    }

    private void OnFilterChanged(object? sender, EventArgs e)
    {
        ShowCatalog();
    }

    private void OnProductSelected(object? sender, EventArgs e)
    {
        Product? product = view.SelectedProduct;
        if (product == null)
        {
            view.ClearProduct();
            return;
        }

        IReadOnlyList<string> sizes = catalog.GetSizes(product);

        string details =
            $"{product.Category} · {product.Gender}\n" +
            $"Цвет: {product.Color}\n" +
            $"Материал: {product.Material}\n" +
            (product.Preowned
                ? "Ресейл: подлинная вещь с экспертизой, один экземпляр"
                : "Новая вещь");

        // Если размер один (например, у сумок — OS), выбираем его сразу
        string? selectedSize = sizes.Count == 1 ? sizes[0] : null;

        view.ShowProduct(product.Brand, product.Name, details, FormatPrice(product.Price), sizes, selectedSize);
    }

    private void OnAddToCartRequested(object? sender, EventArgs e)
    {
        // Проверяем, что пользователь выбрал товар и размер
        Product? product = view.SelectedProduct;
        if (product == null)
        {
            view.ShowCartMessage("Сначала выберите товар в каталоге.", isError: true);
            return;
        }

        string? size = view.SelectedSize;
        if (size == null)
        {
            view.ShowCartMessage("Выберите размер.", isError: true);
            return;
        }

        bool added = cart.Add(product, size); // модель обновляется
        ShowCart();
        view.ShowCartMessage(cart.Message, isError: !added);
    }

    private void OnRemoveFromCartRequested(object? sender, EventArgs e)
    {
        CartItem? item = view.SelectedCartItem;
        if (item == null)
        {
            view.ShowCartMessage("Выберите в корзине товар, который нужно удалить.", isError: true);
            return;
        }

        cart.Remove(item);
        ShowCart();
        view.ShowCartMessage(cart.Message, isError: false);
    }

    private void OnApplyPromoRequested(object? sender, EventArgs e)
    {
        // Обработка ввода: убираем пробелы, приводим к верхнему регистру
        string code = view.PromoCode.Trim().ToUpperInvariant();
        if (code.Length == 0)
        {
            view.ShowCartMessage("Введите промокод.", isError: true);
            return;
        }

        bool applied = cart.ApplyPromoCode(code);
        ShowCart();
        view.ShowCartMessage(cart.Message, isError: !applied);
    }

    /// <summary>Ищет товары по фильтрам из вида и выводит результат.</summary>
    private void ShowCatalog()
    {
        string? brand = view.SelectedBrand == AllBrands ? null : view.SelectedBrand;
        string? group = view.SelectedGroup == AllGroups ? null : view.SelectedGroup;
        int sortIndex = Math.Clamp(view.SelectedSortIndex, 0, SortNames.Length - 1);

        catalog.Search(brand, group, view.SearchText, (ProductSort)sortIndex);

        int count = catalog.FoundProducts.Count;
        view.ShowProducts(catalog.FoundProducts,
            count == 0 ? "Ничего не найдено" : $"Найдено товаров: {count}");

        // После нового поиска выбранного товара в списке больше нет
        view.ClearProduct();
    }

    /// <summary>Выводит содержимое корзины и суммы.</summary>
    private void ShowCart()
    {
        string countText = cart.Count == 0 ? "пусто" : $"товаров: {cart.Count}";

        string discount = cart.AppliedPromo == null
            ? "—"
            : $"−{FormatPrice(cart.Discount)} ({cart.AppliedPromo.Code}, {cart.AppliedPromo.Percent}%)";

        view.ShowCart(cart.Items.ToList(), countText, FormatPrice(cart.Subtotal), discount, FormatPrice(cart.Total));
    }
}
