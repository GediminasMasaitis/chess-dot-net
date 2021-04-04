using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

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

        private const ushort SIMD_WIDTH = 256;
        private const byte RegisterCount = 16;
        private const ushort TileHeight = (RegisterCount * SIMD_WIDTH / 16);

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

        private unsafe struct IndexList2
        {
            public uint size;
            public fixed uint values[30];
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
            Span<uint> input_mask = stackalloc uint[(int)FtOutDims / (8 * sizeof(uint))];
            Span<uint> hidden1_mask = stackalloc uint[8 / sizeof(uint)];
            Span<uint> fake = stackalloc uint[0];
            var buf = new NetData();

            int outValue;
            if(Avx2.IsSupported)
            {
                TransformAvx2(pos, buf.input, ref input_mask);
                AffineTransformAvx2(buf.input, buf.hidden1_out, FtOutDims, _parameters.Hidden1.Biases, _parameters.Hidden1.Weights, ref input_mask, ref hidden1_mask, true);
                AffineTransformAvx2(buf.hidden1_out, buf.hidden2_out, 32, _parameters.Hidden2.Biases, _parameters.Hidden2.Weights, ref hidden1_mask, ref fake, false);
                outValue = AffinePropagateAvx2(buf.hidden2_out, _parameters.Output.Biases, _parameters.Output.Weights);
            }
            else
            {
                Transform(pos, buf.input);
                AffineTransform(buf.input, buf.hidden1_out, FtOutDims, 32, _parameters.Hidden1.Biases, _parameters.Hidden1.Weights);
                AffineTransform(buf.hidden1_out, buf.hidden2_out, 32, 32, _parameters.Hidden2.Biases, _parameters.Hidden2.Weights);
                outValue = AffinePropagate(buf.hidden2_out, _parameters.Output.Biases, _parameters.Output.Weights);
            }
            var result = outValue / 16;
            return result;
        }

        private void AffineTransform
        (
            sbyte[] input,
            sbyte[] output,
            uint inDims,
            uint outDims,
            int[] biases,
            sbyte[] weights
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

        private unsafe void AffineTransformAvx2
        (
            sbyte[] input,
            sbyte[] output,
            uint inDims,
            //uint outDims,
            int[] biases,
            sbyte[] weights,
            ref Span<uint> inMask,
            ref Span<uint> outMask,
            bool packAndGetMask
        )
        {
            Vector256<sbyte> kZeroBytes = Vector256<sbyte>.Zero;
            Vector256<short> kZeroShort = Vector256<short>.Zero;
            fixed (int* biasesPtr = biases)
            fixed (sbyte* weightsPtr = weights)
            fixed (uint* inMaskPtr = inMask)
            fixed (sbyte* outputPtr = output)
            {
                var out0 = ((Vector256<int>*)biasesPtr)[0];
                var out1 = ((Vector256<int>*)biasesPtr)[1];
                var out2 = ((Vector256<int>*)biasesPtr)[2];
                var out4 = ((Vector256<int>*)biasesPtr)[3];

                ulong v = *(ulong*)inMaskPtr;
                for (uint offset = 0; offset < inDims;)
                {
                    if (!next_idx(out uint idx, ref offset, ref v, ref inMask, inDims))
                    {
                        break;
                    }

                    Vector256<sbyte> first = ((Vector256<sbyte>*) weightsPtr)[idx];
                    Vector256<sbyte> second;

                    short factor = (short)input[idx];
                    
                    if (next_idx(out idx, ref offset, ref v, ref inMask, inDims))
                    {
                        second = ((Vector256<sbyte>*) weightsPtr)[idx];
                        factor |= (short)(input[idx] << 8);
                    }
                    else
                    {
                        second = kZeroBytes;
                    }

                    Vector256<byte> multiply = Vector256.Create(factor).AsByte();
                    Vector256<sbyte> unpacked = Avx2.UnpackLow(first, second);
                    Vector256<short> product = Avx2.MultiplyAddAdjacent(multiply, unpacked);
                    Vector256<short> signs = Avx2.CompareGreaterThan(kZeroShort, product);
                    out0 = Avx2.Add(out0, Avx2.UnpackLow(product, signs).AsInt32());
                    out1 = Avx2.Add(out1, Avx2.UnpackHigh(product, signs).AsInt32());
                    unpacked = Avx2.UnpackHigh(first, second);
                    product = Avx2.MultiplyAddAdjacent(multiply, unpacked);
                    signs = Avx2.CompareGreaterThan(kZeroShort, product);
                    out2 = Avx2.Add(out2, Avx2.UnpackLow(product, signs).AsInt32());
                    out4 = Avx2.Add(out4, Avx2.UnpackHigh(product, signs).AsInt32());
                }

                Vector256<short> result0 = Avx2.ShiftRightArithmetic(Avx2.PackSignedSaturate(out0, out1), SHIFT);
                Vector256<short> result1 = Avx2.ShiftRightArithmetic(Avx2.PackSignedSaturate(out2, out4), SHIFT);

                var outVec = (Vector256<sbyte>*) outputPtr;
                outVec[0] = Avx2.PackSignedSaturate(result0, result1);
                if (packAndGetMask)
                {
                    outMask[0] = (uint) Avx2.MoveMask(Avx2.CompareGreaterThan(outVec[0], kZeroBytes));
                }
                else
                {
                    outVec[0] = Avx2.Max(outVec[0], kZeroBytes);
                }
            }
        }

        bool next_idx(out uint idx, ref uint offset, ref ulong v, ref Span<uint> mask, uint inDims)
        {
            while (v == 0)
            {
                offset += 8 * sizeof(ulong);
                if (offset >= inDims)
                {
                    idx = default;
                    return false;
                }

                var offs = (int)offset / 32 + 1;
                var offs2 = (int)offset / 32;
                v = (ulong)mask[offs] << 32 | mask[offs2];
                //memcpy(v, (char*)mask + (*offset / 8), sizeof(mask2_t));
            }
            idx = offset + v.BitScanForward();
            v &= v - 1;
            return true;
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

        private unsafe int AffinePropagateAvx2(sbyte[] input, int[] biases, sbyte[] weights)
        {
            fixed (sbyte* inputPtr = input)
            fixed (sbyte* weightsPtr = weights)
            {
                var iv = (Vector256<byte>*)inputPtr;
                var row = (Vector256<sbyte>*)weightsPtr;
                var prod1 = Avx2.MultiplyAddAdjacent(iv[0], row[0]);
                var prod = Avx2.MultiplyAddAdjacent(prod1, Vector256.Create((short) 1));
                var sum = Sse2.Add(prod.GetLower(), Avx2.ExtractVector128(prod, 1));
                sum = Sse2.Add(sum, Sse2.Shuffle(sum, 0x1b));
                var result = Sse2.ConvertToInt32(sum) + Sse41.Extract(sum, 1) + biases[0];
                return result;
            }
        }

        private void Transform(NnuePosition pos, sbyte[] output)
        {
            //if (!UpdateAccumulator(pos))
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

        private unsafe void TransformAvx2(NnuePosition pos, sbyte[] output, ref Span<uint> outMask)
        {
            RefreshAccumulatorAvx2(pos);
            var accumulation = pos.nnue[0].accumulator.accumulation;
            var perspectives = new int[] { pos.player, pos.player ^ 1 };
            var outputMaskIndex = 0;
            for (uint perspective = 0; perspective < 2; perspective++)
            {
                var offset = kHalfDimensions * perspective;
                const uint numChunks = (16 * kHalfDimensions) / SIMD_WIDTH;
                fixed (sbyte* outputPtr = output)
                fixed (short* accumulationsPtr = accumulation[perspectives[perspective]])
                {
                    var outEntry = (Vector256<sbyte>*) &outputPtr[offset];
                    for (uint i = 0; i < numChunks / 2; i++)
                    {
                        var s0 = ((Vector256<short>*) accumulationsPtr)[i * 2];
                        var s1 = ((Vector256<short>*) accumulationsPtr)[i * 2 + 1];
                        outEntry[i] = Avx2.PackSignedSaturate(s0, s1);
                        var x = outEntry[i];
                        outMask[outputMaskIndex++] = (uint)Avx2.MoveMask(Avx2.CompareGreaterThan(outEntry[i], Vector256<sbyte>.Zero));
                    }
                }
            }
        }

        private unsafe void RefreshAccumulator(NnuePosition pos)
        {
            var accumulator = pos.nnue[0].accumulator;
            Span<IndexList2> activeIndices = stackalloc IndexList2[2];
            AppendActiveIndices(pos, activeIndices);

            for (int color = 0; color < 2; color++)
            {
                Array.Copy(_parameters.FeatureTransformer.Biases, accumulator.accumulation[color], kHalfDimensions);

                for (uint i = 0; i < activeIndices[color].size; i++)
                {
                    uint index = activeIndices[color].values[i];
                    uint offset = kHalfDimensions * index;

                    for (uint j = 0; j < kHalfDimensions; j++)
                    {
                        accumulator.accumulation[color][j] += _parameters.FeatureTransformer.Weights[offset + j];
                    }
                }
            }

            accumulator.computedAccumulation = 1;
        }

        private unsafe void RefreshAccumulatorAvx2(NnuePosition pos)
        {
            var accumulator = pos.nnue[0].accumulator;
            Span<IndexList2> activeIndices = stackalloc IndexList2[2];
            AppendActiveIndices(pos, activeIndices);
            var acc = stackalloc Vector256<short>[RegisterCount];
            for (int color = 0; color < 2; color++)
            {
                for (uint i = 0; i < kHalfDimensions / TileHeight; i++)
                {
                    fixed (short* biasPtr = _parameters.FeatureTransformer.Biases)
                    fixed (short* weightsPtr = _parameters.FeatureTransformer.Weights)
                    fixed (short* accumulationsPtr = accumulator.accumulation[color])
                    {
                        var biasesTile = (Vector256<short>*)(&biasPtr[i * TileHeight]);
                        var accTile = (Vector256<short>*)(&accumulationsPtr[i * TileHeight]);
                        for (var j = 0; j < RegisterCount; j++)
                        {
                            acc[j] = biasesTile[j];
                        }

                        for (var j = 0; j < activeIndices[color].size; j++)
                        {
                            uint index = activeIndices[color].values[j];
                            uint offset = kHalfDimensions * index + i * TileHeight;
                            Vector256<short>* column = (Vector256<short>*)&weightsPtr[offset];

                            for (uint k = 0; k < RegisterCount; k++)
                            {
                                acc[k] = Avx2.Add(acc[k], column[k]);
                            }
                        }

                        for (uint j = 0; j < RegisterCount; j++)
                        {
                            accTile[j] = acc[j];
                        }
                    }
                }
            }
            accumulator.computedAccumulation = 1;
        }

        private bool UpdateAccumulator(NnuePosition pos)
        {
            return false;
        }

        private void AppendActiveIndices(NnuePosition pos, Span<IndexList2> active)
        {
            for (int c = 0; c < 2; c++)
            {
                HalfKpAppendActiveIndices(pos, c, ref active[c]);
            }
        }

        private unsafe void HalfKpAppendActiveIndices(NnuePosition pos, int c, ref IndexList2 active)
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