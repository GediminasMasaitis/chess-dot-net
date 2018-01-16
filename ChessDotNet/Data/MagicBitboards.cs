using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.MoveGeneration;

namespace ChessDotNet.Data
{
    public class MagicBitboards
    {
        public static IReadOnlyList<MagicBitboardEntry> Rooks { get; set; }
        public static IReadOnlyList<MagicBitboardEntry> Bishops { get; set; }
    }
}
