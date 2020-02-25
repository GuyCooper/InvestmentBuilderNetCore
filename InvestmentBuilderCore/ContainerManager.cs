using System;
using Unity;
using Unity.Injection;
using Unity.Lifetime;

namespace InvestmentBuilderCore
{
    public static class ContainerManager
    {
        private static IUnityContainer _container = new UnityContainer();

        public static IUnityContainer GetContainer()
        {
            return _container;
        }

        public static void RegisterType(Type typeFrom, bool bHierachialLifetime)
        {
            if(bHierachialLifetime)
            {
                _container.RegisterType(typeFrom, new HierarchicalLifetimeManager());
            }
            else
            {
                _container.RegisterType(typeFrom, new ContainerControlledLifetimeManager());
            }
        }

        public static void RegisterType(Type typeFrom, Type typeTo, bool bHierachialLifetime, params object[] prm)
        {
            ITypeLifetimeManager lifetimeManager = new ContainerControlledLifetimeManager();
            if (bHierachialLifetime)
            {
                lifetimeManager = new HierarchicalLifetimeManager();
            }

            _container.RegisterType(typeFrom, typeTo, typeFrom.ToString(), lifetimeManager, new InjectionConstructor(prm));

        }

        public static T ResolveValueOnContainer<T>(IUnityContainer container) where T : class
        {
            var result = container.Resolve<T>();
            if (result != null)
            {
                return result;
            }
            throw new ArgumentException($"type {typeof(T)} has not been registered!");

        }

        public static T ResolveValueOnContainer<T>(Type typeOf, IUnityContainer container) where T : class
        {
            var result = container.Resolve(typeOf);
            if(result == null)
            {
                throw new ArgumentException(string.Format("type {0} has not been registered!", typeOf));
            }
            return result as T;
        }

        public static T ResolveValue<T>() where T : class
        {
            return ResolveValueOnContainer<T>(_container);
        }

        public static IUnityContainer CreateChildContainer()
        {
            return _container.CreateChildContainer();
        }
    }
}
