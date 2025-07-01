using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FinancialInstrumentAPI.Services
{
    /// <summary>
    /// HTTP client service for interacting with Tiingo API endpoints
    /// Provides methods for fetching financial data including FX rates and cryptocurrency prices
    /// Handles REST API calls with proper error handling and JSON deserialization
    /// </summary>
    public class TiingoFxHttp
    {
        #region Private Fields
        
        /// <summary>
        /// Base URL for Tiingo API endpoints
        /// </summary>
        private readonly string _tiingoApiUrl = "https://api.tiingo.com/tiingo/";
        
        /// <summary>
        /// API key for authenticating with Tiingo services
        /// Initially set to a default value but overridden by configuration
        /// </summary>
        private readonly string _apiKey = "161c3af5c43829930880f787f8f789daa49053a8";

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the TiingoFxHttp service
        /// Retrieves API key from application configuration
        /// </summary>
        /// <param name="configuration">Application configuration containing Tiingo API settings</param>
        /// <exception cref="ArgumentNullException">Thrown when Tiingo:ApiKey is not configured</exception>
        public TiingoFxHttp(IConfiguration configuration)
        {
            _apiKey = configuration["Tiingo:ApiKey"] ?? throw new ArgumentNullException("Tiingo:ApiKey not configured.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes a generic REST query to Tiingo API for price data
        /// Returns raw JSON response as string for flexible processing
        /// Used for custom queries with user-defined parameters
        /// </summary>
        /// <param name="ticker">Financial instrument ticker symbol (e.g., "AAPL", "eurusd")</param>
        /// <param name="_params">Query parameters string (e.g., "startDate=2023-01-01&endDate=2023-12-31")</param>
        /// <returns>Raw JSON response string from Tiingo API, or null if request fails</returns>
        public async Task<string?> MainRestQuery(string ticker, string _params)
        {
            // Construct the complete API request URL with ticker, parameters, and authentication
            string requestUri = $"{_tiingoApiUrl}{ticker}/prices?{_params}&token={_apiKey}";
            
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Execute GET request to Tiingo API
                    // Note: Content-type header is commented out as it's not needed for GET requests
                    // client.DefaultRequestHeaders.Add("Content-type", "application/json");
                    HttpResponseMessage response = await client.GetAsync(requestUri);
                    response.EnsureSuccessStatusCode();
                    
                    // Read and return the response body
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    // Debug output for development and troubleshooting
                    Console.WriteLine("\n--- Raw Response ---");
                    Console.WriteLine(responseBody);
                    Console.WriteLine("--------------------\n");
                    
                    return responseBody;
                }
                catch (HttpRequestException e)
                {
                    // Handle HTTP-specific errors (network issues, HTTP status errors)
                    Console.WriteLine($"Request Error: {e.Message}");
                    if (e.StatusCode.HasValue)
                    {
                        Console.WriteLine($"Status Code: {e.StatusCode.Value}");
                    }
                }
                catch (Exception e)
                {
                    // Handle any other unexpected errors
                    Console.WriteLine($"An unexpected error occurred: {e.Message}");
                }
            }
            return null;
        }

        /// <summary>
        /// Fetches top-of-book FX rate data from Tiingo API
        /// Returns structured data objects for easier consumption
        /// Specifically designed for foreign exchange rate queries
        /// </summary>
        /// <param name="ticker">FX pair ticker (e.g., "eurusd", "gbpjpy")</param>
        /// <returns>List of TiingoPriceData objects containing FX rate information, or null if request fails</returns>
        public async Task<List<TiingoPriceData>?> MainRestQueryTop(string ticker)
        {
            // Construct FX-specific API endpoint URL
            string requestUri = $"{_tiingoApiUrl}fx/top?tickers={ticker}&token={_apiKey}";
            
            // Configure JSON deserialization options for case-insensitive property matching
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Execute GET request to Tiingo FX API endpoint
                    // client.DefaultRequestHeaders.Add("Content-type", "application/json");
                    HttpResponseMessage response = await client.GetAsync(requestUri);
                    response.EnsureSuccessStatusCode();
                    
                    // Read raw JSON response
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    // Debug output for development and troubleshooting
                    Console.WriteLine("\n--- Raw Response ---");
                    Console.WriteLine(responseBody);
                    Console.WriteLine("--------------------\n");
                    
                    // Deserialize JSON response to strongly-typed objects
                    List<TiingoPriceData>? priceDataList = JsonSerializer.Deserialize<List<TiingoPriceData>>(responseBody, options);
                    return priceDataList;
                }
                catch (HttpRequestException e)
                {
                    // Handle HTTP-specific errors (network issues, HTTP status errors)
                    Console.WriteLine($"Request Error: {e.Message}");
                    if (e.StatusCode.HasValue)
                    {
                        Console.WriteLine($"Status Code: {e.StatusCode.Value}");
                    }
                }
                catch (JsonException ex)
                {
                    // Handle JSON deserialization errors with detailed error information
                    Console.WriteLine($"JSON deserialization error: {ex.Message}");
                    Console.WriteLine($"Error occurred at Line: {ex.LineNumber}, Position: {ex.BytePositionInLine}");
                }
                catch (Exception e)
                {
                    // Handle any other unexpected errors
                    Console.WriteLine($"An unexpected error occurred: {e.Message}");
                }
            }
            return null;
        }

        /// <summary>
        /// Fetches top-of-book cryptocurrency price data from Tiingo API
        /// Returns structured data objects specifically for cryptocurrency markets
        /// Handles crypto-specific data fields and formatting
        /// </summary>
        /// <param name="ticker">Cryptocurrency ticker symbol (e.g., "btcusd", "ethusd")</param>
        /// <returns>List of TiingoFxTopCrypToData objects containing cryptocurrency price information, or null if request fails</returns>
        public async Task<List<TiingoFxTopCrypToData>?> MainRestCrypToQueryTop(string ticker)
        {
            // Construct crypto-specific API endpoint URL
            string requestUri = $"{_tiingoApiUrl}crypto/top?tickers={ticker}&token={_apiKey}";
            
            // Configure JSON deserialization options for case-insensitive property matching
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Execute GET request to Tiingo Crypto API endpoint
                    // client.DefaultRequestHeaders.Add("Content-type", "application/json");
                    HttpResponseMessage response = await client.GetAsync(requestUri);
                    response.EnsureSuccessStatusCode();
                    
                    // Read raw JSON response
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    // Debug output for development and troubleshooting
                    Console.WriteLine("\n--- Raw Response ---");
                    Console.WriteLine(responseBody);
                    Console.WriteLine("--------------------\n");
                    
                    // Deserialize JSON response to strongly-typed cryptocurrency data objects
                    List<TiingoFxTopCrypToData>? priceDataList = JsonSerializer.Deserialize<List<TiingoFxTopCrypToData>>(responseBody, options);
                    return priceDataList;
                }
                catch (HttpRequestException e)
                {
                    // Handle HTTP-specific errors (network issues, HTTP status errors)
                    Console.WriteLine($"Request Error: {e.Message}");
                    if (e.StatusCode.HasValue)
                    {
                        Console.WriteLine($"Status Code: {e.StatusCode.Value}");
                    }
                }
                catch (JsonException ex)
                {
                    // Handle JSON deserialization errors with detailed error information
                    Console.WriteLine($"JSON deserialization error: {ex.Message}");
                    Console.WriteLine($"Error occurred at Line: {ex.LineNumber}, Position: {ex.BytePositionInLine}");
                }
                catch (Exception e)
                {
                    // Handle any other unexpected errors
                    Console.WriteLine($"An unexpected error occurred: {e.Message}");
                }
            }
            return null;
        }

        #endregion
    }
}