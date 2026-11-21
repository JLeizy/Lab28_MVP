using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views.Controls;

namespace AmfibiaShop.Views.Pages;

/// <summary>ВИД страницы товара.</summary>
public partial class ProductView : UserControl, IProductView
{
    private bool isUpdating;

    public ProductView()
    {
        InitializeComponent();
        ProductCard.Attach(this, id => ProductOpened?.Invoke(this, id), id => FavoriteToggled?.Invoke(this, id));
    }

    public event EventHandler<string>? ProductOpened;
    public event EventHandler<string>? FavoriteToggled;
    public event EventHandler? BrandOpened;
    public event EventHandler<string>? VariantOpened;
    public event EventHandler<int>? QuantityChangeRequested;
    public event EventHandler? AddToCartRequested;
    public event EventHandler? BuyNowRequested;
    public event EventHandler? FavoriteRequested;
    public event EventHandler? CompareRequested;
    public event EventHandler? ReviewSubmitted;

    public string? SelectedSize => SizeList.SelectedItem as string;

    public int ReviewRating =>
        StarsPanel.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true)?.Tag is string tag ? int.Parse(tag) : 0;

    public string ReviewText => ReviewTextBox.Text;

    // ----- события элементов -----

    private void BrandButton_Click(object sender, RoutedEventArgs e) => BrandOpened?.Invoke(this, EventArgs.Empty);

    private void VariantList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isUpdating && VariantList.SelectedItem is VariantData { IsCurrent: false } v) VariantOpened?.Invoke(this, v.Id);
    }

    private void Quantity_Click(object sender, RoutedEventArgs e) =>
        QuantityChangeRequested?.Invoke(this, int.Parse((string)((Button)sender).Tag));

    private void AddToCartButton_Click(object sender, RoutedEventArgs e) => AddToCartRequested?.Invoke(this, EventArgs.Empty);

    private void BuyNowButton_Click(object sender, RoutedEventArgs e) => BuyNowRequested?.Invoke(this, EventArgs.Empty);

    private void FavoriteButton_Click(object sender, RoutedEventArgs e) => FavoriteRequested?.Invoke(this, EventArgs.Empty);

    private void CompareButton_Click(object sender, RoutedEventArgs e) => CompareRequested?.Invoke(this, EventArgs.Empty);

    private void SubmitReviewButton_Click(object sender, RoutedEventArgs e) => ReviewSubmitted?.Invoke(this, EventArgs.Empty);

    // ----- команды Представителя -----

    public void ShowProduct(ProductDetailsData details)
    {
        Root.DataContext = details;
    }

    public void ShowVariants(IReadOnlyList<VariantData> variants)
    {
        isUpdating = true;
        VariantList.ItemsSource = variants;
        VariantList.SelectedItem = variants.FirstOrDefault(v => v.IsCurrent);
        isUpdating = false;
    }

    public void ShowSizes(IReadOnlyList<string> sizes, string? selected, string hint)
    {
        SizeList.ItemsSource = sizes;
        SizeList.SelectedItem = selected;
        SizeHint.Text = hint;
    }

    public void ShowQuantity(string quantity, bool canDecrease, bool canIncrease)
    {
        QuantityText.Text = quantity;
        MinusButton.IsEnabled = canDecrease;
        PlusButton.IsEnabled = canIncrease;
    }

    public void SetFavorite(bool isFavorite) => FavoriteButton.Content = isFavorite ? "♥  В избранном" : "♡  В избранное";

    public void SetCompared(bool isCompared) => CompareButton.Content = isCompared ? "✓  В сравнении" : "⇄  Сравнить";

    public void ShowReviews(IReadOnlyList<ReviewData> reviews, string summary)
    {
        ReviewItems.ItemsSource = reviews;
        ReviewsSummary.Text = summary;
        NoReviewsText.Visibility = reviews.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ShowReviewForm(bool enabled, string hint)
    {
        ReviewHint.Text = hint;
        StarsPanel.IsEnabled = enabled;
        ReviewTextBox.IsEnabled = enabled;
        SubmitReviewButton.IsEnabled = enabled;
    }

    public void ClearReviewForm()
    {
        ReviewTextBox.Text = string.Empty;
        foreach (RadioButton r in StarsPanel.Children.OfType<RadioButton>()) r.IsChecked = false;
    }

    public void ShowSimilar(IReadOnlyList<ProductCardData> items) => SimilarItems.ItemsSource = items;

    public void ShowMessage(string message, bool isError)
    {
        MessageText.Text = message;
        MessageText.Foreground = (Brush)FindResource(isError ? "Error" : "Success");
        MessageText.Visibility = Visibility.Visible;
    }
}
