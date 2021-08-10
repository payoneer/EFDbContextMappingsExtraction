using System;

namespace DbContextMappingDump
{
    internal class WrappedException : Exception
    {
        private readonly string _stackTrace;

        public WrappedException(string type, string message, string stackTrace)
            : base(message)
        {
            Type = type;
            _stackTrace = stackTrace;
        }

        public string Type { get; }

        public override string ToString()
            => _stackTrace;
    }
}
