using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml.Schema;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;
using ChessDotNet.Fen;
using ChessDotNet.MoveGeneration;
using ChessDotNet.Searching;

using Bitboard = System.UInt64;
using ZobristKey = System.UInt64;
using Position = System.Byte;
using Piece = System.Byte;
using TranspositionTableFlag = System.Byte;
using MoveKey = System.UInt64;

namespace ChessDotNet.Search2
{
    public static class TranspositionTableFlags
    {
        public const TranspositionTableFlag None = 0;
        public const TranspositionTableFlag Alpha = 1;
        public const TranspositionTableFlag Beta = 2;
        public const TranspositionTableFlag Exact = 3;
    }

    public struct TranspositionTableEntry
    {
        public ZobristKey Key { get; }
        public Move Move { get; }
        public int Depth { get; }
        public int Score { get; }
        public TranspositionTableFlag Flag { get; }

        public TranspositionTableEntry(ZobristKey key, Move move, int depth, int score, TranspositionTableFlag flag)
        {
            Key = key;
            Move = move;
            Depth = depth;
            Score = score;
            Flag = flag;
        }
    }

    public class TranspositionTable
    {
        private readonly ulong _size;
        private readonly TranspositionTableEntry[] _entries;

        public TranspositionTable(ulong size)
        {
            _size = size;
            //var entrySize = Marshal.SizeOf<TranspositionTableEntry>();
            _entries = new TranspositionTableEntry[size];
        }

        public void Store(ZobristKey key, Move move, int depth, int score, TranspositionTableFlag flag)
        {
            var index = GetTableIndex(key);
            var existingEntry = _entries[index];

            if (existingEntry.Depth > depth && existingEntry.Key == key)
            {
                return;
            }
            
            //if (existingEntry.Key != 0 && existingEntry.Key != key)
            //{
            //    return;
            //}

            if (flag == TranspositionTableFlags.Exact || existingEntry.Key != key || depth > existingEntry.Depth - 4)
            {
                var entry = new TranspositionTableEntry(key, move, depth, score, flag);
                _entries[index] = entry;
            }
        }

        public bool TryProbe(ZobristKey key, out TranspositionTableEntry entry)
        {
            //entry = default; return false;
            var index = GetTableIndex(key);
            entry = _entries[index];
            var exists = entry.Flag != TranspositionTableFlags.None;
            //var valid = entry.Key == key;
            return exists;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong GetTableIndex(ZobristKey key)
        {
            var index = key % _size;
            return index;
        }

        public void Clear()
        {
            Array.Clear(_entries, 0, _entries.Length);
        }
    }

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

    public static class SearchConstants
    {
        public const int MaxDepth = 255;
        public const int MateScore = 50000;
        public const int MateThereshold = 49000;
        public const int Inf = int.MaxValue;

        public const int EndgameMaterial = 51300;
    }

    public class MoveOrderingService
    {
        public void OrderNextMove(int currentIndex, IList<Move> moves, int ply, Move? pvMove, MoveKey[,] killers, int[,] history)
        {
            var bestScore = -SearchConstants.Inf;
            var bestScoreIndex = -1;
            for (var i = currentIndex; i < moves.Count; i++)
            {
                var score = CalculateMoveScore(moves[i], ply, pvMove, killers, history);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestScoreIndex = i;
                }
            }

            var temp = moves[currentIndex];
            moves[currentIndex] = moves[bestScoreIndex];
            moves[bestScoreIndex] = temp;
        }

        private int CalculateMoveScore(Move move, int ply, Move? principalVariationMove, MoveKey[,] killers, int[,] history)
        {
            //return 0;
            MoveKey moveKey = move.Key2;
            var isPrincipalVariation = principalVariationMove.HasValue && principalVariationMove.Value.Key2 == moveKey;
            if (isPrincipalVariation)
            {
                return 2000000;
            }

            var mvvLvaScore = MVVLVAScoreCalculation.Scores[move.Piece, move.TakesPiece];
            if (mvvLvaScore > 0)
            {
                return mvvLvaScore + 1000000;
            }

            if (killers[ply, 0] == moveKey)
            {
                return 9000000;
            }
            if (killers[ply, 1] == moveKey)
            {
                return 8000000;
            }

            var historyScore = history[move.From, move.To];
            return historyScore;
        }
    }

