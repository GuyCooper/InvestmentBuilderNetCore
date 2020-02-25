using InvestmentBuilderLib;
using InvestmentBuilderService.Dtos;
using System;
using System.Collections.Generic;
using Transports;

namespace InvestmentBuilderService.Channels
{
    /// <summary>
    /// AddTransactionRequestDto DTO class.
    /// </summary>
    public class AddTransactionRequestDto : Dto
    {
        public string TransactionDate { get; set; }
        public string ParamType { get; set; }
        public string[] Parameter { get; set; }
        public double Amount { get; set; }
        public string DateRequestedFrom { get; set; }
    }

    /// <summary>
    /// Handler class for adding transactions.
    /// </summary>
    internal abstract class AddTransactionChannel : AuthorisationEndpointChannel<AddTransactionRequestDto, ChannelUpdater>
    {
        #region Public Methods

        /// <summary>
        /// Constructor. Dependencies are injected here using Unity framework.
        /// </summary>
        public AddTransactionChannel(string requestName, string responseName,
                                    ServiceAggregator aggregator)
            : base(requestName, responseName, aggregator.AccountService)
        {
            _cashTransactionManager = aggregator.CashTransactionManager;
            _cashFlowManager = aggregator.CashFlowManager;
        }

        #endregion

        #region Protected Overrides

        /// <summary>
        /// Request Handler for adding transactions.
        /// </summary>
        protected override Dto HandleEndpointRequest(UserSession userSession, AddTransactionRequestDto payload, ChannelUpdater updater)
        {
            var token = GetCurrentUserToken(userSession);
            if (payload.TransactionDate != null && payload.Amount > 0)
            {
                var transactionDate = DateTime.Parse(payload.TransactionDate);
                //parameters list may be null which is valid
                var paramList = payload.Parameter ?? new List<string> { null }.ToArray();
                foreach (var param in paramList)
                {
                    _cashTransactionManager.AddTransaction(token, userSession.ValuationDate,
                                            transactionDate,
                                            payload.ParamType,
                                            param,
                                            payload.Amount);
                }
            }
            return CashFlowModelAndParams.GenerateCashFlowModelAndParams(userSession, _cashFlowManager, payload.DateRequestedFrom);
        }

        #endregion

        #region Private Data Members

        private readonly CashAccountTransactionManager _cashTransactionManager;
        private readonly CashFlowManager _cashFlowManager;

        #endregion


    }

    /// <summary>
    /// Handler class for adding receipt transactions
    /// </summary>
    internal class AddRecieptTransactionChannel : AddTransactionChannel
    {
        /// <summary>
        /// Constructor. Define the AddReceipt Request / Reposne channels 
        /// </summary>
        public AddRecieptTransactionChannel(ServiceAggregator aggregator) 
            : base("ADD_RECEIPT_TRANSACTION_REQUEST", "ADD_RECEIPT_TRANSACTION_RESPONSE", 
                    aggregator)
        {
        }
    }

    /// <summary>
    /// Handler class for adding payment transactions.
    /// </summary>
    internal class AddPaymentTransactionChannel : AddTransactionChannel
    {
        /// <summary>
        /// Constructor. Define the AddPayment request/response channels.
        /// </summary>
        public AddPaymentTransactionChannel(ServiceAggregator aggregator)
            : base("ADD_PAYMENT_TRANSACTION_REQUEST", "ADD_PAYMENT_TRANSACTION_RESPONSE",
                    aggregator)
        {
        }
    }
}
