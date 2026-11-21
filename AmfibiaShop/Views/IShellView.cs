using System;
using System.Collections.Generic;

namespace AmfibiaShop.Views;

/// <summary>Вид главного окна: шапка, навигация, счётчики, место для страниц.</summary>
public interface IShellView
{
    /// <summary>Пункт меню: home, catalog, brands, wishlist, compare, cart, orders, profile.</summary>
    event EventHandler<string>? NavigateRequested;

    event EventHandler<string>? CurrencySelected;

    /// <summary>Поиск из шапки.</summary>
    event EventHandler<string>? SearchRequested;

    event EventHandler? BackRequested;

    event EventHandler? LogoutRequested;

    void SetUser(string name, string initials, string role);

    void SetCurrencies(IReadOnlyList<string> codes, string current);

    void SetCounters(int wishlist, int compare, int cart);

    void SetActiveSection(string section);

    void SetBackVisible(bool visible);

    /// <summary>Показывает страницу (Вид страницы создаёт IViewFactory).</summary>
    void ShowPage(object page);

    void ShowToast(string message, bool isError);
}
