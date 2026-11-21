using System;
using System.Collections.Generic;
using System.Linq;
using AmfibiaShop.Models;
using AmfibiaShop.Presenters.Display;
using AmfibiaShop.Views;

namespace AmfibiaShop.Presenters;

/// <summary>
/// ПРЕДСТАВИТЕЛЬ страницы товара: размеры, количество, корзина, избранное,
/// сравнение, другие цвета модели, отзывы и похожие товары.
/// </summary>
public class ProductPresenter : PagePresenter
{
    private readonly IProductView view;
    private readonly Product? product;
    private int quantity = 1;

    public ProductPresenter(IProductView view, ShopContext shop, INavigator navigator, string productId) : base(shop, navigator)
    {
        this.view = view;
        product = Shop.Catalog.Find(productId);

        view.BrandOpened += (_, _) => { if (product != null) Navigator.GoBrand(product.BrandSlug); };
        view.VariantOpened += (_, id) => { if (id != product?.Id) Navigator.GoProduct(id); };
        view.ProductOpened += (_, id) => Navigator.GoProduct(id);
        view.FavoriteToggled += (_, id) => ToggleFavorite(id);
        view.QuantityChangeRequested += OnQuantityChangeRequested;
        view.AddToCartRequested += (_, _) => AddToCart(goToCart: false);
        view.BuyNowRequested += (_, _) => AddToCart(goToCart: true);
        view.FavoriteRequested += OnFavoriteRequested;
        view.CompareRequested += OnCompareRequested;
        view.ReviewSubmitted += OnReviewSubmitted;
        Shop.Currency.Changed += OnCurrencyChanged;
        Shop.Wishlist.Changed += OnWishlistChanged;
        Shop.Compare.Changed += OnCompareChanged;

        if (product == null)
        {
            view.ShowMessage("Товар не найден.", isError: true);
            return;
        }

        Render();
    }

    public int Quantity => quantity;

    public override void Dispose()
    {
        Shop.Currency.Changed -= OnCurrencyChanged;
        Shop.Wishlist.Changed -= OnWishlistChanged;
        Shop.Compare.Changed -= OnCompareChanged;
        base.Dispose();
    }

    private int MaxQuantity => product!.Preowned ? 1 : CartModel.MaxQuantity;

    private void Render()
    {
        Product p = product!;
        view.ShowProduct(Details(p));
        view.ShowVariants(Shop.Catalog.Variants(p)
            .Select(v => new VariantData(v.Id, v.Color, v.ColorHex ?? "", v.Id == p.Id))
            .ToList());
        view.ShowSizes(p.Sizes, p.Sizes.Count == 1 ? p.Sizes[0] : null,
                       p.Preowned ? "Вещь с ресейла: единственный экземпляр этого размера." : SizeHint(p));
        ShowQuantity();
        view.SetFavorite(Shop.Wishlist.Contains(p.Id));
        view.SetCompared(Shop.Compare.Contains(p.Id));
        RenderReviews();
        view.ShowSimilar(Cards(Shop.Catalog.Similar(p, 6)));
    }

    private static string SizeHint(Product p) => p.SizeType switch
    {
        "clothing" => "Размеры INT. Мужская и женская сетки — как на сайте AMFIBIA.",
        "shoes" => "Размеры EU.",
        "ring" => "Внутренний диаметр кольца в миллиметрах.",
        "volume_ml" => "Объём флакона.",
        _ => "Единый размер."
    };

    private ProductDetailsData Details(Product p)
    {
        var badges = new List<string>();
        if (p.Preowned) badges.Add("Ресейл · экспертиза подлинности");
        if (p.IsNew) badges.Add("Новинка");
        if (p.Iconic) badges.Add("Культовая модель");
        if (p.DiscountPercent > 0) badges.Add($"Скидка {p.DiscountPercent}%");

        return new ProductDetailsData
        {
            Id = p.Id,
            Product = p,
            Brand = p.Brand,
            BrandSlug = p.BrandSlug,
            Name = p.Name,
            NameEn = p.NameEn,
            Price = Money(p.Price),
            OldPrice = p.DiscountPercent > 0 ? Money(p.OldPrice!.Value) : null,
            DiscountText = p.DiscountPercent > 0 ? $"−{p.DiscountPercent}%" : null,
            Badges = badges,
            Description = p.Description,
            Composition = p.Composition,
            Care = p.Care,
            Condition = p.Condition,
            RatingText = RatingText(p),
            Specs = new[]
            {
                new SpecRow("Артикул", p.Id),
                new SpecRow("Бренд", p.Brand),
                new SpecRow("Категория", $"{p.Group} · {p.Category}"),
                new SpecRow("Цвет", p.Color),
                new SpecRow("Материал", p.Material),
                new SpecRow("Для кого", p.Gender),
                new SpecRow("Состояние", p.Condition)
            }
        };
    }

