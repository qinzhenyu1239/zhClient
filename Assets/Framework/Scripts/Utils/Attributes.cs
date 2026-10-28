using System;

namespace Framework.Core.Utils
{

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ResponesProcesser : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ResponesMapping : Attribute
    {
    }


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class MessageMeta : Attribute
    {
        public short module;
        public short cmd;

        public MessageMeta(short module, short cmd)
        {
            this.module = module;
            this.cmd = cmd;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class HttpController : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class HttpRequestMapping : Attribute
    {
        public string uri;

        public HttpRequestMapping(string uri)
        {
            this.uri = uri;
        }
    }
}


