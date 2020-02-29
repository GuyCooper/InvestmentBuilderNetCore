using MarketDataServices;
using System.Threading.Tasks;
using Transports;

namespace InvestmentBuilderService.Channels
{
    /// <summary>
    /// Dto for getting a price.
    /// </summary>
    class GetPriceDto : Dto
    {
        public string Symbol { get; set; }
        public string Exchange { get; set; }
        public string Source { get; set; }
    }

    /// <summary>
    /// Dto for returning the price.
    /// </summary>
    class PriceResponseDto : Dto
    {
        public double Price { get; set; }
    }

    /// <summary>
    /// Handler class for getting a market data price
    /// </summary>
    class GetPriceChannel : AuthorisationEndpointChannel<GetPriceDto, ChannelUpdater>
    {
        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public GetPriceChannel(ServiceAggregator aggregator)
            : base("GET_PRICE_REQUEST", "GET_PRICE_RESPONSE", aggregator.AccountService)
        {
            _marketDataSource = aggregator.MarketDataSource;
        }

        #endregion

        #region Protected Overrides

        /// <summary>
        /// Handle the get price request.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, GetPriceDto payload, ChannelUpdater updater)
        {
            var task = _marketDataSource.RequestPrice(payload.Symbol, payload.Exchange, payload.Source).ContinueWith< PriceResponseDto >(ts =>
            {
                var result = new PriceResponseDto();
                if (ts.Result == null)
                {
                    result.IsError = true;
                    result.Error = "Fail. Invalid Symbol!";
                }
                else
                {
                    result.Price = ts.Result.Price;
                }
                return result;
            });

            //this call blocks until the price has been equested/
            return task.Result;
        }

        #endregion

        #region Private Data

        private readonly IMarketDataSource _marketDataSource;

        #endregion
    }
}
