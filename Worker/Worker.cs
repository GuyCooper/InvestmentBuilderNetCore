using InvestmentBuilderCore;
using InvestmentBuilderCore.Schedule;
using NLog;
using RabbitTransport;
using System;
using System.Collections.Generic;
using Transports;
using Transports.Session;
using Transports.Utils;

namespace Worker
{
    class Worker
    {
        /// <summary>
        /// Main Entry point.
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                if(args.Length == 0)
                {
                    Console.WriteLine("Syntax: Worker <service1> <service2> ...");
                    return;
                }

                var services = new List<string>();
                for(int i = 0; i < args.Length; i++)
                {
                    services.Add(args[i]);
                }

                _logger.Info($"Starting worker services {string.Join(',', services)}");

                var configfile = "InvestmentBuilderConfig.xml";
                var connectionsFile = "Connections.xml";

                var configSettings = new ConfigurationSettings(configfile);
                ContainerManager.RegisterInstance<IConfigurationSettings>(configSettings);

                var connectionSettings = new ConnectionSettings(connectionsFile);
                ContainerManager.RegisterInstance<IConnectionSettings>(connectionSettings);

                var schedulerFactory = new ScheduledTaskFactory();
                ContainerManager.RegisterInstance(schedulerFactory);

                ContainerManager.RegisterType(typeof(IConnectionSession), typeof(RabbitSession), true);

                using (var child = ContainerManager.CreateChildContainer())
                {
                    // Connect to the middleware service
                    _logger.Info("Connecting to middleware...");
                    var session = ContainerManager.ResolveValueOnContainer<IConnectionSession>(child);

                    var channelManager = new ChannelEndpointManager(session);

                    foreach (var service in services)
                    {
                        _logger.Info($"Registering service {service}...");
                        // Register all the endpoints for this service
                        ApplicationBootstrapper.LoadService(child, service, channelManager);
                    }

                    if(!ContainerManager.IsRegistered<ISessionManager>())
                    {
                        ContainerManager.RegisterType(typeof(UserSessionManager), true);
                    }

                    _logger.Info("Service started ok");
                    using (var scheduler = new Scheduler(schedulerFactory, new List<ScheduledTaskDetails>()))
                    {
                        scheduler.Run();
                    }

                    _logger.Info("Service stopping....");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        #region Private Data

        private static Logger _logger = LogManager.GetLogger("InvestmentBuilderService");

        #endregion

    }
}
