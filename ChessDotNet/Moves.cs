using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet
{
    class Moves
    {
        ulong GetPossibleWhitePawnMoves(BitBoards bitBoards)
        {
            var takeLeft = bitBoards.WhitePawns << 7;

            return 0;
        }
    }
}
