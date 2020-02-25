using InvestmentBuilderCore;
using System;
using Transports;

namespace InvestmentBuilderService.Channels
{
    /// <summary>
    /// Cash Transaction Dto
    /// </summary>
    internal class TransactionDto : Dto
    {
        public bool Success { get; set; }
        public string InvestmentName { get; set; }
        public string TransactionType { get; set; }
        public double Quantity { get; set; }
        public double Amount { get; set; }
    }

    /// <summary>
    /// Returns the last transaction made on this users account.
    /// </summary>
    class GetLastTransactionChannel : AuthorisationEndpointChannel<Dto, ChannelUpdater>
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public GetLastTransactionChannel(ServiceAggregator aggregator) : 
            base("GET_LAST_TRANSACTION_REQUEST", "GET_LAST_TRANSACTION_RESPONSE", aggregator.AccountService)
        {
            _clientData = aggregator.DataLayer.ClientData;
        }

        #endregion

        #region Protected Overrides

        /// <summary>
        /// Handle the GetLastTransaction request.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, Dto payload, ChannelUpdater updater)
        {
            var userToken = GetCurrentUserToken(userSession);
            var lastValuationDate = _clientData.GetLatestValuationDate(userToken);
            var transaction = _clientData.GetLastTransaction(userToken, lastValuationDate ?? new DateTime());

            if (transaction != null)
            {
                return new TransactionDto
                {
                    Success = true,
                    InvestmentName = transaction.InvestmentName,
                    TransactionType = transaction.TransactionType.ToString(),
                    Quantity = transaction.Quantity,
                    Amount = transaction.Amount
                };
            }

            return new TransactionDto
            {
                Success = false
            };
        }

        #endregion

        #region Private Data

        private readonly IClientDataInterface _clientData;

        #endregion

    }
}
