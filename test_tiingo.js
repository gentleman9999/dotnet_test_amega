var WebSocket = require("ws");
var ws = new WebSocket("wss://api.tiingo.com/fx");
var subscribe = {
  eventName: "subscribe",
  authorization: "161c3af5c43829930880f787f8f789daa49053a8",
  eventData: {
    subscriptionId: 13706,
    tickers: ["*"],
  },
};
ws.on("open", function open() {
  ws.send(JSON.stringify(subscribe));
});

ws.on("message", function (data, flags) {
  console.log(data);
});
