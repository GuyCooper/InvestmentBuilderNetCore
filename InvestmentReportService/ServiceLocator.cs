using InvestmentBuilderCore;
using InvestmentBuilderLib;
using InvestmentReportGenerator;
using MarketDataServices;
using PerformanceBuilderLib;
using SQLServerDataLayer;

namespace InvestmentReportService
{
    /// <summary>
    /// Service Locator for InvestmentReport Service
    /// </summary>
    public static class ServiceLocator
    {
        public static void RegisterServices()
        {
            ContainerManager.RegisterType(typeof(IAuthorizationManager), typeof(SQLAuthorizationManager), true);
            ContainerManager.RegisterType(typeof(IMarketDataService), typeof(MarketDataService), true);
            MarketDataRegisterService.RegisterServices();
            ContainerManager.RegisterType(typeof(IInvestmentReportWriter), typeof(PdfInvestmentReportWriter), true);
            ContainerManager.RegisterType(typeof(IDataLayer), typeof(SQLServerDataLayer.SQLServerDataLayer), true);
            ContainerManager.RegisterType(typeof(IInvestmentRecordDataManager), typeof(InvestmentRecordBuilder), true);
            ContainerManager.RegisterType(typeof(PerformanceBuilder), true);
        }
    }
}