    public class SearchState
    {
        public TranspositionTable Table { get; }
        public MoveKey[,] Killers { get; }
        public int[,] History { get; }


        public SearchState()
        {
            Table = new TranspositionTable(1024 * 1024 * 32);
            Killers = new MoveKey[SearchConstants.MaxDepth, 2]; // Non-captures causing beta cutoffs
            History = new int[64, 64];
        }

        public void OnNewSearch()
        {
            Array.Clear(Killers, 0, Killers.Length);
            Array.Clear(History, 0, History.Length);
        }

        public void OnIterativeDeepen()
        {
            Array.Clear(Killers, 0, Killers.Length);
            Array.Clear(History, 0, History.Length);

            //Table.Clear();
        }
    }

    public class SearchStopper
    {
        private readonly Stopwatch _stopwatch;

        private CancellationTokenSource _cancellationTokenSource;
        private SearchParameters _parameters;
        private long _minTime;
        private long _maxTime;

        public SearchStopper()
        {
            _stopwatch = new Stopwatch();
        }

        public void NewSearch(SearchParameters parameters, bool whiteToMove, CancellationToken externalToken)
        {
            _stopwatch.Restart();
            _parameters = parameters;

            var time = whiteToMove ? parameters.WhiteTime : parameters.BlackTime;
            var increment = whiteToMove ? parameters.WhiteTimeIncrement : parameters.BlackTimeIncrement;

            // Need to adjust this
            _minTime = parameters.Infinite ? long.MaxValue : time / 60 + increment / 3;
            _maxTime = parameters.Infinite ? long.MaxValue : time / 20 + increment;
            
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        }

        public bool ShouldStopOnDepthIncrease(int depthReached)
        {
            if (_parameters.MaxDepth.HasValue && depthReached >= _parameters.MaxDepth.Value)
            {
                _cancellationTokenSource.Cancel();
                return true;
            }

            var elapsed = GetSearchedTime();
            if (elapsed >= _minTime)
            {
                _cancellationTokenSource.Cancel();
                return true;
            }

            var cancellationRequested = _cancellationTokenSource.IsCancellationRequested;
            return cancellationRequested;
        }

        public bool ShouldStop()
        {
            var elapsed = GetSearchedTime();
            if (elapsed >= _maxTime)
            {
                _cancellationTokenSource.Cancel();
                return true;
            }

            return _cancellationTokenSource.IsCancellationRequested;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetSearchedTime()
        {
            return _stopwatch.Elapsed.TotalMilliseconds;
        }
    }

    public class SearchService2
    {
        private readonly PossibleMovesService _possibleMoves;
        private readonly EvaluationService _evaluation;
        private readonly SearchState _state;
        private readonly SearchStopper _stopper;
        private readonly MoveOrderingService _moveOrdering;

        private readonly SearchStatistics _statistics;



        public event Action<SearchInfo> SearchInfo;

        public SearchService2(PossibleMovesService possibleMoves, EvaluationService evaluation)
        {
            _possibleMoves = possibleMoves;
            _evaluation = evaluation;
            _moveOrdering = new MoveOrderingService();
            _state = new SearchState();
            _stopper = new SearchStopper();
            _statistics = new SearchStatistics();
        }

        public IList<Move> Run(Board board, SearchParameters parameters, CancellationToken token = default)
        {
            _stopper.NewSearch(parameters, board.WhiteToMove, token);
            _state.OnNewSearch();
            RunIterativeDeepening(board);
            var principalVariation = GetPrincipalVariation(board);
            var moves = principalVariation.Select(entry => entry.Move).ToList();
            return moves;
        }

