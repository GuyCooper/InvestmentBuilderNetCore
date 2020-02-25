using System;
using System.Collections.Generic;
using System.Linq;
using InvestmentBuilderCore;
using InvestmentBuilderLib;
using Transports;

namespace InvestmentBuilderService.Channels
{
    /// <summary>
    /// request dto for Transaction parameters.
    /// </summary>
    internal class TransactionParametersRequestDto : Dto
    {
        public string ParameterType { get; set; }
    }

    /// <summary>
    /// Response dto for Transaction parameters.
    /// </summary>
    internal class TransactionParametersResponseDto : Dto
    {
        public IEnumerable<string> Parameters { get; set; }
    }

    /// <summary>
    /// handler class for retreiving a list of transaction parameters
    /// </summary>
    internal class GetTransactionParametersChannel : AuthorisationEndpointChannel<TransactionParametersRequestDto, ChannelUpdater>
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public GetTransactionParametersChannel(ServiceAggregator aggregator)
            : base("GET_TRANSACTION_PARAMETERS_REQUEST", "GET_TRANSACTION_PARAMETERS_RESPONSE", aggregator.AccountService)
        {
            _recordData = aggregator.DataLayer.InvestmentRecordData;
            _builder = aggregator.Builder;
        }

        #endregion

        #region Protected Overrides

        /// <summary>
        /// Handle GetTransaction parameters request.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, TransactionParametersRequestDto payload, ChannelUpdater update)
        {
            var token = GetCurrentUserToken(userSession);
            var latestRecordDate = _recordData.GetLatestRecordInvestmentValuationDate(token) ?? DateTime.Today;

            var parameters = _builder.GetParametersForTransactionType(token, latestRecordDate, payload.ParameterType).ToList();
            if (parameters.Count == 0)
            {
                parameters.Add(payload.ParameterType);
            }
            else
            {
                parameters.Add("ALL");
            }

            return new TransactionParametersResponseDto
            {
                Parameters = parameters
            };

        }

        #endregion

        #region Private Data

        private readonly IInvestmentRecordInterface _recordData;
        private readonly InvestmentBuilder _builder;

        #endregion
    }
}
