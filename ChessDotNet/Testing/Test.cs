using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet.Testing
{
    static class Test
    {
        public static void Assert(bool assertion, string assertionDescription = null)
        {
            if (!assertion)
            {
                throw new AssertionFailedException(assertionDescription);
            }
        }
    }
}