        public IList<TranspositionTableEntry> GetPrincipalVariation(Board board)
        {
            var entries = new List<TranspositionTableEntry>();
            //while (true)
            for(var i = 0; i < SearchConstants.MaxDepth; i++)
            {
                var success = _state.Table.TryProbe(board.Key, out var entry);
                if (!success)
                {
                    break;
                }

                entries.Add(entry);
                board = board.DoMove(entry.Move);
                //Console.WriteLine(board.Print(new EvaluationService(), new FenSerializerService()));
            }

            return entries;
        }


        private void RunIterativeDeepening(Board board)
        {
            const int initialDepth = 1;
            var score = SearchToDepth(board, initialDepth, 0, -SearchConstants.Inf, SearchConstants.Inf, true, true);

            for (var depth = initialDepth + 1; depth < SearchConstants.MaxDepth; depth++)
            {
                _state.OnIterativeDeepen();
                //score = SearchToDepth(board, depth, 0, -SearchConstants.Inf, SearchConstants.Inf, true, true);
                score = SearchAspirationWindow(board, depth, score);
                
                var principalVariation = GetPrincipalVariation(board);

                var searchInfo = new SearchInfo();
                searchInfo.Depth = depth;
                searchInfo.Score = score;
                searchInfo.NodesSearched = _statistics.NodesSearched;
                searchInfo.Time = (long)_stopper.GetSearchedTime();
                searchInfo.PrincipalVariation = principalVariation;
                if (score > SearchConstants.MateThereshold)
                {
                    searchInfo.MateIn = SearchConstants.MateScore - score;
                }
                SearchInfo?.Invoke(searchInfo);

                Console.WriteLine($"{_statistics}");
                Console.WriteLine();

                if (_stopper.ShouldStopOnDepthIncrease(depth))
                {
                    return;
                }
            }
        }

        private int SearchAspirationWindow(Board board, int depth, int previousScore)
        {
            const int window = 50;
            var alpha = previousScore - window;
            var beta = previousScore + window;

            var windowScore = SearchToDepth(board, depth, 0, alpha, beta, true, true);
            if (windowScore <= alpha || windowScore >= beta)
            {
                _statistics.AspirationFail++;
                var fullSearchScore = SearchToDepth(board, depth, 0, -SearchConstants.Inf, SearchConstants.Inf, true, true);
                return fullSearchScore;
            }
            else
            {
                _statistics.AspirationSuccess++;
                return windowScore;
            }
        }


        //private int SearchRoot(Board board, int depth, int ply, int alpha, int beta, bool nullMoveAllowed, bool isPrincipalVariation)
        //{
        //    var enemyAttacks = _possibleMoves.AttacksService.GetAllAttacked(board, !board.WhiteToMove);
        //    var myKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
        //    var inCheck = (enemyAttacks & myKing) != 0;

        //    // IN CHECK EXTENSION
        //    if (inCheck)
        //    {
        //        depth++;
        //    }

        //    // PROBE TRANSPOSITION TABLE
        //    Move? principalVariationMove = null;
        //    var probeResult = ProbeTranspositionTable(board.Key, depth, alpha, beta, isPrincipalVariation, out var tableEntry);
        //    switch (probeResult)
        //    {
        //        case TranspositionTableProbeResult.HitCutoff:
        //            return tableEntry.Score;
        //        case TranspositionTableProbeResult.HitContinue:
        //            principalVariationMove = tableEntry.Move;
        //            break;
        //        case TranspositionTableProbeResult.Miss:
        //            break;
        //        default:
        //            throw new ArgumentOutOfRangeException(nameof(probeResult), probeResult, null);
        //    }

        //}

