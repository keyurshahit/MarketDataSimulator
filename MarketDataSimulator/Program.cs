using MarketDataSimulator.Middleware.WebSockets;
using MarketDataSimulator.Simulator;
using Microsoft.AspNetCore.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(logging =>
                        logging.AddSimpleConsole(options =>
                        {
                            options.SingleLine = true;
                            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
                            options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
                        }));

var configuration = builder.Configuration;

// Registering MarketSimulator with parameters from configuration
//
builder.Services.AddSingleton<IMarketSimulator>(provider =>
{
    var maxRows = configuration.GetValue<int>("MaxRows");
    var refreshRateMs = configuration.GetValue<int>("RefreshRateMs");
    return new MarketSimulator(maxRows, refreshRateMs); // this will initiate the simulator loop
});

var app = builder.Build();

app.UseWebSockets();

app.UseMiddleware<WebSocketHandler>();

await app.RunAsync();