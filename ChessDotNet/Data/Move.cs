using System;
using System.Runtime.CompilerServices;
using ChessDotNet.Testing;

using Bitboard = System.UInt64;
using Key = System.UInt64;
using Position = System.Byte;
using Piece = System.Byte;
using MoveKey = System.UInt32;

namespace ChessDotNet.Data
{
    public struct Move
    {
        private readonly ulong Value;
        public Move(Position from, Position to, Piece piece, Piece takesPiece = 0, bool enPassant = false, Piece? pawnPromoteTo = null)
        {
            var promote = pawnPromoteTo.HasValue ? pawnPromoteTo.Value : ChessPiece.Empty;
            ulong key = 0;
            key |= from;
            key |= (ulong)to << 8;
            key |= (ulong)piece << 16;
            key |= (ulong)takesPiece << 24;
            key |= (ulong)promote << 32;
            key |= Convert.ToUInt64(enPassant) << 40;
            key |= Convert.ToUInt64((piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing) && Math.Abs(@from - to) == 2) << 41;
            key |= Convert.ToUInt64(piece == 0) << 42;
            Value = key;

#if TEST
            Test.Assert(From >= 0);
            Test.Assert(From < 64);
            Test.Assert(To >= 0);
            Test.Assert(To < 64);
            Test.Assert(Piece >= 0);
            Test.Assert(Piece < 13);
            Test.Assert(TakesPiece >= 0);
            Test.Assert(TakesPiece < 13);
            Test.Assert(TakesPiece != 6);
            Test.Assert(TakesPiece != 12);
#endif
        }

        public Position From => (byte)(Value & 0xFF);
        public Position To => (byte)((Value >> 8) & 0xFF);
        public Piece Piece => (byte) ((Value >> 16) & 0xFF);
        public Piece TakesPiece => (byte) ((Value >> 24) & 0xFF);
        public Piece? PawnPromoteTo => (byte) ((Value >> 32) & 0xFF) != 0 ? (byte?)((Value >> 32) & 0xFF) : null;
        public bool EnPassant => ((Value >> 40) & 0x01) == 1;
        public bool Castle => ((Value >> 41) & 0x01) == 1;
        public bool NullMove => ((Value >> 42) & 0x01) == 1;

        //public int MVVLVAScore => TakesPiece > 0 ? MVVLVAScoreCalculation.Scores[Piece, TakesPiece] : 0;
        public MoveKey Key => (uint)(From << 16) + To;
        //private readonly ulong _staticKey;

        public ulong Key2
        {
            get
            {
                return Value;
                //return _staticKey;
                //return (uint) (From << 16) + To;
                //return Key;

            }
        }


        private static string PositionToText(Position position)
        {
            var rank = position / 8;
            var file = position % 8;

            var str = (char)(file + 97) + (rank + 1).ToString();
            return str;
        }

        private static Position TextToPosition(string text)
        {
            var textLower = text.ToLower();
            var file = text[0] - 97;
            var rank = text[1] - 0x31;
            var position = (Position)(rank*8 + file);
            return position;
        }

        public static Move FromPositionString(Board board, string moveText)
        {
            var from = TextToPosition(moveText.Substring(0, 2));
            var to = TextToPosition(moveText.Substring(2, 2));
            var piece = board.ArrayBoard[from];
            var takesPiece = board.ArrayBoard[to];
            var enPassant = false;
            Piece? pawnPromotesTo = null;
            var isWhite = board.WhiteToMove;
            if (piece == ChessPiece.WhitePawn || piece == ChessPiece.BlackPawn)
            {
                if (from%8 != to%8) // Must be take
                {
                    if (takesPiece == ChessPiece.Empty) // Must be en-passant
                    {
                        takesPiece = isWhite ? ChessPiece.BlackPawn : ChessPiece.WhitePawn;
                        enPassant = true;
                    }
                }
            }

            if (moveText.Length == 5)
            {
                switch (moveText[4])
                {
                    case 'Q':
                        pawnPromotesTo = isWhite ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
                        break;
                    case 'N':
                        pawnPromotesTo = isWhite ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight;
                        break;
                    case 'B':
                        pawnPromotesTo = isWhite ? ChessPiece.WhitePawn : ChessPiece.BlackPawn;
                        break;
                    case 'R':
                        pawnPromotesTo = isWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook;
                        break;
                }
            }

            var move = new Move(from, to, piece, takesPiece, enPassant, pawnPromotesTo);

            return move;
        }

        public string ToPositionString()
        {
            var text = PositionToText(From) + PositionToText(To);
            if (PawnPromoteTo.HasValue)
            {
                char promotionLetter;
                switch (PawnPromoteTo.Value)
                {
                    case ChessPiece.WhiteKnight:
                    case ChessPiece.BlackKnight:
                        promotionLetter = 'N';
                        break;
                    case ChessPiece.WhiteBishop:
                    case ChessPiece.BlackBishop:
                        promotionLetter = 'B';
                        break;
                    case ChessPiece.WhiteRook:
                    case ChessPiece.BlackRook:
                        promotionLetter = 'R';
                        break;
                    case ChessPiece.WhiteQueen:
                    case ChessPiece.BlackQueen:
                        promotionLetter = 'Q';
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(PawnPromoteTo), PawnPromoteTo, "Invalid pawn promotion");
                }
                text += promotionLetter;
            }
            return text;
        }

