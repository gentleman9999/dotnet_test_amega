using System.Net.WebSockets;
using FinancialInstrumentAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddSingleton<TiingoFxWebSocketClient>();
builder.Services.AddHostedService<PriceUpdateBackgroundService>(); // Register the background service
string apiKey = builder.Configuration["Tiingo:ApiKey"];
builder.Services.AddSingleton<TiingoFxHttp>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseWebSockets();
// WebSocket endpoint for clients
app.Use(async (context, next) =>
{
    if ((context.Request.Path.StartsWithSegments("/ws/fx") || context.Request.Path.StartsWithSegments("/ws/crypto") || context.Request.Path.StartsWithSegments("/ws"))
        && context.WebSockets.IsWebSocketRequest)
    {
        // 1. Read and parse the "tickers" query parameter
        var tickersParam = context.Request.Query["tickers"].ToString() ?? "";
        var subscriptions = tickersParam
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLower())
            .ToList();

        // 2. Accept the WebSocket
        WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();

        // 3. Add connection along with its subscriptions
        var manager = context.RequestServices
            .GetRequiredService<WebSocketConnectionManager>();
        var connectionId = manager.AddConnection(socket, subscriptions);

        // 4. Keep the socket open until client closes
        var buffer = new byte[4 * 1024];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await manager.RemoveConnectionAsync(connectionId);
                    break;
                }
            }
        }
        finally
        {
            await manager.RemoveConnectionAsync(connectionId);
        }
    }
    else
    {
        await next(); // Pass to the next middleware (e.g., API controllers)
    }
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
