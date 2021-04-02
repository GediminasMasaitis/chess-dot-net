using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;
using ChessDotNet.Evaluation.V2;
using ChessDotNet.MoveGeneration;

using Position = System.Byte;
using Bitboard = System.UInt64;
using Piece = System.Byte;

namespace ChessDotNet.Search2
{
    public class SeeService
    {
        private readonly AttacksService _attacks;

        private readonly short[] SeeWeights;

        public SeeService(AttacksService attacks)
        {
            _attacks = attacks;

            SeeWeights = new short[ChessPiece.Count];
            SeeWeights[ChessPiece.WhitePawn] = 100;
            SeeWeights[ChessPiece.BlackPawn] = 100;

            SeeWeights[ChessPiece.WhiteKnight] = 325;
            SeeWeights[ChessPiece.BlackKnight] = 325;

            SeeWeights[ChessPiece.WhiteBishop] = 325; // 325 instead of 335, exchanging bishop for knight
            SeeWeights[ChessPiece.BlackBishop] = 325; // is not strictly "losing"

            SeeWeights[ChessPiece.WhiteRook] = 500;
            SeeWeights[ChessPiece.BlackRook] = 500;

            SeeWeights[ChessPiece.WhiteQueen] = 975;
            SeeWeights[ChessPiece.BlackQueen] = 975;

            SeeWeights[ChessPiece.WhiteKing] = 0;
            SeeWeights[ChessPiece.BlackKing] = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Bitboard GetLeastValuablePiece(Board board, Bitboard attadef, byte colorToMove, ref Piece piece)
        {
            Piece start = (byte) (ChessPiece.Pawn | colorToMove);
            Piece end = (byte)(ChessPiece.King | colorToMove);
            for (piece = start; piece <= end; piece += ChessPiece.NextPiece)
            {
                var subset = attadef & board.BitBoard[piece];
                if (subset > 0)
                {
                    //var singlePos = subset.BitScanForward();
                    //var singleBitboard =  1UL << singlePos;
                    var singleBitboard = (ulong) ((long)subset & -(long)subset);
                    return singleBitboard;
                }
            }
            return 0UL; // empty set
        }

        public void CalculateSeeScores(Board board, Move[] moves, int moveCount, int[] seeScores)
        {
            for (int i = 0; i < moveCount; i++)
            {
                var move = moves[i];
                if (move.TakesPiece != ChessPiece.Empty)
                {
                    seeScores[i] = See(board, move);
                }
                else
                {
                    seeScores[i] = 0;
                }
            }
        }

        public int See(Board board, Move move)
        {
            Debug.Assert(move.TakesPiece != ChessPiece.Empty);

            return See(board, move.From, move.To, move.Piece, move.TakesPiece);
        }

        //private int[] gain = new int[32];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int See(Board board, Position from, Position to, Piece piece, Piece takesPiece)
        {
#if NET5_0
            Span<int> gain = stackalloc int[32];
#else
            var gain = new int[32];
#endif

            var depth = 0;
            //U64 mayXray = pawns | bishops | rooks | queen;
            var fromSet = 1UL << from;
            var occ = board.AllPieces;
            var colorToMove = board.ColorToMove;
            gain[depth] = SeeWeights[takesPiece];
            do
            {
                depth++; // next depth and side
                gain[depth] = SeeWeights[piece] - gain[depth - 1]; // speculative store, if defended
                if (Math.Max(-gain[depth - 1], gain[depth]) < 0)
                {
                    break; // pruning does not influence the result
                }

                //attadef ^= fromSet; // reset bit in set to traverse
                occ ^= fromSet; // reset bit in temporary occupancy (for x-Rays)
                var attadef = _attacks.GetAttackersOf(board, to, occ) & occ;
                //if (fromSet & mayXray)
                //{
                //    attadef = _attacks.GetAttackersOf(board, to, white, occ);
                //}
                colorToMove ^= 1;
                fromSet = GetLeastValuablePiece(board, attadef, colorToMove, ref piece);
            } while (fromSet != 0);

            while (--depth != 0)
            {
                gain[depth - 1] = -Math.Max(-gain[depth - 1], gain[depth]);
            }

            return gain[0];
        }
    }
}
