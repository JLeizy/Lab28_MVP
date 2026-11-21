using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AmfibiaShop.Presenters.Display;

namespace AmfibiaShop.Views.Pages;

/// <summary>ВИД списка модных домов.</summary>
public partial class BrandsView : UserControl, IBrandsView
{
    public BrandsView()
    {
        InitializeComponent();
        BrandItems.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler((_, e) =>
        {
            if (e.OriginalSource is FrameworkElement { DataContext: BrandCardData brand }) BrandOpened?.Invoke(this, brand.Slug);
        }));
    }

    public event EventHandler<string>? BrandOpened;

    public void ShowBrands(IReadOnlyList<BrandCardData> brands, string countText)
    {
        BrandItems.ItemsSource = brands;
        CountText.Text = countText;
    }
}
