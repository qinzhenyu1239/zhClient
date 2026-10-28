using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Framework.ECS
{
    public sealed class EntitySystemRegistry
    {
        private sealed class Handlers
        {
            public readonly Dictionary<string, Action<Entity>> Methods = new Dictionary<string, Action<Entity>>();
        }

        private readonly Dictionary<Type, Handlers> handlers = new Dictionary<Type, Handlers>();

        public EntitySystemRegistry(IEnumerable<Type> systemTypes)
        {
            foreach (Type type in systemTypes)
            {
                var attribute = type.GetCustomAttribute<EntitySystemOfAttribute>();
                if (attribute == null) continue;
                Type component = attribute.ComponentType;
                if (!type.IsAbstract || !type.IsSealed || component == null || !typeof(Entity).IsAssignableFrom(component))
                    throw new InvalidOperationException("Invalid ECS system declaration: " + type.FullName);
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!method.IsDefined(typeof(EntitySystemAttribute), false)) continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    Type contract = Contract(method.Name);
                    if (contract == null || !contract.IsAssignableFrom(component) || method.ReturnType != typeof(void) ||
                        method.IsGenericMethod || parameters.Length != 1 || parameters[0].ParameterType != component ||
                        !method.IsDefined(typeof(ExtensionAttribute), false))
                        throw new InvalidOperationException("Invalid ECS lifecycle signature: " + type.FullName + "." + method.Name);
                    if (!handlers.TryGetValue(component, out Handlers entry))
                        handlers.Add(component, entry = new Handlers());
                    if (entry.Methods.ContainsKey(method.Name))
                        throw new InvalidOperationException("Duplicate ECS lifecycle: " + component.FullName + "." + method.Name);
                    // Reflection runs during registration; frame dispatch uses the resulting typed delegate.
                    MethodInfo factory = typeof(EntitySystemRegistry).GetMethod(nameof(CreateInvoker), BindingFlags.Static | BindingFlags.NonPublic);
                    var invoke = (Action<Entity>)factory.MakeGenericMethod(component).Invoke(null, new object[] { method });
                    entry.Methods.Add(method.Name, invoke);
                }
            }
        }

        public static EntitySystemRegistry Scan(params Assembly[] assemblies)
        {
            var types = new List<Type>();
            var seen = new HashSet<Assembly>();
            foreach (Assembly assembly in assemblies)
                if (seen.Add(assembly)) types.AddRange(assembly.GetTypes());
            return new EntitySystemRegistry(types);
        }

        private static Type Contract(string name)
        {
            switch (name)
            {
                case "Awake": return typeof(IAwake);
                case "Update": return typeof(IUpdate);
                case "LateUpdate": return typeof(ILateUpdate);
                case "Destroy": return typeof(IDestroy);
                default: return null;
            }
        }

        private static Action<Entity> CreateInvoker<T>(MethodInfo method) where T : Entity
        {
            var typed = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), method);
            return entity => typed((T)entity);
        }

        internal bool Has(Entity entity, string lifecycle)
        {
            return handlers.TryGetValue(entity.GetType(), out Handlers entry) && entry.Methods.ContainsKey(lifecycle);
        }

        internal void Invoke(Entity entity, string lifecycle)
        {
            if (handlers.TryGetValue(entity.GetType(), out Handlers entry) && entry.Methods.TryGetValue(lifecycle, out Action<Entity> action))
                action(entity);
        }
    }
}
