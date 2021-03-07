using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ChessDotNet.Data;

namespace ChessDotNet.Search2
{
    public class SearchLog
    {
        public Move? Move { get; set; }
        public Board Board { get; set; }
        public TranspositionTableEntry TableEntry { get; set; }
        public IList<SearchLogMessage> Messages { get; set; }
        public IList<SearchLog> Children { get; set; }

        private SearchLog(Move? move)
        {
            Messages = new List<SearchLogMessage>();
            Children = new List<SearchLog>();
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
            Messages.Add(message);
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
            var serialized = Serialize();
            Console.WriteLine(serialized);
        }

        [Conditional("LOG")]
        public void PrintLastChild()
        {
            var lastChild = Children[Children.Count - 1];
            lastChild.Print();
        }

        public string Serialize()
        {
            var builder = new StringBuilder();
            SerializeInner(this, builder, 0);
            return builder.ToString();
        }

        private void SerializeInner(SearchLog log, StringBuilder builder, int logDepth)
        {
            //Pad(builder, logDepth);
            //builder.AppendLine(">");

            foreach (var message in log.Messages)
            {
                Pad(builder, logDepth);
                message.Serialize(builder);
                builder.AppendLine();
            }

            foreach (var child in log.Children)
            {
                SerializeInner(child, builder, logDepth + 1);
            }
        }

        private void Pad(StringBuilder builder, int logDepth)
        {
            const string pad = "  ";
            for (var i = 0; i < logDepth; i++)
            {
                builder.Append(pad);
            }
        }
    }

}