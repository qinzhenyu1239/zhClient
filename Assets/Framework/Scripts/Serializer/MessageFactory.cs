using Framework.Core.Serializer;
using Framework.Core.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

class MessageFactory
{
    public static readonly MessageFactory Instance = new MessageFactory();
    private readonly Dictionary<int, Type> keyTypeDic = new Dictionary<int, Type>();

    public void InitMeesagePool()
    {
        keyTypeDic.Clear();
        foreach (Type type in TypeScanner.ListAllSubTypes(typeof(Message)))
        {
            if (type.IsAbstract) continue;
            MessageMeta meta = type.GetCustomAttribute<MessageMeta>();
            if (meta == null) throw new InvalidOperationException($"Missing MessageMeta: {type.FullName}");
            int key = BuildKey(meta.module, meta.cmd);
            if (keyTypeDic.ContainsKey(key)) throw new InvalidOperationException($"Duplicate protocol: {meta.module}_{meta.cmd}");
            keyTypeDic.Add(key, type);
        }
    }

    public int BuildKey(short module, short cmd) => ((int)(ushort)module << 16) | (ushort)cmd;
    public bool GetMessage(short module, short cmd, out Type msgType) => keyTypeDic.TryGetValue(BuildKey(module, cmd), out msgType);
}
