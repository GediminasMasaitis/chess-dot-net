using System;
using System.Collections.Generic;
using ChessDotNet.Hashing;

using Bitboard = System.UInt64;
using Key = System.UInt64;
using Position = System.Byte;
using Piece = System.Byte;

namespace ChessDotNet.Data
{
    public class BoardFactory
    {
        public Board ParseFEN(string fen)
        {
            fen = fen.Trim().Replace("/",string.Empty);
            var board = new Board();
            board.ArrayBoard = new Piece[64];
            board.BitBoard = new ulong[13];
            board.CastlingPermissions = CastlingPermission.None;
            board.History2 = new UndoMove[2048];
            board.PieceCounts = new int[13];

            var boardPosition = 0;
            var fenPosition = 0;
            for (; fenPosition < fen.Length; fenPosition++)
            {
                var fixedBoardPosition = (7 - boardPosition/8)*8 + boardPosition%8;
                var ch = fen[fenPosition];
                var pieceBitBoard = 1UL << fixedBoardPosition;
                var success = TryParsePiece(ch, out var piece);
                if (success)
                {
                    board.BitBoard[piece] |= pieceBitBoard;
                    board.ArrayBoard[fixedBoardPosition] = piece;
                    board.PieceCounts[piece]++;
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

            fenPosition++;
            if (fen[fenPosition] == 'w')
            {
                board.WhiteToMove = true;
            }

            fenPosition += 2;

            for (var i = 0; i < 4; i++)
            {
                if (fenPosition >= fen.Length)
                    break;
                bool done = false;
                switch (fen[fenPosition])
                {
                    case 'K':
                        board.CastlingPermissions |= CastlingPermission.WhiteKing;
                        break;
                    case 'Q':
                        board.CastlingPermissions |= CastlingPermission.WhiteQueen;
                        break;
                    case 'k':
                        board.CastlingPermissions |= CastlingPermission.BlackKing;
                        break;
                    case 'q':
                        board.CastlingPermissions |= CastlingPermission.BlackQueen;
                        break;
                    case ' ':
                        fenPosition--;
                        done = true;
                        break;
                    case '-':
                        done = true;
                        break;
                    default:
                        throw new Exception("Unknown character in castling permissions");
                }
                fenPosition++;
                if (done)
                {
                    break;
                }
            }

            fenPosition++;
            if (fenPosition < fen.Length && fen[fenPosition] != '-')
            {
                var lower = char.ToLowerInvariant(fen[fenPosition]);
                var file = lower - 0x61;
                //var rank = fen[fenPosition] - 0x30;
                if (file >= 0 && file < 8)
                {
                    board.EnPassantFileIndex = file;
                    board.EnPassantFile = BitboardConstants.Files[file];
                }
                fenPosition++;
                board.EnPassantRankIndex = fen[fenPosition] - '0' - 1;
            }
            

            board.SyncExtraBitBoards();
            //board.SyncBitBoardsToArrayBoard();
            //board.SyncPiecesCount();
            board.SyncMaterial();
            board.Key = ZobristKeys.CalculateKey(board);
            return board;
        }

        private bool TryParsePiece(char ch, out Piece piece)
        {
            piece = default(Piece);
            switch(ch)
            {
                case 'p': piece = ChessPiece.BlackPawn;
                    return true;
                case 'P': piece = ChessPiece.WhitePawn;
                    return true;

                case 'n': piece = ChessPiece.BlackKnight;
                    return true;
                case 'N': piece = ChessPiece.WhiteKnight;
                    return true;

                case 'b': piece = ChessPiece.BlackBishop;
                    return true;
                case 'B': piece = ChessPiece.WhiteBishop;
                    return true;

                case 'r': piece = ChessPiece.BlackRook;
                    return true;
                case 'R': piece = ChessPiece.WhiteRook;
                    return true;

                case 'q': piece = ChessPiece.BlackQueen;
                    return true;
                case 'Q': piece = ChessPiece.WhiteQueen;
                    return true;

                case 'k': piece = ChessPiece.BlackKing;
                    return true;
                case 'K': piece = ChessPiece.WhiteKing;
                    return true;

                default:
                    return false;
            }
        }

        private Piece ParsePiece(char ch)
        {
            var success = TryParsePiece(ch, out var piece);
            if (!success)
            {
                throw new Exception("Failed to parse piece character");
            }
            return piece;
        }

        public Bitboard PositionsToBitBoard(IEnumerable<Position> positions)
        {
            var board = 0UL;
            foreach (var piece in positions)
            {
                board |= 1UL << piece;
            }
            return board;
        }
    }
}
