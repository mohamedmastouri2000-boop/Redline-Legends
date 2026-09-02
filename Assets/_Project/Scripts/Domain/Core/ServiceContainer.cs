using System;
using System.Collections.Generic;

namespace RedlineLegends.Core
{
    /// <summary>
    /// Explicit service registry populated by the composition root (GameBootstrap).
    /// Services are registered by interface/type once at boot; nothing else creates singletons.
    /// Keeping wiring in one place is what lets multiplayer later swap implementations
    /// (e.g. a networked race session) without touching consumers.
    /// </summary>
    public sealed class ServiceContainer
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>(32);
        private readonly List<object> _registrationOrder = new List<object>(32);

        public void Register<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var key = typeof(T);
            if (_services.ContainsKey(key))
                throw new InvalidOperationException($"Service {key.Name} is already registered.");
            _services.Add(key, instance);
            _registrationOrder.Add(instance);
        }

        public T Resolve<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var value))
                return (T)value;
            throw new InvalidOperationException($"Service {typeof(T).Name} is not registered. Was GameBootstrap run?");
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var value))
            {
                service = (T)value;
                return true;
            }
            service = null;
            return false;
        }

        public bool Contains<T>() where T : class => _services.ContainsKey(typeof(T));

        /// <summary>Disposes every IDisposable service in reverse registration order and clears the registry.</summary>
        public void Clear()
        {
            for (int i = _registrationOrder.Count - 1; i >= 0; i--)
            {
                if (_registrationOrder[i] is IDisposable disposable)
                    disposable.Dispose();
            }
            _registrationOrder.Clear();
            _services.Clear();
        }
    }
}
