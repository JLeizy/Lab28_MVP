using System;
using System.Text.Json.Serialization;

namespace CurrencyConverter.Models;

/// <summary>
/// Одна запись из ответа API НБРБ:
/// https://api.nbrb.by/exrates/rates?periodicity=0
/// Пример: {"Cur_Abbreviation":"USD","Cur_Scale":1,"Cur_OfficialRate":2.9950,"Date":"2026-11-19T00:00:00"}
/// </summary>
public class NbrbRate
{
    [JsonPropertyName("Cur_Abbreviation")]
    public string Abbreviation { get; set; } = string.Empty;

    [JsonPropertyName("Cur_Scale")]
    public int Scale { get; set; }

    [JsonPropertyName("Cur_OfficialRate")]
    public decimal? OfficialRate { get; set; }

    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }
}
