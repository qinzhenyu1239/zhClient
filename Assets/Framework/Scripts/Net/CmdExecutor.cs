using System;
using System.Reflection;

public class CmdExecutor
{
    /** logic controller  */
    public object handler;

    /** logic handler method */
    public MethodInfo method;

    /** arguments passed to method */
    public Type[] @params;


    public static CmdExecutor Create(MethodInfo method, Type[] @params, object handler)
    {
        CmdExecutor executor = new CmdExecutor();
        executor.method = method;
        executor.@params = @params;
        executor.handler = handler;

        return executor;
    }
}