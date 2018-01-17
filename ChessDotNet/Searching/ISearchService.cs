using System;
using System.Collections.Generic;
using ChessDotNet.Data;

namespace ChessDotNet.Searching
{
    public interface ISearchService
    {
        event Action<SearchInfo> SearchInfo;

        IList<SearchTTEntry> Search(Board board, SearchParams searchParams = null);
    }
}