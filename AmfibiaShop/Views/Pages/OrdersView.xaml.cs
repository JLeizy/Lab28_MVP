using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AmfibiaShop.Presenters.Display;

namespace AmfibiaShop.Views.Pages;

/// <summary>ВИД «Мои заказы»: список слева, подробности и шкала статусов справа.</summary>
public partial class OrdersView : UserControl, IOrdersView
{
    private bool isUpdating;

    public OrdersView()
    {
        InitializeComponent();
    }

    public event EventHandler<string>? OrderSelected;
    public event EventHandler<int>? FilterChanged;
    public event EventHandler? CancelRequested;
    public event EventHandler? ReturnRequested;
    public event EventHandler? RepeatRequested;
    public event EventHandler<string>? ProductOpened;

    private void OrderList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isUpdating && OrderList.SelectedItem is OrderRowData row) OrderSelected?.Invoke(this, row.Number);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => CancelRequested?.Invoke(this, EventArgs.Empty);
    private void ReturnButton_Click(object sender, RoutedEventArgs e) => ReturnRequested?.Invoke(this, EventArgs.Empty);
    private void RepeatButton_Click(object sender, RoutedEventArgs e) => RepeatRequested?.Invoke(this, EventArgs.Empty);
    private void Product_Click(object sender, RoutedEventArgs e) => ProductOpened?.Invoke(this, (string)((Button)sender).Tag);

    public void SetFilters(IReadOnlyList<string> filters, int selected)
    {
        FilterPanel.Children.Clear();
        for (int i = 0; i < filters.Count; i++)
        {
            int index = i;
            var chip = new RadioButton
            {
                Content = filters[i],
                Style = (Style)FindResource("Radio.Chip"),
                GroupName = "OrderFilter",
                IsChecked = i == selected
            };
            chip.Checked += (_, _) => FilterChanged?.Invoke(this, index);
            FilterPanel.Children.Add(chip);
        }
    }

    public void ShowOrders(IReadOnlyList<OrderRowData> orders, string? selectedNumber, string countText)
    {
        isUpdating = true;
        OrderList.ItemsSource = orders;
        OrderList.SelectedItem = orders.FirstOrDefault(o => o.Number == selectedNumber);
        isUpdating = false;
        CountText.Text = countText;
    }

    public void ShowOrder(OrderDetailsData? order)
    {
        DetailsPanel.DataContext = order;
        DetailsPanel.Visibility = order == null ? Visibility.Collapsed : Visibility.Visible;
        EmptyPanel.Visibility = order == null ? Visibility.Visible : Visibility.Collapsed;
        MessageText.Visibility = Visibility.Collapsed;
    }

    public void ShowMessage(string message, bool isError)
    {
        MessageText.Text = message;
        MessageText.Foreground = (Brush)FindResource(isError ? "Error" : "Success");
        MessageText.Visibility = Visibility.Visible;
    }
}
