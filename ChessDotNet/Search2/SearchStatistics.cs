using System.Globalization;
using System.Text;

namespace ChessDotNet.Search2
{
    public class SearchStatistics
    {
        public long NodesSearched { get; set; }

        public long AspirationSuccess { get; set; }
        public long AspirationFail { get; set; }

        public long MateAlpha { get; set; }
        public long MateBeta { get; set; }
        public long MateCutoff { get; set; }

        public long HashMiss { get; set; }
        public long HashCollision { get; set; }
        public long HashInsufficientDepth { get; set; }
        public long HashAlphaCutoff { get; set; }
        public long HashAlphaContinue { get; set; }
        public long HashBetaCutoff { get; set; }
        public long HashBetaContinue { get; set; }
        public long HashCutoffExact { get; set; }

        public long StoresBeta { get; set; }
        public long StoresAlpha { get; set; }
        public long StoresExact { get; set; }

        public long BetaCutoffs { get; set; }

        public long NullMoveCutoffs { get; set; }


        public long PvsScoutSuccess { get; set; }
        public long PvsScoutFail { get; set; }

        public long Repetitions { get; set; }
        public long Mates { get; set; }
        public long Stalemates { get; set; }


        public SearchStatistics()
        {
            Reset();
        }

        public void Reset()
        {
            NodesSearched = 0;
        }

        public string TrimNumber(long number, double divisor, string sufffix)
        {
            var divided = number / divisor;
            var str = divided.ToString(CultureInfo.InvariantCulture);
            if (str.Contains("."))
            {
                var newLength = str.Length < 5 ? str.Length : 5;
                str = str.Substring(0, newLength).TrimEnd('0').TrimEnd('.');
            }
            else
            {
                var newLength = str.Length < 4 ? str.Length : 4;
                str = str.Substring(0, newLength);
            }

            return str + sufffix;
        }

        public string FormatNumber(long number)
        {
            // This doesn't round numbers, just floors them
            var numberStr = number.ToString();
            string suffix;
            switch (numberStr.Length)
            {
                case 1:
                case 2:
                case 3:
                    return numberStr;
                case 4:
                case 5:
                case 6:
                    return TrimNumber(number, 1000, "k");
                case 7:
                case 8:
                case 9:
                    return TrimNumber(number, 1000000, "M");
                case 10:
                case 11:
                case 12:
                    return TrimNumber(number, 1000000000, "B");
                case 13:
                case 14:
                case 15:
                    return TrimNumber(number, 1000000000000, "T");
                default:
                    return numberStr;
            }
        }

        private void AppendStatistic(StringBuilder builder, string name, long value, string units = null)
        {
            var valueStr = FormatNumber(value);
            builder.Append($"{name}: {valueStr}");
            if (units != null)
            {
                builder.Append($" {units}");
            }
            builder.Append(", ");
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            AppendStatistic(builder, nameof(NodesSearched), NodesSearched);
            AppendStatistic(builder, nameof(AspirationSuccess), AspirationSuccess);
            AppendStatistic(builder, nameof(AspirationFail), AspirationFail);
            AppendStatistic(builder, nameof(MateAlpha), MateAlpha);
            AppendStatistic(builder, nameof(MateBeta), MateBeta);
            AppendStatistic(builder, nameof(MateCutoff), MateCutoff);
            AppendStatistic(builder, nameof(HashMiss), HashMiss);
            AppendStatistic(builder, nameof(HashCollision), HashCollision);
            AppendStatistic(builder, nameof(HashInsufficientDepth), HashInsufficientDepth);
            AppendStatistic(builder, nameof(HashAlphaCutoff), HashAlphaCutoff);
            AppendStatistic(builder, nameof(HashAlphaContinue), HashAlphaContinue);
            AppendStatistic(builder, nameof(HashBetaCutoff), HashBetaCutoff);
            AppendStatistic(builder, nameof(HashBetaContinue), HashBetaContinue);
            AppendStatistic(builder, nameof(HashCutoffExact), HashCutoffExact);
            AppendStatistic(builder, nameof(StoresBeta), StoresBeta);
            AppendStatistic(builder, nameof(StoresAlpha), StoresAlpha);
            AppendStatistic(builder, nameof(StoresExact), StoresExact);
            AppendStatistic(builder, nameof(BetaCutoffs), BetaCutoffs);
            AppendStatistic(builder, nameof(NullMoveCutoffs), NullMoveCutoffs);
            AppendStatistic(builder, nameof(PvsScoutSuccess), PvsScoutSuccess);
            AppendStatistic(builder, nameof(PvsScoutFail), PvsScoutFail);
            AppendStatistic(builder, nameof(Repetitions), Repetitions);
            AppendStatistic(builder, nameof(Mates), Mates);
            AppendStatistic(builder, nameof(Stalemates), Stalemates);

            return builder.ToString(0, builder.Length - 2);
        }
    }
}