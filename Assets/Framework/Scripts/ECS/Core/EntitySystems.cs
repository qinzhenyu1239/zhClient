using System;

namespace Framework.ECS
{
    public interface IAwake { }
    public interface IUpdate { }
    public interface ILateUpdate { }
    public interface IDestroy { }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ComponentOfAttribute : Attribute
    {
        public Type ParentType { get; }
        public ComponentOfAttribute(Type parentType) { ParentType = parentType; }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class EntitySystemOfAttribute : Attribute
    {
        public Type ComponentType { get; }
        public EntitySystemOfAttribute(Type componentType) { ComponentType = componentType; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class EntitySystemAttribute : Attribute { }
}
