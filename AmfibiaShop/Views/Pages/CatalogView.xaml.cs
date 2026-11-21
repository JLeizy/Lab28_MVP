using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views.Controls;

namespace AmfibiaShop.Views.Pages;

/// <summary>ВИД каталога: фильтры слева, сетка карточек и страницы справа.</summary>
public partial class CatalogView : UserControl, ICatalogView
{
    private readonly DispatcherTimer searchDelay = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private bool isUpdating;

    public CatalogView()
    {
        InitializeComponent();
        ProductCard.Attach(this, id => ProductOpened?.Invoke(this, id), id => FavoriteToggled?.Invoke(this, id));
        searchDelay.Tick += (_, _) =>
        {
            searchDelay.Stop();
            FiltersChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    public event EventHandler<string>? ProductOpened;
    public event EventHandler<string>? FavoriteToggled;
    public event EventHandler? FiltersChanged;
    public event EventHandler? GroupChanged;
    public event EventHandler? ResetRequested;
    public event EventHandler<int>? PageRequested;

    // ----- значения фильтров -----

    public string SearchText => SearchBox.Text;
    public string? SelectedGroup => KeyOrNull(GroupList.SelectedItem);
    public string? SelectedCategory => KeyOrNull(CategoryList.SelectedItem);
    public IReadOnlyList<string> SelectedBrands => BrandList.SelectedItems.Cast<OptionData>().Select(o => o.Key).ToList();
    public string? SelectedGender => KeyOrNull(GenderList.SelectedItem);
    public IReadOnlyList<string> SelectedColors => ColorList.SelectedItems.Cast<OptionData>().Select(o => o.Key).ToList();
    public string MinPriceText => MinPriceBox.Text;
    public string MaxPriceText => MaxPriceBox.Text;
    public bool OnlyNew => OnlyNewBox.IsChecked == true;
    public bool OnlySale => OnlySaleBox.IsChecked == true;
    public bool OnlyPreowned => OnlyPreownedBox.IsChecked == true;
    public bool OnlyIconic => OnlyIconicBox.IsChecked == true;
    public int SortIndex => SortBox.SelectedIndex;

    private static string? KeyOrNull(object? item) => item is OptionData { Key.Length: > 0 } o ? o.Key : null;

    // ----- события элементов -----

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (isUpdating) return;
        searchDelay.Stop();
        searchDelay.Start();
    }

    private void GroupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isUpdating) GroupChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isUpdating) FiltersChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Check_Changed(object sender, RoutedEventArgs e)
    {
        if (!isUpdating) FiltersChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Price_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!isUpdating) FiltersChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Price_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) FiltersChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e) => ResetRequested?.Invoke(this, EventArgs.Empty);

    // ----- команды Представителя -----

    public void SetOptions(IReadOnlyList<OptionData> groups, IReadOnlyList<OptionData> brands, IReadOnlyList<OptionData> genders,
                           IReadOnlyList<OptionData> colors, IReadOnlyList<string> sorts)
    {
        isUpdating = true;
        GroupList.ItemsSource = groups;
        BrandList.ItemsSource = brands;
        GenderList.ItemsSource = genders;
        ColorList.ItemsSource = colors;
        SortBox.ItemsSource = sorts;
        isUpdating = false;
    }

    public void SetCategories(IReadOnlyList<OptionData> categories)
    {
        isUpdating = true;
        CategoryList.ItemsSource = categories;
        CategoryList.SelectedIndex = 0;
        isUpdating = false;
    }

    public void ApplyState(string search, string? group, string? category, IReadOnlyCollection<string> brands, string? gender,
                           IReadOnlyCollection<string> colors, bool onlyNew, bool onlySale, bool onlyPreowned, bool onlyIconic, int sort)
    {
        isUpdating = true;
        SearchBox.Text = search;
        Select(GroupList, group);
        Select(CategoryList, category);
        Select(GenderList, gender);
        BrandList.SelectedItems.Clear();
        foreach (OptionData o in BrandList.Items) if (brands.Contains(o.Key)) BrandList.SelectedItems.Add(o);
        ColorList.SelectedItems.Clear();
        foreach (OptionData o in ColorList.Items) if (colors.Contains(o.Key)) ColorList.SelectedItems.Add(o);
        MinPriceBox.Text = string.Empty;
        MaxPriceBox.Text = string.Empty;
        OnlyNewBox.IsChecked = onlyNew;
        OnlySaleBox.IsChecked = onlySale;
        OnlyPreownedBox.IsChecked = onlyPreowned;
        OnlyIconicBox.IsChecked = onlyIconic;
        SortBox.SelectedIndex = sort;
        isUpdating = false;
    }

    private static void Select(ListBox list, string? key)
    {
        list.SelectedItem = list.Items.Cast<OptionData>().FirstOrDefault(o => o.Key == (key ?? string.Empty));
    }

    public void ShowProducts(IReadOnlyList<ProductCardData> items, string countText, int page, int pageCount)
    {
        ProductsItems.ItemsSource = items;
        CountText.Text = countText;
        ProductsScroll.ScrollToTop();
        BuildPager(page, pageCount);
    }

    public void ShowPriceError(string? message) => PriceError.Text = message ?? string.Empty;

    /// <summary>Кнопки страниц: ‹ 1 … 4 5 6 … 30 ›</summary>
    private void BuildPager(int page, int pageCount)
    {
        PagerPanel.Children.Clear();
        if (pageCount <= 1) return;

        void Add(string text, int target, bool active = false, bool enabled = true)
        {
            var button = new Button
            {
                Content = text,
                Style = (Style)FindResource(active ? "Btn.Primary" : "Btn.Ghost"),
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(4, 0, 4, 0),
                MinWidth = 42,
                IsEnabled = enabled,
                Tag = target
            };
            button.Click += (_, _) => PageRequested?.Invoke(this, target);
            PagerPanel.Children.Add(button);
        }

        Add("‹", page - 1, enabled: page > 1);
        int from = Math.Max(1, page - 2), to = Math.Min(pageCount, page + 2);
        if (from > 1) { Add("1", 1); if (from > 2) PagerPanel.Children.Add(new TextBlock { Text = "…", Foreground = (System.Windows.Media.Brush)FindResource("Muted"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) }); }
        for (int i = from; i <= to; i++) Add(i.ToString(), i, active: i == page);
        if (to < pageCount) { if (to < pageCount - 1) PagerPanel.Children.Add(new TextBlock { Text = "…", Foreground = (System.Windows.Media.Brush)FindResource("Muted"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) }); Add(pageCount.ToString(), pageCount); }
        Add("›", page + 1, enabled: page < pageCount);
    }
}
