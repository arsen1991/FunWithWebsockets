using Common.Caching;
using FunWithWebSockets.Common;
using FunWithWebSockets.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Restless.Tiingo.Data;
using Restless.Tiingo.Socket.Core;
using Restless.Tiingo.Socket.Data;

namespace FunWithWebSockets.Services
{
    public class TiingoWebsocketHostedService : BackgroundService
    {
        private readonly ILogger<TiingoWebsocketHostedService> _logger;
        private readonly TiingoConnectionConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDistributedCache _cache;
        private readonly TiingoIntegrationService _tiingoIntegrationService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
        };

        public TiingoWebsocketHostedService(ILogger<TiingoWebsocketHostedService> logger, IOptions<TiingoConnectionConfiguration> configuration, IServiceProvider serviceProvider, IDistributedCache cache, TiingoIntegrationService tiingoIntegrationService, IHubContext<NotificationHub> hubContext)
        {
            _logger = logger;
            _configuration = configuration.Value;
            _serviceProvider = serviceProvider;
            _cache = cache;
            _tiingoIntegrationService = tiingoIntegrationService;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Initial data load.
            await _tiingoIntegrationService.GetCurrentPriceAsync(_configuration.Tickers.Select(x => new TickerPair(x.From, x.To)).ToList());
#pragma warning disable CA2000 // Dispose objects before losing scope
            var client = Restless.Tiingo.Socket.Client.TiingoClient.Create(_configuration.ApiKey);
#pragma warning restore CA2000 // Dispose objects before losing scope

            int subscriptionId = 0;

            await client.Forex.GetAsync(
                new ForexParameters()
                {
                    MessageType = MessageType.All,
                    Threshold = ForexThreshold.LastQuote,
                    Tickers = _configuration.Tickers.Select(x => x.From + x.To).ToArray(),
                }, result =>
                {
                    if (result is SubscriptionMessage sub)
                    {
                        subscriptionId = sub.Data.SubscriptionId;
                        _logger.LogInformation($"Subscribed to {sub.Data.SubscriptionId}");
                    }
                    else if (result is ForexQuoteMessage quote)
                    {
                        var cacheItem = _cache.GetAsync<ForexTopDataPoint>($"ForexTopDataPoint-{quote.Ticker}", stoppingToken).Result;
                        var topDataPoint = new ForexTopDataPoint()
                        {
                            Ticker = quote.Ticker,
                            Timestamp = quote.Timestamp,
                            BidPrice = quote.BidPrice,
                            MidPrice = quote.MidPrice,
                            AskPrice = quote.AskPrice,
                            AskSize = quote.AskSize,
                            BidSize = quote.BidSize,
                        };

                        var message = $"Forex Quote: {quote.Ticker} {quote.Timestamp} {quote.BidPrice} {quote.MidPrice}";

                        bool isChanged = false;
                        if (cacheItem != null)
                        {
                            isChanged = cacheItem.BidPrice != topDataPoint.BidPrice || cacheItem.MidPrice != topDataPoint.MidPrice;
                        }
                        else
                        {
                            isChanged = true;
                        }

                        // Only update the cache and send a message if the data has changed.
                        if (isChanged)
                        {
                            _logger.LogInformation("{message} (Changed)", message);
                            _cache.SetObjectAsync($"ForexTopDataPoint-{quote.Ticker}", topDataPoint, _cacheOptions, stoppingToken).Wait();

                            // Send the message to the SignalR hub.
                            _hubContext.Clients.All.SendAsync("ReceiveMessage", message).Wait();
                        }
                        else
                        {
                            _logger.LogInformation(message);
                        }
                    }
                    else if (result is SocketClosedMessage)
                    {
                        _logger.LogInformation("Socket closed. SubscriptionId: {subscriptionId}", subscriptionId);
                    }
                });
        }
    }
}
