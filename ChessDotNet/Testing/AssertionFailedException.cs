using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet.Testing
{
    class AssertionFailedException : Exception
    {
        public AssertionFailedException()
        {
        }

        public AssertionFailedException(string message) : base(message)
        {
        }

        public AssertionFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
