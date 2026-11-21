using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AmfibiaShop.Presenters.Display;

namespace AmfibiaShop.Views.Controls;

/// <summary>
/// Карточка товара. Сама ничего не решает: только сообщает странице,
/// что карточку открыли или нажали «сердечко» (всплывающие события).
/// </summary>
public partial class ProductCard : UserControl
{
    public static readonly RoutedEvent OpenRequestedEvent = EventManager.RegisterRoutedEvent(
        "OpenRequested", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ProductCard));

    public static readonly RoutedEvent FavoriteRequestedEvent = EventManager.RegisterRoutedEvent(
        "FavoriteRequested", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ProductCard));

    public ProductCard()
    {
        InitializeComponent();
    }

    public string? ProductId => (DataContext as ProductCardData)?.Id;

    private void Root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(OpenRequestedEvent, this));
    }

    private void HeartButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        RaiseEvent(new RoutedEventArgs(FavoriteRequestedEvent, this));
    }

    /// <summary>Подписывает страницу на события всех карточек внутри неё.</summary>
    public static void Attach(UIElement page, System.Action<string> open, System.Action<string> favorite)
    {
        page.AddHandler(OpenRequestedEvent, new RoutedEventHandler((_, e) =>
        {
            if (e.OriginalSource is ProductCard { ProductId: string id }) open(id);
        }));
        page.AddHandler(FavoriteRequestedEvent, new RoutedEventHandler((_, e) =>
        {
            if (e.OriginalSource is ProductCard { ProductId: string id }) favorite(id);
        }));
    }
}
