using System;
using System.Collections.Generic;

namespace InvestmentBuilderCore
{
    /// <summary>
    /// Interface defines an investment report writer service. Implenetations allow writing
    /// a report to excel, pdf etc...
    /// </summary>
    public interface IInvestmentReportWriter
    {
        void WriteAssetReport(AssetReport report, double startOfYear, string outputPath, ProgressCounter progress);
        void WritePerformanceData(IList<IndexedRangeData> data, string outputPath, DateTime dtValuation, ProgressCounter progress);

        string GetReportFileName(DateTime dtValuation);
    }

    /// <summary>
    /// Dummy investment report writer
    /// </summary>
    public class DummyInvestmentReportWriter : IInvestmentReportWriter
    {
        public string GetReportFileName(DateTime dtValuation)
        {
            return null;
        }
        public void WriteAssetReport(AssetReport report, double startOfYear, string outputPath, ProgressCounter progress)
        {
        }

        public void WritePerformanceData(IList<IndexedRangeData> data, string outputPath, DateTime dtValuation, ProgressCounter progress)
        {
        }
    }
}
