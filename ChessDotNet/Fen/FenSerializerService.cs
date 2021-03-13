using System;
using System.Collections.Generic;
using System.Text;
using ChessDotNet.Data;


using Bitboard = System.UInt64;
using ZobristKey = System.UInt64;
using Position = System.Byte;
using Piece = System.Byte;

namespace ChessDotNet.Fen
{
    public class FenSerializerService
    {
        public string SerializeToFen(Board board)
        {
            var builder = new StringBuilder();
            for (int i = 7; i >= 0; i--)
            {
                var pawns = 0;
                for (int j = 0; j < 8; j++)
                {
                    var index = i * 8 + j;
                    Piece piece = board.ArrayBoard[index];
                    var ch = PieceToChar(piece);
                    if (ch == '\0')
                    {
                        pawns++;
                    }
                    else
                    {
                        if (pawns != 0)
                        {
                            builder.Append(pawns);
                            pawns = 0;
                        }
                        builder.Append(ch);
                    }
                }
                if (pawns != 0)
                {
                    builder.Append(pawns);
                }
                if (i != 0)
                {
                    builder.Append("/");
                }
            }

            builder.Append(" ");
            builder.Append(board.WhiteToMove ? 'w' : 'b');
            builder.Append(" ");
            if (board.CastlingPermissions == CastlingPermission.None)
            {
                builder.Append("-");
            }
            else
            {
                if ((board.CastlingPermissions & CastlingPermission.WhiteKing) != CastlingPermission.None)
                {
                    builder.Append("K");
                }
                if ((board.CastlingPermissions & CastlingPermission.WhiteQueen) != CastlingPermission.None)
                {
                    builder.Append("Q");
                }
                if ((board.CastlingPermissions & CastlingPermission.BlackKing) != CastlingPermission.None)
                {
                    builder.Append("k");
                }
                if ((board.CastlingPermissions & CastlingPermission.BlackQueen) != CastlingPermission.None)
                {
                    builder.Append("q");
                }
            }
            builder.Append(" ");
            if (board.EnPassantFileIndex == -1)
            {
                builder.Append("-");
            }
            else
            {
                var fileLetter = (char)('a' + board.EnPassantFileIndex);
                builder.Append(fileLetter);
                builder.Append(board.EnPassantRankIndex + 1);
            }

            var fen = builder.ToString();
            return fen;
        }

        private char PieceToChar(Piece piece)
        {
            switch (piece)
            {
                case ChessPiece.Empty: return '\0';
                case ChessPiece.WhitePawn: return 'P';
                case ChessPiece.WhiteKnight: return 'N';
                case ChessPiece.WhiteBishop: return 'B';
                case ChessPiece.WhiteRook: return 'R';
                case ChessPiece.WhiteQueen: return 'Q';
                case ChessPiece.WhiteKing: return 'K';
                case ChessPiece.BlackPawn: return 'p';
                case ChessPiece.BlackKnight: return 'n';
                case ChessPiece.BlackBishop: return 'b';
                case ChessPiece.BlackRook: return 'r';
                case ChessPiece.BlackQueen: return 'q';
                case ChessPiece.BlackKing: return 'k';
                default: throw new ArgumentOutOfRangeException(nameof(piece), piece, null);
            }
        }
    }
}
