//#define LOG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;
using ChessDotNet.Evaluation.V2;
using ChessDotNet.MoveGeneration;
using ChessDotNet.MoveGeneration.SlideGeneration;
using ChessDotNet.Searching;
using ChessDotNet.Testing;
using Force.DeepCloner;
using Bitboard = System.UInt64;
using ZobristKey = System.UInt64;
using Position = System.Byte;
using Piece = System.Byte;
using TranspositionTableFlag = System.Byte;

namespace ChessDotNet.Search2
{
    public class SearchService2
    {
        public event Action<SearchInfo> SearchInfo;

        private readonly MoveGenerator _possibleMoves;
        private readonly IEvaluationService _evaluation;
        private SearchState _state;
        private readonly SearchStopper _stopper;
        private readonly MoveOrderingService _moveOrdering;
        private readonly SearchStatistics _statistics;
        private readonly AttacksService _attacksService;
        private readonly MoveValidator _validator;
        private readonly PinDetector _pinDetector;
        private readonly SeeService _see;

        private Board _initialBoard;
        private SearchState _initialState;
        


        public SearchService2
        (
            MoveGenerator possibleMoves,
            IEvaluationService evaluation
        )
        {
            _possibleMoves = possibleMoves;
            _evaluation = evaluation;
            var slides = new MagicBitboardsService();
            _attacksService = new AttacksService(slides);
            _pinDetector = new PinDetector(slides);
            _validator = new MoveValidator(_attacksService, slides, _pinDetector);
            _see = new SeeService(_attacksService);
            _moveOrdering = new MoveOrderingService();
            _state = new SearchState();
            _stopper = new SearchStopper();
            _statistics = new SearchStatistics();
        }

        public void SetState(SearchState state)
        {
            _state = state;
        }

        public IList<Move> Run
        (
            Board board,
            SearchParameters parameters,
            CancellationToken token = default
        )
        {
            _stopper.NewSearch(parameters, board.WhiteToMove, token);
            _statistics.Reset();
            _state.OnNewSearch();
            //_initialBoard = board.Clone();
            //_initialState = _state.Clone();
            var log = SearchLog.New();

            if (SearchConstants.Multithreading)
            {
                RunLazySmp(board, log);
            }
            else
            {
                RunStandard(board, log);
            }

            log.PrintLastChild();

            var principalVariation = _state.TranspositionTable.GetSavedPrincipalVariation();
            var moves = principalVariation.Select(entry => entry.Move).ToList();

            //var moves = _state.PrincipalVariationTable.GetPrincipalVariation();
            return moves;
        }

        public void NewGame()
        {
            _state.OnNewGame();
        }

        private void RunStandard(Board board, SearchLog log)
        {
            var initialScore = RunInitialIteration(board, log);
            RunIterativeDeepening(0, board, initialScore, log);
        }

        private void RunLazySmp(Board board, SearchLog log)
        {
            var initialScore = RunInitialIteration(board, log);

            //RunIterativeDeepening(board, score, log);
            var tasks = new Task<int>[SearchConstants.ThreadCount];
            for (var threadId = 0; threadId < SearchConstants.ThreadCount; threadId++)
            {
                var threadId1 = threadId;
                var board1 = board.Clone();
                var task = Task.Run(() => RunIterativeDeepening(threadId1, board1, initialScore, log));
                tasks[threadId] = task;
            }

            var scores = new List<int>(SearchConstants.ThreadCount);
            for (var threadId = 0; threadId < SearchConstants.ThreadCount; threadId++)
            {
                var task = tasks[threadId];
                var threadScore = task.GetAwaiter().GetResult();
                scores.Add(threadScore);
            }

            var score = scores[0];
            for (var threadId = 1; threadId < SearchConstants.ThreadCount; threadId++)
            {
                var otherScore = scores[threadId];
                if (otherScore != score)
                {
                    throw new Exception();
                }
            }
        }

        //private void RunAbdada()
        //{
        //    var initialScore = RunInitialIteration(board, log);
        //    RunIterativeDeepening(0, board, initialScore, log);
        //}

