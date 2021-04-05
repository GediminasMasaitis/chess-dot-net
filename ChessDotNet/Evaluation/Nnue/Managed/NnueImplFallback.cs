using System;
using System.Diagnostics;

namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public class NnueImplFallback : NnueImplBase
    {
        private readonly HalfKpParameters _parameters;

        public NnueImplFallback(HalfKpParameters parameters)
        {
            _parameters = parameters;
        }

        public override int Evaluate(NnuePosition pos)
        {
            //pos.nnue[0].accumulator.computedAccumulation = false;

            Span<sbyte> input = stackalloc sbyte[FtOutDims];
            Span<sbyte> hidden1Out = stackalloc sbyte[32];
            Span<sbyte> hidden2Out = stackalloc sbyte[32];

            Transform(pos, input);
            AffineTransform(input, hidden1Out, FtOutDims, 32, _parameters.Hidden1.Biases, _parameters.Hidden1.Weights);
            AffineTransform(hidden1Out, hidden2Out, 32, 32, _parameters.Hidden2.Biases, _parameters.Hidden2.Weights);
            var outValue = AffinePropagate(hidden2Out, _parameters.Output.Biases, _parameters.Output.Weights);
            var result = outValue / 16;
            return result;
        }

        private void Transform(NnuePosition pos, Span<sbyte> output)
        {
            if (!UpdateAccumulator(pos))
            {
                RefreshAccumulator(pos);
            }

            var accumulation = pos.nnue[0].accumulator.accumulation;
            Span<int> perspectives = stackalloc int[] { pos.player, pos.player ^ 1 };
            for (var perspective = 0; perspective < 2; perspective++)
            {
                var offset = kHalfDimensions * perspective;
                for (var i = 0; i < kHalfDimensions; i++)
                {
                    short sum = accumulation[perspectives[perspective]][i];
                    output[offset + i] = (sbyte)Clamp((int)sum, 0, 127);
                }
            }
        }

        private unsafe bool UpdateAccumulator(NnuePosition pos)
        {
            var accumulator = pos.nnue[0].accumulator;
            if (accumulator.computedAccumulation)
            {
                return true;
            }

            NnueAccumulator prevAcc;
            if(pos.nnue[1] != null && pos.nnue[1].accumulator.computedAccumulation)
            {
                prevAcc = pos.nnue[1].accumulator;
            }
            else if (pos.nnue[2] != null && pos.nnue[2].accumulator.computedAccumulation)
            {
                prevAcc = pos.nnue[2].accumulator;
            }
            else
            {
                return false;
            }

            Span<IndexList2> removedIndices = stackalloc IndexList2[2];
            Span<IndexList2> addedIndices = stackalloc IndexList2[2];
            Span<bool> reset = stackalloc bool[2];
            AppendChangedIndices(pos, removedIndices, addedIndices, reset);

            for (var color = 0; color < 2; color++)
            {
                if (reset[color])
                {
                    Array.Copy(_parameters.FeatureTransformer.Biases, accumulator.accumulation[color], kHalfDimensions);
                }
                else
                {
                    Array.Copy(prevAcc.accumulation[color], accumulator.accumulation[color], kHalfDimensions);
                    // Difference calculation for the deactivated features
                    for (uint k = 0; k < removedIndices[color].size; k++)
                    {
                        uint index = removedIndices[color].values[k];
                        uint offset = kHalfDimensions * index;

                        for (uint j = 0; j < kHalfDimensions; j++)
                        {
                            accumulator.accumulation[color][j] -= _parameters.FeatureTransformer.Weights[offset + j];
                        }
                    }
                }

                // Difference calculation for the activated features
                for (uint k = 0; k < addedIndices[color].size; k++)
                {
                    uint index = addedIndices[color].values[k];
                    uint offset = kHalfDimensions * index;

                    for (uint j = 0; j < kHalfDimensions; j++)
                    {
                        accumulator.accumulation[color][j] += _parameters.FeatureTransformer.Weights[offset + j];
                    }
                }
            }

            accumulator.computedAccumulation = true;
            return true;
        }


        private unsafe void RefreshAccumulator(NnuePosition pos)
        {
            var accumulator = pos.nnue[0].accumulator;
            Span<IndexList2> activeIndices = stackalloc IndexList2[2];
            AppendActiveIndices(pos, activeIndices);

            for (var color = 0; color < 2; color++)
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

            accumulator.computedAccumulation = true;
        }

        private void AffineTransform
        (
            Span<sbyte> input,
            Span<sbyte> output,
            uint inDims,
            uint outDims,
            int[] biases,
            sbyte[] weights
        )
        {
            //var tmp = new int[outDims];
            Span<int> tmp = stackalloc int[32];

            for (var i = 0; i < outDims; i++)
            {
                tmp[i] = biases[i];
            }

            for (var idx = 0; idx < inDims; idx++)
            {
                var inputVal = input[idx];
                if (inputVal != 0)
                {
                    for (int i = 0; i < outDims; i++)
                    {
                        var weight = weights[(outDims * idx) + i];
                        var product = inputVal * weight;
                        tmp[i] += product;
                    }
                }
            }

            for (var i = 0; i < outDims; i++)
            {
                output[i] = (sbyte)Clamp(tmp[i] >> SHIFT, 0, 127);
            }
        }

        private int AffinePropagate(Span<sbyte> input, int[] biases, sbyte[] weights)
        {
            var sum = biases[0];
            for (var j = 0; j < 32; j++)
            {
                sum += weights[j] * input[j];
            }
            return sum;
        }
    }
}