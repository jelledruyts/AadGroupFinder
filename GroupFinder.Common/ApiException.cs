using System;
using System.Runtime.Serialization;

namespace GroupFinder.Common
{

    [Serializable]
    public class ApiException : Exception
    {
        public string Code { get; private set; }

        public ApiException(string message, string code)
            : this(message)
        {
            this.Code = code;
        }

        // Default exception constructors.
        public ApiException() { }
        public ApiException(string message) : base(message) { }
        public ApiException(string message, Exception inner) : base(message, inner) { }
        protected ApiException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}