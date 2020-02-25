using System;
using System.Collections.Generic;
using System.Linq;
using InvestmentBuilderCore;

namespace InvestmentReportGenerator
{
    /// <summary>
    /// Container class for INvestmentreport generators. Contains a reference to an Excel
    /// and pdf report generator. Uses the IConfigurationSettings to determine which generator 
    /// to use.
    /// </summary>
    public class InvestmentReportWriter : IInvestmentReportWriter, IDisposable
    {
        #region Public Methods

        public InvestmentReportWriter(IConfigurationSettings settings)
        {
            _settings = settings;
        }

        public void Dispose()
        {
            //if(_excelReport != null)
            //    _excelReport.Dispose();

            if(_pdfReport != null)
                _pdfReport.Dispose();
        }

        /// <summary>
        /// get the name of the report file used to generate the report
        /// if pdf available use that one otherwise use the excel one
        /// </summary>
        public string GetReportFileName(DateTime ValuationDate)
        {
            return PdfInvestmentReportWriter.GetPdfReportFile(ValuationDate);
        }

        public void WriteAssetReport(AssetReport report, double startOfYear, string outputPath, ProgressCounter progress)
        {
            var reports = _InitReports().ToList();
           foreach (var reportType in reports)
            {
                reportType.WriteAssetReport(report, startOfYear, outputPath, progress);
            }
        }

        public void WritePerformanceData(IList<IndexedRangeData> data, string path, DateTime dtValuation, ProgressCounter progress)
        {
            foreach (var reportType in _InitReports())
            {
                reportType.WritePerformanceData(data, path, dtValuation, progress);
            }
        }

        #endregion

        #region Private Methods

        private IEnumerable<IInvestmentReportWriter> _InitReports()
        {
            if (_settings.ReportFormats != null)
            {
                //if (_settings.ReportFormats.Contains("EXCEL") == true)
                //{
                //    if (_excelReport == null)
                //        _excelReport = new ExcelInvestmentReportWriter(_settings.GetTemplatePath());
                //    yield return _excelReport;
                //}
                if (_settings.ReportFormats.Contains("PDF") == true)
                {
                    if (_pdfReport == null)
                        _pdfReport = new PdfInvestmentReportWriter();
                    yield return _pdfReport;
                }
            }
        }

        #endregion

        #region Private Data Members

        //private ExcelInvestmentReportWriter _excelReport;
        private PdfInvestmentReportWriter _pdfReport;
        private readonly IConfigurationSettings _settings;

        #endregion
    }
}
