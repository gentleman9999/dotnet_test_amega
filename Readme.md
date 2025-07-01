## Overview
This project provides a simple .NET Web API for fetching FX rates and cryptocurrency prices via the Tiingo REST API. It also includes a WebSocket service for streaming real-time FX and crypto price updates. The core HTTP client implementation lives in `Services/TiingoFxHttp.cs`.

## Prerequisites
- .NET 6.0 SDK or later
- A valid Tiingo API key (sign up at https://api.tiingo.com/)

## Configuration
1. Clone the repository:
   ```bash
   git clone https://github.com/gentleman9999/dotnet_test_amega.git
   ```
2. Navigate into the project folder:
   ```bash
   cd dotnet_test_amega
   ```
3. Create an `appsettings.json` file in the project root (if not already present) and add your Tiingo API key:
   ```json
   {
     "Tiingo": {
       "ApiKey": "YOUR_TIINGO_API_KEY_HERE"
     }
   }
   ```

## Build and Run
Restore dependencies, build the solution, and start the Web API:

```bash
dotnet restore
```
```bash
dotnet build
```
```bash
dotnet run --project FinancialInstrumentAPI
```

By default, the API will listen on `http://localhost:5000` (or `https://localhost:5001`), and WebSocket endpoints on the same host/port.

## Usage Examples

### Fetch FX Top-of-Book Rate
Request:
```bash
curl "http://localhost:5000/api/Financial/instrument_item/eurusd"
```
Response:
```json
[
  {
    "ticker": "EURUSD",
    "bid": 1.1023,
    "ask": 1.1025,
    "timestamp": "2023-07-01T12:34:56Z"
  }
]
```

### Fetch Cryptocurrency Top-of-Book Price
Request:
```bash
curl "http://localhost:5000/api/Financial/instrument_btc/btcusd"
```
Response:
```json
[
  {
    "ticker": "BTCUSD",
    "bid": 30000.12,
    "ask": 30010.45,
    "timestamp": "2023-07-01T12:35:00Z"
  }
]
```

## WebSocket Service
You can subscribe to live price updates via WebSocket:

- FX Rates:  
  Connect to `ws://localhost:5000/ws/fx?tickers=eurusd,gbpjpy`  
- Crypto Prices:  
  Connect to `ws://localhost:5000/ws/crypto?tickers=btcusd,ethusd`  

Messages are pushed as JSON objects with the following structure:
```json
{
  "ticker": "EURUSD",
  "bid": 1.1024,
  "ask": 1.1026,
  "timestamp": "2023-07-01T12:36:10Z"
}
```

## WebSocket Test Page
A simple test page `test.html` is provided under `wwwroot` (or project root).  
1. Start the API as described above.  
2. Open your browser and navigate to:
   ```
   http://localhost:5000/test.html
   ```

## Extending the Service
- All HTTP calls are implemented in `Services/TiingoFxHttp.cs`.
- To add new REST endpoints, inject `TiingoFxHttp` into your controller and call:
  - `MainRestQuery(string ticker, string parameters)`
  - `MainRestQueryTop(string fxTicker)`
  - `MainRestCrypToQueryTop(string cryptoTicker)`
- To add or modify WebSocket behavior, update the WebSocket handlers in `Controllers/WebSocketController.cs` (or wherever you manage WS endpoints).

