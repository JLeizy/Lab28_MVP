using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views.Controls;

namespace AmfibiaShop.Views.Pages;

/// <summary>ВИД главной страницы.</summary>
public partial class HomeView : UserControl, IHomeView
{
    public HomeView()
    {
        InitializeComponent();
        ProductCard.Attach(this, id => ProductOpened?.Invoke(this, id), id => FavoriteToggled?.Invoke(this, id));
        BrandItems.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((_, e) =>
        {
            if (e.OriginalSource is FrameworkElement { DataContext: BrandCardData brand }) BrandOpened?.Invoke(this, brand.Slug);
        }));
    }

    public event EventHandler<string>? ProductOpened;
    public event EventHandler<string>? FavoriteToggled;
    public event EventHandler<string>? BrandOpened;
    public event EventHandler<string>? CollectionRequested;

    private void Collection_Click(object sender, RoutedEventArgs e) =>
        CollectionRequested?.Invoke(this, (string)((Button)sender).Tag);

    public void ShowHero(string title, string subtitle, IReadOnlyList<SpecRow> stats)
    {
        HeroTitle.Text = title;
        HeroSubtitle.Text = subtitle;
        StatsItems.ItemsSource = stats;
    }

    public void ShowNew(IReadOnlyList<ProductCardData> items) => NewItems.ItemsSource = items;

    public void ShowIconic(IReadOnlyList<ProductCardData> items) => IconicItems.ItemsSource = items;

    public void ShowSale(IReadOnlyList<ProductCardData> items) => SaleItems.ItemsSource = items;

    public void ShowBrands(IReadOnlyList<BrandCardData> brands) => BrandItems.ItemsSource = brands;
}
