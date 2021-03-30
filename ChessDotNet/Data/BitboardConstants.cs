using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bitboard = System.UInt64;
using Key = System.UInt64;
using Position = System.Byte;
using Piece = System.Int32;

namespace ChessDotNet.Data
{
    public static class BitboardConstants
    {
        public static Bitboard AllBoard { get; private set; }
        public static Bitboard KnightSpan { get; private set; }
        public static Position KnightSpanPosition { get; private set; }
        public static Bitboard KingSpan { get; private set; }
        public static Position KingSpanPosition { get; private set; }

        public static Bitboard DiagonalSpan { get; private set; }
        public static Bitboard VerticalSpan { get; private set; }

        public static Bitboard[] PawnSpans { get; private set; }
        public static Position[] PawnPositions { get; private set; }

        public static Bitboard[] KingExtendedSpans { get; private set; }
        public static Position[] KingExtendedPositions { get; private set; }

        public static Bitboard[] PawnSupportSpans { get; private set; }
        public static Position[] PawnSupportPositions { get; private set; }

        public static Bitboard[] Files { get; private set; }
        public static Bitboard[] Ranks { get; private set; }
        public static Bitboard[] Diagonals { get; private set; }
        public static Bitboard[] Antidiagonals { get; private set; }
        public static Bitboard KingSide { get; private set; }
        public static Bitboard QueenSide { get; private set; }

        public static Bitboard WhiteQueenSideCastleMask { get; private set; }
        public static Bitboard WhiteKingSideCastleMask { get; private set; }
        public static Bitboard BlackQueenSideCastleMask { get; private set; }
        public static Bitboard BlackKingSideCastleMask { get; private set; }

        public static Bitboard WhiteQueenSideCastleAttackMask { get; private set; }
        public static Bitboard WhiteKingSideCastleAttackMask { get; private set; }
        public static Bitboard BlackKingSideCastleAttackMask { get; private set; }
        public static Bitboard BlackQueenSideCastleAttackMask { get; private set; }

        public static Bitboard[] KnightJumps { get; private set; }
        public static Bitboard[] KingJumps { get; private set; }
        //public static Bitboard[] DiagonalJumps { get; private set; }
        //public static Bitboard[] VerticalJumps { get; private set; }
        //public static Bitboard[] VerticalDiagonalJumps { get; private set; }
        public static Bitboard[,] PawnJumps { get; private set; }
        public static Bitboard[][] KingExtendedJumps { get; private set; }
        public static Bitboard[][] PawnSupportJumps { get; private set; }
        public static Bitboard[][] ColumnInFront { get; private set; }
        public static Bitboard[][] ColumnSortOfBehind { get; private set; }

