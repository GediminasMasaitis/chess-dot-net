using System.IO;
using System.Text;

namespace ChessDotNet.Evaluation.Nnue.Managed
{
    public class NnueLoader
    {
        private const uint ExpectedVersion = 0x7AF32F16U;
        private const uint ExpectedFeatureTransformerHash = 1567217080U ^ 512;
        private const uint ExpectedNetworkHash = 1664315734U;
        private const uint ExpectedMainHash = 1046128366U;

        public HalfKpParameters Load(string path)
        {
            using var stream = File.OpenRead(path);
            return Load(stream);
        }

        public HalfKpParameters Load(Stream stream)
        {
            using var reader = new BinaryReader(stream, Encoding.ASCII, true);
            return Load(reader);
        }

        public HalfKpParameters Load(BinaryReader reader)
        {
            ReadHeader(reader);
            var parameters = ReadHalfKpParameters(reader);

            if (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                throw new NnueException("Expected end of stream");
            }

            return parameters;
        }

        private NnueHeader ReadHeader(BinaryReader reader)
        {
            var header = new NnueHeader();
            header.Version = reader.ReadUInt32();
            if (header.Version != ExpectedVersion)
            {
                throw new NnueException($"Invalid version, expected {ExpectedVersion}, read {header.Version}");
            }

            header.HashValue = reader.ReadUInt32();
            if (header.HashValue != ExpectedMainHash)
            {
                throw new NnueException($"Invalid main hash, expected {ExpectedMainHash}, read {header.HashValue}");
            }

            var architectureLength = reader.ReadInt32();
            var architectureBytes = reader.ReadBytes(architectureLength);
            header.Architecture = Encoding.ASCII.GetString(architectureBytes);
            return header;
        }

        private HalfKpParameters ReadHalfKpParameters(BinaryReader reader)
        {
            var featureTransformerHash = reader.ReadUInt32();
            if (featureTransformerHash != ExpectedFeatureTransformerHash)
            {
                throw new NnueException($"Invalid feature transformer hash, expected {ExpectedFeatureTransformerHash}, read {featureTransformerHash}");
            }

            var parameters = new HalfKpParameters();
            parameters.FeatureTransformer = ReadFeatureTransformerParameters(reader, 41024, 256);

            var networkHash = reader.ReadUInt32();
            if (networkHash != ExpectedNetworkHash)
            {
                throw new NnueException($"Invalid network hash, expected {ExpectedNetworkHash}, read {networkHash}");
            }

            parameters.Hidden1 = ReadHiddenParameters(reader, 512, 32);
            parameters.Hidden2 = ReadHiddenParameters(reader, 32, 32);
            parameters.Output = ReadOutputParameters(reader, 32, 1);

            return parameters;
        }

        private int GetWeightIndex(int r, int c, int dims)
        {
            return c * 32 + r;
        }

        private NnueParameters ReadHiddenParameters(BinaryReader reader, int inputDimensions, int outputDimensions)
        {
            var parameters = new NnueParameters();
            parameters.Biases = new int[outputDimensions];
            parameters.Weights = new sbyte[outputDimensions * inputDimensions];

            for (var i = 0; i < parameters.Biases.Length; i++)
            {
                parameters.Biases[i] = reader.ReadInt32();
            }

            for (var i = 0; i < outputDimensions; i++)
            {
                for (var j = 0; j < inputDimensions; j++)
                {
                    var index = GetWeightIndex(i, j, inputDimensions);
                    parameters.Weights[index] = reader.ReadSByte();
                }
            }

            //for (int i = 0; i < parameters.Weights.Length; i++)
            //{
            //    parameters.Weights[i] = reader.ReadSByte();
            //}

            return parameters;
        }

        private NnueParameters ReadOutputParameters(BinaryReader reader, int inputDimensions, int outputDimensions)
        {
            var parameters = new NnueParameters();
            parameters.Biases = new int[outputDimensions];
            parameters.Weights = new sbyte[outputDimensions * inputDimensions];

            for (var i = 0; i < parameters.Biases.Length; i++)
            {
                parameters.Biases[i] = reader.ReadInt32();
            }

            for (int i = 0; i < parameters.Weights.Length; i++)
            {
                parameters.Weights[i] = reader.ReadSByte();
            }

            return parameters;
        }

        private NnueFeatureTransformerParameters ReadFeatureTransformerParameters(BinaryReader reader, int inputDimensions, int outputDimensions)
        {
            var parameters = new NnueFeatureTransformerParameters();
            parameters.Biases = new short[outputDimensions];
            parameters.Weights = new short[outputDimensions * inputDimensions];

            for (var i = 0; i < parameters.Biases.Length; i++)
            {
                parameters.Biases[i] = reader.ReadInt16();
            }

            for (int i = 0; i < parameters.Weights.Length; i++)
            {
                parameters.Weights[i] = reader.ReadInt16();
            }

            return parameters;
        }
    }
}