using Framework.Core.Serializer;
using Framework.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class MessageDispatcher
{
    public static readonly MessageDispatcher Instance = new MessageDispatcher();
    private readonly Dictionary<string, CmdExecutor> handlers = new Dictionary<string, CmdExecutor>();
    private bool subscribed;
    private EventMgr eventMgr;

    public void Init()
    {
        if (subscribed) return;
        handlers.Clear();
        foreach (Type controller in TypeScanner.ListTypesWithAttribute(typeof(ResponesProcesser)))
        {
            object instance = Activator.CreateInstance(controller);
            foreach (MethodInfo method in controller.GetMethods())
            {
                if (method.GetCustomAttribute<ResponesMapping>() == null) continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 1 || !typeof(Message).IsAssignableFrom(parameters[0].ParameterType))
                    throw new InvalidOperationException($"Response handler must accept exactly one Message: {method.Name}");
                MessageMeta meta = parameters[0].ParameterType.GetCustomAttribute<MessageMeta>();
                if (meta == null) throw new InvalidOperationException($"Response MessageMeta missing: {method.Name}");
                string key = meta.module + "_" + meta.cmd;
                if (handlers.ContainsKey(key)) throw new InvalidOperationException($"Duplicate response handler: {key}");
                handlers.Add(key, CmdExecutor.Create(method, parameters.Select(p => p.ParameterType).ToArray(), instance));
            }
        }
        eventMgr = EventMgr.Instance;
        eventMgr.AddListener(TcpSocketMgr.NetRecvCmdEvent, OnReceived);
        subscribed = true;
    }

    private void OnReceived(int eventType, object data, object metadata)
    {
        if (!(data is Message message)) return;
        if (!handlers.TryGetValue(message.key(), out CmdExecutor executor)) return;
        // TcpSocketMgr delivers this event on Unity's main thread.
        executor.method.Invoke(executor.handler, new object[] { message });
    }

    public void Dispose()
    {
        if (subscribed && eventMgr != null)
            eventMgr.RemoveListener(TcpSocketMgr.NetRecvCmdEvent, OnReceived);
        eventMgr = null;
        subscribed = false;
        handlers.Clear();
    }
}
