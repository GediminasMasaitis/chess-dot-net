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
            var takeRight = (bitBoards.WhitePawns << 9) & bitBoards.BlackPieces & ~bitBoards.Files[0];
            var moveOne = (bitBoards.WhitePawns << 8) & bitBoards.EmptySquares;
            var moveTwo = (bitBoards.WhitePawns << 16) & bitBoards.EmptySquares & bitBoards.EmptySquares << 8 & bitBoards.Ranks[3];

            for (byte i = 0; i < 64; i++)
            {
                if (takeLeft.HasBit(i))
                {
                    var move = new Move((byte)(i-7), i);
                    yield return move;
                }

                if (takeRight.HasBit(i))
                {
                    var move = new Move((byte)(i - 9), i);
                    yield return move;
                }

                if (moveOne.HasBit(i))
                {
                    var move = new Move((byte)(i - 8), i);
                    yield return move;
                }

                if (moveTwo.HasBit(i))
                {
                    var move = new Move((byte)(i - 16), i);
                    yield return move;
                }
            }
        }
    }
}
