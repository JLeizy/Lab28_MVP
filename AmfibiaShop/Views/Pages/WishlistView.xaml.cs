using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views.Controls;

namespace AmfibiaShop.Views.Pages;

/// <summary>ВИД избранного.</summary>
public partial class WishlistView : UserControl, IWishlistView
{
    public WishlistView()
    {
        InitializeComponent();
        ProductCard.Attach(this, id => ProductOpened?.Invoke(this, id), id => FavoriteToggled?.Invoke(this, id));
    }

    public event EventHandler<string>? ProductOpened;
    public event EventHandler<string>? FavoriteToggled;
    public event EventHandler? ClearRequested;
    public event EventHandler? CatalogRequested;

    private void ClearButton_Click(object sender, RoutedEventArgs e) => ClearRequested?.Invoke(this, EventArgs.Empty);

    private void CatalogButton_Click(object sender, RoutedEventArgs e) => CatalogRequested?.Invoke(this, EventArgs.Empty);

    public void ShowItems(IReadOnlyList<ProductCardData> items, string countText)
    {
        Items.ItemsSource = items;
        CountText.Text = countText;
        bool empty = items.Count == 0;
        EmptyPanel.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        ClearButton.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }
}
