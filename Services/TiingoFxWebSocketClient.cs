using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using FinancialInstrumentAPI.Models; // Ensure this matches your Models namespace

namespace FinancialInstrumentAPI.Services
{
    /// <summary>
    /// WebSocket client for connecting to Tiingo's real-time financial data streams.
    /// Supports both FX (Foreign Exchange) and Crypto currency data feeds.
    /// </summary>
    public class TiingoFxWebSocketClient : IDisposable
    {
        private readonly string _tiingoWsUrl;
        private readonly string _apiKey;
        public ClientWebSocket? _ws;
        private CancellationTokenSource? _cts;

        public ClientWebSocket? _ws1;
        private CancellationTokenSource? _cts1;

        private readonly ILogger<TiingoFxWebSocketClient> _logger;

        // Event to notify when a new message is received
        public event EventHandler<string>? OnMessageReceived;

        /// <summary>
        /// Initializes a new instance of the TiingoFxWebSocketClient.
        /// </summary>
        /// <param name="configuration">Application configuration containing Tiingo WebSocket URL and API key</param>
        /// <param name="logger">Logger instance for logging connection events and errors</param>
        /// <exception cref="ArgumentNullException">Thrown when required configuration values are missing</exception>
        public TiingoFxWebSocketClient(IConfiguration configuration, ILogger<TiingoFxWebSocketClient> logger)
        {
            _tiingoWsUrl = configuration["Tiingo:WebSocketUrl"] ?? throw new ArgumentNullException("Tiingo:WebSocketUrl not configured.");
            _apiKey = configuration["Tiingo:ApiKey"] ?? throw new ArgumentNullException("Tiingo:ApiKey not configured.");
            _logger = logger;
        }

        /// <summary>
        /// Starts the WebSocket connection to Tiingo's FX data feed and subscribes to specified currency pairs.
        /// Establishes connection, sends subscription message, and begins receiving real-time price updates.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _ws = new ClientWebSocket();

            try
            {
                // Establish WebSocket connection to Tiingo FX endpoint
                _logger.LogInformation("Connecting to Tiingo WebSocket at {Url}...", _tiingoWsUrl);
                await _ws.ConnectAsync(new Uri(_tiingoWsUrl), _cts.Token);
                _logger.LogInformation("Connected to Tiingo WebSocket.");

                // Create subscription message for FX currency pairs
                var subscribeMessage = new
                {
                    eventName = "subscribe",
                    authorization = _apiKey,
                    eventData = new
                    {
                        thresholdLevel = 2, // Or other levels based on your needs,
                        tickers = new[] { "eurusd", "gbpusd", "jpyusd" }
                        // tickers = new[] { "usdeur", "gbpusd", "btcusd" }
                    }
                };

                // Send subscription request to Tiingo
                string jsonMessage = JsonConvert.SerializeObject(subscribeMessage);
                byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage);
                await _ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, _cts.Token);
                _logger.LogInformation("Subscription request sent to Tiingo.");

