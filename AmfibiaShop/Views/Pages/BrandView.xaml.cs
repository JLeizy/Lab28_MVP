using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views.Controls;

namespace AmfibiaShop.Views.Pages;

/// <summary>ВИД страницы модного дома.</summary>
public partial class BrandView : UserControl, IBrandView
{
    public BrandView()
    {
        InitializeComponent();
        ProductCard.Attach(this, id => ProductOpened?.Invoke(this, id), id => FavoriteToggled?.Invoke(this, id));
    }

    public event EventHandler<string>? ProductOpened;
    public event EventHandler<string>? FavoriteToggled;
    public event EventHandler? AllProductsRequested;

    private void AllProducts_Click(object sender, RoutedEventArgs e) => AllProductsRequested?.Invoke(this, EventArgs.Empty);

    public void ShowBrand(BrandDetailsData brand) => Root.DataContext = brand;

    public void ShowProducts(IReadOnlyList<ProductCardData> items) => ProductItems.ItemsSource = items;
}