        private int SearchToDepth(Board board, int depth, int ply, int alpha, int beta, bool nullMoveAllowed, bool isPrincipalVariation)
        {
            // STOP CHECK
            if (_stopper.ShouldStop())
            {
                return 0;
            }

            // REPETITION DETECTION
            if (ply > 0)
            {
                var isRepetition = IsRepetition(board);
                if (isRepetition)
                {
                    _statistics.Repetitions++;
                    return 0;
                }
            }

            // MATE DISTANCE PRUNE
            var currentMateScore = SearchConstants.MateScore - ply;
            if (ply > 0)
            {
                if (alpha < -currentMateScore)
                {
                    _statistics.MateAlpha++;
                    alpha = -currentMateScore;
                }

                if (beta > currentMateScore - 1)
                {
                    _statistics.MateBeta++;
                    beta = currentMateScore - 1;
                }

                if (alpha >= beta)
                {
                    _statistics.MateCutoff++;
                    return alpha;
                }
            }

            // IN CHECK EXTENSION
            var enemyAttacks = _possibleMoves.AttacksService.GetAllAttacked(board, !board.WhiteToMove);
            var myKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            var inCheck = (enemyAttacks & myKing) != 0;

            if (inCheck)
            {
                depth++;
            }

            // QUIESCENCE
            if (depth == 0)
            {
                //var evaluatedScore = _evaluation.Evaluate(board);
                //_statistics.NodesSearched++;
                var evaluatedScore = Quiescence(board, depth, ply, alpha, beta);

                return evaluatedScore;
            }

            // PROBE TRANSPOSITION TABLE
            Move? principalVariationMove = null;
            var probeResult = ProbeTranspositionTable(board.Key, depth, alpha, beta, isPrincipalVariation,  out var tableEntry);
            switch (probeResult)
            {
                case TranspositionTableProbeResult.HitCutoff:
                    return tableEntry.Score;
                case TranspositionTableProbeResult.HitContinue:
                    principalVariationMove = tableEntry.Move;
                    break;
                case TranspositionTableProbeResult.Miss:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(probeResult), probeResult, null);
            }

            // NULL MOVE PRUNING
            var nullDepthReduction = depth > 6 ? 3 : 2;
            if (nullMoveAllowed && !inCheck && depth > 2 && ply > 0)
            {
                var material = board.WhiteToMove ? board.WhiteMaterial : board.BlackMaterial;
                if (material > SearchConstants.EndgameMaterial)
                {
                    var nullMove = new Move(0, 0, 0);
                    var nullBoard = board.DoMove(nullMove);
                    var nullMoveScore = SearchToDepth(nullBoard, depth - nullDepthReduction - 1, ply + 1, -beta, -beta + 1, false, false);
                    if (nullMoveScore >= beta)
                    {
                        _statistics.NullMoveCutoffs++;
                        return beta;
                    }
                }
            }

            // FUTILITY PRUNING
            var futilityPruning = false;
            var futilityMargins = new int[4] { 0, 200, 300, 500 };
            if (depth <= 3 && !isPrincipalVariation && !inCheck && Math.Abs(alpha) < 9000)
            {
                var futilityEvaluation = _evaluation.Evaluate(board);
                if (futilityEvaluation + futilityMargins[depth] <= alpha)
                {
                    futilityPruning = true;
                }
            }

            var bestScore = -SearchConstants.Inf;
            Move bestMove = default;


            // CHILD SEARCH
            var initialAlpha = alpha;
            var potentialMoves = _possibleMoves.GetAllPotentialMoves(board);
            var movesEvaluated = 0;
            var raisedAlpha = false;

