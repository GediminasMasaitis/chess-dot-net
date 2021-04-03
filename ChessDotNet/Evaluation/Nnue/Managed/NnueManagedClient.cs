using System;
using System.Runtime.CompilerServices;

namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public class NnueManagedClient : INnueClient
    {
        private readonly HalfKpParameters _parameters;
        public bool RequiresManagedData => true;

        private const byte white = 0;
        private const byte black = 1;

        private const byte blank = 0;
        private const byte wking = 1;
        private const byte wqueen = 2;
        private const byte wrook = 3;
        private const byte wbishop = 4;
        private const byte wknight = 5;
        private const byte wpawn = 6;
        private const byte bking = 7;
        private const byte bqueen = 8;
        private const byte brook = 9;
        private const byte bbishop = 10;
        private const byte bknight = 11;
        private const byte bpawn = 12;

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

        private const uint FV_SCALE = 16;
        private const int SHIFT = 6;
        private const uint kHalfDimensions = 256;
        private const uint FtInDims = 64 * PS_END;
        private const uint FtOutDims = kHalfDimensions * 2;

        public NnueManagedClient(HalfKpParameters parameters)
        {
            _parameters = parameters;
        }

        private class NetData
        {
            public sbyte[] input = new sbyte[FtOutDims];
            public sbyte[] hidden1_out = new sbyte[32];
            public sbyte[] hidden2_out = new sbyte[32];
        }

        private class IndexList
        {
            public uint size;
            public uint[] values = new uint[30];
        }
        int orient(int c, int s)
        {
            return s ^ (c == white ? 0x00 : 0x3f);
        }
        uint make_index(int c, int s, int pc, int ksq)
        {
            return (uint)(orient(c, s) + PieceToIndex[c][pc] + PS_END * ksq);
        }

        public int Evaluate(NnuePosition pos)
        {
            var input_mask = new uint[FtOutDims / (8 * sizeof(uint))];
            var hidden1_mask = new uint[8 / sizeof(uint)];
            var buf = new NetData();

            Transform(pos, buf.input, input_mask);
            AffineTxfm(buf.input, buf.hidden1_out, FtOutDims, 32, _parameters.Hidden1.Biases, _parameters.Hidden1.Weights, input_mask, hidden1_mask, true);
            AffineTxfm(buf.hidden1_out, buf.hidden2_out, 32, 32, _parameters.Hidden2.Biases, _parameters.Hidden2.Weights, hidden1_mask, null, false);
            var outValue = AffinePropagate(buf.hidden2_out, _parameters.Output.Biases, _parameters.Output.Weights);
            var result = outValue / 16;
            return result;
        }

        private void AffineTxfm
        (
            sbyte[] input,
            sbyte[] output,
            uint inDims,
            uint outDims,
            int[] biases,
            sbyte[] weights,
            uint[] inMask,
            uint[] outMask,
            bool pack8_and_calc_mask
        )
        {
            var tmp = new int[outDims];

            for (uint i = 0; i < outDims; i++)
            {
                tmp[i] = biases[i];
            }

            for (uint idx = 0; idx < inDims; idx++)
            {
                var inputVal = input[idx];
                if (inputVal != 0)
                {
                    for (uint i = 0; i < outDims; i++)
                    {
                        var weight = weights[(outDims * idx) + i];
                        var product = (sbyte)inputVal * weight;
                        tmp[i] += product;
                    }
                }
            }

            var outVec = output;
            for (uint i = 0; i < outDims; i++)
            {
                outVec[i] = (sbyte)Clamp(tmp[i] >> SHIFT, 0, 127);
            }
        }

        private int AffinePropagate(sbyte[] input, int[] biases, sbyte[] weights)
        {
            var sum = biases[0];
            for (var j = 0; j < 32; j++)
            {
                sum += weights[j] * input[j];
            }
            return sum;
        }

        private void Transform(NnuePosition pos, sbyte[] output, uint[] outMask)
        {
            if (!UpdateAccumulator(pos))
            {
                RefreshAccumulator(pos);
            }

            var accumulation = pos.nnue[0].accumulator.accumulation;
            var perspectives = new int[] { pos.player, pos.player ^ 1 };
            for (uint p = 0; p < 2; p++)
            {
                var offset = kHalfDimensions * p;
                for (uint i = 0; i < kHalfDimensions; i++)
                {
                    short sum = accumulation[perspectives[p]][i];
                    output[offset + i] = (sbyte)Clamp((int)sum, 0, 127);
                }
            }
        }

        private bool UpdateAccumulator(NnuePosition pos)
        {
            return false;
        }

        private void RefreshAccumulator(NnuePosition pos)
        {
            var accumulator = pos.nnue[0].accumulator;
            var activeIndices = new IndexList[2];
            for (int i = 0; i < 2; i++)
            {
                activeIndices[i] = new IndexList();
                activeIndices[i].size = 0;
            }
            AppendActiveIndices(pos, activeIndices);

            for (uint c = 0; c < 2; c++)
            {
                //memcpy(accumulator.accumulation[c], ft_biases, kHalfDimensions * sizeof(int16_t));
                Array.Copy(_parameters.FeatureTransformer.Biases, accumulator.accumulation[c], kHalfDimensions);

                for (uint k = 0; k < activeIndices[c].size; k++)
                {
                    uint index = activeIndices[c].values[k];
                    uint offset = kHalfDimensions * index;

                    for (uint j = 0; j < kHalfDimensions; j++)
                    {
                        accumulator.accumulation[c][j] += _parameters.FeatureTransformer.Weights[offset + j]; //ft_weights[offset + j];
                    }

                }
            }
        }

        private void AppendActiveIndices(NnuePosition pos, IndexList[] active)
        {
            for (int c = 0; c < 2; c++)
            {
                HalfKpAppendActiveIndices(pos, c, active[c]);
            }
        }

        private void HalfKpAppendActiveIndices(NnuePosition pos, int c, IndexList active)
        {
            int ksq = pos.squares[c];
            ksq = orient(c, ksq);
            for (int i = 2; pos.pieces[i] != 0; i++)
            {
                int sq = pos.squares[i];
                int pc = pos.pieces[i];
                active.values[active.size++] = make_index(c, sq, pc, ksq);
            }
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private short Clamp(short a, short b, short c)
        //{
        //    return a < b ? b : a > c ? c : a;
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Clamp(int a, int b, int c)
        {
            return a < b ? b : a > c ? c : a;
        }
    }
}