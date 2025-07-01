using System.Text.Json.Serialization;

public class TiingoFxTopCrypToData
{
    [JsonPropertyName("ticker")]
    public string Ticker { get; set; } = string.Empty;
    [JsonPropertyName("baseCurrency")]
    public string BaseCurrency { get; set; } = string.Empty;

    [JsonPropertyName("quoteCurrency")]
    public string QuoteCurrency { get; set; } = string.Empty;

    [JsonPropertyName("topOfBookData")]
    public List<TopOfBookDatum> TopOfBookData { get; set; } = new List<TopOfBookDatum>();

}

public class TopOfBookDatum
{
    [JsonPropertyName("quoteTimestamp")]
    public DateTime QuoteTimestamp { get; set; }

    [JsonPropertyName("lastSaleTimestamp")]
    public DateTime LastSaleTimestamp { get; set; }

    [JsonPropertyName("bidSize")]
    public decimal BidSize { get; set; } // Use decimal for precision in sizes/quantities

    [JsonPropertyName("bidPrice")]
    public decimal BidPrice { get; set; }

    [JsonPropertyName("askSize")]
    public decimal AskSize { get; set; }

    [JsonPropertyName("askPrice")]
    public decimal AskPrice { get; set; }

    [JsonPropertyName("lastSize")]
    public decimal LastSize { get; set; }

    [JsonPropertyName("lastSizeNotional")]
    public decimal LastSizeNotional { get; set; }

    [JsonPropertyName("lastPrice")]
    public decimal LastPrice { get; set; }

    [JsonPropertyName("bidExchange")]
    public string BidExchange { get; set; } = string.Empty;

    [JsonPropertyName("askExchange")]
    public string AskExchange { get; set; } = string.Empty;

    [JsonPropertyName("lastExchange")]
    public string LastExchange { get; set; } = string.Empty;
}