                // Start receiving messages in the background
                await ReceiveMessagesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect or subscribe to Tiingo WebSocket.");
                await StopAsync(); // Attempt to clean up
            }
        }

        /// <summary>
        /// Starts the WebSocket connection to Tiingo's Crypto data feed and subscribes to cryptocurrency pairs.
        /// Establishes connection to crypto-specific endpoint, sends subscription message, and begins receiving real-time crypto price updates.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task StartAsyncCrypto()
        {
            _cts1 = new CancellationTokenSource();
            _ws1 = new ClientWebSocket();

            try
            {
                // Establish WebSocket connection to Tiingo Crypto endpoint
                _logger.LogInformation("Connecting to Tiingo WebSocket at {Url}...", _tiingoWsUrl);
                await _ws1.ConnectAsync(new Uri("wss://api.tiingo.com/crypto"), _cts1.Token);
                _logger.LogInformation("Connected to Tiingo Crypto WebSocket.");

                // Create subscription message for cryptocurrency pairs
                var subscribeMessage = new
                {
                    eventName = "subscribe",
                    authorization = _apiKey,
                    eventData = new
                    {
                        thresholdLevel = 2, // Or other levels based on your needs,
                        tickers = new[] { "btcusd" }
                    }
                };

                // Send subscription request to Tiingo Crypto endpoint
                string jsonMessage = JsonConvert.SerializeObject(subscribeMessage);
                byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage);
                await _ws1.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, _cts1.Token);
                _logger.LogInformation("Subscription request sent to Tiingo.");

                // Start receiving messages in the background
                await ReceiveMessagesCrypToAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect or subscribe to Tiingo WebSocket.");
                await StopCrypToAsync(); // Attempt to clean up
            }
        }

        /// <summary>
        /// Stops the FX WebSocket connection gracefully.
        /// Cancels ongoing operations and closes the WebSocket connection with normal closure status.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task StopAsync()
        {
            // Cancel any ongoing operations
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }

            // Close WebSocket connection if it's in a valid state
            if (_ws != null && (_ws.State == WebSocketState.Open || _ws.State == WebSocketState.CloseReceived || _ws.State == WebSocketState.CloseSent))
            {
                _logger.LogInformation("Closing Tiingo WebSocket connection...");
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client initiated close", CancellationToken.None);
                _logger.LogInformation("Tiingo WebSocket connection closed.");
            }
        }

        /// <summary>
        /// Stops the Crypto WebSocket connection gracefully.
        /// Cancels ongoing operations and closes the crypto WebSocket connection with normal closure status.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task StopCrypToAsync()
        {
            // Cancel any ongoing crypto operations
            if (_cts1 != null && !_cts1.IsCancellationRequested)
            {
                _cts1.Cancel();
            }

            // Close crypto WebSocket connection if it's in a valid state
            if (_ws1 != null && (_ws1.State == WebSocketState.Open || _ws1.State == WebSocketState.CloseReceived || _ws1.State == WebSocketState.CloseSent))
            {
                _logger.LogInformation("Closing Tiingo WebSocket connection...");
                await _ws1.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client initiated close", CancellationToken.None);
                _logger.LogInformation("Tiingo WebSocket connection closed.");
            }
        }

        /// <summary>
        /// Continuously receives messages from the FX WebSocket connection.
        /// Processes incoming text messages, handles connection closures, and raises events for received data.
        /// Runs in a loop until the connection is closed or cancelled.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[4 * 1024]; // 4KB buffer for incoming messages
            try
            {
                // Continue receiving messages while connection is open and not cancelled
                while (_ws != null && _ws.State == WebSocketState.Open && !_cts!.IsCancellationRequested)
                {
                    // Receive message from WebSocket
                    WebSocketReceiveResult result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // Process text message and raise event for subscribers
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogDebug("Tiingo Raw Message: {Message}", receivedMessage);
                        OnMessageReceived?.Invoke(this, receivedMessage); // Raise the event
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // Handle server-initiated connection closure
                        _logger.LogWarning("Tiingo WebSocket closed by server. Status: {Status}, Description: {Description}", result.CloseStatus, result.CloseStatusDescription);
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server requested close", CancellationToken.None);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        // Log unexpected binary messages (Tiingo typically sends text)
                        _logger.LogWarning("Received unexpected binary message from Tiingo.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Handle graceful cancellation
                _logger.LogInformation("Tiingo WebSocket message reception cancelled.");
            }
            catch (Exception ex)
            {
                // Handle unexpected errors during message reception
                _logger.LogError(ex, "Error during Tiingo WebSocket message reception.");
            }
            finally
            {
                // Ensure cleanup regardless of how the loop exits
                await StopAsync(); // Ensure cleanup if loop exits
            }
        }

        /// <summary>
        /// Continuously receives messages from the Crypto WebSocket connection.
        /// Processes incoming crypto text messages, handles connection closures, and raises events for received data.
        /// Runs in a loop until the connection is closed or cancelled.
        /// </summary>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task ReceiveMessagesCrypToAsync()
        {
            var buffer = new byte[4 * 1024]; // 4KB buffer for incoming crypto messages
            try
            {
                // Continue receiving messages while crypto connection is open and not cancelled
                while (_ws1 != null && _ws1.State == WebSocketState.Open && !_cts1!.IsCancellationRequested)
                {
                    // Receive message from crypto WebSocket
                    WebSocketReceiveResult result = await _ws1.ReceiveAsync(new ArraySegment<byte>(buffer), _cts1.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // Process crypto text message and raise event for subscribers
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogDebug("Tiingo Crypto Raw Message: {Message}", receivedMessage);
                        OnMessageReceived?.Invoke(this, receivedMessage); // Raise the event
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        // Handle server-initiated crypto connection closure
                        _logger.LogWarning("Tiingo WebSocket closed by server. Status: {Status}, Description: {Description}", result.CloseStatus, result.CloseStatusDescription);
                        await _ws1.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server requested close", CancellationToken.None);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        // Log unexpected binary messages from crypto feed
                        _logger.LogWarning("Received unexpected binary message from Tiingo.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Handle graceful cancellation of crypto feed
                _logger.LogInformation("Tiingo WebSocket message reception cancelled.");
            }
            catch (Exception ex)
            {
                // Handle unexpected errors during crypto message reception
                _logger.LogError(ex, "Error during Tiingo WebSocket message reception.");
            }
            finally
            {
                // Ensure cleanup regardless of how the crypto loop exits
                await StopCrypToAsync(); // Ensure cleanup if loop exits
            }
        }

        /// <summary>
        /// Disposes of managed resources including WebSocket connections and cancellation tokens.
        /// Implements IDisposable pattern to ensure proper cleanup of network resources.
        /// </summary>
        public void Dispose()
        {
            // Dispose of FX connection resources
            _cts?.Dispose();
            _ws?.Dispose();
            
            // Dispose of Crypto connection resources
            _cts1?.Dispose();
            _ws1?.Dispose();
        }
    }
}