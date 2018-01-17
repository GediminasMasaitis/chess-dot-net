using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Data;

namespace ChessDotNet.Searching
{
    class NewSearchService : ISearchService
    {
        public event Action<SearchInfo> SearchInfo;

        public IList<SearchTTEntry> Search(Board board, SearchParams searchParams = null)
        {
            //throw new NotImplementedException();
            searchParams = searchParams ?? new SearchParams();
            var maxDepth = searchParams.MaxDepth ?? 64;


            // Iterative deepening
            for (var iteration = 0; iteration < maxDepth; iteration++)
            {

            }

            return null;
        }
    }
}
