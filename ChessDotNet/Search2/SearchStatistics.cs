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


        public long StaticEvaluationCutoffs { get; set; }
        public long NullMoveCutoffs { get; set; }
        

        public long PvsScoutSuccess { get; set; }
        public long PvsScoutFail { get; set; }

        public long RazoringSuccess { get; set; }
        public long RazoringFail { get; set; }

        public long FutilityReductions { get; set; }
        public long LateMoveReductions1 { get; set; }
        public long LateMoveReductions2 { get; set; }
        public long LateMoveFail { get; set; }

        public long DeltaPruning { get; set; }
        public long SeePruning { get; set; }

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
            AspirationSuccess = 0;
            AspirationFail = 0;
            MateAlpha = 0;
            MateBeta = 0;
            MateCutoff = 0;
            HashMiss = 0;
            HashCollision = 0;
            HashInsufficientDepth = 0;
            HashAlphaCutoff = 0;
            HashAlphaContinue = 0;
            HashBetaCutoff = 0;
            HashBetaContinue = 0;
            HashCutoffExact = 0;
            StoresBeta = 0;
            StoresAlpha = 0;
            StoresExact = 0;
            BetaCutoffs = 0;
            StaticEvaluationCutoffs = 0;
            NullMoveCutoffs = 0;
            PvsScoutSuccess = 0;
            PvsScoutFail = 0;
            RazoringSuccess = 0;
            RazoringFail = 0;
            FutilityReductions = 0;
            LateMoveReductions1 = 0;
            LateMoveReductions2 = 0;
            LateMoveFail = 0;
            FutilityReductions = 0;
            DeltaPruning = 0;
            SeePruning = 0;
            Repetitions = 0;
            Mates = 0;
            Stalemates = 0;
        }



        private void AppendStatistic(StringBuilder builder, string name, long value, string units = null)
        {
            var valueStr = value.ToUserFriendly();
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
            AppendStatistic(builder, nameof(StaticEvaluationCutoffs), StaticEvaluationCutoffs);
            AppendStatistic(builder, nameof(NullMoveCutoffs), NullMoveCutoffs);
            AppendStatistic(builder, nameof(PvsScoutSuccess), PvsScoutSuccess);
            AppendStatistic(builder, nameof(PvsScoutFail), PvsScoutFail);
            AppendStatistic(builder, nameof(RazoringSuccess), RazoringSuccess);
            AppendStatistic(builder, nameof(RazoringFail), RazoringFail);
            AppendStatistic(builder, nameof(FutilityReductions), FutilityReductions);
            AppendStatistic(builder, nameof(LateMoveReductions1), LateMoveReductions1);
            AppendStatistic(builder, nameof(LateMoveReductions2), LateMoveReductions2);
            AppendStatistic(builder, nameof(LateMoveFail), LateMoveFail);
            AppendStatistic(builder, nameof(DeltaPruning), DeltaPruning);
            AppendStatistic(builder, nameof(SeePruning), SeePruning);
            AppendStatistic(builder, nameof(Repetitions), Repetitions);
            AppendStatistic(builder, nameof(Mates), Mates);
            AppendStatistic(builder, nameof(Stalemates), Stalemates);

            return builder.ToString(0, builder.Length - 2);
        }
    }
}