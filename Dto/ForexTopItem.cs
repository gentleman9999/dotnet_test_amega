
using System.Text.Json.Serialization;

public class TiingoPriceData
{
  [JsonPropertyName("ticker")]
  public string Ticker { get; set; } = string.Empty;

  [JsonPropertyName("quoteTimestamp")]
  public DateTime QuoteTimestamp { get; set; }

  [JsonPropertyName("bidPrice")]
  public decimal BidPrice { get; set; }

  [JsonPropertyName("bidSize")]
  public decimal BidSize { get; set; }

  [JsonPropertyName("askPrice")]
  public decimal AskPrice { get; set; }

  [JsonPropertyName("askSize")]
  public decimal AskSize { get; set; }

  [JsonPropertyName("midPrice")]
  public decimal MidPrice { get; set; }
}