using System;
using System.Collections.Generic;
using AmfibiaShop.Models;

namespace AmfibiaShop.Views;

/// <summary>
/// Интерфейс ВИДА окна магазина (каталог + карточка товара + корзина).
/// </summary>
public interface IShopView
{
    // ----- Что выбрал и ввёл пользователь -----

    string? SelectedBrand { get; }

    string? SelectedGroup { get; }

    int SelectedSortIndex { get; }

    string SearchText { get; }

    Product? SelectedProduct { get; }

    string? SelectedSize { get; }

    CartItem? SelectedCartItem { get; }

    string PromoCode { get; }

    // ----- События, которые Вид передаёт Представителю -----

    /// <summary>Изменился поиск, бренд, раздел или сортировка.</summary>
    event EventHandler? FilterChanged;

    /// <summary>Выбран товар в каталоге.</summary>
    event EventHandler? ProductSelected;

    /// <summary>Нажата кнопка «В корзину».</summary>
    event EventHandler? AddToCartRequested;

    /// <summary>Нажата кнопка «Удалить» в корзине.</summary>
    event EventHandler? RemoveFromCartRequested;

    /// <summary>Нажата кнопка «Применить» промокод.</summary>
    event EventHandler? ApplyPromoRequested;

    // ----- Команды, которыми Представитель обновляет окно -----

    void ShowUser(string text);

    void SetFilters(IEnumerable<string> brands, IEnumerable<string> groups, IEnumerable<string> sortModes);

    void ShowProducts(IEnumerable<Product> products, string countText);

    void ShowProduct(string brand, string name, string details, string price,
                     IEnumerable<string> sizes, string? selectedSize);

    void ClearProduct();

    void ShowCart(IEnumerable<CartItem> items, string countText, string subtotal, string discount, string total);

    void ShowCartMessage(string message, bool isError);
}
