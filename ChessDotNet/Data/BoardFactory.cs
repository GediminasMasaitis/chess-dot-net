using System;
using System.Collections.Generic;

namespace ChessDotNet.Data
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
                var fixedBoardPosition = (7 - boardPosition/8)*8 + boardPosition%8;
                var ch = fen[fenPosition];
                switch (ch)
                {
                    case 'p':
                        board[fixedBoardPosition] = ChessPiece.BlackPawn;
                        boardPosition++;
                        continue;
                    case 'r':
                        board[fixedBoardPosition] = ChessPiece.BlackRook;
                        boardPosition++;
                        continue;
                    case 'n':
                        board[fixedBoardPosition] = ChessPiece.BlackKnight;
                        boardPosition++;
                        continue;
                    case 'b':
                        board[fixedBoardPosition] = ChessPiece.BlackBishop;
                        boardPosition++;
                        continue;
                    case 'q':
                        board[fixedBoardPosition] = ChessPiece.BlackQueen;
                        boardPosition++;
                        continue;
                    case 'k':
                        board[fixedBoardPosition] = ChessPiece.BlackKing;
                        boardPosition++;
                        continue;

                    case 'P':
                        board[fixedBoardPosition] = ChessPiece.WhitePawn;
                        boardPosition++;
                        continue;
                    case 'R':
                        board[fixedBoardPosition] = ChessPiece.WhiteRook;
                        boardPosition++;
                        continue;
                    case 'N':
                        board[fixedBoardPosition] = ChessPiece.WhiteKnight;
                        boardPosition++;
                        continue;
                    case 'B':
                        board[fixedBoardPosition] = ChessPiece.WhiteBishop;
                        boardPosition++;
                        continue;
                    case 'Q':
                        board[fixedBoardPosition] = ChessPiece.WhiteQueen;
                        boardPosition++;
                        continue;
                    case 'K':
                        board[fixedBoardPosition] = ChessPiece.WhiteKing;
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

        public ulong PiecesToBitBoard(IEnumerable<int> pieces)
        {
            var board = 0UL;
            foreach (var piece in pieces)
            {
                board |= 1UL << piece;
            }
            return board;
        }

        public BitBoards ParseFENToBitBoards(string fen)
        {
            var arrayBoard = ParseFENToArrayBoard(fen);
            var bitBoard = ArrayBoardToBitBoards(arrayBoard);
            return bitBoard;
        }

        public BitBoards ArrayBoardToBitBoards(ArrayBoard arrayBoard)
        {
            var bitBoard = new BitBoards();

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
                        bitBoard.WhiteNights |= (ulong)1 << i;
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
                        bitBoard.BlackNights |= (ulong)1 << i;
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
            bitBoard.Sync();
            return bitBoard;
        }
    }
}
