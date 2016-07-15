using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet.Searching
{
    public class SearchParams
    {
        public int WhiteTime { get; set; }
        public int BlackTime { get; set; }
        public int WhiteTimeIncrement { get; set; }
        public int BlackTimeIncrement { get; set; }
    }
}
