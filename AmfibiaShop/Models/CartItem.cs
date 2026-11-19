namespace AmfibiaShop.Models;

/// <summary>Строка корзины: товар, размер и количество.</summary>
public class CartItem
{
    public CartItem(Product product, string size)
    {
        Product = product;
        Size = size;
        Quantity = 1;
    }

    public Product Product { get; }

    public string Size { get; }

    public int Quantity { get; internal set; }

    /// <summary>Стоимость строки: цена × количество.</summary>
    public decimal Total => Product.Price * Quantity;
}
