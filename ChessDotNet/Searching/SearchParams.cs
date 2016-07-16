using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet.Searching
{
    public class SearchParams
    {
        public SearchParams()
        {
            //Infinite = true;
        }

        public long WhiteTime { get; set; }
        public long BlackTime { get; set; }
        public long WhiteTimeIncrement { get; set; }
        public long BlackTimeIncrement { get; set; }
        public bool Infinite { get; set; }

        public int? MaxDepth { get; set; }
    }
}
