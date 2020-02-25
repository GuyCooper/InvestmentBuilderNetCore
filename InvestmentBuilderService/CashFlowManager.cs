using System;
using System.Collections.Generic;
using InvestmentBuilderCore;
using InvestmentBuilderLib;
using Transports;

namespace InvestmentBuilderService
{
    internal class CashFlowModel
    {
        public IEnumerable<ReceiptTransaction> Receipts { get; set; }
        public IEnumerable<PaymentTransaction> Payments { get; set; }
        public string ReceiptsTotal { get; set; }
        public string PaymentsTotal { get; set; }
        public string ValuationDate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanBuild { get; set; }
    }

    /// <summary>
    /// class is a wrapper for the CashAccountTransactionManager. prinipally generates the a list
    /// of CashFlowModel objects that are used by the cachflow web page
    /// </summary>
    internal class CashFlowManager
    {
        private AccountService _accountService;
        private IClientDataInterface _clientData;
        private CashAccountTransactionManager _cashTransactionManager;

        public CashFlowManager(AccountService accountService, IDataLayer dataLayer,
            CashAccountTransactionManager cashTransactionManager)
        {
            _accountService = accountService;
            _clientData = dataLayer.ClientData;
            _cashTransactionManager = cashTransactionManager;
        }

        /// <summary>
        /// returns a list of cachflow models from the earliest date requested
        /// </summary>
        public IEnumerable<CashFlowModel> GetCashFlowModel(UserSession userSession, string sDateFrom)
        {
            var token = _accountService.GetUserAccountToken(userSession, null);
            var dtDateEarliest = string.IsNullOrEmpty(sDateFrom) ? userSession.ValuationDate : DateTime.Parse(sDateFrom);
            var dtDateLatest = userSession.ValuationDate;
            var dtDateNext = dtDateLatest;

            var finished = false;
            while (!finished)
            {
                var dtDateFrom = _clientData.GetPreviousAccountValuationDate(token, dtDateNext);

                double dReceiptTotal, dPaymentTotal;
                var cashFlowModel = new CashFlowModel();
                cashFlowModel.Receipts = _cashTransactionManager.GetReceiptTransactions(token, dtDateNext, dtDateFrom, out dReceiptTotal);
                cashFlowModel.Payments = _cashTransactionManager.GetPaymentTransactions(token, dtDateNext, out dPaymentTotal);
                cashFlowModel.ReceiptsTotal = dReceiptTotal.ToString("#0.00");
                cashFlowModel.PaymentsTotal = dPaymentTotal.ToString("#0.00");
                cashFlowModel.ValuationDate = dtDateNext.ToString("yyyy-MM-dd"); //ISO 8601

                cashFlowModel.CanEdit = dtDateNext == dtDateLatest;
                cashFlowModel.CanBuild = cashFlowModel.CanEdit && dReceiptTotal > 0 && cashFlowModel.ReceiptsTotal == cashFlowModel.PaymentsTotal;

                if (dtDateFrom.HasValue == false)
                {
                    finished = true;
                }
                else
                {
                    dtDateNext = dtDateFrom.Value;
                    if (dtDateFrom <= dtDateEarliest)
                    {
                        finished = true;
                    }
                }

                yield return cashFlowModel;
            }
        }

        public IEnumerable<string> GetReceiptParamTypes()
        {
            return _cashTransactionManager.GetTransactionTypes(_cashTransactionManager.ReceiptMnemomic);
        }

        public IEnumerable<string> GetPaymentParamTypes()
        {
            return _cashTransactionManager.GetTransactionTypes(_cashTransactionManager.PaymentMnemomic);
        }
    }
}
