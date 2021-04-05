using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public abstract class NnueImplBase : INnueClient
    {
        private const uint PS_W_PAWN = 1;
        private const uint PS_B_PAWN = 1 * 64 + 1;
        private const uint PS_W_KNIGHT = 2 * 64 + 1;
        private const uint PS_B_KNIGHT = 3 * 64 + 1;
        private const uint PS_W_BISHOP = 4 * 64 + 1;
        private const uint PS_B_BISHOP = 5 * 64 + 1;
        private const uint PS_W_ROOK = 6 * 64 + 1;
        private const uint PS_B_ROOK = 7 * 64 + 1;
        private const uint PS_W_QUEEN = 8 * 64 + 1;
        private const uint PS_B_QUEEN = 9 * 64 + 1;
        private const uint PS_END = 10 * 64 + 1;

        private static readonly uint[][] PieceToIndex = new uint[][]
        {
            new uint[] { 0, 0, PS_W_QUEEN, PS_W_ROOK, PS_W_BISHOP, PS_W_KNIGHT, PS_W_PAWN, 0, PS_B_QUEEN, PS_B_ROOK, PS_B_BISHOP, PS_B_KNIGHT, PS_B_PAWN, 0},
            new uint[] { 0, 0, PS_B_QUEEN, PS_B_ROOK, PS_B_BISHOP, PS_B_KNIGHT, PS_B_PAWN, 0, PS_W_QUEEN, PS_W_ROOK, PS_W_BISHOP, PS_W_KNIGHT, PS_W_PAWN, 0}
        };

        protected const uint FV_SCALE = 16;
        protected const int SHIFT = 6;
        protected const int kHalfDimensions = 256;
        protected const uint FtInDims = 64 * PS_END;
        protected const int FtOutDims = kHalfDimensions * 2;


        protected unsafe struct IndexList2
        {
            public uint size;
            public fixed uint values[30];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected int Clamp(int a, int b, int c)
        {
            return a < b ? b : a > c ? c : a;
        }

        private byte GetKing(byte color)
        {
            return color == NnueConstants.white ? NnueConstants.wking : NnueConstants.bking;
        }

        private bool IsKing(byte piece)
        {
            return piece == NnueConstants.wking || piece == NnueConstants.bking;
        }

        protected void AppendActiveIndices(NnuePosition pos, Span<IndexList2> active)
        {
            for (int c = 0; c < 2; c++)
            {
                HalfKpAppendActiveIndices(pos, c, ref active[c]);
            }
        }

        protected void AppendChangedIndices(NnuePosition pos, Span<IndexList2> removed, Span<IndexList2> added, Span<bool> reset)
        {
            var dp = pos.Nnue[0].dirtyPiece;
            Debug.Assert(dp.dirtyNum != 0);

            if (pos.Nnue[1].accumulator.computedAccumulation)
            {
                for (byte color = 0; color < 2; color++)
                {
                    reset[color] = dp.pc[0] == GetKing(color);
                    if (reset[color])
                    {
                        HalfKpAppendActiveIndices(pos, color, ref added[color]);
                    }
                    else
                    {
                        HaldKpAppendChangedIndices(pos, color, dp, ref removed[color], ref added[color]);
                    }
                }
            }
            else
            {
                var dp2 = pos.Nnue[1].dirtyPiece;
                for (byte c = 0; c < 2; c++)
                {
                    reset[c] = dp.pc[0] == GetKing(c) || dp2.pc[0] == GetKing(c);
                    if (reset[c])
                    {
                        HalfKpAppendActiveIndices(pos, c, ref added[c]);
                    }
                    else
                    {
                        HaldKpAppendChangedIndices(pos, c, dp, ref removed[c], ref added[c]);
                        HaldKpAppendChangedIndices(pos, c, dp2, ref removed[c], ref added[c]);
                    }
                }
            }
        }

        private unsafe void HalfKpAppendActiveIndices(NnuePosition pos, int c, ref IndexList2 active)
        {
            int ksq = pos.Squares[c];
            ksq = orient(c, ksq);
            for (int i = 2; pos.Pieces[i] != 0; i++)
            {
                int sq = pos.Squares[i];
                int pc = pos.Pieces[i];
                active.values[active.size++] = make_index(c, sq, pc, ksq);
            }
        }

        private unsafe void HaldKpAppendChangedIndices(NnuePosition pos, byte color, NnueDirtyPiece dirtyPiece, ref IndexList2 removed, ref IndexList2 added)
        {
            int ksq = pos.Squares[color];
            ksq = orient(color, ksq);
            for (int i = 0; i < dirtyPiece.dirtyNum; i++)
            {
                var pc = dirtyPiece.pc[i];
                if (IsKing(pc))
                {
                    continue;
                }

                if (dirtyPiece.from[i] != 64)
                {
                    removed.values[removed.size++] = make_index(color, dirtyPiece.from[i], pc, ksq);
                }

                if (dirtyPiece.to[i] != 64)
                {
                    added.values[added.size++] = make_index(color, dirtyPiece.to[i], pc, ksq);
                }
            }
        }

        private int orient(int c, int s)
        {
            return s ^ (c == NnueConstants.white ? 0x00 : 0x3f);
        }

        private uint make_index(int c, int s, int pc, int ksq)
        {
            return (uint)(orient(c, s) + PieceToIndex[c][pc] + PS_END * ksq);
        }

        public bool RequiresManagedData => true;
        public abstract int Evaluate(NnuePosition position);
    }
}