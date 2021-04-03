using System;
using System.Runtime.Serialization;

namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public class NnueException : Exception
    {
        public NnueException()
        {
        }

        protected NnueException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public NnueException(string? message) : base(message)
        {
        }

        public NnueException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}