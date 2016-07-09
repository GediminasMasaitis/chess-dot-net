using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet
{
    public class PossibleMovesService
    {
        public IEnumerable<Move> GetPossibleWhitePawnMoves(BitBoards bitBoards)
        {
            var takeLeft = (bitBoards.WhitePawns << 7) & bitBoards.BlackPieces & ~bitBoards.Files[7];
            for (byte i = 0; i < 64; i++)
            {
                if (takeLeft.HasBit(i))
                {
                    var move = new Move((byte)(i-7), i);
                    yield return move;
                }
            }
        }
    }
}
