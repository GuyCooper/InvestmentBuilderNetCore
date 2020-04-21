using InvestmentBuilderCore;
using InvestmentBuilderLib;
using InvestmentReportGenerator;
using MarketDataServices;
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
            ContainerManager.RegisterType(typeof(IAuthorizationManager), typeof(SQLAuthorizationManager), false);
            ContainerManager.RegisterType(typeof(IMarketDataService), typeof(MarketDataService), false);
            MarketDataRegisterService.RegisterServices();
            ContainerManager.RegisterType(typeof(IInvestmentReportWriter), typeof(PdfInvestmentReportWriter), false);
            ContainerManager.RegisterType(typeof(IDataLayer), typeof(SQLServerDataLayer.SQLServerDataLayer), false);
            ContainerManager.RegisterType(typeof(IInvestmentRecordDataManager), typeof(InvestmentRecordBuilder), false);
        }
    }
}
