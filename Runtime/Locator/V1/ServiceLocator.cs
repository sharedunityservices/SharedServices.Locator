using System;
using System.Collections.Generic;
using System.Linq;
using SharedServices.V1;
using UnityEngine;

namespace SharedServices.Locator.V1
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IService> Services = new();
        private static Type[] _tempAllTypes;

        static ServiceLocator()
        {
            GetAllTypes();
            OverrideServices();
            AutoDetectServices();
            ClearAllTypes();
        }

        public static T Get<T>() where T : IService
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException($"Type <{typeof(T).Name}> must be an interface");

            var serviceType = typeof(T);
            if (Services.TryGetValue(serviceType, out var service))
                return (T)service;

            Debug.LogWarning($"Service {serviceType} not found");
            return default;
        }

        private static void GetAllTypes()
        {
            _tempAllTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .ToArray();
        }

        private static void OverrideServices()
        {
            var overrideServices = _tempAllTypes
                .Where(type => typeof(IOverrideServices).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

            foreach (var overrideService in overrideServices)
            {
                try
                {
                    var service = (IOverrideServices)Activator.CreateInstance(overrideService);
                    service.OverrideServices(Services);
                }
                catch (Exception e)
                {
#if UNITY_EDITOR
                    if (Application.isPlaying) throw;
                    Debug.LogWarning($"Failed to override services with {overrideService.Name}: {e.Message}");
#endif
                }
            }
        }

        private static void AutoDetectServices()
        {
            var serviceTypes = _tempAllTypes
                .Where(type => typeof(IService).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

            foreach (var serviceType in serviceTypes)
            {
                if (Services.ContainsKey(serviceType)) continue;
                    
                foreach (var serviceInterfaceType in serviceType.GetInterfaces().Where(type =>
                             type != typeof(IService) && typeof(IService).IsAssignableFrom(type)))
                {
                    try
                    {
                        if (!Services.ContainsKey(serviceInterfaceType))
                            Services.Add(serviceInterfaceType, (IService)Activator.CreateInstance(serviceType));
                    }
                    catch (Exception e)
                    {
#if UNITY_EDITOR
                        if (Application.isPlaying) throw;
                        Debug.LogWarning($"Failed to create service {serviceType.Name}: {e.Message}");
#endif
                    }
                }
            }

            foreach (var service in Services.Values) 
                service.Initialize();
        }

        private static void ClearAllTypes()
        {
            _tempAllTypes = null;
        }
    }
}