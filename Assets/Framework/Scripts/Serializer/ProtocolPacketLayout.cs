namespace Framework.Core.Serializer
{
    public static class ProtocolPacketLayout
    {
        public const int ModuleOffset = 0;
        public const int CommandOffset = 2;
        public const int RouteTagOffset = 4;
        public const int LogicServerIdOffset = 12;
        public const int RequestIdOffset = 14;
        public const int HeaderLength = 22;
    }
}