            for (var moveIndex = 0; moveIndex < potentialMoves.Count; moveIndex++)
            {
                _moveOrdering.OrderNextMove(moveIndex, potentialMoves, ply, principalVariationMove, _state.Killers, _state.History);
                var move = potentialMoves[moveIndex];
                var childBoard = _possibleMoves.DoMoveIfKingSafe(board, move);
                if (childBoard == null)
                {
                    continue;
                }

                //futilityPruning = true;
                //if (futilityPruning && movesEvaluated > 0 && move.TakesPiece != ChessPiece.Empty && move.PawnPromoteTo != 0)
                //{
                //    var attacks = _possibleMoves.AttacksService.GetAllAttacked(childBoard, !childBoard.WhiteToMove);
                //    var opponentKing = board.WhiteToMove ? board.BitBoard[ChessPiece.BlackKing] : board.BitBoard[ChessPiece.WhiteKing];
                //    var oponnentInCheck = (attacks & opponentKing) != 0;
                //    if (!oponnentInCheck)
                //    {
                //        continue;
                //    }
                //}

                int childScore;
                if (raisedAlpha)
                {
                    childScore = -SearchToDepth(childBoard, depth - 1, ply + 1, -alpha - 1, -alpha, true, false);
                    if (childScore > alpha)
                    {
                        childScore = -SearchToDepth(childBoard, depth - 1, ply + 1, -beta, -alpha, true, isPrincipalVariation);
                        _statistics.PvsScoutFail++;
                    }
                    else
                    {
                        _statistics.PvsScoutSuccess++;
                    }
                }
                else
                {
                    childScore = -SearchToDepth(childBoard, depth - 1, ply + 1, -beta, -alpha, true, isPrincipalVariation);
                }

                movesEvaluated++;

                if (childScore > bestScore)
                {
                    bestScore = childScore;
                    bestMove = move;

                    if (childScore > alpha)
                    {
                        if (childScore >= beta)
                        {
                            _statistics.StoresBeta++;
                            _statistics.BetaCutoffs++;
                            StoreTranspositionTable(board.Key, move, depth, childScore, TranspositionTableFlags.Beta);
                            

                            if (move.TakesPiece == 0)
                            {
                                _state.Killers[ply, 1] = _state.Killers[ply, 0];
                                _state.Killers[ply, 0] = move.Key2;
                            }

                            return beta;
                        }

                        if (move.TakesPiece == ChessPiece.Empty)
                        {
                            _state.History[move.From, move.To] += depth; //depth*depth;
                        }

                        alpha = childScore;
                        raisedAlpha = true;
                    }
                }
            }

            if (movesEvaluated == 0)
            {
                if (inCheck)
                {
                    _statistics.Mates++;
                    alpha = -currentMateScore;
                }
                else
                {
                    _statistics.Stalemates++;
                    alpha = 0;
                }
                return alpha; // Should we store to TT if it's mate / stalemate?
            }

            if (alpha == initialAlpha)
            {
                _statistics.StoresAlpha++;
                StoreTranspositionTable(board.Key, bestMove, depth, alpha, TranspositionTableFlags.Alpha);
            }
            else
            {
                _statistics.StoresExact++;
                StoreTranspositionTable(board.Key, bestMove, depth, bestScore, TranspositionTableFlags.Exact);
            }

            return alpha;
        }

        private bool IsRepetition(Board board)
        {
            for (var i = board.LastTookPieceHistoryIndex; i < board.History.Length; i++)
            {
                var previousBoard = board.History[i].Board;
                if (board.Key == previousBoard.Key)
                {
                    return true;
                }
            }
            return false;
        }

        private int Quiescence(Board board, int depth, int ply, int alpha, int beta)
        {
            var evaluatedScore = _evaluation.Evaluate(board);
            _statistics.NodesSearched++;

            if (evaluatedScore >= beta)
            {
                return beta;
            }

            if (alpha < evaluatedScore)
            {
                alpha = evaluatedScore;
            }
            

            // PROBE TRANSPOSITION TABLE
            Move? principalVariationMove = null;
            var probeResult = ProbeTranspositionTable(board.Key, depth, alpha, beta, true /* ? */, out var tableEntry);
            switch (probeResult)
            {
                case TranspositionTableProbeResult.HitCutoff:
                    return tableEntry.Score;
                case TranspositionTableProbeResult.HitContinue:
                    principalVariationMove = tableEntry.Move;
                    break;
                case TranspositionTableProbeResult.Miss:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(probeResult), probeResult, null);
            }

