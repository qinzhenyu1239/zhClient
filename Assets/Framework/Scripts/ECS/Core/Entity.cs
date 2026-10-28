using System;
using System.Collections.Generic;
using System.Reflection;

namespace Framework.ECS
{
    public class Entity : IDisposable
    {
        private readonly Dictionary<Type, Entity> components = new Dictionary<Type, Entity>();
        private readonly List<Entity> children = new List<Entity>();
        public long Id { get; private set; }
        public Entity Parent { get; private set; }
        public EcsWorld World { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool Enabled { get; set; } = true;
        public bool IsActive => !IsDisposed && Enabled && (Parent == null || Parent.IsActive);
        public IReadOnlyCollection<Entity> Components => components.Values;
        public IReadOnlyList<Entity> Children => children.AsReadOnly();
        public T GetParent<T>() where T : Entity => Parent as T;

        internal void Attach(EcsWorld world, Entity parent)
        {
            World = world;
            Parent = parent;
            Id = world.AllocateId();
        }

        public T GetComponent<T>() where T : Entity
        {
            return components.TryGetValue(typeof(T), out Entity value) && !value.IsDisposed ? (T)value : null;
        }

        public bool HasComponent<T>() where T : Entity => GetComponent<T>() != null;

        public T AddComponent<T>() where T : Entity, new()
        {
            EnsureAttached();
            Type type = typeof(T);
            if (components.ContainsKey(type)) throw new InvalidOperationException("Component already attached: " + type.FullName);
            var constraint = type.GetCustomAttribute<ComponentOfAttribute>();
            if (constraint != null && !constraint.ParentType.IsInstanceOfType(this))
                throw new InvalidOperationException(type.FullName + " cannot be attached to " + GetType().FullName);
            var component = new T();
            component.Attach(World, this);
            components.Add(type, component);
            try
            {
                World.Systems.Invoke(component, "Awake");
                if (!component.IsDisposed) World.Register(component);
                return component;
            }
            catch
            {
                component.Dispose();
                throw;
            }
        }

        public bool RemoveComponent<T>() where T : Entity
        {
            T component = GetComponent<T>();
            if (component == null) return false;
            component.Dispose();
            return true;
        }

        public T AddChild<T>() where T : Entity, new()
        {
            EnsureAttached();
            if (typeof(T).IsDefined(typeof(ComponentOfAttribute), false))
                throw new InvalidOperationException("Use AddComponent for " + typeof(T).FullName);
            var child = new T();
            child.Attach(World, this);
            children.Add(child);
            try
            {
                World.Systems.Invoke(child, "Awake");
                if (!child.IsDisposed) World.Register(child);
                return child;
            }
            catch
            {
                child.Dispose();
                throw;
            }
        }

        private void EnsureAttached()
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().Name);
            if (World == null || World.IsDisposed) throw new InvalidOperationException("Create entities through a live EcsWorld.");
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            foreach (Entity child in children.ToArray()) child.Dispose();
            foreach (Entity component in new List<Entity>(components.Values)) component.Dispose();
            try { World?.Systems.Invoke(this, "Destroy"); }
            catch (Exception exception) { World?.Report(new InvalidOperationException("ECS Destroy failed: " + GetType().FullName, exception)); }
            if (Parent != null)
            {
                if (Parent.components.TryGetValue(GetType(), out Entity component) && ReferenceEquals(component, this))
                    Parent.components.Remove(GetType());
                Parent.children.Remove(this);
            }
            components.Clear();
            children.Clear();
            Parent = null;
        }
    }
}
