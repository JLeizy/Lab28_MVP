using System;
using System.Collections.Generic;
using AmfibiaShop.Models;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>Запомненное состояние каталога (фильтры и страница) — чтобы «Назад» возвращал туда же.</summary>
public class CatalogState
{
    public CatalogFilter Filter { get; set; } = new();
    public int Page { get; set; } = 1;
}

/// <summary>
/// ПРЕДСТАВИТЕЛЬ главного окна: навигация, счётчики в шапке, валюта, выход.
/// Создаёт страницы (Вид + Представитель) и освобождает предыдущую.
/// </summary>
public class ShellPresenter : INavigator, IDisposable
{
    private readonly IShellView view;
    private readonly ShopContext shop;
    private readonly IViewFactory factory;
    private readonly Stack<Action> history = new();
    private Action? currentOpen;
    private PagePresenter? currentPage;
    private CatalogState catalogState = new();

    public ShellPresenter(IShellView view, ShopContext shop, IViewFactory factory)
    {
        this.view = view;
        this.shop = shop;
        this.factory = factory;

        view.NavigateRequested += OnNavigateRequested;
        view.CurrencySelected += OnCurrencySelected;
        view.SearchRequested += OnSearchRequested;
        view.BackRequested += (_, _) => GoBack();
        view.LogoutRequested += (_, _) => Logout();

        shop.Wishlist.Changed += OnCountersChanged;
        shop.Compare.Changed += OnCountersChanged;
        shop.Cart.Changed += OnCountersChanged;
        shop.Auth.UserChanged += OnUserChanged;

        OnUserChanged(this, EventArgs.Empty);
        view.SetCurrencies(shop.Currency.Codes, shop.Currency.Current);
        OnCountersChanged(this, EventArgs.Empty);
        GoHome();
    }

    /// <summary>Пользователь вышел — приложение показывает окно входа.</summary>
    public event EventHandler? LoggedOut;

    /// <summary>Текущий страничный Представитель (для проверок).</summary>
    public PagePresenter? CurrentPage => currentPage;

    // ----- INavigator -----

    public void GoHome() => Open("home", () =>
    {
        IHomeView page = factory.CreateHome();
        return (page, new HomePresenter(page, shop, this));
    });

    public void GoCatalog(CatalogFilter? preset = null)
    {
        if (preset != null)
        {
            catalogState = new CatalogState { Filter = preset };
        }

        CatalogState state = catalogState;
        Open("catalog", () =>
        {
            ICatalogView page = factory.CreateCatalog();
            return (page, new CatalogPresenter(page, shop, this, state));
        });
    }

    public void GoProduct(string productId) => Open("catalog", () =>
    {
        IProductView page = factory.CreateProduct();
        return (page, new ProductPresenter(page, shop, this, productId));
    });

    public void GoBrands() => Open("brands", () =>
    {
        IBrandsView page = factory.CreateBrands();
        return (page, new BrandsPresenter(page, shop, this));
    });

    public void GoBrand(string slug) => Open("brands", () =>
    {
        IBrandView page = factory.CreateBrand();
        return (page, new BrandPresenter(page, shop, this, slug));
    });

    public void GoWishlist() => Open("wishlist", () =>
    {
        IWishlistView page = factory.CreateWishlist();
        return (page, new WishlistPresenter(page, shop, this));
    });

    public void GoCompare() => Open("compare", () =>
    {
        ICompareView page = factory.CreateCompare();
        return (page, new ComparePresenter(page, shop, this));
    });

    public void GoCart() => Open("cart", () =>
    {
        ICartView page = factory.CreateCart();
        return (page, new CartPresenter(page, shop, this));
    });

    public void GoCheckout()
    {
        if (shop.Cart.IsEmpty)
        {
            Notify("Корзина пуста — добавьте товары, чтобы оформить заказ.", true);
            return;
        }

        Open("cart", () =>
        {
            ICheckoutView page = factory.CreateCheckout();
            return (page, new CheckoutPresenter(page, shop, this));
        });
    }

    public void GoOrders(string? orderNumber = null) => Open("orders", () =>
    {
        IOrdersView page = factory.CreateOrders();
        return (page, new OrdersPresenter(page, shop, this, orderNumber));
    });

    public void GoProfile() => Open("profile", () =>
    {
        IProfileView page = factory.CreateProfile();
        return (page, new ProfilePresenter(page, shop, this));
    });

    public void GoBack()
    {
        if (history.Count == 0)
        {
            return;
        }

        currentOpen = history.Pop();
        currentOpen();
    }

    public void Logout()
    {
        Dispose();
        shop.EndSession();
        LoggedOut?.Invoke(this, EventArgs.Empty);
    }

    public void Notify(string message, bool isError = false)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            view.ShowToast(message, isError);
        }
    }

    public void Dispose()
    {
        currentPage?.Dispose();
        currentPage = null;
        shop.Wishlist.Changed -= OnCountersChanged;
        shop.Compare.Changed -= OnCountersChanged;
        shop.Cart.Changed -= OnCountersChanged;
        shop.Auth.UserChanged -= OnUserChanged;
        GC.SuppressFinalize(this);
    }

    // ----- внутреннее -----

    /// <summary>Открывает страницу и запоминает предыдущую для кнопки «Назад».</summary>
    private void Open(string section, Func<(object View, PagePresenter Presenter)> create)
    {
        void Show()
        {
            currentPage?.Dispose();
            (object page, PagePresenter presenter) = create();
            currentPage = presenter;
            view.ShowPage(page);
            view.SetActiveSection(section);
            view.SetBackVisible(history.Count > 0);
        }

        if (currentOpen != null)
        {
            history.Push(currentOpen);
        }

        currentOpen = Show;
        Show();
    }

    private void OnNavigateRequested(object? sender, string section)
    {
        switch (section)
        {
            case "home": GoHome(); break;
            case "catalog": GoCatalog(); break;
            case "brands": GoBrands(); break;
            case "wishlist": GoWishlist(); break;
            case "compare": GoCompare(); break;
            case "cart": GoCart(); break;
            case "orders": GoOrders(); break;
            case "profile": GoProfile(); break;
        }
    }

    private void OnCurrencySelected(object? sender, string code)
    {
        // Модель сообщит страницам об изменении — они пересчитают цены сами
        shop.Currency.Select(code);
    }

    private void OnSearchRequested(object? sender, string text)
    {
        GoCatalog(new CatalogFilter { Search = text.Trim() });
    }

    private void OnCountersChanged(object? sender, EventArgs e)
    {
        view.SetCounters(shop.Wishlist.Count, shop.Compare.Count, shop.Cart.Count);
    }

    private void OnUserChanged(object? sender, EventArgs e)
    {
        User user = shop.User;
        view.SetUser(user.FullName, user.Initials, user.RoleName);
    }
}