            var bestScore = -SearchConstants.Inf;
            Move bestMove = default;

            var initialAlpha = alpha;
            var potentialMoves = _possibleMoves.GetAllPotentialMoves(board);
            var movesEvaluated = 0;
            for (var moveIndex = 0; moveIndex < potentialMoves.Count; moveIndex++)
            {
                _moveOrdering.OrderNextMove(moveIndex, potentialMoves, ply, principalVariationMove, _state.Killers, _state.History);
                var move = potentialMoves[moveIndex];

                if (move.TakesPiece == ChessPiece.Empty)
                {
                    continue;
                }

                var childBoard = _possibleMoves.DoMoveIfKingSafe(board, move);
                if (childBoard == null)
                {
                    continue;
                }

                var childScore = -Quiescence(childBoard, depth - 1, ply + 1, -beta, -alpha);
                movesEvaluated++;

                if (childScore > bestScore)
                {
                    bestScore = childScore;
                    bestMove = move;

                    if (childScore > alpha)
                    {
                        if (childScore >= beta)
                        {
                            _statistics.StoresBeta++;
                            _statistics.BetaCutoffs++;
                            _state.Table.Store(board.Key, move, depth, childScore, TranspositionTableFlags.Beta);

                            return beta;
                        }
                        alpha = childScore;
                    }
                }
            }

            if (movesEvaluated == 0)
            {
                return alpha;
            }

            if (alpha == initialAlpha)
            {
                _statistics.StoresAlpha++;
                _state.Table.Store(board.Key, bestMove, depth, alpha, TranspositionTableFlags.Alpha);
            }
            else
            {
                _statistics.StoresExact++;
                _state.Table.Store(board.Key, bestMove, depth, bestScore, TranspositionTableFlags.Exact);
            }

            return alpha;
        }

        private enum TranspositionTableProbeResult
        {
            Miss,
            HitContinue,
            HitCutoff
        }

        private void StoreTranspositionTable(ZobristKey key, Move move, int depth, int score, TranspositionTableFlag flag)
        {
            if (_stopper.ShouldStop())
            {
                return;
            }
            _state.Table.Store(key, move, depth, score, flag);
        }


        private TranspositionTableProbeResult ProbeTranspositionTable(ZobristKey key, int depth, int alpha, int beta, bool isPrincipalVariation, out TranspositionTableEntry entry)
        {
            var found = _state.Table.TryProbe(key, out entry);
            if (!found)
            {
                _statistics.HashMiss++;
                entry = default;
                return TranspositionTableProbeResult.Miss;
            }

            if (entry.Key != key)
            {
                _statistics.HashCollision++;
                return TranspositionTableProbeResult.Miss;
            }

            if (entry.Depth < depth)
            {
                _statistics.HashInsufficientDepth++;
                return TranspositionTableProbeResult.HitContinue;
            }

            switch (entry.Flag)
            {
                case TranspositionTableFlags.Alpha:
                    if (entry.Score <= alpha && !isPrincipalVariation)
                    {
                        _statistics.HashAlphaCutoff++;
                        return TranspositionTableProbeResult.HitCutoff;
                    }
                    else
                    {
                        _statistics.HashAlphaContinue++;
                        return TranspositionTableProbeResult.HitContinue;
                    }
                case TranspositionTableFlags.Beta:
                    if (entry.Score >= beta && !isPrincipalVariation)
                    {
                        _statistics.HashBetaCutoff++;
                        return TranspositionTableProbeResult.HitCutoff;
                    }
                    else
                    {
                        _statistics.HashBetaContinue++;
                        return TranspositionTableProbeResult.HitContinue;
                    }
                case TranspositionTableFlags.Exact:
                    _statistics.HashCutoffExact++;
                    return TranspositionTableProbeResult.HitCutoff;
                default:
                    throw new ArgumentOutOfRangeException(nameof(entry.Flag), entry.Flag, "Unknown flag");
            }
        }
    }
}