    private string RatingText(Product p)
    {
        int count = Shop.Reviews.Count(p.Id);
        return count == 0
            ? "Отзывов пока нет"
            : $"★ {Shop.Reviews.Average(p.Id).ToString("0.0", Format.Ru)} · {Format.Plural(count, "отзыв", "отзыва", "отзывов")}";
    }

    private void RenderReviews()
    {
        Product p = product!;
        var reviews = Shop.Reviews.ForProduct(p.Id, Shop.User.Email)
            .Select(r => new ReviewData(r.Author, Format.Stars(r.Rating), Format.Date(r.Created), r.Text,
                r.Status switch { "pending" => "Ваш отзыв на модерации", "rejected" => "Отклонён модератором", _ => "" }))
            .ToList();
        view.ShowReviews(reviews, RatingText(p));

        bool can = Shop.Reviews.CanReview(Shop.User.Email, p.Id, out string reason);
        view.ShowReviewForm(can, can ? $"Оценка и текст от {ReviewModel.MinTextLength} до {ReviewModel.MaxTextLength} символов." : reason);
    }

    private void ShowQuantity() =>
        view.ShowQuantity(quantity.ToString(), quantity > 1, quantity < MaxQuantity);

    private void OnQuantityChangeRequested(object? sender, int delta)
    {
        if (product == null) return;
        quantity = Math.Clamp(quantity + delta, 1, MaxQuantity);
        ShowQuantity();
    }

    private void AddToCart(bool goToCart)
    {
        if (product == null) return;

        string? size = view.SelectedSize;
        if (size == null)
        {
            view.ShowMessage("Выберите размер.", isError: true);
            return;
        }

        if (!Shop.Cart.Add(product, size, quantity))
        {
            view.ShowMessage(Shop.Cart.Message, isError: true);
            return;
        }

        view.ShowMessage(Shop.Cart.Message, isError: false);
        Navigator.Notify(Shop.Cart.Message);
        if (goToCart)
        {
            Navigator.GoCart();
        }
    }

    private void OnFavoriteRequested(object? sender, EventArgs e)
    {
        if (product != null) ToggleFavorite(product.Id);
    }

    private void OnCompareRequested(object? sender, EventArgs e)
    {
        if (product == null) return;

        if (Shop.Compare.Contains(product.Id))
        {
            Shop.Compare.Remove(product.Id);
            Navigator.Notify(Shop.Compare.Message);
            return;
        }

        bool added = Shop.Compare.Add(product.Id);
        Navigator.Notify(Shop.Compare.Message, isError: !added);
    }

    private void OnReviewSubmitted(object? sender, EventArgs e)
    {
        if (product == null) return;

        if (view.ReviewRating is < 1 or > 5)
        {
            view.ShowMessage("Поставьте оценку — от 1 до 5 звёзд.", isError: true);
            return;
        }

        if (!Shop.Reviews.Add(Shop.User, product.Id, view.ReviewRating, view.ReviewText, DateTime.Now))
        {
            view.ShowMessage(Shop.Reviews.Message, isError: true);
            return;
        }

        view.ClearReviewForm();
        RenderReviews();
        view.ShowMessage(Shop.Reviews.Message, isError: false);
    }

    private void OnCurrencyChanged(object? sender, EventArgs e)
    {
        if (product == null) return;
        view.ShowProduct(Details(product));
        view.ShowSimilar(Cards(Shop.Catalog.Similar(product, 6)));
    }

    private void OnWishlistChanged(object? sender, EventArgs e)
    {
        if (product == null) return;
        view.SetFavorite(Shop.Wishlist.Contains(product.Id));
        view.ShowSimilar(Cards(Shop.Catalog.Similar(product, 6)));
    }

    private void OnCompareChanged(object? sender, EventArgs e)
    {
        if (product != null) view.SetCompared(Shop.Compare.Contains(product.Id));
    }
}