        public static void Init()
        {
            AllBoard = ~0UL;

            KnightSpan = 43234889994UL;
            KnightSpanPosition = 18;

            KingSpan = 460039UL;
            KingSpanPosition = 9;

            PawnSpans = new Bitboard[2];
            PawnPositions = new Position[2];
            PawnSpans[ChessPiece.White] = 1280;
            PawnPositions[ChessPiece.White] = 1;
            PawnSpans[ChessPiece.Black] = 5;
            PawnPositions[ChessPiece.Black] = 9;

            KingExtendedSpans = new Bitboard[2];
            KingExtendedPositions = new Position[2];
            KingExtendedSpans[ChessPiece.White] = KingSpan | (1UL << 24) | (1UL << 25) | (1UL << 26);
            KingExtendedPositions[ChessPiece.White] = 9;
            KingExtendedSpans[ChessPiece.Black] = (KingSpan << 8) | (1UL << 0) | (1UL << 1) | (1UL << 2);
            KingExtendedPositions[ChessPiece.Black] = 17;
            
            PawnSupportSpans = new Bitboard[2];
            PawnSupportPositions = new Position[2];
            PawnSupportSpans[ChessPiece.White] = (1UL << 0) | (1UL << 2) | (1UL << 8) | (1UL << 10);
            PawnSupportPositions[ChessPiece.White] = 9;
            PawnSupportSpans[ChessPiece.Black] = (1UL << 0) | (1UL << 2) | (1UL << 8) | (1UL << 10);
            PawnSupportPositions[ChessPiece.Black] = 1;

            DiagonalSpan = (1UL << 0) | (1UL << 2) | (1UL << 16) | (1UL << 18);
            VerticalSpan = (1UL << 1) | (1UL << 8) | (1UL << 10) | (1UL << 17);

            var files = new Bitboard[8];
            for(var i = 0; i < 8; i++)
            {
                var file = 0UL;
                for(var j = 0; j < 8; j++)
                {
                    file |= 1UL << i << (j * 8);
                }
                files[i] = file;
            }
            Files = files;

            QueenSide = Files[0] | Files[1] | Files[2] | Files[3];
            KingSide = ~QueenSide;

            var ranks = new Bitboard[8];
            for(var i = 0; i < 8; i++)
            {
                var rank = 0UL;
                for(var j = 0; j < 8; j++)
                {
                    rank |= 1UL << (i * 8) << j;
                }

                ranks[i] = rank;
            }
            Ranks = ranks;

            Diagonals = new Bitboard[]
            {
                0x1UL,
                0x102UL,
                0x10204UL,
                0x1020408UL,
                0x102040810UL,
                0x10204081020UL,
                0x1020408102040UL,
                0x102040810204080UL,
                0x204081020408000UL,
                0x408102040800000UL,
                0x810204080000000UL,
                0x1020408000000000UL,
                0x2040800000000000UL,
                0x4080000000000000UL,
                0x8000000000000000UL
            };

            Antidiagonals = new Bitboard[]
            {
                0x80UL,
                0x8040UL,
                0x804020UL,
                0x80402010UL,
                0x8040201008UL,
                0x804020100804UL,
                0x80402010080402UL,
                0x8040201008040201UL,
                0x4020100804020100UL,
                0x2010080402010000UL,
                0x1008040201000000UL,
                0x804020100000000UL,
                0x402010000000000UL,
                0x201000000000000UL,
                0x100000000000000UL
            };

            var queenSideCastleMask = Files[1] | Files[2] | Files[3];
            var kingSideCastleMask = Files[5] | Files[6];
            WhiteQueenSideCastleMask = queenSideCastleMask & Ranks[0];
            WhiteKingSideCastleMask = kingSideCastleMask & Ranks[0];
            BlackQueenSideCastleMask = queenSideCastleMask & Ranks[7];
            BlackKingSideCastleMask = kingSideCastleMask & Ranks[7];

            var queenSideCastleAttackMask = Files[2] | Files[3] | Files[4];
            var kingSideCastleAttackMask = Files[4] | Files[5] | Files[6];
            WhiteQueenSideCastleAttackMask = queenSideCastleAttackMask & Ranks[0];
            WhiteKingSideCastleAttackMask = kingSideCastleAttackMask & Ranks[0];
            BlackQueenSideCastleAttackMask = queenSideCastleAttackMask & Ranks[7];
            BlackKingSideCastleAttackMask = kingSideCastleAttackMask & Ranks[7];

            KnightJumps = new Bitboard[64];
            KingJumps = new Bitboard[64];
            PawnJumps = new ulong[2, 64];
            //DiagonalJumps = new Bitboard[64];
            //VerticalJumps = new Bitboard[64];
            //VerticalDiagonalJumps = new Bitboard[64];
            KingExtendedJumps = new ulong[2][];
            PawnSupportJumps = new ulong[2][];
            for (int i = 0; i < 2; i++)
            {
                KingExtendedJumps[i] = new ulong[64];
                PawnSupportJumps[i] = new ulong[64];
            }

            for (Position i = 0; i < 64; i++)
            {
                KnightJumps[i] = GetAttackedByJumpingPiece(i, KnightSpan, KnightSpanPosition);
                KingJumps[i] = GetAttackedByJumpingPiece(i, KingSpan, KingSpanPosition);
                for (int j = 0; j < 2; j++)
                {
                    var pawnSpan = PawnSpans[j];
                    var pawnPosition = PawnPositions[j];
                    PawnJumps[j, i] = GetAttackedByJumpingPiece(i, pawnSpan, pawnPosition);

                    var kingExtendedSpan = KingExtendedSpans[j];
                    var kingExtendedPosition = KingExtendedPositions[j];
                    KingExtendedJumps[j][i] = GetAttackedByJumpingPiece(i, kingExtendedSpan, kingExtendedPosition);

                    var pawnSupportSpan = PawnSupportSpans[j];
                    var pawnSupportPosition = PawnSupportPositions[j];
                    PawnSupportJumps[j][i] = GetAttackedByJumpingPiece(i, pawnSupportSpan, pawnSupportPosition);
                }

                //DiagonalJumps[i] = GetAttackedByJumpingPiece(i, DiagonalSpan, 9);
                //VerticalJumps[i] = GetAttackedByJumpingPiece(i, VerticalSpan, 9);
                //VerticalDiagonalJumps[i] = GetAttackedByJumpingPiece(i, VerticalSpan | DiagonalSpan, 9);
            }

            ColumnInFront = new ulong[2][];
            ColumnSortOfBehind = new ulong[2][];
            for (int i = 0; i < 2; i++)
            {
                ColumnInFront[i] = new ulong[64];
                ColumnSortOfBehind[i] = new ulong[64];
            }

            for (int i = 0; i < 64; i++)
            {
                var col = i & 7;
                var row = i >> 3;
                var bitboard = 0UL;
                for (int j = row + 1; j < 8; j++)
                {
                    bitboard |= 1UL << (col + j * 8);
                }
                ColumnInFront[ChessPiece.White][i] = bitboard;

                bitboard = 0;
                for (int j = row - 1; j >= 0; j--)
                {
                    bitboard |= 1UL << (col + j * 8);
                }
                ColumnInFront[ChessPiece.Black][i] = bitboard;

                bitboard = 0;
                for (int j = row + 1; j >= 0; j--)
                {
                    if (j > 7)
                    {
                        continue;
                    }
                    bitboard |= 1UL << (col + j * 8);
                }
                ColumnSortOfBehind[ChessPiece.White][i] = bitboard;

                bitboard = 0;
                for (int j = row - 1; j < 8; j++)
                {
                    if (j < 0)
                    {
                        continue;
                    }
                    bitboard |= 1UL << (col + j * 8);
                }
                ColumnSortOfBehind[ChessPiece.Black][i] = bitboard;
            }
        }

