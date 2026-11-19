using System;

namespace AmfibiaShop.Models;

/// <summary>Промокод: скидка в процентах и срок действия.</summary>
public class PromoCode
{
    public PromoCode(string code, int percent, DateTime validFrom, DateTime? validTo)
    {
        Code = code;
        Percent = percent;
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public string Code { get; }

    public int Percent { get; }

    public DateTime ValidFrom { get; }

    /// <summary>Последний день действия (null — бессрочный).</summary>
    public DateTime? ValidTo { get; }
}
