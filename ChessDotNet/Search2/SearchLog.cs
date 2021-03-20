//#define LOG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ChessDotNet.Data;

namespace ChessDotNet.Search2
{
    public class SearchLog : SearchLogBase
    {
        public Move? Move { get; set; }
        public Board Board { get; set; }
        public TranspositionTableEntry TableEntry { get; set; }
        public IList<ISearchEntry> Children { get; set; }

        private SearchLog _pendingChild;

        private SearchLog(Move? move)
        {
            Children = new List<ISearchEntry>();
            Move = move;
        }

        public static SearchLog New(Move? move = null)
        {
#if LOG
            return new SearchLog(move);
#endif
            return default;
        }

        [Conditional("LOG")]
        public void AddMessage(string text, int? depth = null, int? alpha = null, int? beta = null, int? final = null)
        {
            var message = new SearchLogMessage(text, depth, alpha, beta, final);
            Children.Add(message);
        }

/*        public SearchLog AddNewChild(Move? move = null)
        {
#if LOG
            var childLog = new SearchLog(move);
            Children.Add(childLog);
            return childLog;
#endif
            return default;
        }*/

        [Conditional("LOG")]
        public void AddChild(SearchLog log)
        {
            Children.Add(log);
        }

        [Conditional("LOG")]
        public void Print()
        {
            var builder = new StringBuilder();
            Serialize(builder, 0);
            var serialized = builder.ToString();
            Console.WriteLine(serialized);
        }

        [Conditional("LOG")]
        public void PrintLastChild()
        {
            var lastChild = (SearchLog)Children[Children.Count - 1];
            lastChild.Print();
        }
        
        public override void Serialize(StringBuilder builder, int logDepth)
        {
            Pad(builder, logDepth);
            builder.Append($">>>");
            if (Move.HasValue)
            {
                builder.Append($" {Move.Value.ToPositionString()}");
            }
            builder.AppendLine();
            
            foreach (var child in Children)
            {
                child.Serialize(builder, logDepth + 1);
            }

            Pad(builder, logDepth);
            builder.Append($"<<<");
            if (Move.HasValue)
            {
                builder.Append($" {Move.Value.ToPositionString()}");
            }
            builder.AppendLine();
        }
    }

}