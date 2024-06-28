namespace FunWithWebSockets.Common
{
    public record TiingoConnectionConfiguration
    {
        public string ApiKey { get; init; }

        public List<TickerItem> Tickers { get; init; }
    }
}