        private static ulong GetAttackedByJumpingPiece(Position position, Bitboard jumpMask, Position jumpMaskCenter)
        {
            ulong jumps;
            if (position > jumpMaskCenter)
            {
                jumps = jumpMask << (position - jumpMaskCenter);
            }
            else
            {
                jumps = jumpMask >> (jumpMaskCenter - position);
            }

            jumps &= ~(position % 8 < 4 ? BitboardConstants.Files[6] | BitboardConstants.Files[7] : BitboardConstants.Files[0] | BitboardConstants.Files[1]);
            jumps &= ~((position >> 3) < 4 ? BitboardConstants.Ranks[6] | BitboardConstants.Ranks[7] : BitboardConstants.Ranks[0] | BitboardConstants.Ranks[1]);
            return jumps;
        }
    }

    public static class ChessPosition
    {
        public const Position A1 = 0;
        public const Position B1 = 1;
        public const Position C1 = 2;
        public const Position D1 = 3;
        public const Position E1 = 4;
        public const Position F1 = 5;
        public const Position G1 = 6;
        public const Position H1 = 7;

        public const Position A2 = 8;
        public const Position B2 = 9;
        public const Position C2 = 10;
        public const Position D2 = 11;
        public const Position E2 = 12;
        public const Position F2 = 13;
        public const Position G2 = 14;
        public const Position H2 = 15;

        public const Position A3 = 16;
        public const Position B3 = 17;
        public const Position C3 = 18;
        public const Position D3 = 19;
        public const Position E3 = 20;
        public const Position F3 = 21;
        public const Position G3 = 22;
        public const Position H3 = 23;

        public const Position A4 = 24;
        public const Position B4 = 25;
        public const Position C4 = 26;
        public const Position D4 = 27;
        public const Position E4 = 28;
        public const Position F4 = 29;
        public const Position G4 = 30;
        public const Position H4 = 31;

        public const Position A5 = 32;
        public const Position B5 = 33;
        public const Position C5 = 34;
        public const Position D5 = 35;
        public const Position E5 = 36;
        public const Position F5 = 37;
        public const Position G5 = 38;
        public const Position H5 = 39;

        public const Position A6 = 40;
        public const Position B6 = 41;
        public const Position C6 = 42;
        public const Position D6 = 43;
        public const Position E6 = 44;
        public const Position F6 = 45;
        public const Position G6 = 46;
        public const Position H6 = 47;

        public const Position A7 = 48;
        public const Position B7 = 49;
        public const Position C7 = 50;
        public const Position D7 = 51;
        public const Position E7 = 52;
        public const Position F7 = 53;
        public const Position G7 = 54;
        public const Position H7 = 55;

        public const Position A8 = 56;
        public const Position B8 = 57;
        public const Position C8 = 58;
        public const Position D8 = 59;
        public const Position E8 = 60;
        public const Position F8 = 61;
        public const Position G8 = 62;
        public const Position H8 = 63;
    }

    public static class ChessFile
    {
        public const Position A = 0;
        public const Position B = 1;
        public const Position C = 2;
        public const Position D = 3;
        public const Position E = 4;
        public const Position F = 5;
        public const Position G = 6;
        public const Position H = 7;
    }
}
