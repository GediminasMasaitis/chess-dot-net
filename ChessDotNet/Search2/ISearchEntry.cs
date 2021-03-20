using System.Text;

namespace ChessDotNet.Search2
{
    public interface ISearchEntry
    {
        void Serialize(StringBuilder builder, int logDepth);
    }
}