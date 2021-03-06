﻿//#define LOG

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
using ChessDotNet.MoveGeneration.SlideGeneration.Magics;
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
            _state.OriginalColor = board.ColorToMove;
            //board.NnueData.Reset();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Contempt(Board board)
        {
            return 0;
            var score = -10;
            if (board.PieceMaterial[_state.OriginalColor] < SearchConstants.EndgameMaterial)
            {
                score = 0;
            }

            if (board.ColorToMove != _state.OriginalColor)
            {
                score = -score;
            }

            return score;
        }

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
                var score = Contempt(board);
                log.AddMessage("Stop requested", depth, alpha, beta, score);
                return score;
            }

            // REPETITION DETECTION
            if (nullMoveAllowed && !rootNode)
            {
                var isRepetition = IsRepetitionOr50Move(board);
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
            var checkers = _attacksService.GetCheckers(board);
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

            //Array.Fill(threadState.CurrentContinuations, _state.EmptyContinuation);
            //if (ply > 0)
            //{
            //    var move = board.History2[board.HistoryDepth - 1].Move;
            //    threadState.CurrentContinuations[0] = threadState.AllContinuations[move.Piece][move.To];
            //    if (ply > 1)
            //    {
            //        move = board.History2[board.HistoryDepth - 2].Move;
            //        threadState.CurrentContinuations[1] = threadState.AllContinuations[move.Piece][move.To];
            //        if (ply > 3)
            //        {
            //            move = board.History2[board.HistoryDepth - 4].Move;
            //            threadState.CurrentContinuations[2] = threadState.AllContinuations[move.Piece][move.To];
            //            if (ply > 5)
            //            {
            //                move = board.History2[board.HistoryDepth - 6].Move;
            //                threadState.CurrentContinuations[3] = threadState.AllContinuations[move.Piece][move.To];
            //            }
            //        }
            //    }
            //}


            // PROBE TRANSPOSITION TABLE
            Move principalVariationMove = default;
            bool hashEntryExists = true;
            var bonus = depth * depth + depth - 1;
            if (EngineOptions.UseTranspositionTable)
            {
                var probeSuccess = TryProbeTranspositionTable(board.Key, depth, alpha, beta, ref principalVariationMove, out var probedScore, out hashEntryExists);
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

                        if (principalVariationMove.TakesPiece == ChessPiece.Empty)
                        {
                            UpdateHistory(threadState, principalVariationMove, bonus);
                        }

                        return probedScore;
                    }
                }
            }

            Span<ulong> pins = stackalloc ulong[2];
            _pinDetector.GetPinnedToKings(board, pins);
            
            // STATIC EVALUATION PRUNING
            var staticScore = _evaluation.Evaluate(board, pins);
            board.Evaluation = staticScore;
            var improving = board.HistoryDepth < 2 || staticScore >= board.History2[board.HistoryDepth - 2].Evaluation;
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

            //if
            //(
            //    isPrincipalVariation
            //    && !inCheck
            //    && !hashEntryExists
            //    && depth >= 6
            //)
            //{
            //    depth -= 2;
            //}

            // CHILD SEARCH
            var bestScore = -SearchConstants.Inf;
            Move bestMove = default;
            SearchLog bestLog = default;




            var initialAlpha = alpha;
            var potentialMoves = threadState.Moves[ply];
            var moveCount = 0;
            var movesEvaluated = 0;
            var raisedAlpha = false;

            var pinnedPieces = pins[board.ColorToMove];
            _possibleMoves.GetAllPotentialMoves(board, potentialMoves, ref moveCount, checkers, pinnedPieces);
            var seeScores = threadState.SeeScores[ply];
            _see.CalculateSeeScores(board, potentialMoves, moveCount, seeScores);
            var moveStaticScores = threadState.MoveStaticScores[ply];
            var previousMove = rootNode ? default : board.History2[board.HistoryDepth - 1].Move;
            var countermove = threadState.Countermove[previousMove.Piece][previousMove.To];
            //var searched = new List<Move>();
            var failedMoves = threadState.FailedMoves[ply];
            var failedMoveCount = 0;
            _moveOrdering.CalculateStaticScores(board, potentialMoves, moveCount, ply, principalVariationMove, threadState.Killers, EngineOptions.UseSeeOrdering, seeScores, countermove, moveStaticScores);

            var betaCutoff = false;
            for (var moveIndex = 0; moveIndex < moveCount; moveIndex++)
            {
                //var countermove2 = threadState.Countermove[previousMove.Piece][previousMove.To];
                _moveOrdering.OrderNextMove(moveIndex, potentialMoves, moveStaticScores, seeScores, moveCount, threadState);
                var move = potentialMoves[moveIndex];
                var seeScore = seeScores[moveIndex];
                var kingSafe = _validator.IsKingSafeAfterMove2(board, move, checkers, pinnedPieces);
                if (!kingSafe)
                {
                    continue;
                }

                board.DoMove2(move);
                var childLog = SearchLog.New(move);

                //if
                //(
                //    !rootNode
                //    && move.TakesPiece == ChessPiece.Empty
                //    && move.PawnPromoteTo == ChessPiece.Empty
                //    && move.Key2 != threadState.Killers[ply][0]
                //    && move.Key2 != threadState.Killers[ply][1]
                //    && !inCheck
                //)
                //{
                //    var opponentKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
                //    var opponentKingPos = opponentKing.BitScanForward();
                //    var opponentInCheck = _attacksService.IsPositionAttacked(board, opponentKingPos, !board.WhiteToMove);
                //    //var opponentInCheck = board.Checkers != 0;
                //    if (!opponentInCheck)
                //    {
                //        if (depth < 16)
                //        {
                //            if (movesEvaluated >= SearchConstants.FutilityMoveCounts[improving ? 1 : 0][depth])
                //            {
                //                board.UndoMove();
                //                continue;
                //            }
                //        }

                //        if
                //        (
                //            depth <= 4
                //            && threadState.History[move.ColorToMove][move.From][move.To] < 0
                //            && threadState.PieceToHistory[move.Piece][move.To] < 0
                //        )
                //        {
                //            board.UndoMove();
                //            continue;
                //        }
                //    }
                //}

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

                //if
                //(
                //    !inCheck
                //    && !rootNode
                //    && !raisedAlpha
                //    && !isPrincipalVariation
                //    && movesEvaluated > 4
                //    && seeScore < -10
                //    && seeScore < depth * -350
                //    && move.PawnPromoteTo == ChessPiece.Empty
                //)
                //{
                //    var opponentKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
                //    var opponentKingPos = opponentKing.BitScanForward();
                //    var opponentInCheck = _attacksService.IsPositionAttacked(board, opponentKingPos, !board.WhiteToMove);
                //    if (!opponentInCheck)
                //    {
                //        //_statistics.SeePruning++;
                //        board.UndoMove();
                //        continue;
                //    }
                //}

                //var childDepth = depth - 1;
                var reduction = 0;
                // LATE MOVE REDUCTION

                threadState.Cutoff[move.ColorToMove][move.From][move.To] -= 1;
                if
                (
                    EngineOptions.UseLateMoveReductions
                    //&& !isPrincipalVariation
                    && movesEvaluated > 1
                    && (!rootNode || movesEvaluated > 3)
                    && depth >= 3
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
                    //var opponentKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
                    //var opponentKingPos = opponentKing.BitScanForward();
                    //var opponentInCheck = _attacksService.IsPositionAttacked(board, opponentKingPos, !board.WhiteToMove);
                    ////var opponentInCheck = board.Checkers != 0;
                    //if (!opponentInCheck)
                    {
                        reduction = SearchConstants.Reductions[isPrincipalVariation ? 1 : 0][improving ? 1 : 0][depth][movesEvaluated];

                        //if
                        //(
                        //    false
                        //    //|| !isPrincipalVariation
                        //    || threadState.PieceToHistory[move.Piece][move.To] < 0
                        //)
                        //{
                        //    reduction++;
                        //}

                        //if
                        //((
                        //    false
                        //    //|| !isPrincipalVariation
                        //    || threadState.PieceToHistory[move.Piece][move.To] > 0
                        //) && reduction > 0)
                        //{
                        //    reduction--;
                        //}

                        //if
                        //((
                        //    false
                        //    //|| !isPrincipalVariation
                        //    || threadState.PieceToHistory[move.Piece][move.To] > 0
                        //) && reduction > 0)
                        //{

                        //    reduction--;
                        //}

                        //threadState.Cutoff[move.ColorToMove][move.From][move.To] = 50;
                        //_statistics.LateMoveReductions1++;
                        //reduction++;
                        //if (movesEvaluated > 6)
                        //{
                        //    _statistics.LateMoveReductions2++;
                        //    reduction++;
                        //}

                        //if (movesEvaluated > 12)
                        //{
                        //    _statistics.LateMoveReductions2++;
                        //    reduction++;
                        //}

                        //if (seeScore < -1)
                        //{
                        //    reduction++;
                        //}

                        //if (seeScore < -150)
                        //{
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

                        //if (bestMove.TakesPiece == ChessPiece.Empty)
                        //{
                        //    //threadState.History[bestMove.ColorToMove][bestMove.From][bestMove.To] -= (threadState.History[bestMove.ColorToMove][bestMove.From][bestMove.To] * bonus) >> 9;
                        //    threadState.History[bestMove.ColorToMove][bestMove.From][bestMove.To] += bonus/* << 6*/;
                        //    //foreach (var continuation in threadState.CurrentContinuations)
                        //    //{
                        //    //    if (continuation == null)
                        //    //    {
                        //    //        break;
                        //    //    }

                        //    //    continuation.Scores[bestMove.Piece][bestMove.To] -= (continuation.Scores[bestMove.Piece][bestMove.To] * bonus) >> 9;
                        //    //    continuation.Scores[bestMove.Piece][bestMove.To] += bonus << 6;
                        //    //}

                        //    //threadState.PieceToHistory[move.Piece][move.To] += depth * depth;
                        //}
                        //else
                        //{
                        //    //threadState.CaptureHistory[bestMove.Piece][bestMove.To][bestMove.TakesPiece] -= (threadState.CaptureHistory[bestMove.Piece][bestMove.To][bestMove.TakesPiece] * bonus) >> 9;
                        //    threadState.CaptureHistory[bestMove.Piece][bestMove.To][bestMove.TakesPiece] += bonus/* << 6*/;
                        //}
                        alpha = childScore;
                        raisedAlpha = true;

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
                                threadState.Countermove[previousMove.Piece][previousMove.To] = move.Key2;
                            }

                            betaCutoff = true;
                            log.AddChild(childLog);
                            log.AddMessage($"Beta cutoff, move {move.ToPositionString()}, child score {childScore}", depth, alpha, beta, beta);
                            break;
                        }
                    }
                    else
                    {
                        //if (move.TakesPiece == ChessPiece.Empty)
                        //{
                        //    threadState.History[move.ColorToMove][move.From][move.To] -= depth * depth / 50;
                        //    foreach (var continuation in threadState.CurrentContinuations)
                        //    {
                        //        if (continuation == null)
                        //        {
                        //            break;
                        //        }
                        //        continuation.Scores[move.Piece][move.To] -= depth * depth / 50;
                        //    }

                        //    //threadState.PieceToHistory[move.Piece][move.To] += depth * depth;
                        //}
                        //else
                        //{
                        //    threadState.CaptureHistory[move.Piece][move.To][move.TakesPiece] -= depth * depth / 50;
                        //}
                    }
                }
                else
                {
                    failedMoves[failedMoveCount++] = move;
                }
            }

            if (raisedAlpha)
            {
                if (bestMove.TakesPiece == ChessPiece.Empty)
                {
                    //threadState.History[bestMove.ColorToMove][bestMove.From][bestMove.To] -= (threadState.History[bestMove.ColorToMove][bestMove.From][bestMove.To] * bonus) >> 9;
                    //threadState.History[bestMove.ColorToMove][bestMove.From][bestMove.To] += bonus /*<< 6*/;
                    UpdateHistory(threadState, bestMove, bonus);
                    //foreach (var continuation in threadState.CurrentContinuations)
                    //{
                    //    if (continuation == null)
                    //    {
                    //        break;
                    //    }

                    //    continuation.Scores[bestMove.Piece][bestMove.To] -= (continuation.Scores[bestMove.Piece][bestMove.To] * bonus) >> 9;
                    //    continuation.Scores[bestMove.Piece][bestMove.To] += bonus << 6;
                    //}

                    //threadState.PieceToHistory[move.Piece][move.To] += depth * depth;
                }
                else
                {
                    //threadState.CaptureHistory[bestMove.Piece][bestMove.To][bestMove.TakesPiece] -= (threadState.CaptureHistory[bestMove.Piece][bestMove.To][bestMove.TakesPiece] * bonus) >> 9;
                    threadState.CaptureHistory[bestMove.Piece][bestMove.To][bestMove.TakesPiece] += bonus /*<< 6*/;
                }

                for (var i = 0; i < failedMoveCount; i++)
                {
                    var move = failedMoves[i];
                    if (move.TakesPiece == ChessPiece.Empty)
                    {
                        UpdateHistory(threadState, move, -bonus);
                        //threadState.History[move.ColorToMove][move.From][move.To] -= (threadState.History[move.ColorToMove][move.From][move.To] * bonus) >> 9;
                        //threadState.History[move.ColorToMove][move.From][move.To] -= bonus << 6;
                        //foreach (var continuation in threadState.CurrentContinuations)
                        //{
                        //    if (continuation == null)
                        //    {
                        //        break;
                        //    }
                        //    continuation.Scores[move.Piece][move.To] -= (continuation.Scores[move.Piece][move.To] * bonus) >> 9;
                        //    continuation.Scores[move.Piece][move.To] -= bonus << 6;
                        //}

                        //threadState.PieceToHistory[move.Piece][move.To] += depth * depth;
                    }
                    else
                    {
                        //threadState.CaptureHistory[move.Piece][move.To][move.TakesPiece] -= (threadState.CaptureHistory[move.Piece][move.To][move.TakesPiece] * bonus) >> 9;
                        //threadState.CaptureHistory[move.Piece][move.To][move.TakesPiece] -= bonus << 6;
                    }
                }
            }

            if(betaCutoff)
            {
                return beta;
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
                    alpha = Contempt(board);
                    log.AddMessage($"Stalemate detected", depth, alpha, beta, alpha);
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

        private void UpdateHistory(ThreadUniqueState state, Move move, int value)
        {
            var abs = Math.Abs(value);
            if (abs >= 324)
            {
                return;
            }

            state.History[move.ColorToMove][move.From][move.To] -= state.History[move.ColorToMove][move.From][move.To] * abs / 324;
            state.History[move.ColorToMove][move.From][move.To] += value * 32;

            state.PieceToHistory[move.Piece][move.To] -= state.PieceToHistory[move.Piece][move.To] * abs / 324;
            state.PieceToHistory[move.Piece][move.To] += value * 32;

            foreach (var continuation in state.CurrentContinuations)
            {
                if (continuation == null)
                {
                    break;
                }

                continuation.Scores[move.Piece][move.To] -= (continuation.Scores[move.Piece][move.To] * value) / 324;
                continuation.Scores[move.Piece][move.To] += value * 32;
            }
        }

        private bool IsRepetitionOr50Move(Board board)
        {
            if (board.HistoryDepth - board.FiftyMoveRuleIndex > 100)
            {
                return true;
            }

            for (var i = board.FiftyMoveRuleIndex; i < board.HistoryDepth; i++)
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

            Span<ulong> pins = stackalloc ulong[2];
            _pinDetector.GetPinnedToKings(board, pins);
            var standPat = _evaluation.Evaluate(board, pins);
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

            var checkers = _attacksService.GetCheckers(board);
            var pinnedPieces = pins[board.ColorToMove];
            _possibleMoves.GetAllPotentialCaptures(board, potentialMoves, ref moveCount, checkers, pinnedPieces);
            var seeScores = threadState.SeeScores[ply];
            _see.CalculateSeeScores(board, potentialMoves, moveCount, seeScores);
            var moveStaticScores = threadState.MoveStaticScores[ply];
            var previousMove = board.History2[board.HistoryDepth - 1].Move;
            var countermove = threadState.Countermove[previousMove.Piece][previousMove.To];
            _moveOrdering.CalculateStaticScores(board, potentialMoves, moveCount, ply, principalVariationMove, threadState.Killers, EngineOptions.UseSeeOrdering, seeScores, countermove, moveStaticScores);
            
            
            for (var moveIndex = 0; moveIndex < moveCount; moveIndex++)
            {
                _moveOrdering.OrderNextMove(moveIndex, potentialMoves, moveStaticScores, seeScores, moveCount, threadState);
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
        private bool TryProbeTranspositionTable(ZobristKey key, int depth, int alpha, int beta, ref Move bestMove, out int score, out bool entryExists)
        {
            score = default;
            entryExists = false;

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

            entryExists = true;
            bestMove = entry.Move;

            if (entry.Depth < depth)
            {
                _statistics.HashInsufficientDepth++;
                return false;
            }

            switch (entry.Flag)
            {
                case TranspositionTableFlags.Exact:
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
                _possibleMoves.GetAllLegalMoves(clone, possibleMoves, ref moveCount);
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
