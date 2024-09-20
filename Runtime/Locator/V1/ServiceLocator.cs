using System;
using System.Collections.Generic;
using System.Linq;
using SharedServices.Files.V1;
using SharedServices.Log;
using SharedServices.V1;
using UnityEditor;
using UnityEngine;

namespace SharedServices.Locator.V1
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IService> Services = new();
        private static Type[] _tempAllTypes;
        private static Type Context => typeof(ServiceLocator);

        static ServiceLocator()
        {
            InitializeLogService();
            GetAllServiceTypes();
            CreateInstancesOfOverrideServices();
            AutoCreateInstancesOfFirstFoundServices();
            ILog.Trace("All Service Instances Created!", Context);
            ReleaseAllTypesFromMemory();
            InitializeAllServices();
        }

        private static void InitializeLogService()
        {
            // Required so we can ILog.Trace before other services are created and initialized
            Services[typeof(IFileService)] = new FallbackPathFileService();
            Services[typeof(IFileService)].Initialize();
            Services[typeof(ILog)] = new Log.Log();
            Services[typeof(ILog)].Initialize();
        }

        public static T Get<T>() where T : IService
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException($"Type <{typeof(T).Name}> must be an interface");

            var serviceType = typeof(T);
            if (Services.TryGetValue(serviceType, out var service))
                return (T)service;
            
            ILog.Warn($"Service {serviceType} not found", Context);
            return default;
        }
        
        public static IService Get(Type serviceType)
        {
            if (!serviceType.IsInterface)
                throw new ArgumentException($"Type <{serviceType.Name}> must be an interface");

            if (Services.TryGetValue(serviceType, out var service))
                return service;
            
            ILog.Warn($"Service {serviceType} not found", Context);
            return null;
        }

        private static void GetAllServiceTypes()
        {
            ILog.Trace("Getting all types...", Context);
            _tempAllTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .ToArray();
            ILog.Trace($"Found {_tempAllTypes.Length} types", Context);
        }

        private static void CreateInstancesOfOverrideServices()
        {
            var overrideServices = _tempAllTypes
                .Where(type =>
                    typeof(IOverrideServices).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

            foreach (var overrideService in overrideServices)
            {
                try
                {
                    ILog.Trace($"Found Override Service Class: {overrideService.Name}", Context);
                    var service = (IOverrideServices)Activator.CreateInstance(overrideService);
                    service.OverrideServices(Services);
                }
                catch (Exception e)
                {
#if UNITY_EDITOR
                    if (Application.isPlaying) throw;
                    ILog.Warn($"Failed to override services with {overrideService.Name}: {e.Message}", Context);
#endif
                }
            }
        }

        private static void AutoCreateInstancesOfFirstFoundServices()
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
                        if (Services.ContainsKey(serviceInterfaceType)) continue;
                        ILog.Trace($"Creating instance of {serviceType.Name} for {serviceInterfaceType.Name}", Context); 
                        Services.Add(serviceInterfaceType, (IService)Activator.CreateInstance(serviceType));
                    }
                    catch (Exception e)
                    {
#if UNITY_EDITOR
                        if (Application.isPlaying) throw;
                        ILog.Warn($"Failed to create service {serviceType.Name}: {e.Message}", Context);
#endif
                    }
                }
            }
        }

        private static void ReleaseAllTypesFromMemory()
        {
            ILog.Trace("Releasing all types from memory...", Context);
            _tempAllTypes = null;
        }

        private static void InitializeAllServices()
        {
            foreach (var service in Services.Values)
            {
                if (service is ILog) continue;
                if (service is IFileService) continue;
                
                try
                {
                    service.Initialize();
#if UNITY_EDITOR
                    var context = AssetDatabase
                        .FindAssets("t:MonoScript " + service.GetType().Name)
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Select(AssetDatabase.LoadAssetAtPath<MonoScript>)
                        .FirstOrDefault(script => script.GetClass() == service.GetType());
                    ILog.Debug("Initialized service", context);
#else
                    ILog.Debug($"Initialized service {service.GetType().Name}", Context); 
#endif
                }
                catch (Exception e)
                {
                    if (Application.isPlaying) throw;
#if UNITY_EDITOR
                    ILog.Warn($"Failed to initialize service {service.GetType().Name}: {e.Message}", Context);
#endif
                }
            }
        }
    }
}