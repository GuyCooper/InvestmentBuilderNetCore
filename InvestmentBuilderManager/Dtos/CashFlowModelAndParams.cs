using System.Collections.Generic;
using Transports;

namespace InvestmentBuilderManager.Dtos
{
    internal class CashFlowModelAndParams : Dto
    {
        public IEnumerable<CashFlowModel> CashFlows { get; set; }
        public IEnumerable<string> ReceiptParamTypes { get; set; }
        public IEnumerable<string> PaymentParamTypes { get; set; }

        public static CashFlowModelAndParams GenerateCashFlowModelAndParams(UserSession userSession, CashFlowManager cashFlowManager, string dateFrom)
        {
            return new CashFlowModelAndParams
            {
                CashFlows = cashFlowManager.GetCashFlowModel(userSession, dateFrom),
                ReceiptParamTypes = cashFlowManager.GetReceiptParamTypes(),
                PaymentParamTypes = cashFlowManager.GetPaymentParamTypes()
            };
        }
    }
}
