using InvestmentBuilderCore;
using SQLServerDataLayer;

namespace AuthenticationService
{
    /// <summary>
    /// Service Locator for the InvestmentBuilderService module. Allows
    /// any services to be registered before the handlers are loaded
    /// </summary>
    public static class ServiceLocator
    {
        public static void RegisterServices()
        {
            ContainerManager.RegisterType(typeof(IAuthorizationManager), typeof(SQLAuthorizationManager), true);
            ContainerManager.RegisterType(typeof(IAuthDataLayer), typeof(SQLAuthData), true);
            ContainerManager.RegisterType(typeof(IDataLayer), typeof(SQLServerDataLayer.SQLServerDataLayer), true);
            ContainerManager.RegisterType(typeof(AuthenticationManager), true);
        }
    }
}
