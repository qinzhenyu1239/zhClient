using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


public class TypeScanner {
    public static IEnumerable<Type> ListAllSubTypes(Type parent)
    {
        return Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(parent));
    }

    /*
    public static IEnumerable<Type> ListAllAchieveInterface(Type _interface)
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => type.IsAssignableTo(_interface) && type != _interface);
    }*/

    public static IEnumerable<Type> ListTypesWithAttribute(Type attribute)
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => type.IsDefined(attribute));
    }
}

