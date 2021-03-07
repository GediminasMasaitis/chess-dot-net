using System.Text;

namespace ChessDotNet.Search2
{
    public class SearchLogMessage
    {
        public string Text { get; }
        public int? Depth { get; }
        public int? Alpha { get; }
        public int? Beta { get; }
        public int? Final { get; }

        public SearchLogMessage(string text, int? depth = null, int? alpha = null, int? beta = null, int? final = null)
        {
            Text = text;
            Depth = depth;
            Alpha = alpha;
            Beta = beta;
            Final = final;
        }

        /*public override string ToString()
        {
            return $"A: {Alpha}, B: {Beta}; {Text}";
        }*/

        public void Serialize(StringBuilder builder)
        {
            if (Depth.HasValue)
            {
                builder.Append($"D={Depth.Value} ");
            }
            if (Alpha.HasValue)
            {
                builder.Append($"A={Alpha.Value} ");
            }
            if (Beta.HasValue)
            {
                builder.Append($"B={Beta.Value} ");
            }
            if (Final.HasValue)
            {
                builder.Append($"F={Final.Value} ");
            }

            builder.Append($"; {Text}");
        }
    }
}