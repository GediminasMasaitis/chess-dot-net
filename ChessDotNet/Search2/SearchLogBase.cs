using System.Text;

namespace ChessDotNet.Search2
{
    public abstract class SearchLogBase : ISearchEntry
    {
        protected void Pad(StringBuilder builder, int logDepth)
        {
            const string pad = "  ";
            for (var i = 0; i < logDepth; i++)
            {
                builder.Append(pad);
            }
        }

        public abstract void Serialize(StringBuilder builder, int logDepth);
    }
}