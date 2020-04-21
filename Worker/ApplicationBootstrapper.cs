using InvestmentBuilderCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Transports;
using Unity;

namespace Worker
{
    /// <summary>
    /// Bootstrap all the handlers
    /// </summary>
    internal static class ApplicationBootstrapper
    {
        /// <summary>
        /// load the channel handlers for the specified service.
        /// </summary>
        public static void LoadService(IUnityContainer container, string service, ChannelEndpointManager endpointManager)
        {
            //register and resolve all concrete classes in this module that are derived \
            //from EndpointChannel
            //first find a list of all the endpoint channels types
            var channelTypes = new List<Type>();
            var assembley = LoadServiceAssembley(service);
            Type sessionManagerType = null;
            Type serviceLocatorType = null;
            foreach (var t in assembley.GetTypes())
            {
                if (t.IsAbstract == false)
                {
                    if (IsEndpointChannelClass(t))
                    {
                        channelTypes.Add(t);
                    }
                    else if (IsClassOfType(t, typeof(ISessionManager)))
                    {
                        sessionManagerType = t;
                    }
                }
                else if (t.Name == "ServiceLocator")
                {
                    serviceLocatorType = t;
                }
            }

            //call the register services method for this assembley if it has a service locator
            if (serviceLocatorType != null)
            {
                var registerMethod = serviceLocatorType.GetMethod("RegisterServices", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (registerMethod != null)
                {
                    registerMethod.Invoke(null, null);
                }
            }

            //register the usersesionmanager if it has one.
            if (sessionManagerType == null)
            {
                sessionManagerType = typeof(UserSessionManager);
            }

            ContainerManager.RegisterType(sessionManagerType, true);
            var sessionManager = ContainerManager.ResolveValueOnContainer<ISessionManager>(sessionManagerType, container);
            endpointManager.SetSessionManager(sessionManager);

            //now register each endpoint channel
            foreach (var channel in channelTypes)
            {
                ContainerManager.RegisterType(channel, true);
            }

            //now resolve (instantiate) each endpointchannel and register the 
            //channels with the middleware layer
            foreach (var channel in channelTypes)
            {
                var endpoint = ContainerManager.ResolveValueOnContainer<IEndpointChannel>(channel, container);
                endpointManager.RegisterChannel(endpoint);
            }
        }

        #region Private Methods

        /// <summary>
        /// Helper method for determining if an object is of type IEndpointChannel
        /// </summary>
        private static bool IsEndpointChannelClass(Type objType)
        {
            return IsClassOfType(objType, typeof(IEndpointChannel));
        }

        /// <summary>
        /// Helper method for determining if an object is of specified type.
        /// </summary>
        private static bool IsClassOfType(Type objType, Type IsOfType)
        {
            var types = from interfaceType in objType.GetInterfaces()
                        where interfaceType == IsOfType
                        select interfaceType;

            return types.Any();
        }

        /// <summary>
        /// Attempt to load the specified service assembley
        /// </summary>
        private static System.Reflection.Assembly LoadServiceAssembley(string service)
        {
            string filename = $"{service}.dll";
            return System.Reflection.Assembly.LoadFrom(filename);
        }

        #endregion

    }
}
