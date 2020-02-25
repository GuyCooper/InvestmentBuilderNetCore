using InvestmentBuilderCore;
using System;
using Transports;

namespace InvestmentBuilderService.Channels
{
    /// <summary>
    /// UndoLastTransactionResult Dto.
    /// </summary>
    internal class UndoLastTransactionResult : Dto
    {
        /// <summary>
        /// Rows affected by the UndoLastTransaction operation.
        /// </summary>
        public int RowsAffected { get; set; }
    }

    /// <summary>
    /// Handler class for UndoLastTransaction operation.
    /// </summary>
    class UndoLastTransactionChannel : AuthorisationEndpointChannel<Dto, ChannelUpdater>
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public UndoLastTransactionChannel(ServiceAggregator aggregator) : 
            base("UNDO_LAST_TRANSACTION_REQUEST", "UNDO_LAST_TRANSACTION_RESPONSE", aggregator.AccountService)
        {
            _clientData = aggregator.DataLayer.ClientData;
        }

        #endregion

        #region Protected Overrides

        /// <summary>
        /// Handle the UndoLastTransaction request.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, Dto payload, ChannelUpdater updater)
        {
            var userToken = GetCurrentUserToken(userSession);
            var lastValuationDate = _clientData.GetLatestValuationDate(userToken);
            var rowsAffected = _clientData.UndoLastTransaction(userToken, lastValuationDate ?? new DateTime());

            return new UndoLastTransactionResult
            {
                RowsAffected = rowsAffected
            };
        }

        #endregion

        #region Private Data

        private readonly IClientDataInterface _clientData;

        #endregion
    }
}