        private int RunInitialIteration(Board board, SearchLog log)
        {
            const int initialDepth = SearchConstants.InitialDepth;

            _state.PrincipalVariationTable.SetCurrentDepth(initialDepth);
            var initialSearchLog = SearchLog.New();
            var initialScore = SearchToDepth(0, board, initialDepth, 0, -SearchConstants.Inf, SearchConstants.Inf, 0, true, true, initialSearchLog);
            log.AddChild(initialSearchLog);
            if (_stopper.IsStopped())
            {
                return initialScore;
            }
            _state.TranspositionTable.SavePrincipalVariation(board);
            _state.PrincipalVariationTable.SetSearchedDepth(initialDepth);
            LogOutput(0, board, initialDepth, initialScore);
            _state.Synchronize();
            return initialScore;
        }

        private int RunIterativeDeepening(int threadId, Board board, int score, SearchLog log)
        {
            var skipSize = new[] { 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4 };
            var skipPhase = new[] { 0, 1, 0, 1, 2, 3, 0, 1, 2, 3, 4, 5, 0, 1, 2, 3, 4, 5, 6, 7 };

            var startDepth = SearchConstants.InitialDepth + 1;
            //if (threadId % 1 == 1)
            //{
            //    startDepth++;
            //}
            //startDepth += threadId;

            for (var depth = startDepth; depth < SearchConstants.MaxDepth; depth++)
            {
                //if (threadId > 0)
                //{
                //    int i = (threadId - 1) % 20;
                //    if (((depth + rootPos.game_ply() + skipPhase[i]) / skipSize[i]) % 2)
                //        continue;
                //}

                _state.OnIterativeDeepen();
                _state.PrincipalVariationTable.SetCurrentDepth(depth);
                var iterativeLog = SearchLog.New();

                score = SearchAspirationWindow(threadId, board, depth, score, iterativeLog);

                log.AddChild(iterativeLog);

                if (!_stopper.IsStopped())
                {
                    _state.TranspositionTable.SavePrincipalVariation(board);
                    _state.PrincipalVariationTable.SetSearchedDepth(depth);
                }

                LogOutput(0, board, depth, score);
                //ValidatePv(); // TODO: validation
                //if (depth == 10)
                //{
                //    State.SaveState(board, _state);
                //    return score;
                //}

                if (_stopper.ShouldStopOnDepthIncrease(depth))
                {
                    return score;
                }
            }
            return 0;
        }

        private void LogOutput(int threadId, Board board, int depth, int score)
        {
            if (EngineOptions.Debug)
            {
                Console.WriteLine($"Thread {threadId}: {depth}");
            }

            if (threadId == 0)
            {
                ReportCurrentState(board);
                if (!_stopper.IsStopped())
                {
                    ReportSearchInfo(board, depth, score);
                }

                if (EngineOptions.Debug)
                {
                    Console.WriteLine();
                }
            }
        }

        private void ReportSearchInfo(Board board, int depth, int score)
        {
            var principalVariation = _state.TranspositionTable.GetSavedPrincipalVariation();
            var principalMoves = principalVariation.Select(entry => entry.Move).ToList();

            //var principalMoves = _state.PrincipalVariationTable.GetPrincipalVariation();

            var searchInfo = new SearchInfo();
            searchInfo.Depth = depth;
            searchInfo.Score = score;
            searchInfo.NodesSearched = _statistics.NodesSearched;
            searchInfo.Time = (long)_stopper.GetSearchedTime();
            searchInfo.PrincipalVariation = principalMoves;
            if (score > SearchConstants.MateThereshold)
            {
                searchInfo.MateIn = SearchConstants.MateScore - score;
            }
            SearchInfo?.Invoke(searchInfo);
        }

        private void ReportCurrentState(Board board)
        {
            if (!EngineOptions.Debug)
            {
                return;
            }

            Console.WriteLine($"{_statistics}");

            var currentTTPV = _state.TranspositionTable.GetPrincipalVariation(board);
            var currentTTPVStr = currentTTPV.Select(entry => entry.Move).ToPositionsString();
            Console.WriteLine($"CTT: {currentTTPVStr}");

            var savedTTPV = _state.TranspositionTable.GetSavedPrincipalVariation();
            var savedTTPVStr = savedTTPV.Select(entry => entry.Move).ToPositionsString();
            Console.WriteLine($"STT: {savedTTPVStr}");

            var principalVariation = _state.PrincipalVariationTable.GetPrincipalVariation();
            var principalVariationStr = principalVariation.ToPositionsString();
            Console.WriteLine($"PV: {principalVariationStr}");
            var nodes = _statistics.NodesSearched;
            var elapsed = _stopper.GetSearchedTime() / 1000;
            var nps = (long)(nodes / elapsed);
            Console.WriteLine($"NPS: {nps.ToUserFriendly()}");

            //if (transpositionTablePv.Count == 0)
            //{
            //    Console.WriteLine($"Failed to find a move. Key: {board.Key}");
            //    var hasEntry = _state.TranspositionTable.TryProbe(board.Key, out var entry);
            //    if(!hasEntry)
            //    {
            //        Console.WriteLine("NO ENTRY");
            //    }
            //    Console.WriteLine($"Key: {entry.Key}, Flag: {entry.Flag}, Depth: {entry.Depth}, Score: {entry.Score}, Move: {entry.Move.ToPositionString()}");
            //    Environment.Exit(-1);
            //}

        }

