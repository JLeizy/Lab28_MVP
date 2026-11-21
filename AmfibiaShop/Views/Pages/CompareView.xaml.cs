using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using AmfibiaShop.Presenters.Display;

namespace AmfibiaShop.Views.Pages;

/// <summary>ВИД сравнения: слева названия строк, справа столбцы-товары.</summary>
public partial class CompareView : UserControl, ICompareView
{
    public CompareView()
    {
        InitializeComponent();
    }

    public event EventHandler<string>? ProductOpened;
    public event EventHandler<string>? RemoveRequested;
    public event EventHandler? ClearRequested;
    public event EventHandler? CatalogRequested;

    private void Open_Click(object sender, RoutedEventArgs e) => ProductOpened?.Invoke(this, (string)((Button)sender).Tag);

    private void Remove_Click(object sender, RoutedEventArgs e) => RemoveRequested?.Invoke(this, (string)((Button)sender).Tag);

    private void ClearButton_Click(object sender, RoutedEventArgs e) => ClearRequested?.Invoke(this, EventArgs.Empty);

    private void Catalog_Click(object sender, RoutedEventArgs e) => CatalogRequested?.Invoke(this, EventArgs.Empty);

    public void ShowComparison(IReadOnlyList<string> rowTitles, IReadOnlyList<CompareColumnData> columns, string countText)
    {
        RowTitles.ItemsSource = rowTitles;
        Columns.ItemsSource = columns;
        CountText.Text = countText;
        bool empty = columns.Count == 0;
        TablePanel.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
        EmptyPanel.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        ClearButton.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }
}
