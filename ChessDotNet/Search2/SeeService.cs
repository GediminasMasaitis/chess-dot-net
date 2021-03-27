using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;
using ChessDotNet.MoveGeneration;

using Position = System.Byte;
using Bitboard = System.UInt64;
using Piece = System.Byte;

namespace ChessDotNet.Search2
{
    public class SeeService
    {
        private readonly AttacksService _attacks;

        public SeeService(AttacksService attacks)
        {
            _attacks = attacks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Bitboard GetLeastValuablePiece(Board board, Bitboard attadef, bool whiteToMove, ref Piece piece)
        {
            Piece start = whiteToMove ? ChessPiece.WhitePawn : ChessPiece.BlackPawn;
            Piece end = whiteToMove ? ChessPiece.WhiteKing : ChessPiece.BlackKing;
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
            var whiteToMove = board.WhiteToMove;
            gain[depth] = EvaluationService.Weights[takesPiece];
            do
            {
                depth++; // next depth and side
                gain[depth] = EvaluationService.Weights[piece] - gain[depth - 1]; // speculative store, if defended
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
                whiteToMove = !whiteToMove;
                fromSet = GetLeastValuablePiece(board, attadef, whiteToMove, ref piece);
            } while (fromSet != 0);

            while (--depth != 0)
            {
                gain[depth - 1] = -Math.Max(-gain[depth - 1], gain[depth]);
            }

            return gain[0];
        }
    }
}
