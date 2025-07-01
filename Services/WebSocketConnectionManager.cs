using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace FinancialInstrumentAPI.Services
{
    public class WebSocketConnectionManager
    {
        // connectionId -> (WebSocket, Subscribed tickers)
        private readonly ConcurrentDictionary<string, (
            WebSocket Socket,
            List<string> Subscriptions)> _connections
            = new();

        private readonly ILogger<WebSocketConnectionManager> _logger;

        public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
        {
            _logger = logger;
        }

        public string AddConnection(WebSocket socket, List<string> subscriptions)
        {
            var connectionId = Guid.NewGuid().ToString();
            _connections.TryAdd(connectionId, (socket, subscriptions));
            _logger.LogInformation("New WebSocket connection added: {ConnectionId}. Total connections: {Count}", connectionId, _connections.Count);
            return connectionId;
        }

        public async Task RemoveConnectionAsync(string connectionId)
        {
            if (_connections.TryRemove(connectionId, out var entry))
            {
                _logger.LogInformation("WebSocket connection removed: {ConnectionId}. Remaining connections: {Count}", connectionId, _connections.Count);
                if (entry.Socket.State != WebSocketState.Closed && entry.Socket.State != WebSocketState.Aborted)
                {
                    try
                    {
                        // Give some time for client to close if it hasn't
                        await entry.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while closing WebSocket for {ConnectionId}", connectionId);
                    }
                }
                entry.Socket.Dispose();
            }
        }

        // Return all connections
        public IReadOnlyDictionary<string, (WebSocket Socket, List<string> Subscriptions)>
            GetAllConnections() => _connections;

        // Filter only connections subscribed to the given ticker
        public List<KeyValuePair<string, WebSocket>> GetConnectionsByTicker(string ticker)
        {
            return _connections
                .Where(kvp => kvp.Value.Subscriptions
                    .Contains(ticker, StringComparer.OrdinalIgnoreCase))
                .Select(kvp => new KeyValuePair<string, WebSocket>(
                    kvp.Key, kvp.Value.Socket))
                .ToList();
        }
    }
}