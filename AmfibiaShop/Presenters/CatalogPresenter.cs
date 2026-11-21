using System;
using System.Collections.Generic;
using System.Linq;
using AmfibiaShop.Models;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>
/// ПРЕДСТАВИТЕЛЬ каталога: читает фильтры из вида, проверяет цену «от/до»,
/// передаёт условия модели и выводит страницу результатов.
/// </summary>
public class CatalogPresenter : PagePresenter
{
    public const int PageSize = 24;

    public static readonly string[] SortNames =
    {
        "Популярные", "Сначала новинки", "Сначала дешевле", "Сначала дороже", "По размеру скидки", "По названию"
    };

    private readonly ICatalogView view;
    private readonly CatalogState state;

    public CatalogPresenter(ICatalogView view, ShopContext shop, INavigator navigator, CatalogState state) : base(shop, navigator)
    {
        this.view = view;
        this.state = state;

        view.FiltersChanged += OnFiltersChanged;
        view.GroupChanged += OnGroupChanged;
        view.ResetRequested += OnResetRequested;
        view.PageRequested += OnPageRequested;
        view.ProductOpened += (_, id) => Navigator.GoProduct(id);
        view.FavoriteToggled += (_, id) => ToggleFavorite(id);
        Shop.Currency.Changed += OnModelChanged;
        Shop.Wishlist.Changed += OnModelChanged;

        CatalogModel catalog = Shop.Catalog;
        view.SetOptions(
            Options(catalog.Groups(), "Все разделы", catalog.Products.Count),
            catalog.BrandOptions().Select(o => new OptionData(o.Key, o.Title, o.Count.ToString())).ToList(),
            Options(catalog.Genders(), "Для всех", catalog.Products.Count),
            catalog.Colors().Select(o => new OptionData(o.Key, o.Title, o.Count.ToString(), o.Extra ?? "")).ToList(),
            SortNames);
        ShowState();
        Render();
    }

    public override void Dispose()
    {
        Shop.Currency.Changed -= OnModelChanged;
        Shop.Wishlist.Changed -= OnModelChanged;
        base.Dispose();
    }

    private static List<OptionData> Options(IEnumerable<CatalogOption> items, string allTitle, int total)
    {
        var list = new List<OptionData> { new(string.Empty, allTitle, total.ToString()) };
        list.AddRange(items.Select(o => new OptionData(o.Key, o.Title, o.Count.ToString())));
        return list;
    }

    /// <summary>Выводит в вид сохранённые фильтры (после «Назад» или перехода с другой страницы).</summary>
    private void ShowState()
    {
        CatalogFilter f = state.Filter;
        view.SetCategories(Options(Shop.Catalog.Categories(f.Group), "Все категории", Shop.Catalog.Categories(f.Group).Sum(c => c.Count)));
        view.ApplyState(f.Search, f.Group, f.Category, f.Brands, f.Gender, f.Colors,
                        f.OnlyNew, f.OnlySale, f.OnlyPreowned, f.OnlyIconic, (int)f.Sort);
    }

    private void Render()
    {
        IReadOnlyList<Product> found = Shop.Catalog.Search(state.Filter);
        IReadOnlyList<Product> page = CatalogModel.Page(found, state.Page, PageSize, out int pageCount, out int actualPage);
        state.Page = actualPage;

        string countText = found.Count == 0
            ? "Ничего не найдено — попробуйте изменить фильтры"
            : $"Найдено {Format.Plural(found.Count, "вещь", "вещи", "вещей")}";
        view.ShowProducts(Cards(page), countText, actualPage, pageCount);
    }

    /// <summary>Собирает фильтр из полей вида. Возвращает false, если цена введена неверно.</summary>
    private bool ReadFilter(out CatalogFilter filter)
    {
        filter = new CatalogFilter
        {
            Search = view.SearchText.Trim(),
            Group = view.SelectedGroup,
            Category = view.SelectedCategory,
            Brands = new HashSet<string>(view.SelectedBrands),
            Gender = view.SelectedGender,
            Colors = new HashSet<string>(view.SelectedColors),
            OnlyNew = view.OnlyNew,
            OnlySale = view.OnlySale,
            OnlyPreowned = view.OnlyPreowned,
            OnlyIconic = view.OnlyIconic,
            Sort = (ProductSort)Math.Clamp(view.SortIndex, 0, SortNames.Length - 1)
        };

        if (!Validation.TryPrice(view.MinPriceText, out decimal? min) || !Validation.TryPrice(view.MaxPriceText, out decimal? max))
        {
            view.ShowPriceError("Цена — это число, например 1500.");
            return false;
        }

        if (min != null && max != null && min > max)
        {
            view.ShowPriceError("Цена «от» больше цены «до».");
            return false;
        }

        // Цены в фильтре вводятся в выбранной валюте — переводим в BYN
        decimal rate = Shop.Currency.Rate(Shop.Currency.Current);
        filter.MinPrice = min * rate;
        filter.MaxPrice = max * rate;
        view.ShowPriceError(null);
        return true;
    }

    private void OnFiltersChanged(object? sender, EventArgs e)
    {
        if (!ReadFilter(out CatalogFilter filter))
        {
            return;
        }

        state.Filter = filter;
        state.Page = 1;
        Render();
    }

    private void OnGroupChanged(object? sender, EventArgs e)
    {
        var categories = Shop.Catalog.Categories(view.SelectedGroup);
        view.SetCategories(Options(categories, "Все категории", categories.Sum(c => c.Count)));
        OnFiltersChanged(sender, e);
    }

    private void OnResetRequested(object? sender, EventArgs e)
    {
        state.Filter = new CatalogFilter();
        state.Page = 1;
        ShowState();
        view.ShowPriceError(null);
        Render();
    }

    private void OnPageRequested(object? sender, int page)
    {
        state.Page = page;
        Render();
    }

    private void OnModelChanged(object? sender, EventArgs e) => Render();
}
