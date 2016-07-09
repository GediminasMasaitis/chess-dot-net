using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet
{
    public class BoardFactory
    {
        public ArrayBoard ParseFENToArrayBoard(string fen)
        {
            var board = new ArrayBoard();
            var boardPosition = 0;
            var fenPosition = 0;
            for (; fenPosition < fen.Length; fenPosition++)
            {
                var ch = fen[fenPosition];
                switch (ch)
                {
                    case 'p':
                        board[boardPosition] = ChessPiece.BlackPawn;
                        boardPosition++;
                        continue;
                    case 'r':
                        board[boardPosition] = ChessPiece.BlackRook;
                        boardPosition++;
                        continue;
                    case 'n':
                        board[boardPosition] = ChessPiece.BlackKnight;
                        boardPosition++;
                        continue;
                    case 'b':
                        board[boardPosition] = ChessPiece.BlackBishop;
                        boardPosition++;
                        continue;
                    case 'q':
                        board[boardPosition] = ChessPiece.BlackQueen;
                        boardPosition++;
                        continue;
                    case 'k':
                        board[boardPosition] = ChessPiece.BlackKing;
                        boardPosition++;
                        continue;

                    case 'P':
                        board[boardPosition] = ChessPiece.WhitePawn;
                        boardPosition++;
                        continue;
                    case 'R':
                        board[boardPosition] = ChessPiece.WhiteRook;
                        boardPosition++;
                        continue;
                    case 'N':
                        board[boardPosition] = ChessPiece.WhiteKnight;
                        boardPosition++;
                        continue;
                    case 'B':
                        board[boardPosition] = ChessPiece.WhiteBishop;
                        boardPosition++;
                        continue;
                    case 'Q':
                        board[boardPosition] = ChessPiece.WhiteQueen;
                        boardPosition++;
                        continue;
                    case 'K':
                        board[boardPosition] = ChessPiece.WhiteKing;
                        boardPosition++;
                        continue;
                }

                byte emptySpaces;
                if (byte.TryParse(ch.ToString(), out emptySpaces))
                {
                    boardPosition += emptySpaces;
                    continue;
                }

                if (ch == ' ')
                {
                    break;
                }

            }

            return board;
        }

        public BitBoard ParseFENToBitBoard(string fen)
        {
            var arrayBoard = ParseFENToArrayBoard(fen);
            var bitBoard = ArrayBoardToBitBoard(arrayBoard);
            return bitBoard;
        }

        public BitBoard ArrayBoardToBitBoard(ArrayBoard arrayBoard)
        {
            var bitBoard = new BitBoard();

            for (var i = 0; i < 64; i++)
            {
                var piece = arrayBoard[i];
                switch (piece)
                {
                    case ChessPiece.Empty:
                        break;
                    case ChessPiece.WhitePawn:
                        bitBoard.WhitePawns |= (ulong)1 << i;
                        break;
                    case ChessPiece.WhiteKnight:
                        bitBoard.WhiteKnights |= (ulong)1 << i;
                        break;
                    case ChessPiece.WhiteBishop:
                        bitBoard.WhiteBishops |= (ulong)1 << i;
                        break;
                    case ChessPiece.WhiteRook:
                        bitBoard.WhiteRooks |= (ulong)1 << i;
                        break;
                    case ChessPiece.WhiteQueen:
                        bitBoard.WhiteQueens |= (ulong)1 << i;
                        break;
                    case ChessPiece.WhiteKing:
                        bitBoard.WhiteKings |= (ulong)1 << i;
                        break;
                    case ChessPiece.BlackPawn:
                        bitBoard.BlackPawns |= (ulong)1 << i;
                        break;
                    case ChessPiece.BlackKnight:
                        bitBoard.BlackKnights |= (ulong)1 << i;
                        break;
                    case ChessPiece.BlackBishop:
                        bitBoard.BlackBishops |= (ulong)1 << i;
                        break;
                    case ChessPiece.BlackRook:
                        bitBoard.BlackRooks |= (ulong)1 << i;
                        break;
                    case ChessPiece.BlackQueen:
                        bitBoard.BlackQueens |= (ulong)1 << i;
                        break;
                    case ChessPiece.BlackKing:
                        bitBoard.BlackKings |= (ulong)1 << i;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(piece), piece, null);
                }
            }

            return bitBoard;
        }
    }
}
