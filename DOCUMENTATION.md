# Financial Instrument API - Complete Documentation

## Table of Contents
1. [Overview](#overview)
2. [Setup Guide](#setup-guide)
3. [API Reference](#api-reference)
4. [WebSocket Documentation](#websocket-documentation)
5. [Debug & Troubleshooting](#debug--troubleshooting)
6. [Architecture](#architecture)
7. [Configuration](#configuration)

## Overview

The Financial Instrument API is a .NET 6 Web API that provides real-time access to financial market data through Tiingo's API. It offers both REST endpoints for on-demand data retrieval and WebSocket connections for real-time streaming of FX rates and cryptocurrency prices.

### Key Features
- **REST API**: Fetch current FX rates and cryptocurrency prices
- **Real-time WebSocket**: Stream live price updates for FX and crypto pairs
- **Background Service**: Continuous data streaming with automatic reconnection
- **High-performance Broadcasting**: Efficient message distribution to multiple WebSocket clients
- **Comprehensive Error Handling**: Robust error management and logging

### Supported Instruments
- **FX Pairs**: EUR/USD, GBP/USD, JPY/USD, and more
- **Cryptocurrencies**: BTC/USD, ETH/USD, and other crypto pairs

## Setup Guide

### Prerequisites
- .NET 6.0 SDK or later
- A valid Tiingo API key (sign up at [https://api.tiingo.com/](https://api.tiingo.com/))
- Visual Studio 2022, VS Code, or any .NET-compatible IDE

### Installation Steps

1. **Clone the Repository**
   ```bash
   git clone https://github.com/gentleman9999/dotnet_test_amega.git
   cd dotnet_test_amega
   ```

2. **Configure API Key**
   
   Edit `appsettings.json` and replace the API key with your own:
   ```json
   {
     "Tiingo": {
       "ApiKey": "YOUR_TIINGO_API_KEY_HERE",
       "WebSocketUrl": "wss://api.tiingo.com/fx"
     }
   }
   ```

3. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

4. **Build the Project**
   ```bash
   dotnet build
   ```

5. **Run the Application**
   ```bash
   dotnet run
   ```

   The API will be available at:
   - HTTP: `http://localhost:5000`
   - HTTPS: `https://localhost:5001`
   - Swagger UI: `https://localhost:5001/swagger`

### Environment Configuration

For different environments, you can use:
- `appsettings.Development.json` for development
- `appsettings.Production.json` for production
- Environment variables for sensitive data

## API Reference

### Base URL
```
http://localhost:5000/api/Financial
```

### Endpoints

#### 1. Get All FX Instruments
Retrieves top-of-book data for multiple FX pairs.

**Endpoint:** `GET /api/Financial/instrument_all`

**Response:**
```json
[
  {
    "ticker": "EURUSD",
    "quoteTimestamp": "2023-07-01T12:34:56Z",
    "bidPrice": 1.1023,
    "bidSize": 1000000,
    "askPrice": 1.1025,
    "askSize": 1000000,
    "midPrice": 1.1024
  },
  {
    "ticker": "GBPUSD",
    "quoteTimestamp": "2023-07-01T12:34:56Z",
    "bidPrice": 1.2850,
    "bidSize": 1000000,
    "askPrice": 1.2852,
    "askSize": 1000000,
    "midPrice": 1.2851
  }
]
```

**cURL Example:**
```bash
curl "http://localhost:5000/api/Financial/instrument_all"
```

#### 2. Get Specific FX Instrument
Retrieves top-of-book data for a specific FX pair.

**Endpoint:** `GET /api/Financial/instrument_item/{ticker}`

**Parameters:**
- `ticker` (path): FX pair symbol (e.g., "eurusd", "gbpjpy")

**Response:**
```json
[
  {
    "ticker": "EURUSD",
    "quoteTimestamp": "2023-07-01T12:34:56Z",
    "bidPrice": 1.1023,
    "bidSize": 1000000,
    "askPrice": 1.1025,
    "askSize": 1000000,
    "midPrice": 1.1024
  }
]
```

**cURL Example:**
```bash
curl "http://localhost:5000/api/Financial/instrument_item/eurusd"
```

#### 3. Get Cryptocurrency Data
Retrieves top-of-book data for Bitcoin.

**Endpoint:** `GET /api/Financial/instrument_btc`

**Response:**
```json
[
  {
    "ticker": "BTCUSD",
    "baseCurrency": "BTC",
    "quoteCurrency": "USD",
    "topOfBookData": [
      {
        "quoteTimestamp": "2023-07-01T12:35:00Z",
        "lastSaleTimestamp": "2023-07-01T12:35:00Z",
        "bidSize": 0.5,
        "bidPrice": 30000.12,
        "askSize": 0.3,
        "askPrice": 30010.45,
        "lastSize": 0.1,
        "lastSizeNotional": 3005.23,
        "lastPrice": 30052.30,
        "bidExchange": "COINBASE",
        "askExchange": "BINANCE",
        "lastExchange": "COINBASE"
      }
    ]
  }
]
```

**cURL Example:**
```bash
curl "http://localhost:5000/api/Financial/instrument_btc"
```

### Error Responses

**400 Bad Request:**
```json
{
  "message": "Failed to retrieve data from the API."
}
```

**500 Internal Server Error:**
```json
{
  "message": "An error occurred while processing your request."
}
```

## WebSocket Documentation

### Overview
The WebSocket service provides real-time streaming of financial data. It supports both FX and cryptocurrency data streams with subscription-based filtering.

### Connection Endpoints

#### FX WebSocket
**URL:** `ws://localhost:5000/ws/fx?tickers={ticker1},{ticker2},...`

**Parameters:**
- `tickers` (query): Comma-separated list of FX pairs (e.g., "eurusd,gbpjpy")

**Example:**
```javascript
const ws = new WebSocket("ws://localhost:5000/ws/fx?tickers=eurusd,gbpusd");
```

#### Crypto WebSocket
**URL:** `ws://localhost:5000/ws/crypto?tickers={ticker1},{ticker2},...`

**Parameters:**
- `tickers` (query): Comma-separated list of crypto pairs (e.g., "btcusd,ethusd")

**Example:**
```javascript
const ws = new WebSocket("ws://localhost:5000/ws/crypto?tickers=btcusd,ethusd");
```

### Message Format

#### FX Price Update
```json
[
  "Q",
  "EURUSD",
  "2023-07-01T12:36:10.472Z",
  1000000,
  1.1024,
  1.1024,
  1000000,
  1.1026
]
```

**Fields:**
- `[0]`: Message type ("Q" for Quote, "A" for Aggregate)
- `[1]`: Ticker symbol
- `[2]`: Timestamp
- `[3]`: Bid size
- `[4]`: Bid price
- `[5]`: Mid price
- `[6]`: Ask size
- `[7]`: Ask price

#### Crypto Price Update
```json
[
  "Q",
  "BTCUSD",
  "2023-07-01T12:36:10.472Z",
  0.5,
  30000.12,
  30005.28,
  0.3,
  30010.45,
  0.1,
  30052.30
]
```

**Fields:**
- `[0]`: Message type
- `[1]`: Ticker symbol
- `[2]`: Timestamp
- `[3]`: Bid size
- `[4]`: Bid price
- `[5]`: Mid price
- `[6]`: Ask size
- `[7]`: Ask price
- `[8]`: Last size
- `[9]`: Last price

### JavaScript Client Example

```javascript
const ws = new WebSocket("ws://localhost:5000/ws/fx?tickers=eurusd,gbpusd");

ws.onopen = function(event) {
    console.log("Connected to WebSocket");
};

ws.onmessage = function(event) {
    const data = JSON.parse(event.data);
    console.log("Price update:", data);
    
    // Process the price update
    const ticker = data[1];
    const bidPrice = data[4];
    const askPrice = data[7];
    
    console.log(`${ticker}: Bid ${bidPrice}, Ask ${askPrice}`);
};

ws.onclose = function(event) {
    console.log("WebSocket connection closed");
};

ws.onerror = function(error) {
    console.error("WebSocket error:", error);
};
```

### WebSocket Test Page
A test page is provided at `test.html` for testing WebSocket connections:

1. Start the API server
2. Open `http://localhost:5000/test.html` in your browser
3. The page will automatically connect and display real-time price updates

## Debug & Troubleshooting

### Logging Configuration

The application uses structured logging with different levels:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "FinancialInstrumentAPI.Services": "Debug"
    }
  }
}
```

### Common Issues and Solutions

#### 1. API Key Issues
**Problem:** "Tiingo:ApiKey not configured" error
**Solution:** 
- Verify your API key in `appsettings.json`
- Ensure the key is valid and has proper permissions
- Check Tiingo account status

#### 2. WebSocket Connection Failures
**Problem:** WebSocket connections fail to establish
**Solution:**
- Verify the API server is running
- Check firewall settings
- Ensure correct WebSocket URL format
- Check browser console for CORS issues

#### 3. No Real-time Data
**Problem:** WebSocket connected but no price updates received
**Solution:**
- Check Tiingo WebSocket connection status in logs
- Verify subscription to correct tickers
- Check Tiingo service status
- Review background service logs

#### 4. High Memory Usage
**Problem:** Application consuming excessive memory
**Solution:**
- Monitor WebSocket connection count
- Check for memory leaks in connection management
- Review background service performance

### Debug Commands

#### Check Service Status
```bash
# Check if the service is running
curl http://localhost:5000/api/Financial/instrument_all

# Check WebSocket endpoint
curl -i -N -H "Connection: Upgrade" -H "Upgrade: websocket" \
     -H "Sec-WebSocket-Version: 13" \
     -H "Sec-WebSocket-Key: x3JJHMbDL1EzLkh9GBhXDw==" \
     http://localhost:5000/ws/fx?tickers=eurusd
```

#### Monitor Logs
```bash
# View application logs
dotnet run --environment Development

# Check specific log levels
dotnet run --environment Development --verbosity detailed
```

### Performance Monitoring

#### Key Metrics to Monitor
- WebSocket connection count
- Message broadcast latency
- Memory usage
- CPU utilization
- Network I/O

#### Monitoring Tools
- Application Insights (if configured)
- Built-in logging
- Performance counters
- Custom metrics in background service

## Architecture

### System Components

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Web Clients   │    │   REST API       │    │   Tiingo API    │
│                 │    │   Controllers    │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                       │                       │
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   WebSocket     │    │   TiingoFxHttp   │    │   REST Endpoints│
│   Manager       │    │   Service        │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                       │
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌──────────────────┐
│   Background    │    │   WebSocket      │
│   Service       │    │   Client         │
└─────────────────┘    └──────────────────┘
```

### Data Flow

1. **REST API Requests:**
   - Client → Controller → TiingoFxHttp → Tiingo API → Response

2. **WebSocket Streaming:**
   - Tiingo API → WebSocket Client → Background Service → Connection Manager → Clients

3. **Real-time Updates:**
   - Background service continuously receives data
   - Filters and broadcasts to subscribed clients
   - Handles connection management and error recovery

### Key Services

#### TiingoFxHttp
- Handles REST API calls to Tiingo
- Manages authentication and error handling
- Provides structured data responses

#### WebSocketConnectionManager
- Manages active WebSocket connections
- Handles subscription filtering
- Provides connection lifecycle management

#### PriceUpdateBackgroundService
- Coordinates real-time data streaming
- Manages WebSocket client connections
- Handles message broadcasting and error recovery

#### TiingoFxWebSocketClient
- Establishes WebSocket connections to Tiingo
- Handles message parsing and event raising
- Manages connection state and reconnection

## Configuration

### Application Settings

#### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Tiingo": {
    "ApiKey": "YOUR_API_KEY",
    "WebSocketUrl": "wss://api.tiingo.com/fx"
  },
  "AllowedHosts": "*"
}
```

#### Environment Variables
```bash
# Set API key via environment variable
export Tiingo__ApiKey="your_api_key_here"

# Set WebSocket URL
export Tiingo__WebSocketUrl="wss://api.tiingo.com/fx"
```

### Service Configuration

#### Dependency Injection
```csharp
// Program.cs
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddSingleton<TiingoFxWebSocketClient>();
builder.Services.AddHostedService<PriceUpdateBackgroundService>();
builder.Services.AddSingleton<TiingoFxHttp>();
```

#### WebSocket Configuration
```csharp
// WebSocket middleware configuration
app.UseWebSockets();
```

### Performance Tuning

#### Background Service Settings
```csharp
// PriceUpdateBackgroundService.cs
private const int MAX_CONCURRENT_SENDS = 100;
private const int BATCH_SIZE = 50;
```

#### Connection Limits
- Default WebSocket buffer size: 4KB
- Maximum concurrent sends: 100
- Batch processing size: 50 connections

### Security Considerations

1. **API Key Protection:**
   - Store API keys in secure configuration
   - Use environment variables in production
   - Never commit API keys to source control

2. **WebSocket Security:**
   - Implement authentication if needed
   - Validate ticker subscriptions
   - Rate limit connections

3. **Input Validation:**
   - Validate ticker symbols
   - Sanitize WebSocket messages
   - Implement proper error handling

---

## Support

For issues and questions:
1. Check the troubleshooting section above
2. Review application logs
3. Verify Tiingo API status
4. Test with the provided test page

## Version Information

- **Framework:** .NET 6.0
- **API Version:** 1.0
- **Tiingo API:** Latest
- **Last Updated:** 2024 