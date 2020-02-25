using System;
using InvestmentBuilderCore;
using Transports;

namespace InvestmentBuilderManager.Dtos
{
    /// <summary>
    /// Class identifies an account including its details
    /// </summary>
    internal class InvestmentSummaryModel : Dto
    {
        public AccountIdentifier AccountName { get; set; }
        public string ReportingCurrency { get; set; }
        public DateTime ValuationDate { get; set; }
        public string TotalAssetValue { get; set; }
        public string BankBalance { get; set; }
        public string TotalAssets { get; set; }
        public string NetAssets { get; set; }
        public string ValuePerUnit { get; set; }
        public string MonthlyPnL { get; set; }
    }
}
