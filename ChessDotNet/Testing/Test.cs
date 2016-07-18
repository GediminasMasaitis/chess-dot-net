using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Data;
using ChessDotNet.Hashing;

#if TEST
namespace ChessDotNet.Testing
{
    static class Test
    {
        public static void Assert(bool assertion, string assertionDescription = null)
        {
            if (!assertion)
            {
                throw new AssertionFailedException(assertionDescription);
            }
        }

        public static void CheckBoard(this Board board)
        {
            Assert(board.PieceCounts[ChessPiece.WhiteKing] == 1, "White king count != 1");
            Assert(board.PieceCounts[ChessPiece.BlackKing] == 1, "Black king count != 1");

            Assert(board.WhitePieces == (board.BitBoard[ChessPiece.WhitePawn]
                | board.BitBoard[ChessPiece.WhiteKnight]
                | board.BitBoard[ChessPiece.WhiteBishop]
                | board.BitBoard[ChessPiece.WhiteRook]
                | board.BitBoard[ChessPiece.WhiteQueen]
                | board.BitBoard[ChessPiece.WhiteKing]));

            Assert(board.BlackPieces == (board.BitBoard[ChessPiece.BlackPawn]
                | board.BitBoard[ChessPiece.BlackKnight]
                | board.BitBoard[ChessPiece.BlackBishop]
                | board.BitBoard[ChessPiece.BlackRook]
                | board.BitBoard[ChessPiece.BlackQueen]
                | board.BitBoard[ChessPiece.BlackKing]));

            Assert(board.AllPieces == (board.WhitePieces | board.BlackPieces));
            Assert(board.EmptySquares == ~board.AllPieces);
            Assert(board.Key == ZobristKeys.CalculateKey(board), "Zobrist key mismatch when checking board");
        }
    }
}
#endif