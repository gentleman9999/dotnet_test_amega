using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using FinancialInstrumentAPI.Models;
using System.Timers;
using System.Threading.Channels;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace FinancialInstrumentAPI.Services
{
    /// <summary>
    /// Background service responsible for managing real-time price updates from Tiingo API
    /// and broadcasting them to connected WebSocket clients
    /// </summary>
    public class PriceUpdateBackgroundService : IHostedService, IDisposable
    {
        private readonly TiingoFxWebSocketClient _tiingoClient;
        private readonly WebSocketConnectionManager _connectionManager;
        private readonly TiingoFxHttp _tiingoFxHttp;
        private readonly ILogger<PriceUpdateBackgroundService> _logger;
        private readonly Channel<string> _broadcastChannel;
        private System.Timers.Timer? _reconnectTimer;

        // Concurrency control for high-volume broadcasting
        private readonly SemaphoreSlim _broadcastSemaphore;
        private const int MAX_CONCURRENT_SENDS = 100; // Limit concurrent send operations
        private const int BATCH_SIZE = 50; // Number of connections to process per batch

        /// <summary>
        /// Initializes a new instance of the PriceUpdateBackgroundService
        /// Sets up dependencies, event handlers, and concurrency controls for high-volume broadcasting
        /// </summary>
        /// <param name="tiingoClient">WebSocket client for real-time Tiingo data</param>
        /// <param name="connectionManager">Manager for WebSocket client connections</param>
        /// <param name="tiingoFxHttp">HTTP client for Tiingo REST API calls</param>
        /// <param name="logger">Logger instance for service logging</param>
        public PriceUpdateBackgroundService(
            TiingoFxWebSocketClient tiingoClient,
            WebSocketConnectionManager connectionManager,
            TiingoFxHttp tiingoFxHttp,
            ILogger<PriceUpdateBackgroundService> logger)
        {
            _tiingoClient = tiingoClient;
            _tiingoFxHttp = tiingoFxHttp;
            _connectionManager = connectionManager;
            _logger = logger;

            // Initialize semaphore for concurrency control
            _broadcastSemaphore = new SemaphoreSlim(MAX_CONCURRENT_SENDS, MAX_CONCURRENT_SENDS);

            // Create an unbounded channel for broadcasting messages to clients
            _broadcastChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            // Subscribe to Tiingo client messages for real-time price updates
            _tiingoClient.OnMessageReceived += async (sender, message) => await HandleTiingoMessageAsync(sender, message);

            // Set up a reconnect timer for resilience (currently set to 1 hour intervals)
            _reconnectTimer = new System.Timers.Timer(3600000); // Check every 3600 seconds (1 hour)
            // Note: Timer event handlers are commented out - can be enabled for automatic reconnection
            // _reconnectTimer.Elapsed += async (sender, e) => await CheckAndReconnectTiingo(); /// using websocket 
            // _reconnectTimer.Elapsed += async (sender, e) => await CheckAndReconnectTiingoRest();
            _reconnectTimer.AutoReset = true;
        }

        /// <summary>
        /// Starts the background service when the application starts
        /// Initiates connections to Tiingo WebSocket endpoints for both FX and crypto data
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
        /// <returns>Task representing the asynchronous start operation</returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PriceUpdateBackgroundService is starting.");
            // _reconnectTimer?.Start(); // Start the reconnect timer (currently disabled)

            // Initial connection attempt to Tiingo WebSocket endpoints
            _tiingoClient.StartAsyncCrypto(); // Start crypto data stream
            _tiingoClient.StartAsync();       // Start FX data stream
        }

        /// <summary>
        /// Stops the background service during application shutdown
        /// Cleanly disconnects from Tiingo WebSocket endpoints and disposes resources
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
        /// <returns>Task representing the asynchronous stop operation</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PriceUpdateBackgroundService is stopping.");
            // _reconnectTimer?.Stop(); // Stop the reconnect timer (currently disabled)

            // Stop both WebSocket connections gracefully
            await _tiingoClient.StopAsync();      // Stop FX data stream
            await _tiingoClient.StopCrypToAsync(); // Stop crypto data stream
            Dispose(); // Clean up resources
        }

        /// <summary>
        /// Checks the WebSocket connection status and attempts reconnection if needed
        /// Only reconnects if the current connection is not in an Open state
        /// </summary>
        /// <returns>Task representing the asynchronous reconnection check</returns>
        private async Task CheckAndReconnectTiingo()
        {
            // Only attempt reconnect if not currently connected
            if (_tiingoClient._ws == null || _tiingoClient._ws.State != WebSocketState.Open) // Accessing _ws directly for check
            {
                _logger.LogWarning("Tiingo client not connected or in an invalid state. Attempting reconnect...");
                await _tiingoClient.StartAsync();
            }
        }

        /// <summary>
        /// Alternative reconnection method using REST API calls instead of WebSocket
        /// Fetches latest price data via HTTP and broadcasts to connected clients
        /// Useful as a fallback when WebSocket connections are unstable
        /// </summary>
        /// <returns>Task representing the asynchronous REST API data fetch and broadcast</returns>
        private async Task CheckAndReconnectTiingoRest()
        {
            // Log the reconnection attempt
            _logger.LogWarning("Tiingo client not connected or in an invalid state. Attempting reconnect...");

            // Fetch FX data for major currency pairs via REST API
            var response = await _tiingoFxHttp.MainRestQueryTop("jpyusd,eurusd,gbpusd");
            string broadcastJson = JsonConvert.SerializeObject(response);
            await BroadcastMessageAsync(broadcastJson, string.Empty);

            // Fetch crypto data (Bitcoin) via REST API
            var btc_response = await _tiingoFxHttp.MainRestCrypToQueryTop("btcusd");
            await BroadcastMessageAsync(JsonConvert.SerializeObject(btc_response), string.Empty);
        }

        /// <summary>
        /// Handles incoming messages from the Tiingo WebSocket client
        /// Parses different message types and broadcasts relevant price/quote data to clients
        /// </summary>
        /// <param name="sender">The source of the message (typically the Tiingo client)</param>
        /// <param name="message">Raw JSON message string from Tiingo</param>
        /// <returns>Task representing the asynchronous message handling</returns>
        private async Task HandleTiingoMessageAsync(object? sender, string message)
        {
            // Log the raw message for debugging purposes
            _logger.LogDebug("Received from Tiingo: {Message}", message);

            // Attempt to parse the base message type
            TiingoWsMessage? baseMessage = JsonConvert.DeserializeObject<TiingoWsMessage>(message);

            if (baseMessage == null)
            {
                _logger.LogWarning("Could not deserialize Tiingo message: {Message}", message);
                return;
            }

            // Filter relevant message types for price updates
            if (baseMessage.MessageType == "A" || baseMessage.MessageType == "Q")
            {
                // Handle price/quote messages (A = Aggregate, Q = Quote)
                // Re-serialize and deserialize to the specific type, as 'Data' is 'object'
                string strOut = baseMessage.Data?.ToString() ?? string.Empty;

                if (!string.IsNullOrEmpty(strOut))
                {
                    // Convert the specific price/quote object back to JSON to broadcast
                    _logger.LogInformation("Broadcasting price update: {Json}", strOut);
                    JArray arr = JArray.Parse(strOut);
                    string ticker = arr[1].Value<string>() ?? string.Empty;
                    await BroadcastMessageAsync(strOut, ticker);
                }
                else
                {
                    _logger.LogWarning("Failed to parse typed data for message type {Type}: {Message}", baseMessage.MessageType, message);
                }
            }
            else if (baseMessage.MessageType == "I")
            {
                // Handle informational messages
                _logger.LogInformation("Tiingo Info Message: {Message}", message);
            }
            else if (baseMessage.MessageType == "H")
            {
                // Handle heartbeat messages to confirm connection is alive
                _logger.LogInformation("Tiingo Heartbeat Message received.");
            }
            else if (baseMessage.MessageType == "E")
            {
                // Handle error messages from Tiingo
                _logger.LogError("Tiingo Error Message: {Message}", message);
            }
        }

        /// <summary>
        /// Efficiently broadcasts messages to a large number of WebSocket connections (1000+)
        /// Uses batch processing and concurrency control to handle high-volume broadcasting
        /// Optimizes memory usage by serializing the message only once
        /// </summary>
        /// <param name="message">JSON message string to broadcast to all clients</param>
        /// <returns>Task representing the asynchronous broadcast operation</returns>
        private async Task BroadcastMessageAsync(string message, string ticker)
        {
            // Convert message to bytes only once for memory optimization
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var buffer = new ArraySegment<byte>(messageBytes);

            // Get all currently connected WebSocket clients
            var connections = _connectionManager.GetAllConnections();
            var totalConnections = connections.Count;

            if (totalConnections == 0)
            {
                _logger.LogDebug("No active connections to broadcast to.");
                return;
            }

            _logger.LogInformation("Broadcasting message to {ConnectionCount} connections", totalConnections);

            // Filter only valid connections (Open state connections only)
            var validConnections = connections
                .Where(kvp => kvp.Value.Socket.State == WebSocketState.Open && (kvp.Value.Subscriptions.Count == 0 || kvp.Value.Subscriptions.Contains(ticker)))
                .ToList();

            var validConnectionCount = validConnections.Count;
            if (validConnectionCount == 0)
            {
                _logger.LogDebug("No open connections available for broadcasting.");
                return;
            }

            // Collection to track failed connection IDs for cleanup
            var failedConnections = new ConcurrentBag<string>();

            try
            {
                // Process connections in batches for better resource management
                var batches = validConnections
                    .Select((connection, index) => new { connection, index })
                    .GroupBy(x => x.index / BATCH_SIZE)
                    .Select(g => g.Select(x => x.connection).ToList())
                    .ToList();

                _logger.LogDebug("Processing {BatchCount} batches with batch size {BatchSize}",
                    batches.Count, BATCH_SIZE);

                // Process each batch in parallel
                var batchTasks = batches.Select(batch => ProcessBatchAsync(batch, buffer, failedConnections));
                await Task.WhenAll(batchTasks);

                // Calculate successfully sent connections count
                var successCount = validConnectionCount - failedConnections.Count;
                _logger.LogInformation("Broadcast completed. Success: {SuccessCount}, Failed: {FailedCount}",
                    successCount, failedConnections.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch broadcasting");
            }
            finally
            {
                // Asynchronously cleanup failed connections (fire-and-forget)
                if (!failedConnections.IsEmpty)
                {
                    _ = Task.Run(async () => await CleanupFailedConnectionsAsync(failedConnections));
                }
            }
        }

        /// <summary>
        /// Processes a batch of connections for broadcasting
        /// Handles concurrent sending to multiple connections within a batch
        /// </summary>
        /// <param name="batch">List of connection key-value pairs to process</param>
        /// <param name="buffer">Message buffer to send</param>
        /// <param name="failedConnections">Collection to track failed connections</param>
        /// <returns>Task representing the batch processing operation</returns>
        private async Task ProcessBatchAsync(
            List<KeyValuePair<string, (
            WebSocket Socket,
            List<string> Subscriptions)>> batch,
            ArraySegment<byte> buffer,
            ConcurrentBag<string> failedConnections)
        {
            var sendTasks = batch.Select(kvp => SendToConnectionAsync(kvp.Key, kvp.Value, buffer, failedConnections));
            await Task.WhenAll(sendTasks);
        }

        /// <summary>
        /// Sends message to an individual WebSocket connection with error handling
        /// Uses semaphore to control concurrent send operations and prevent resource exhaustion
        /// Implements timeout to prevent infinite waiting on unresponsive connections
        /// </summary>
        /// <param name="connectionId">Unique identifier for the connection</param>
        /// <param name="webSocket">WebSocket instance to send data to</param>
        /// <param name="buffer">Message buffer to send</param>
        /// <param name="failedConnections">Collection to add failed connection IDs</param>
        /// <returns>Task representing the send operation</returns>
        private async Task SendToConnectionAsync(
            string connectionId,
            (
            WebSocket Socket,
            List<string> Subscriptions) connection,
            ArraySegment<byte> buffer,
            ConcurrentBag<string> failedConnections)
        {
            // Use semaphore to limit concurrent send operations
            await _broadcastSemaphore.WaitAsync();

            try
            {
                // Re-check connection state just before sending
                if (connection.Socket.State != WebSocketState.Open)
                {
                    _logger.LogDebug("Connection {ConnectionId} is no longer open (State: {State})",
                        connectionId, connection.Socket.State);
                    failedConnections.Add(connectionId);
                    return;
                }

                // Set timeout to prevent infinite waiting
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await connection.Socket.SendAsync(buffer, WebSocketMessageType.Text, true, cts.Token);

                _logger.LogTrace("Successfully sent message to connection {ConnectionId}", connectionId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Send operation timed out for connection {ConnectionId}", connectionId);
                failedConnections.Add(connectionId);
            }
            catch (Exception ex) when (IsConnectionException(ex))
            {
                _logger.LogDebug(ex, "Send failed for connection {ConnectionId}", connectionId);
                failedConnections.Add(connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending to connection {ConnectionId}", connectionId);
                failedConnections.Add(connectionId);
            }
            finally
            {
                _broadcastSemaphore.Release();
            }
        }

        /// <summary>
        /// Cleans up failed connections by removing them from the connection manager
        /// Runs asynchronously to avoid blocking the main broadcast operation
        /// </summary>
        /// <param name="failedConnections">Collection of connection IDs that failed to receive messages</param>
        /// <returns>Task representing the cleanup operation</returns>
        private async Task CleanupFailedConnectionsAsync(ConcurrentBag<string> failedConnections)
        {
            try
            {
                var cleanupTasks = failedConnections.Select(connectionId =>
                    _connectionManager.RemoveConnectionAsync(connectionId));

                await Task.WhenAll(cleanupTasks);

                _logger.LogInformation("Cleaned up {Count} failed connections", failedConnections.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during connection cleanup");
            }
        }

        /// <summary>
        /// </summary>
        private static bool IsConnectionException(Exception ex)
            => ex is WebSocketException or IOException or ObjectDisposedException;

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            _reconnectTimer?.Dispose();
            _broadcastSemaphore?.Dispose();
        }
    }
}