using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet.Searching
{
    public class SearchParameters
    {
        public bool Infinite { get; set; }
        public int? MaxDepth { get; set; }

        public long WhiteTime { get; set; }
        public long BlackTime { get; set; }

        public long WhiteTimeIncrement { get; set; }
        public long BlackTimeIncrement { get; set; }

        public SearchParameters()
        {
            // Defaults to 3+1
            Infinite = false;
            MaxDepth = null;
            WhiteTime = 60000 * 3;
            BlackTime = 60000 * 3;
            WhiteTimeIncrement = 1000;
            BlackTimeIncrement = 1000;
        }
    }
}
