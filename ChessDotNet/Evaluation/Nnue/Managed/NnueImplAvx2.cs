using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public class NnueImplAvx2 : NnueImplBase
    {
        private const ushort SimdWidth = 256;
        private const byte RegisterCount = 16;
        private const ushort TileHeight = (RegisterCount * SimdWidth / 16);

        private readonly HalfKpParameters _parameters;

        public NnueImplAvx2(HalfKpParameters parameters)
        {
            _parameters = parameters;
        }

        public override int Evaluate(NnuePosition pos)
        {
            //pos.nnue[0].accumulator.computedAccumulation = false;
            //pos.nnue[1].accumulator.computedAccumulation = false;
            //pos.nnue[2].accumulator.computedAccumulation = false;

            Span<uint> inputMask = stackalloc uint[FtOutDims / (8 * sizeof(uint))];
            Span<uint> hidden1Mask = stackalloc uint[8 / sizeof(uint)];
            Span<uint> fake = stackalloc uint[0];

            Span<sbyte> input = stackalloc sbyte[FtOutDims];
            Span<sbyte> hidden1Out = stackalloc sbyte[32];
            Span<sbyte> hidden2Out = stackalloc sbyte[32];
        
            TransformAvx2(pos, input, inputMask);
            AffineTransformAvx2(input, hidden1Out, FtOutDims, _parameters.Hidden1.Biases, _parameters.Hidden1.Weights, inputMask, hidden1Mask, true);
            AffineTransformAvx2(hidden1Out, hidden2Out, 32, _parameters.Hidden2.Biases, _parameters.Hidden2.Weights, hidden1Mask, fake, false);
            var outValue = AffinePropagateAvx2(hidden2Out, _parameters.Output.Biases, _parameters.Output.Weights);
            var result = outValue / 16;
            return result;
        }

        private unsafe void AffineTransformAvx2
        (
            Span<sbyte> input,
            Span<sbyte> output,
            uint inDims,
            //uint outDims,
            int[] biases,
            sbyte[] weights,
            Span<uint> inMask,
            Span<uint> outMask,
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
                    if (!next_idx(out int idx, ref offset, ref v, inMask, inDims))
                    {
                        break;
                    }

                    Vector256<sbyte> first = ((Vector256<sbyte>*)weightsPtr)[idx];
                    Vector256<sbyte> second;

                    short factor = (short)input[idx];

                    if (next_idx(out idx, ref offset, ref v, inMask, inDims))
                    {
                        second = ((Vector256<sbyte>*)weightsPtr)[idx];
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

                var outVec = (Vector256<sbyte>*)outputPtr;
                outVec[0] = Avx2.PackSignedSaturate(result0, result1);
                if (packAndGetMask)
                {
                    outMask[0] = (uint)Avx2.MoveMask(Avx2.CompareGreaterThan(outVec[0], kZeroBytes));
                }
                else
                {
                    outVec[0] = Avx2.Max(outVec[0], kZeroBytes);
                }
            }
        }

        bool next_idx(out int idx, ref uint offset, ref ulong v, Span<uint> mask, uint inDims)
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
            idx = (int)offset + v.BitScanForward();
            v &= v - 1;
            return true;
        }



        private unsafe int AffinePropagateAvx2(Span<sbyte> input, int[] biases, sbyte[] weights)
        {
            fixed (sbyte* inputPtr = input)
            fixed (sbyte* weightsPtr = weights)
            {
                var iv = (Vector256<byte>*)inputPtr;
                var row = (Vector256<sbyte>*)weightsPtr;
                var prod1 = Avx2.MultiplyAddAdjacent(iv[0], row[0]);
                var prod = Avx2.MultiplyAddAdjacent(prod1, Vector256.Create((short)1));
                var sum = Sse2.Add(prod.GetLower(), Avx2.ExtractVector128(prod, 1));
                sum = Sse2.Add(sum, Sse2.Shuffle(sum, 0x1b));
                var result = Sse2.ConvertToInt32(sum) + Sse41.Extract(sum, 1) + biases[0];
                return result;
            }
        }

        private unsafe void TransformAvx2(NnuePosition pos, Span<sbyte> output, Span<uint> outMask)
        {
            if (!UpdateAccumulator(pos))
            {
                RefreshAccumulatorAvx2(pos);
            }

            var accumulation = pos.Nnue[0].accumulator.accumulation;
            var perspectives = new int[] { pos.Player, pos.Player ^ 1 };
            var outputMaskIndex = 0;
            for (uint perspective = 0; perspective < 2; perspective++)
            {
                var offset = kHalfDimensions * perspective;
                const uint numChunks = (16 * kHalfDimensions) / SimdWidth;
                fixed (sbyte* outputPtr = output)
                fixed (short* accumulationsPtr = accumulation[perspectives[perspective]])
                {
                    var outEntry = (Vector256<sbyte>*)&outputPtr[offset];
                    for (uint i = 0; i < numChunks / 2; i++)
                    {
                        var s0 = ((Vector256<short>*)accumulationsPtr)[i * 2];
                        var s1 = ((Vector256<short>*)accumulationsPtr)[i * 2 + 1];
                        outEntry[i] = Avx2.PackSignedSaturate(s0, s1);
                        var x = outEntry[i];
                        outMask[outputMaskIndex++] = (uint)Avx2.MoveMask(Avx2.CompareGreaterThan(outEntry[i], Vector256<sbyte>.Zero));
                    }
                }
            }
        }

        private unsafe void RefreshAccumulatorAvx2(NnuePosition pos)
        {
            var accumulator = pos.Nnue[0].accumulator;
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
            accumulator.computedAccumulation = true;
        }

        private unsafe bool UpdateAccumulator(NnuePosition pos)
        {
            var accumulator = pos.Nnue[0].accumulator;
            if (accumulator.computedAccumulation)
            {
                return true;
            }

            NnueAccumulator prevAcc;
            if (pos.NnueCount > 0 && pos.Nnue[1].accumulator.computedAccumulation)
            {
                prevAcc = pos.Nnue[1].accumulator;
            }
            else if (pos.NnueCount > 1 && pos.Nnue[2].accumulator.computedAccumulation)
            {
                prevAcc = pos.Nnue[2].accumulator;
            }
            else
            {
                return false;
            }

            Span<IndexList2> removedIndices = stackalloc IndexList2[2];
            Span<IndexList2> addedIndices = stackalloc IndexList2[2];
            Span<bool> reset = stackalloc bool[2];
            AppendChangedIndices(pos, removedIndices, addedIndices, reset);

            var acc = stackalloc Vector256<short>[RegisterCount];
            for (uint i = 0; i < kHalfDimensions / TileHeight; i++)
            {
                for (int color = 0; color < 2; color++)
                {
                    fixed (short* biasPtr = _parameters.FeatureTransformer.Biases)
                    fixed (short* weightsPtr = _parameters.FeatureTransformer.Weights)
                    fixed (short* accumulationsPtr = accumulator.accumulation[color])
                    fixed (short* prevAccPtr = prevAcc.accumulation[color])
                    {
                        var accTile = (Vector256<short>*)(&accumulationsPtr[i * TileHeight]);
                        
                        if (reset[color])
                        {
                            var biasesTile = (Vector256<short>*)(&biasPtr[i * TileHeight]);
                            for (var j = 0; j < RegisterCount; j++)
                            {
                                acc[j] = biasesTile[j];
                            }
                        }
                        else
                        {
                            var prevAccTile = (Vector256<short>*)(&prevAccPtr[i * TileHeight]);
                            for (var j = 0; j < RegisterCount; j++)
                            {
                                acc[j] = prevAccTile[j];
                            }

                            // Difference calculation for the deactivated features
                            for (uint k = 0; k < removedIndices[color].size; k++)
                            {
                                uint index = removedIndices[color].values[k];
                                uint offset = kHalfDimensions * index + i * TileHeight;
                                Vector256<short>* column = (Vector256<short>*)&weightsPtr[offset];
                                for (uint j = 0; j < RegisterCount; j++)
                                {
                                    acc[j] = Avx2.Subtract(acc[j], column[j]);
                                }
                            }
                        }

                        // Difference calculation for the activated features
                        for (uint k = 0; k < addedIndices[color].size; k++)
                        {
                            uint index = addedIndices[color].values[k];
                            uint offset = kHalfDimensions * index + i * TileHeight;

                            Vector256<short>* column = (Vector256<short>*)&weightsPtr[offset];
                            for (uint j = 0; j < RegisterCount; j++)
                            {
                                acc[j] = Avx2.Add(acc[j], column[j]);
                            }
                        }

                        for (uint j = 0; j < RegisterCount; j++)
                        {
                            accTile[j] = acc[j];
                        }
                    }
                }
            }

            accumulator.computedAccumulation = true;
            return true;
        }
    }
}