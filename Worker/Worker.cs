using InvestmentBuilderCore;
using InvestmentBuilderCore.Schedule;
using NLog;
using RabbitTransport;
using System;
using System.Collections.Generic;
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
                    Console.WriteLine("Syntax: Worker <service name>");
                    return;
                }

                var service = args[0];

                _logger.Info($"Starting worker service {service}");

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

                    _logger.Info($"Registering services for {service}...");
                    // Register all the endpoints for this service
                    ApplicationBootstrapper.LoadService(child, service, channelManager);

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
