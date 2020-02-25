using System;
using System.Collections.Generic;

namespace InvestmentBuilderCore
{
    public interface IInvestmentReportWriter
    {
        void WriteAssetReport(AssetReport report, double startOfYear, string outputPath, ProgressCounter progress);
        void WritePerformanceData(IList<IndexedRangeData> data, string outputPath, DateTime dtValuation, ProgressCounter progress);

        string GetReportFileName(DateTime dtValuation);
    }
}
