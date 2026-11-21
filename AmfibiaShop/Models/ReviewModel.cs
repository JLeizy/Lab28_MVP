using System;
using System.Collections.Generic;
using System.Linq;

namespace AmfibiaShop.Models;

/// <summary>
/// МОДЕЛЬ отзывов. Правило сайта: отзыв можно оставить только на купленный
/// и полученный товар; новый отзыв попадает на модерацию.
/// </summary>
public class ReviewModel
{
    public const int MinTextLength = 10;
    public const int MaxTextLength = 1000;
    private const string FileName = "reviews.json";

    private readonly Storage storage;
    private readonly OrderModel orders;
    private readonly List<Review> reviews;

    public ReviewModel(ShopData data, Storage storage, OrderModel orders)
    {
        this.storage = storage;
        this.orders = orders;
        reviews = storage.Load<List<Review>>(FileName) ?? data.Reviews;
    }

    public string Message { get; private set; } = string.Empty;

    /// <summary>Опубликованные отзывы и собственные отзывы пользователя (даже на модерации).</summary>
    public IReadOnlyList<Review> ForProduct(string productId, string? userEmail) =>
        reviews.Where(r => r.Product == productId &&
                           (r.Status == "approved" || string.Equals(r.User, userEmail, StringComparison.OrdinalIgnoreCase)))
               .OrderByDescending(r => r.Created)
               .ToList();

    public int Count(string productId) => reviews.Count(r => r.Product == productId && r.Status == "approved");

    /// <summary>Средняя оценка опубликованных отзывов (0 — отзывов нет).</summary>
    public double Average(string productId)
    {
        var ratings = reviews.Where(r => r.Product == productId && r.Status == "approved").Select(r => r.Rating).ToList();
        return ratings.Count == 0 ? 0 : Math.Round(ratings.Average(), 1);
    }

    public int CountByUser(string email) =>
        reviews.Count(r => string.Equals(r.User, email, StringComparison.OrdinalIgnoreCase));

    public bool CanReview(string email, string productId, out string reason)
    {
        if (reviews.Any(r => r.Product == productId && string.Equals(r.User, email, StringComparison.OrdinalIgnoreCase)))
        {
            reason = "Вы уже оставили отзыв на этот товар.";
            return false;
        }

        if (!orders.HasDelivered(email, productId))
        {
            reason = "Отзыв можно оставить после покупки и получения товара.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public bool Add(User user, string productId, int rating, string text, DateTime now)
    {
        if (!CanReview(user.Email, productId, out string reason))
        {
            Message = reason;
            return false;
        }

        if (rating is < 1 or > 5)
        {
            Message = "Поставьте оценку от 1 до 5 звёзд.";
            return false;
        }

        string clean = text.Trim();
        if (clean.Length < MinTextLength || clean.Length > MaxTextLength)
        {
            Message = $"Текст отзыва — от {MinTextLength} до {MaxTextLength} символов.";
            return false;
        }

        reviews.Add(new Review
        {
            Product = productId,
            User = user.Email,
            Author = user.FirstName,
            Rating = rating,
            Text = clean,
            Status = "pending",
            Created = now
        });
        storage.Save(FileName, reviews);
        Message = "Спасибо! Отзыв появится после проверки модератором.";
        return true;
    }
}
