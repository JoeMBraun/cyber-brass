using System;
using System.Collections.Generic;
using UnityEngine;

namespace CyberBrass.Core
{
    /// <summary>
    /// A simple Service Locator implementation to decouple services and systems in the CyberBrass game.
    /// Provides registration, lookup, and removal of core dependencies.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        /// <summary>
        /// Registers a service implementation of type T.
        /// </summary>
        /// <typeparam name="T">The type of service interface or base class.</typeparam>
        /// <param name="service">The concrete implementation instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if the service is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if a service of type T is already registered.</exception>
        public static void Register<T>(T service) where T : class
        {
            Type type = typeof(T);
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), "Cannot register a null service.");
            }

            if (Services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service of type {type.Name} is already registered. Overwriting existing service.");
                Services[type] = service;
            }
            else
            {
                Services.Add(type, service);
            }
        }

        /// <summary>
        /// Retrieves the registered service of type T.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>The registered service instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no service of type T is registered.</exception>
        public static T Get<T>() where T : class
        {
            Type type = typeof(T);
            if (!Services.TryGetValue(type, out object service))
            {
                throw new KeyNotFoundException($"[ServiceLocator] Service of type {type.Name} is not registered.");
            }

            return (T)service;
        }

        /// <summary>
        /// Attempts to retrieve the registered service of type T.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <param name="service">Output parameter containing the service instance if found, or null.</param>
        /// <returns>True if the service was found and returned, false otherwise.</returns>
        public static bool TryGet<T>(out T service) where T : class
        {
            Type type = typeof(T);
            if (Services.TryGetValue(type, out object serviceObj))
            {
                service = (T)serviceObj;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Unregisters a service of type T.
        /// </summary>
        /// <typeparam name="T">The type of service to unregister.</typeparam>
        public static void Unregister<T>() where T : class
        {
            Type type = typeof(T);
            if (Services.ContainsKey(type))
            {
                Services.Remove(type);
            }
            else
            {
                Debug.LogWarning($"[ServiceLocator] Attempted to unregister service of type {type.Name} which was not registered.");
            }
        }

        /// <summary>
        /// Clears all registered services. Useful when resetting or unloading scenes.
        /// </summary>
        public static void ClearAll()
        {
            Services.Clear();
            Debug.Log("[ServiceLocator] All services cleared.");
        }
    }
}
