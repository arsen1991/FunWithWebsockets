using Common.Caching;
using FunWithWebSockets.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Restless.Tiingo.Data;

namespace FunWithWebSockets.Services
{
    public class TiingoIntegrationService
    {
        private readonly ILogger<TiingoIntegrationService> _logger;
        private readonly TiingoConnectionConfiguration _tiingoConnectionConfiguration;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
        };

        public TiingoIntegrationService(ILogger<TiingoIntegrationService> logger, IOptions<TiingoConnectionConfiguration> tiingoConnectionConfiguration, IDistributedCache cache)
        {
            _logger = logger;
            _tiingoConnectionConfiguration = tiingoConnectionConfiguration.Value;
            _cache = cache;
        }

        public List<TickerItem> GetAvailableTickers()
        {
            return _tiingoConnectionConfiguration.Tickers;
        }

        public async Task<ForexTopDataPoint> GetCurrentPriceAsync(string from, string to)
        {
            if (_tiingoConnectionConfiguration.Tickers.FirstOrDefault(x => x.From == from && x.To == to) is null)
            {
                throw new Exception("Invalid ticker pair");
            }

            var forexTopDataPoint = await _cache.GetAsync<ForexTopDataPoint>($"ForexTopDataPoint-{from}{to}");
            if (forexTopDataPoint is not null)
            {
                return forexTopDataPoint;
            }

            using var restClient = Restless.Tiingo.Client.TiingoClient.Create(_tiingoConnectionConfiguration.ApiKey);
            var data = await restClient.Forex.GetTopOfBookAsync(new Restless.Tiingo.Core.ForexParameters()
            {
                Tickers =
                [
                    new TickerPair(from, to),
                ],
            });

            if (data.Count == 0)
            {
                _logger.LogError("No data found for {From} {To}", from, to);
                throw new Exception("No data found");
            }

            await _cache.SetObjectAsync($"ForexTopDataPoint-{from}{to}", data[0], _cacheOptions);

            return data[0];
        }

        public async Task<ForexTopDataPointCollection> GetCurrentPriceAsync(List<TickerPair> tickerPairs)
        {
            var forexTopDataPoints = new ForexTopDataPointCollection();
            foreach (var tickerPair in tickerPairs)
            {
                forexTopDataPoints.Add(await GetCurrentPriceAsync(tickerPair.FromSymbol, tickerPair.ToSymbol));
            }

            return forexTopDataPoints;
        }
    }
}
