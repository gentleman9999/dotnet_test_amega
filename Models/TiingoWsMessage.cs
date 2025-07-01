using Newtonsoft.Json; // Use Newtonsoft.Json for Tiingo's specific JSON structure

namespace FinancialInstrumentAPI.Models
{
    // Base class for all Tiingo WebSocket messages
    public class TiingoWsMessage
    {
        [JsonProperty("messageType")]
        public string MessageType { get; set; } = string.Empty;

        [JsonProperty("data")]
        public object? Data { get; set; } // Can be different types based on MessageType
    }

    // For 'A' (Aggregated) price messages
    public class TiingoWsAggregatedPrice
    {
        [JsonProperty("service")]
        public string Service { get; set; } = string.Empty; // "fx" or "crypto"

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; } // Unix epoch in nanoseconds

        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty; // Ticker (e.g., "eurusd")

        [JsonProperty("open")]
        public decimal Open { get; set; }

        [JsonProperty("high")]
        public decimal High { get; set; }

        [JsonProperty("low")]
        public decimal Low { get; set; }

        [JsonProperty("close")]
        public decimal Close { get; set; }

        [JsonProperty("volume")]
        public decimal Volume { get; set; }

        // Helper to convert nanoseconds timestamp to DateTime
        public DateTime GetDateTime()
        {
            // Convert nanoseconds to milliseconds (1,000,000 nanoseconds = 1 millisecond)
            long milliseconds = Timestamp / 1_000_000;
            // Unix epoch starts Jan 1, 1970
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(milliseconds);
        }
    }

    // For 'Q' (Quote) messages
    public class TiingoWsQuote
    {
        [JsonProperty("service")]
        public string Service { get; set; } = string.Empty; // "fx" or "crypto"

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; } // Unix epoch in nanoseconds

        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty; // Ticker (e.g., "eurusd")

        [JsonProperty("bidSize")]
        public decimal BidSize { get; set; }

        [JsonProperty("bidPrice")]
        public decimal BidPrice { get; set; }

        [JsonProperty("askSize")]
        public decimal AskSize { get; set; }

        [JsonProperty("askPrice")]
        public decimal AskPrice { get; set; }

        [JsonProperty("midPrice")]
        public decimal MidPrice { get; set; }

        public DateTime GetDateTime()
        {
            long milliseconds = Timestamp / 1_000_000;
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(milliseconds);
        }
    }
}