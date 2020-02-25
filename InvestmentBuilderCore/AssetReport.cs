using System;
using System.Collections.Generic;

namespace InvestmentBuilderCore
{
    /// <summary>
    /// AssetReport Class. A full account valuation on a specified date.
    /// </summary>
    public class AssetReport
    {
        public AccountIdentifier AccountName { get;  set; }
        public string ReportingCurrency { get; set; }
        public DateTime ValuationDate { get; set; }
        public IEnumerable<CompanyData> Assets { get; set; }

        public double TotalAssetValue { get; set; }
        public double BankBalance { get; set; }
        public double TotalAssets { get; set; }
        public double TotalLiabilities { get; set; }
        public double NetAssets { get; set; }
        public double IssuedUnits { get; set; }
        public double TotalDividends { get; set; }
        public double ValuePerUnit { get; set; }
        public double MonthlyPnL { get; set; }
        public double YearToDatePerformance { get; set; }
        public List<Redemption> Redemptions { get; set; }
    }
}
