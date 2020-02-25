using System.Collections.Generic;
using System.Linq;
using InvestmentBuilderCore;
using InvestmentBuilderLib;
using Transports;

namespace InvestmentBuilderManager.Channels
{
    /// <summary>
    /// Portfolio response dto.
    /// </summary>
    class PortfolioResponseDto : Dto
    {
        public IEnumerable<CompanyData> Portfolio { get; private set; }

        public PortfolioResponseDto(IEnumerable<CompanyData> portfolio)
        {
            Portfolio = portfolio;
        }
    }

    /// <summary>
    /// handler class for retreiving the portfolio
    /// </summary>
    class GetPortfolioChannel : AuthorisationEndpointChannel<Dto, ChannelUpdater>
    {
        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public GetPortfolioChannel(ServiceAggregator aggregator) :
            base("GET_PORTFOLIO_REQUEST", "GET_PORTFOLIO_RESPONSE", aggregator.AccountService)
        {
            _builder = aggregator.Builder;
        }

        #endregion

        #region Protected Overrides

        /// <summary>
        /// Handle GetPortfolio request
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, Dto payload, ChannelUpdater update)
        {
            var userToken = GetCurrentUserToken(userSession);
            return new PortfolioResponseDto(_builder.GetCurrentInvestments(userToken, userSession.UserPrices).OrderBy(x => x.Name).ToList());
        }

        #endregion

        #region Private Data

        private readonly  InvestmentBuilder _builder;

        #endregion
    }
}