        private int SearchAspirationWindow(int threadId, Board board, int depth, int previousScore, SearchLog log)
        {
            if (!EngineOptions.UseAspirationWindows)
            {
                var score = SearchToDepth(threadId, board, depth, 0, -SearchConstants.Inf, SearchConstants.Inf, 0, true, true, log);
                return score;
            }

            const int window = 50;
            var alpha = previousScore - window;
            var beta = previousScore + window;
            var windowLog = SearchLog.New();
            var windowScore = SearchToDepth(threadId, board, depth, 0, alpha, beta, 0, true, true, windowLog);
            if (windowScore <= alpha || windowScore >= beta)
            {
                _statistics.AspirationFail++;
                var fullSearchLog = SearchLog.New();
                var fullSearchScore = SearchToDepth(threadId, board, depth, 0, -SearchConstants.Inf, SearchConstants.Inf, 0, true, true, fullSearchLog);
                log.AddChild(fullSearchLog);
                return fullSearchScore;
            }
            else
            {
                log.AddChild(windowLog);
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

        //private unsafe void Prefetch(ZobristKey key)
        //{
        //    //var size = sizeof(TranspositionTableEntry);
        //    var index = _state.TranspositionTable.GetTableIndex(key);
        //    fixed (TranspositionTableEntry* table = _state.TranspositionTable._entries)
        //    {
        //        //var entry = *(table + 3);
        //        System.Runtime.Intrinsics.X86.Sse.PrefetchNonTemporal(table + index);
        //    }
        //}

        private int SearchToDepth(int threadId, Board board, int depth, int ply, int alpha, int beta, int currentReduction, bool nullMoveAllowed, bool isPrincipalVariation, SearchLog log)
        {
            //if (ply == 5)
            //{
            //    State.SaveState(board, _state);
            //    Thread.Sleep(TimeSpan.FromDays(1));
            //}
            //_state.TranspositionTable._entries[3] = new TranspositionTableEntry(123, default, 1, 2, 3);
            //Prefetch(board.Key);

            log.AddMessage("Starting search", depth, alpha, beta);
            var threadState = _state.ThreadStates[threadId];
            var rootNode = ply == 0;

            // STOP CHECK
            if (depth > 2 && _stopper.ShouldStop())
            {
                var score = 0;
                log.AddMessage("Stop requested", depth, alpha, beta, score);
                return score;
            }

            // REPETITION DETECTION
            if (nullMoveAllowed && !rootNode)
            {
                var isRepetition = IsRepetition(board);
                if (isRepetition)
                {
                    _statistics.Repetitions++;
                    var score = 0;
                    log.AddMessage("Repetition detected", depth, alpha, beta, score);
                    return score;
                }
            }

            // MATE DISTANCE PRUNE
            var currentMateScore = SearchConstants.MateScore - ply;
            if (!rootNode)
            {
                if (alpha < -currentMateScore)
                {
                    _statistics.MateAlpha++;
                    alpha = -currentMateScore;
                    log.AddMessage("Mate alpha update", depth, alpha, beta);
                }

                if (beta > currentMateScore - 1)
                {
                    _statistics.MateBeta++;
                    beta = currentMateScore - 1;
                    log.AddMessage("Mate beta update", depth, alpha, beta);
                }

                if (alpha >= beta)
                {
                    _statistics.MateCutoff++;
                    log.AddMessage("Mate cutoff", depth, alpha, beta, alpha);
                    return alpha;
                }
            }

            // IN CHECK EXTENSION
            //var myKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            //var myKingPos = myKing.BitScanForward();
            //var inCheck = _attacksService.IsPositionAttacked(board, myKingPos, !board.WhiteToMove);
            var checkers = _attacksService.GetAttackersOfSide(board, board.KingPositions[board.ColorToMove], !board.WhiteToMove, board.AllPieces);
            var inCheck = checkers != 0;

            if (inCheck)
            {
                depth++;
                log.AddMessage("Check depth extension", depth, alpha, beta);
            }

            // QUIESCENCE
            if (depth <= 0)
            {
                //var evaluatedScore = _evaluation.Evaluate(board);
                //_statistics.NodesSearched++;
                log.AddMessage("Start quiescence search", depth, alpha, beta);
                var evaluatedScore = Quiescence(threadId, board, depth, ply, alpha, beta, log);
                log.AddMessage("End quiescence search", depth, alpha, beta, evaluatedScore);
                return evaluatedScore;
            }

            // PROBE TRANSPOSITION TABLE
            Move principalVariationMove = default;
            if (EngineOptions.UseTranspositionTable)
            {
                var probeSuccess = TryProbeTranspositionTable(board.Key, depth, alpha, beta, ref principalVariationMove, out var probedScore, out var exact);
                if (probeSuccess)
                {
                    if (!isPrincipalVariation || (probedScore > alpha && probedScore < beta))
                    {
                        if (probedScore > alpha && probedScore < beta)
                        {
                            _state.PrincipalVariationTable.SetBestMove(ply, principalVariationMove);
                        }

                        if (probedScore > SearchConstants.MateThereshold)
                        {
                            probedScore -= ply;
                        }
                        else if (probedScore < -SearchConstants.MateThereshold)
                        {
                            probedScore += ply;
                        }

                        return probedScore;
                    }
                }
            }

            // STATIC EVALUATION PRUNING
            var staticScore = _evaluation.Evaluate(board);
            //board.WhiteToMove = !board.WhiteToMove;
            //var staticScore2 = _evaluation.Evaluate(board);
            //Debug.Assert(staticScore == staticScore2);
            //board.WhiteToMove = !board.WhiteToMove;
            if
            (
                EngineOptions.UseStaticEvaluationPruning
                && depth < 3
                && !isPrincipalVariation
                && !inCheck
            )
            {
                if (!(Math.Abs(beta - 1) > -SearchConstants.MateScore + 100))
                {
                    Console.WriteLine("Wow, static eval");
                    Environment.Exit(-1);
                }

                var margin = 120 * depth; // 120? Pawn is 100
                if (staticScore - margin >= beta)
                {
                    _statistics.StaticEvaluationCutoffs++;
                    return staticScore - margin;
                }
            }

            // NULL MOVE PRUNING
            if
            (
                EngineOptions.UseNullMovePruning
                && nullMoveAllowed
                && !inCheck
                && depth > 2
                && ply > 0
            )
            {
                var material = board.PieceMaterial[board.ColorToMove];
                if (material > SearchConstants.EndgameMaterial)
                {
                    var nullDepthReduction = depth > 6 ? 3 : 2;
                    var nullMove = new Move(0, 0, ChessPiece.Empty);
                    board.DoMove2(nullMove);
                    //var nullBoard = board.DoMove(nullMove);
                    var nullLog = SearchLog.New(nullMove);
                    log.AddMessage($"Start null move search, R={nullDepthReduction}", depth, alpha, beta);
                    var nullMoveScore = -SearchToDepth(threadId, board, depth - nullDepthReduction - 1, ply + 1, -beta, -beta + 1, currentReduction, false, false, nullLog);
                    board.UndoMove();
                    log.AddMessage($"End null move search, R={nullDepthReduction}, S={nullMoveScore}", depth);
                    if (nullMoveScore >= beta)
                    {
                        log.AddMessage("Null move cutoff");
                        log.AddChild(nullLog);
                        _statistics.NullMoveCutoffs++;
                        return beta;
                    }
                }
            }

            // RAZORING
            if
            (
                EngineOptions.UseRazoring
                && !isPrincipalVariation
                && !inCheck
                && principalVariationMove.Key2 == 0
                && nullMoveAllowed
                && depth <= 3
            )
            {
                var threshold = alpha - 300 - (depth - 1) * 60;
                if (staticScore < threshold)
                {
                    // TODO depth? log?
                    var razoredScore = Quiescence(threadId, board, depth - 1, ply + 1, alpha, beta, log);
                    if (razoredScore < threshold)
                    {
                        _statistics.RazoringSuccess++;
                        return alpha;
                    }
                    _statistics.RazoringFail++;
                }
            }


            // FUTILITY PRUNING - DETECTION
            var futilityPruning = false;
            var futilityMargins = new int[4] { 0, 200, 300, 500 };
            if
            (
                EngineOptions.UseFutilityPruning
                && depth <= 3
                && !isPrincipalVariation
                && !inCheck
                && Math.Abs(alpha) < 9000
                && staticScore + futilityMargins[depth] <= alpha
            )
            {
                futilityPruning = true;
            }


            var bestScore = -SearchConstants.Inf;
            Move bestMove = default;
            SearchLog bestLog = default;
            // CHILD SEARCH
            var initialAlpha = alpha;
            var potentialMoves = threadState.Moves[ply];
            var moveCount = 0;
            var movesEvaluated = 0;
            var raisedAlpha = false;

            _possibleMoves.GetAllPotentialMoves(board, potentialMoves, ref moveCount);
            var seeScores = threadState.SeeScores[ply];
            _see.CalculateSeeScores(board, potentialMoves, moveCount, seeScores);
            var moveStaticScores = threadState.MoveStaticScores[ply];
            _moveOrdering.CalculateStaticScores(board, potentialMoves, moveCount, ply, principalVariationMove, threadState.Killers, EngineOptions.UseSeeOrdering, seeScores, moveStaticScores);
            
            
            var pinnedPieces = _pinDetector.GetPinned(board, board.ColorToMove, board.KingPositions[board.ColorToMove]);
            for (var moveIndex = 0; moveIndex < moveCount; moveIndex++)
            {
                _moveOrdering.OrderNextMove(moveIndex, potentialMoves, moveStaticScores, seeScores, moveCount, threadState.History);
                var move = potentialMoves[moveIndex];
                var seeScore = seeScores[moveIndex];
                var kingSafe = _validator.IsKingSafeAfterMove2(board, move, checkers, pinnedPieces);
                if (!kingSafe)
                {
                    continue;
                }

                board.DoMove2(move);
                var childLog = SearchLog.New(move);

                // FUTILITY PRUNING - EXECUTION
                if
                (
                    futilityPruning
                    && movesEvaluated > 0
                    && move.TakesPiece == ChessPiece.Empty
                    && move.PawnPromoteTo == ChessPiece.Empty
                )
                {
                    //var attacks = _possibleMoves.AttacksService.GetAllAttacked(board, !board.WhiteToMove);
                    //var opponentKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
                    //var opponentInCheck = (attacks & opponentKing) != 0;

                    var opponentKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
                    var opponentKingPos = opponentKing.BitScanForward();
                    var opponentInCheck = _attacksService.IsPositionAttacked(board, opponentKingPos, !board.WhiteToMove);
                    //var opponentInCheck = board.Checkers != 0;
                    if (!opponentInCheck)
                    {
                        _statistics.FutilityReductions++;
                        board.UndoMove();
                        continue;
                    }
                }

                //var childDepth = depth - 1;
                var reduction = 0;
                // LATE MOVE REDUCTION
                threadState.Cutoff[move.ColorToMove][move.From][move.To] -= 1;
                if
                (
                    EngineOptions.UseLateMoveReductions
                    && !isPrincipalVariation
                    && movesEvaluated > 3
                    //&& (!rootNode || movesEvaluated > 3)
                    && depth > 4
                    && !inCheck
                    //&& threadState.Cutoff[move.ColorToMove][move.From][move.To] < 50
                    //&& currentReduction == 0
                    && move.Key2 != threadState.Killers[ply][0]
                    && move.Key2 != threadState.Killers[ply][1]
                    && seeScore <= 0
                    //&& move.TakesPiece == ChessPiece.Empty
                    && move.PawnPromoteTo == ChessPiece.Empty
                )
                {
                    var opponentKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
                    var opponentKingPos = opponentKing.BitScanForward();
                    var opponentInCheck = _attacksService.IsPositionAttacked(board, opponentKingPos, !board.WhiteToMove);
                    //var opponentInCheck = board.Checkers != 0;
                    if (!opponentInCheck)
                    {
                        threadState.Cutoff[move.ColorToMove][move.From][move.To] = 50;
                        _statistics.LateMoveReductions1++;
                        reduction++;
                        if (movesEvaluated > 6)
                        {
                            _statistics.LateMoveReductions2++;
                            reduction++;
                        }
                        //if (movesEvaluated > 10)
                        //{
                        //    _statistics.LateMoveReductions2++;
                        //    reduction++;
                        //}
                    }
                }

                int childScore;
                if (EngineOptions.UsePrincipalVariationSearch && raisedAlpha)
                {
                    childScore = -SearchToDepth(threadId, board, depth - 1 - reduction, ply + 1, -alpha - 1, -alpha, currentReduction + reduction, true, false, childLog);
                    if (childScore > alpha)
                    {
                        childScore = -SearchToDepth(threadId, board, depth - 1 - reduction, ply + 1, -beta, -alpha, currentReduction + reduction, true, isPrincipalVariation, childLog);
                        _statistics.PvsScoutFail++;
                    }
                    else
                    {
                        _statistics.PvsScoutSuccess++;
                    }
                }
                else
                {
                    childScore = -SearchToDepth(threadId, board, depth - 1 - reduction, ply + 1, -beta, -alpha, currentReduction + reduction, true, isPrincipalVariation, childLog);
                }

                if (reduction > 0 && childScore > alpha)
                {
                    _statistics.LateMoveFail++;
                    //childScore = -SearchToDepth(threadId, board, depth - 1, ply + 1, -alpha - 1, -alpha, currentReduction, true, false, childLog);
                    //if (childScore > alpha)
                    //{
                    //    childScore = -SearchToDepth(threadId, board, depth - 1, ply + 1, -beta, -alpha, currentReduction, true, isPrincipalVariation, childLog);
                    //    _statistics.PvsScoutFail++;
                    //}
                    //else
                    //{
                    //    _statistics.PvsScoutSuccess++;
                    //}
                    childScore = -SearchToDepth(threadId, board, depth - 1, ply + 1, -beta, -alpha, currentReduction, true, isPrincipalVariation, childLog);
                }


                board.UndoMove();
                //log.AddMessage($"Searched move {move.ToPositionString()}, child score {childScore}", depth, alpha, beta);

                movesEvaluated++;

                if (childScore > bestScore)
                {
                    bestScore = childScore;
                    bestMove = move;
                    bestLog = childLog;

                    if (childScore > alpha)
                    {
                        threadState.Cutoff[move.ColorToMove][move.From][move.To] += 6;
                        if (childScore >= beta)
                        {
                            _statistics.StoresBeta++;
                            _statistics.BetaCutoffs++;
                            if (EngineOptions.UseTranspositionTable)
                            {
                                StoreTranspositionTable(board.Key, move, depth, childScore, TranspositionTableFlags.Beta);
                            }

                            if (move.TakesPiece == ChessPiece.Empty)
                            {
                                threadState.Killers[ply][1] = threadState.Killers[ply][0];
                                threadState.Killers[ply][0] = move.Key2;
                            }
                            log.AddChild(childLog);
                            log.AddMessage($"Beta cutoff, move {move.ToPositionString()}, child score {childScore}", depth, alpha, beta, beta);
                            return beta;
                        }

                        if (move.TakesPiece == ChessPiece.Empty)
                        {
                            threadState.History[move.ColorToMove][move.From][move.To] += depth * depth;
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
                    log.AddMessage($"Mate detected", depth, alpha, beta, -currentMateScore);
                    alpha = -currentMateScore;
                }
                else
                {
                    _statistics.Stalemates++;
                    log.AddMessage($"Stalemate detected", depth, alpha, beta, 0);
                    alpha = 0;
                }
                return alpha; // Should we store to TT if it's mate / stalemate?
            }

            if (alpha == initialAlpha)
            {
                _statistics.StoresAlpha++;
                if (EngineOptions.UseTranspositionTable)
                {
                    StoreTranspositionTable(board.Key, bestMove, depth, alpha, TranspositionTableFlags.Alpha);
                }
                log.AddMessage($"Didn't increase alpha, best move {bestMove.ToPositionString()}, best score {bestScore}", depth, alpha, beta, alpha);
            }
            else
            {
                _statistics.StoresExact++;
                _state.PrincipalVariationTable.SetBestMove(ply, bestMove);
                if (EngineOptions.UseTranspositionTable)
                {
                    StoreTranspositionTable(board.Key, bestMove, depth, bestScore, TranspositionTableFlags.Exact);
                }
                //ValidatePv(); // TODO: validation
                log.AddMessage($"Increased alpha, best move {bestMove.ToPositionString()}, best score {bestScore}", depth, alpha, beta, alpha);
            }

            log.AddChild(bestLog);
            return alpha;
        }

        private bool IsRepetition(Board board)
        {
            for (var i = board.LastTookPieceHistoryIndex; i < board.HistoryDepth; i++)
            {
                var previousEntry = board.History2[i];
                var previousKey = previousEntry.Key;
                if (board.Key == previousKey)
                {
                    return true;
                }
            }
            return false;
        }

        private int Quiescence(int threadId, Board board, int depth, int ply, int alpha, int beta, SearchLog log)
        {
            var threadState = _state.ThreadStates[threadId];

            var standPat = _evaluation.Evaluate(board);
            _statistics.NodesSearched++;

            if (standPat >= beta)
            {
                return beta;
            }

            if (alpha < standPat)
            {
                alpha = standPat;
            }


            // PROBE TRANSPOSITION TABLE
            Move principalVariationMove = default;
            if (EngineOptions.UseTranspositionTableQuiessence)
            {
                var probeSuccess = TryProbeTranspositionTable(board.Key, depth, alpha, beta, ref principalVariationMove, out var probedScore, out var exact);
                if (probeSuccess)
                {
                    //if (!isPrincipalVariation || (probedScore > alpha && probedScore < beta))
                    //if(false)
                    {
                        if (probedScore > alpha && probedScore < beta)
                        {
                            _state.PrincipalVariationTable.SetBestMove(ply, principalVariationMove);
                        }

                        if (probedScore > SearchConstants.MateThereshold)
                        {
                            probedScore -= ply;
                        }
                        else if (probedScore < -SearchConstants.MateThereshold)
                        {
                            probedScore += ply;
                        }

                        return probedScore;
                    }
                }
            }

            //const int Delta = 1000; // queen value
            //if (standPat + Delta < alpha)
            //{
            //    // if no move can improve alpha, return
            //    return alpha;
            //}

            var bestScore = -SearchConstants.Inf;
            Move bestMove = default;
            SearchLog bestLog = default;

            var initialAlpha = alpha;
            var potentialMoves = threadState.Moves[ply];
            var moveCount = 0;
            var movesEvaluated = 0;

            _possibleMoves.GetAllPotentialCaptures(board, potentialMoves, ref moveCount);
            var seeScores = threadState.SeeScores[ply];
            _see.CalculateSeeScores(board, potentialMoves, moveCount, seeScores);
            var moveStaticScores = threadState.MoveStaticScores[ply];
            _moveOrdering.CalculateStaticScores(board, potentialMoves, moveCount, ply, principalVariationMove, threadState.Killers, EngineOptions.UseSeeOrdering, seeScores, moveStaticScores);
            var checkers = _attacksService.GetAttackersOfSide(board, board.KingPositions[board.ColorToMove], !board.WhiteToMove, board.AllPieces);
            var pinnedPieces = _pinDetector.GetPinned(board, board.ColorToMove, board.KingPositions[board.ColorToMove]);
            for (var moveIndex = 0; moveIndex < moveCount; moveIndex++)
            {
                _moveOrdering.OrderNextMove(moveIndex, potentialMoves, moveStaticScores, seeScores, moveCount, threadState.History);
                var move = potentialMoves[moveIndex];
                Debug.Assert(move.TakesPiece != ChessPiece.Empty);

                var kingSafe = _validator.IsKingSafeAfterMove2(board, move, checkers, pinnedPieces);
                if (!kingSafe)
                {
                    continue;
                }

                //if (move.TakesPiece == ChessPiece.WhiteKing || move.TakesPiece == ChessPiece.BlackKing)
                //{
                //    var a = 123;
                //}

                var takesMaterial = EvaluationData.PIECE_VALUE[move.TakesPiece];
                var opponentMaterial = board.PieceMaterial[board.ColorToMove ^ 1];
                var resultMaterial = opponentMaterial - takesMaterial;

                // DELTA PRUNING
                if
                (
                    EngineOptions.UseDeltaPruning
                    && standPat + takesMaterial + 200 < alpha
                    && resultMaterial > SearchConstants.EndgameMaterial
                    && move.PawnPromoteTo == ChessPiece.Empty
                )
                {
                    //var opponentKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
                    //var opponentKingPos = opponentKing.BitScanForward();
                    //var opponentInCheck = _possibleMoves.AttacksService.IsPositionAttacked(board, opponentKingPos, !board.WhiteToMove);
                    //if (!opponentInCheck)
                    {
                        _statistics.DeltaPruning++;
                        continue;
                    }
                        
                }

                // SEE PRUNING
                if
                (
                    EngineOptions.UseSeePruning
                    && move.PawnPromoteTo == ChessPiece.Empty
                    && (
                        move.TakesPiece == ChessPiece.WhitePawn
                        || move.TakesPiece == ChessPiece.BlackPawn
                        || resultMaterial > SearchConstants.EndgameMaterial
                    )
                )
                {
                    //var opponentKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
                    //var opponentKingPos = opponentKing.BitScanForward();
                    //var opponentInCheck = _possibleMoves.AttacksService.IsPositionAttacked(board, opponentKingPos, !board.WhiteToMove);
                    //if (!opponentInCheck)
                    {

                        var seeScore = seeScores[moveIndex];
                        if (seeScore < 0) // TODO: -10?
                        {
                            _statistics.SeePruning++;
                            continue;
                        }
                    }
                }
                    

                //var childBoard = board.DoMove(move);
                board.DoMove2(move);
                var childLog = SearchLog.New(move);

                var childScore = -Quiescence(threadId, board, depth - 1, ply + 1, -beta, -alpha, childLog);
                board.UndoMove();
                movesEvaluated++;

                if (childScore > bestScore)
                {
                    bestScore = childScore;
                    bestMove = move;
                    bestLog = childLog;

                    if (childScore > alpha)
                    {
                        if (childScore >= beta)
                        {
                            _statistics.StoresBeta++;
                            _statistics.BetaCutoffs++;
                            if (EngineOptions.UseTranspositionTableQuiessence)
                            {
                                StoreTranspositionTable(board.Key, move, depth, childScore, TranspositionTableFlags.Beta);
                            }

                            log.AddChild(childLog);
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
                if (EngineOptions.UseTranspositionTableQuiessence)
                {
                    _statistics.StoresAlpha++;
                    StoreTranspositionTable(board.Key, bestMove, depth, alpha, TranspositionTableFlags.Alpha);
                }
            }
            else
            {
                if (EngineOptions.UseTranspositionTableQuiessence)
                {
                    _statistics.StoresExact++;
                    StoreTranspositionTable(board.Key, bestMove, depth, bestScore, TranspositionTableFlags.Exact);
                }
            }
            log.AddChild(bestLog);
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
            _state.TranspositionTable.Store(key, move, depth, score, flag);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryProbeTranspositionTable(ZobristKey key, int depth, int alpha, int beta, ref Move bestMove, out int score, out bool exact)
        {
            score = default;
            exact = false;

            var found = _state.TranspositionTable.TryProbe(key, out var entry, out var entryKey);
            if (!found)
            {
                _statistics.HashMiss++;
                return false;
            }

            if (entryKey != key)
            {
                _statistics.HashCollision++;
                return false;
            }

            //if (entry.Key2 != key2)
            //{
            //    var a = 123;
            //}

            bestMove = entry.Move;

            if (entry.Depth < depth)
            {
                _statistics.HashInsufficientDepth++;
                return false;
            }

            switch (entry.Flag)
            {
                case TranspositionTableFlags.Exact:
                    exact = true;
                    score = entry.Score;
                    return true;
                case TranspositionTableFlags.Alpha:
                    if (entry.Score <= alpha)
                    {
                        score = alpha;
                        return true;
                    }

                    return false;
                case TranspositionTableFlags.Beta:
                    if (entry.Score >= beta)
                    {
                        score = beta;
                        return true;
                    }

                    return false;
                default:
                    Debug.Assert(false);
                    break;
            }

            return false;
        }
        
        private void ValidatePv()
        {
            //var pv = _state.TranspositionTable.GetSavedPrincipalVariation();
            var pv = _state.TranspositionTable.GetPrincipalVariation(_initialBoard);

            var moves = pv.Select(x => x.Move).ToList();
            var clone = _initialBoard.Clone();
            var possibleMoves = new Move[SearchConstants.MaxDepth];
            var moveCount = 0;
            for (var i = 0; i < moves.Count; i++)
            {
                var move = moves[i];
                moveCount = 0;
                _possibleMoves.GetAllPossibleMoves(clone, possibleMoves, ref moveCount);
                var ok = false;
                for (int j = 0; j < moveCount; j++)
                {
                    var possibleMove = possibleMoves[j];
                    if (possibleMove.Key2 == move.Key2)
                    {
                        ok = true;
                        break;
                    }
                }

                if (!ok)
                {
                    State.SaveState(_initialBoard, _initialState);
                    Dump.CreateDump();
                }

                clone.DoMove2(move);
            }
        }
    }
}
