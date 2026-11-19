using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AmfibiaShop.Models;

namespace AmfibiaShop.Views;

/// <summary>
/// ВИД окна магазина.
/// Показывает данные и передаёт действия пользователя Представителю.
/// Поиска, расчётов и проверок здесь нет.
/// </summary>
public partial class ShopWindow : Window, IShopView
{
    // true, пока Представитель сам заполняет списки:
    // в это время события выбора не передаются
    private bool isUpdating;

    public ShopWindow()
    {
        InitializeComponent();
    }

    // ----- Данные, которые выбрал и ввёл пользователь -----

    public string? SelectedBrand => BrandComboBox.SelectedItem as string;

    public string? SelectedGroup => GroupComboBox.SelectedItem as string;

    public int SelectedSortIndex => SortComboBox.SelectedIndex;

    public string SearchText => SearchTextBox.Text;

    public Product? SelectedProduct => ProductsListView.SelectedItem as Product;

    public string? SelectedSize => SizesListBox.SelectedItem as string;

    public CartItem? SelectedCartItem => CartListView.SelectedItem as CartItem;

    public string PromoCode => PromoTextBox.Text;

    // ----- События для Представителя -----

    public event EventHandler? FilterChanged;
    public event EventHandler? ProductSelected;
    public event EventHandler? AddToCartRequested;
    public event EventHandler? RemoveFromCartRequested;
    public event EventHandler? ApplyPromoRequested;

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!isUpdating) FilterChanged?.Invoke(this, EventArgs.Empty);
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isUpdating) FilterChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ProductsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isUpdating) ProductSelected?.Invoke(this, EventArgs.Empty);
    }

    private void AddToCartButton_Click(object sender, RoutedEventArgs e)
    {
        AddToCartRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        RemoveFromCartRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyPromoButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyPromoRequested?.Invoke(this, EventArgs.Empty);
    }

    private void PromoTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyPromoRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    // ----- Методы, которыми Представитель обновляет окно -----

    public void ShowUser(string text)
    {
        UserTextBlock.Text = text;
    }

    public void SetFilters(IEnumerable<string> brands, IEnumerable<string> groups, IEnumerable<string> sortModes)
    {
        isUpdating = true;
        BrandComboBox.ItemsSource = brands.ToList();
        BrandComboBox.SelectedIndex = 0;
        GroupComboBox.ItemsSource = groups.ToList();
        GroupComboBox.SelectedIndex = 0;
        SortComboBox.ItemsSource = sortModes.ToList();
        SortComboBox.SelectedIndex = 0;
        isUpdating = false;
    }

    public void ShowProducts(IEnumerable<Product> products, string countText)
    {
        isUpdating = true;
        ProductsListView.ItemsSource = products.ToList();
        isUpdating = false;
        CountTextBlock.Text = countText;
    }

    public void ShowProduct(string brand, string name, string details, string price,
                            IEnumerable<string> sizes, string? selectedSize)
    {
        BrandTextBlock.Text = brand.ToUpperInvariant();
        NameTextBlock.Text = name;
        DetailsTextBlock.Text = details;
        PriceTextBlock.Text = price;
        SizesListBox.ItemsSource = sizes.ToList();
        SizesListBox.SelectedItem = selectedSize;

        ProductHintTextBlock.Visibility = Visibility.Collapsed;
        ProductPanel.Visibility = Visibility.Visible;
    }

    public void ClearProduct()
    {
        SizesListBox.ItemsSource = null;
        ProductPanel.Visibility = Visibility.Collapsed;
        ProductHintTextBlock.Visibility = Visibility.Visible;
    }

    public void ShowCart(IEnumerable<CartItem> items, string countText, string subtotal, string discount, string total)
    {
        CartListView.ItemsSource = items.ToList();
        CartCountTextBlock.Text = countText;
        SubtotalTextBlock.Text = subtotal;
        DiscountTextBlock.Text = discount;
        TotalTextBlock.Text = total;
    }

    public void ShowCartMessage(string message, bool isError)
    {
        CartMessageTextBlock.Text = message;
        CartMessageTextBlock.Foreground = (Brush)FindResource(isError ? "ErrorBrush" : "SuccessBrush");
        CartMessageTextBlock.Visibility = Visibility.Visible;
    }
}
