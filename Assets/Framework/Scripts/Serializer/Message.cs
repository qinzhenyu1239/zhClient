
using Framework.Core.Utils;
using System.Reflection;

namespace Framework.Core.Serializer {
    public abstract class Message
    {
        public short GetModule()
        {
            MessageMeta attribute = GetType().GetCustomAttribute<MessageMeta>();
            if (attribute != null)
            {
                return attribute.module;
            }

            return 0;
        }

        public short GetCmd()
        {
            MessageMeta attribute = GetType().GetCustomAttribute<MessageMeta>();
            if (attribute != null)
            {
                return attribute.cmd;
            }
            return 0;
        }

        public string key()
        {
            return GetModule() + "_" + GetCmd();
        }
    }
}


