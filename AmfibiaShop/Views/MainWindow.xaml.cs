using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AmfibiaShop.Views.Controls;

namespace AmfibiaShop.Views;

/// <summary>
/// ВИД главного окна: шапка и место для страниц.
/// Решения о переходах принимает ShellPresenter.
/// </summary>
public partial class MainWindow : Window, IShellView
{
    private readonly Dictionary<string, RadioButton> sections;
    private bool isUpdating;

    public MainWindow()
    {
        InitializeComponent();
        WindowTheme.UseDarkTitleBar(this);
        sections = new Dictionary<string, RadioButton>
        {
            ["home"] = NavHome, ["catalog"] = NavCatalog, ["brands"] = NavBrands, ["orders"] = NavOrders,
            ["wishlist"] = NavWishlist, ["compare"] = NavCompare, ["cart"] = NavCart, ["profile"] = NavProfile
        };
    }

    public event EventHandler<string>? NavigateRequested;
    public event EventHandler<string>? CurrencySelected;
    public event EventHandler<string>? SearchRequested;
    public event EventHandler? BackRequested;
    public event EventHandler? LogoutRequested;

    // ----- события элементов → события для Представителя -----

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (!isUpdating && sender is RadioButton { Tag: string section })
        {
            NavigateRequested?.Invoke(this, section);
        }
    }

    private void LogoButton_Click(object sender, RoutedEventArgs e) => NavigateRequested?.Invoke(this, "home");

    private void BackButton_Click(object sender, RoutedEventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);

    private void HeaderSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SearchRequested?.Invoke(this, HeaderSearchBox.Text);
            HeaderSearchBox.Text = string.Empty;
        }
    }

    /// <summary>
    /// Адаптивная шапка: на узком экране (например, ноутбук 1366×768) прячем подписи
    /// у значков и имя пользователя — остаются значки со счётчиками и подсказками.
    /// </summary>
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Visibility labels = ActualWidth >= 1560 ? Visibility.Visible : Visibility.Collapsed;
        WishlistLabel.Visibility = labels;
        CompareLabel.Visibility = labels;
        CartLabel.Visibility = labels;
        UserTextPanel.Visibility = ActualWidth >= 1440 ? Visibility.Visible : Visibility.Collapsed;
        HeaderSearchBox.Width = ActualWidth >= 1440 ? 260 : 210;
    }

    private void CurrencyBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!isUpdating && CurrencyBox.SelectedItem is string code)
        {
            CurrencySelected?.Invoke(this, code);
        }
    }

    /// <summary>Выход вызывается со страницы профиля; событие оставлено для полноты интерфейса.</summary>
    internal void RaiseLogout() => LogoutRequested?.Invoke(this, EventArgs.Empty);

    // ----- команды от Представителя -----

    public void SetUser(string name, string initials, string role)
    {
        UserNameText.Text = name;
        InitialsText.Text = initials;
        UserRoleText.Text = role;
    }

    public void SetCurrencies(IReadOnlyList<string> codes, string current)
    {
        isUpdating = true;
        CurrencyBox.ItemsSource = codes;
        CurrencyBox.SelectedItem = current;
        isUpdating = false;
    }

    public void SetCounters(int wishlist, int compare, int cart)
    {
        SetBadge(WishlistBadge, WishlistCount, wishlist);
        SetBadge(CompareBadge, CompareCount, compare);
        SetBadge(CartBadge, CartCount, cart);
    }

    private static void SetBadge(Border badge, TextBlock text, int count)
    {
        text.Text = count.ToString();
        badge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public void SetActiveSection(string section)
    {
        isUpdating = true;
        foreach (RadioButton button in sections.Values)
        {
            button.IsChecked = false;
        }

        if (sections.TryGetValue(section, out RadioButton? active))
        {
            active.IsChecked = true;
        }

        // Аватар подсвечивается, когда открыт профиль
        AvatarBorder.BorderBrush = (Brush)FindResource(section == "profile" ? "Accent" : "LineStrong");
        AvatarBorder.Background = (Brush)FindResource(section == "profile" ? "AccentDeep" : "Surface3");

        isUpdating = false;
    }

    public void SetBackVisible(bool visible) => BackButton.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

    public void ShowPage(object page)
    {
        PageHost.Content = page;
        if (page is UIElement element)
        {
            // Плавное появление страницы
            element.Opacity = 0;
            element.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        }
    }

    public void ShowToast(string message, bool isError)
    {
        ToastText.Text = message;
        ToastMark.Background = (Brush)FindResource(isError ? "Error" : "Success");
        var animation = new DoubleAnimationUsingKeyFrames();
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180))));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(2800))));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(3300))));
        Toast.BeginAnimation(OpacityProperty, animation);
    }
}