        public override string ToString()
        {
            var text = ToPositionString();
            return $"{text}; From: {From}, To: {To}, Piece: {Piece}, TakesPiece: {TakesPiece}, EnPassant: {EnPassant}, PawnPromoteTo: {PawnPromoteTo}";
        }
    }

    public struct Move1
    {
        public Move1(Position from, Position to, Piece piece, Piece takesPiece = 0, bool enPassant = false, Piece? pawnPromoteTo = null)
        {
            From = from;
            To = to;
            Piece = piece;
            TakesPiece = takesPiece;
            EnPassant = enPassant;
            PawnPromoteTo = pawnPromoteTo;

            Castle = (piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing) && Math.Abs(from - to) == 2;
            NullMove = piece == 0;
            //_staticKey = (uint) (From << 16) + To;

#if TEST
            Test.Assert(From >= 0);
            Test.Assert(From < 64);
            Test.Assert(To >= 0);
            Test.Assert(To < 64);
            Test.Assert(Piece >= 0);
            Test.Assert(Piece < 13);
            Test.Assert(TakesPiece >= 0);
            Test.Assert(TakesPiece < 13);
            Test.Assert(TakesPiece != 6);
            Test.Assert(TakesPiece != 12);
#endif
        }

        public readonly Position From;// { get; }
        public readonly Position To;// { get; }
        public readonly Piece Piece;// { get; }
        public readonly Piece TakesPiece;// { get; }
        public readonly Piece? PawnPromoteTo;// { get; }
        public readonly bool EnPassant;// { get; }
        public readonly bool Castle;// { get; }
        public readonly bool NullMove;// { get; }

        //public int MVVLVAScore => TakesPiece > 0 ? MVVLVAScoreCalculation.Scores[Piece, TakesPiece] : 0;
        public MoveKey Key => (uint)(From << 16) + To;
        //private readonly ulong _staticKey;

        public ulong Key2
        {
            get
            {
                //return _staticKey;
                //return (uint) (From << 16) + To;
                //return Key;
                var promote = PawnPromoteTo.HasValue ? PawnPromoteTo.Value : ChessPiece.Empty;
                ulong key = 0;
                key |= From;
                key |= (ulong)(To << 8);
                key |= (ulong)(Piece << 16);
                key |= (ulong)(TakesPiece << 24);
                key |= (ulong)(promote << 32);
                key |= (ulong)(Convert.ToByte(EnPassant) << 40);
                key |= (ulong)(Convert.ToByte(Castle) << 41);
                key |= (ulong)(Convert.ToByte(NullMove) << 42);
                return key;
            }
        }


        private static string PositionToText(Position position)
        {
            var rank = position / 8;
            var file = position % 8;

            var str = (char)(file + 97) + (rank + 1).ToString();
            return str;
        }

        private static Position TextToPosition(string text)
        {
            var textLower = text.ToLower();
            var file = text[0] - 97;
            var rank = text[1] - 0x31;
            var position = (Position)(rank * 8 + file);
            return position;
        }

        public static Move FromPositionString(Board board, string moveText)
        {
            var from = TextToPosition(moveText.Substring(0, 2));
            var to = TextToPosition(moveText.Substring(2, 2));
            var piece = board.ArrayBoard[from];
            var takesPiece = board.ArrayBoard[to];
            var enPassant = false;
            Piece? pawnPromotesTo = null;
            var isWhite = board.WhiteToMove;
            if (piece == ChessPiece.WhitePawn || piece == ChessPiece.BlackPawn)
            {
                if (from % 8 != to % 8) // Must be take
                {
                    if (takesPiece == ChessPiece.Empty) // Must be en-passant
                    {
                        takesPiece = isWhite ? ChessPiece.BlackPawn : ChessPiece.WhitePawn;
                        enPassant = true;
                    }
                }
            }

            if (moveText.Length == 5)
            {
                switch (moveText[4])
                {
                    case 'Q':
                        pawnPromotesTo = isWhite ? ChessPiece.WhiteQueen : ChessPiece.BlackQueen;
                        break;
                    case 'N':
                        pawnPromotesTo = isWhite ? ChessPiece.WhiteKnight : ChessPiece.BlackKnight;
                        break;
                    case 'B':
                        pawnPromotesTo = isWhite ? ChessPiece.WhitePawn : ChessPiece.BlackPawn;
                        break;
                    case 'R':
                        pawnPromotesTo = isWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook;
                        break;
                }
            }

            var move = new Move(from, to, piece, takesPiece, enPassant, pawnPromotesTo);

            return move;
        }

        public string ToPositionString()
        {
            var text = PositionToText(From) + PositionToText(To);
            if (PawnPromoteTo.HasValue)
            {
                char promotionLetter;
                switch (PawnPromoteTo.Value)
                {
                    case ChessPiece.WhiteKnight:
                    case ChessPiece.BlackKnight:
                        promotionLetter = 'N';
                        break;
                    case ChessPiece.WhiteBishop:
                    case ChessPiece.BlackBishop:
                        promotionLetter = 'B';
                        break;
                    case ChessPiece.WhiteRook:
                    case ChessPiece.BlackRook:
                        promotionLetter = 'R';
                        break;
                    case ChessPiece.WhiteQueen:
                    case ChessPiece.BlackQueen:
                        promotionLetter = 'Q';
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(PawnPromoteTo), PawnPromoteTo, "Invalid pawn promotion");
                }
                text += promotionLetter;
            }
            return text;
        }

        public override string ToString()
        {
            var text = ToPositionString();
            return $"{text}; From: {From}, To: {To}, Piece: {Piece}, TakesPiece: {TakesPiece}, EnPassant: {EnPassant}, PawnPromoteTo: {PawnPromoteTo}";
        }
    }
}