//#define LOG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;
using ChessDotNet.MoveGeneration;
using ChessDotNet.Searching;

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

        private readonly PossibleMovesService _possibleMoves;
        private readonly EvaluationService _evaluation;
        private readonly SearchState _state;
        private readonly SearchStopper _stopper;
        private readonly MoveOrderingService _moveOrdering;
        private readonly SearchStatistics _statistics;

        private SearchOptions _options;

        public SearchService2
        (
            PossibleMovesService possibleMoves,
            EvaluationService evaluation
        )
        {
            _possibleMoves = possibleMoves;
            _evaluation = evaluation;
            _moveOrdering = new MoveOrderingService();
            _state = new SearchState();
            _stopper = new SearchStopper();
            _statistics = new SearchStatistics();
        }

        public IList<Move> Run
        (
            Board board,
            SearchParameters parameters,
            SearchOptions options,
            CancellationToken token = default
        )
        {
            _options = options;

            _stopper.NewSearch(parameters, board.WhiteToMove, token);
            _statistics.Reset();
            _state.OnNewSearch(options);
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
            var initialScore = SearchToDepth(0, board, initialDepth, 0, -SearchConstants.Inf, SearchConstants.Inf, false, true, true, initialSearchLog);
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

                if (_stopper.ShouldStopOnDepthIncrease(depth))
                {
                    return score;
                }
            }
            return 0;
        }

        private void LogOutput(int threadId, Board board, int depth, int score)
        {
            if (_options.Debug)
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

                if (_options.Debug)
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
            if (!_options.Debug)
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
            if (!_options.UseAspirationWindows)
            {
                var score = SearchToDepth(threadId, board, depth, 0, -SearchConstants.Inf, SearchConstants.Inf, false, true, true, log);
                return score;
            }

            const int window = 50;
            var alpha = previousScore - window;
            var beta = previousScore + window;
            var windowLog = SearchLog.New();
            var windowScore = SearchToDepth(threadId, board, depth, 0, alpha, beta, false, true, true, windowLog);
            if (windowScore <= alpha || windowScore >= beta)
            {
                _statistics.AspirationFail++;
                var fullSearchLog = SearchLog.New();
                var fullSearchScore = SearchToDepth(threadId, board, depth, 0, -SearchConstants.Inf, SearchConstants.Inf, false, true, true, fullSearchLog);
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

        private int SearchToDepth(int threadId, Board board, int depth, int ply, int alpha, int beta, bool nullMoveMade, bool nullMoveAllowed, bool isPrincipalVariation, SearchLog log)
        {
            log.AddMessage("Starting search", depth, alpha, beta);
            var threadState = _state.ThreadStates[threadId];

            // STOP CHECK
            if (depth > 2 && _stopper.ShouldStop())
            {
                var score = 0;
                log.AddMessage("Stop requested", depth, alpha, beta, score);
                return score;
            }

            // REPETITION DETECTION
            if (nullMoveAllowed && ply > 0)
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
            if (ply > 0)
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
            var myKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            var myKingPos = myKing.BitScanForward();
            var inCheck = _possibleMoves.AttacksService.IsPositionAttacked(board, myKingPos, !board.WhiteToMove);

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
            if (_options.UseTranspositionTable)
            {
                //var probeResult = ProbeTranspositionTable(board.Key, depth, alpha, beta, isPrincipalVariation, out var tableEntry);
                //switch (probeResult)
                //{
                //    case TranspositionTableProbeResult.HitCutoff:
                //        var exactScore = tableEntry.Score;
                //        if (exactScore > SearchConstants.MateThereshold)
                //        {
                //            exactScore -= ply;
                //        }
                //        else if (exactScore < -SearchConstants.MateThereshold)
                //        {
                //            exactScore += ply;
                //        }

                //        log.AddMessage($"Transposition cutoff, flag {tableEntry.Flag}", depth, alpha, beta, exactScore);
                //        //if (tableEntry.Score > alpha)
                //        if (tableEntry.Flag == TranspositionTableFlags.Exact)
                //        {
                //            _state.PrincipalVariationTable.SetBestMove(ply, tableEntry.Move);
                //        }
                //        return exactScore;
                //    //principalVariationMove = tableEntry.Move;
                //    //break;
                //    case TranspositionTableProbeResult.HitContinue:
                //        principalVariationMove = tableEntry.Move;
                //        break;
                //    case TranspositionTableProbeResult.Miss:
                //        break;
                //    default:
                //        throw new ArgumentOutOfRangeException(nameof(probeResult), probeResult, null);
                //}

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
            if
            (
                _options.UseStaticEvaluationPruning
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
            var nullDepthReduction = depth > 6 ? 3 : 2;
            if
            (
                _options.UseNullMovePruning
                && nullMoveAllowed
                && !inCheck
                && depth > 2
                && ply > 0
            )
            {
                var material = board.WhiteToMove ? board.WhiteMaterial : board.BlackMaterial;
                if (material > SearchConstants.EndgameMaterial)
                {
                    var nullMove = new Move(0, 0, 0);
                    board.DoMove2(nullMove);
                    //var nullBoard = board.DoMove(nullMove);
                    var nullLog = SearchLog.New(nullMove);
                    log.AddMessage($"Start null move search, R={nullDepthReduction}", depth, alpha, beta);
                    var nullMoveScore = -SearchToDepth(threadId, board, depth - nullDepthReduction - 1, ply + 1, -beta, -beta + 1, true, false, false, nullLog);
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
                _options.UseRazoring
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
                _options.UseFutilityPruning
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
            _possibleMoves.GetAllPotentialMoves(board, potentialMoves, ref moveCount);
            //_moveOrdering.CalculateMoveScores(potentialMoves, ply, principalVariationMove, _state.Killers, _state.History);
            var movesEvaluated = 0;
            var raisedAlpha = false;

            for (var moveIndex = 0; moveIndex < moveCount; moveIndex++)
            {
                _moveOrdering.OrderNextMove(moveIndex, potentialMoves, moveCount, ply, principalVariationMove, threadState.Killers, threadState.History);
                var move = potentialMoves[moveIndex];

                var kingSafe = _possibleMoves.IsKingSafeAfterMove(board, move);
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
                    var opponentInCheck = _possibleMoves.AttacksService.IsPositionAttacked(board, opponentKingPos, !board.WhiteToMove);
                    
                    if (!opponentInCheck)
                    {
                        _statistics.FutilityReductions++;
                        board.UndoMove();
                        continue;
                    }
                }

                var childDepth = depth - 1;
                // LATE MOVE REDUCTION
                threadState.Cutoff[move.WhiteToMoveNum, move.From, move.To] -= 1;
                if
                (
                    _options.UseLateMoveReductions
                    && !isPrincipalVariation
                    && movesEvaluated > 3
                    && depth > 4
                    && !inCheck
                    && threadState.Cutoff[move.WhiteToMoveNum, move.From, move.To] < 50
                    && move.Key2 != threadState.Killers[ply, 0]
                    && move.Key2 != threadState.Killers[ply, 1]
                    && move.TakesPiece == ChessPiece.Empty
                    && move.PawnPromoteTo == ChessPiece.Empty
                )
                {
                    var attacks = _possibleMoves.AttacksService.GetAllAttacked(board, !board.WhiteToMove);
                    var opponentKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
                    var oponnentInCheck = (attacks & opponentKing) != 0;
                    if (!oponnentInCheck)
                    {
                        threadState.Cutoff[move.WhiteToMoveNum, move.From, move.To] = 50;
                        _statistics.LateMoveReductions1++;
                        childDepth--;
                        if (movesEvaluated > 6)
                        {
                            _statistics.LateMoveReductions2++;
                            childDepth--;
                        }
                    }
                }

                int childScore;
                if (_options.UsePrincipalVariationSearch && raisedAlpha)
                {
                    childScore = -SearchToDepth(threadId, board, childDepth, ply + 1, -alpha - 1, -alpha, nullMoveMade, true, false, childLog);
                    if (childScore > alpha)
                    {
                        childScore = -SearchToDepth(threadId, board, childDepth, ply + 1, -beta, -alpha, nullMoveMade, true, isPrincipalVariation, childLog);
                        _statistics.PvsScoutFail++;
                    }
                    else
                    {
                        _statistics.PvsScoutSuccess++;
                    }
                }
                else
                {
                    childScore = -SearchToDepth(threadId, board, childDepth, ply + 1, -beta, -alpha, nullMoveMade, true, isPrincipalVariation, childLog);
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
                        threadState.Cutoff[move.WhiteToMoveNum, move.From, move.To] += 6;
                        if (childScore >= beta)
                        {
                            _statistics.StoresBeta++;
                            _statistics.BetaCutoffs++;
                            if (_options.UseTranspositionTable)
                            {
                                StoreTranspositionTable(board.Key, move, depth, childScore, TranspositionTableFlags.Beta);
                            }

                            if (move.TakesPiece == 0)
                            {
                                threadState.Killers[ply, 1] = threadState.Killers[ply, 0];
                                threadState.Killers[ply, 0] = move.Key2;
                            }
                            log.AddChild(childLog);
                            log.AddMessage($"Beta cutoff, move {move.ToPositionString()}, child score {childScore}", depth, alpha, beta, beta);
                            return beta;
                        }

                        if (move.TakesPiece == ChessPiece.Empty)
                        {
                            threadState.History[move.WhiteToMoveNum, move.From, move.To] += depth * depth;
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
                if (_options.UseTranspositionTable)
                {
                    StoreTranspositionTable(board.Key, bestMove, depth, alpha, TranspositionTableFlags.Alpha);
                }
                log.AddMessage($"Didn't increase alpha, best move {bestMove.ToPositionString()}, best score {bestScore}", depth, alpha, beta, alpha);
            }
            else
            {
                _statistics.StoresExact++;
                _state.PrincipalVariationTable.SetBestMove(ply, bestMove);
                if (_options.UseTranspositionTable)
                {
                    StoreTranspositionTable(board.Key, bestMove, depth, bestScore, TranspositionTableFlags.Exact);
                }
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
            Move principalVariationMove = default;
            //var probeResult = ProbeTranspositionTable(board.Key, depth, alpha, beta, true /* ? */, out var tableEntry);
            //switch (probeResult)
            //{
            //    case TranspositionTableProbeResult.HitCutoff:
            //        return tableEntry.Score;
            //    case TranspositionTableProbeResult.HitContinue:
            //        principalVariationMove = tableEntry.Move;
            //        break;
            //    case TranspositionTableProbeResult.Miss:
            //        break;
            //    default:
            //        throw new ArgumentOutOfRangeException(nameof(probeResult), probeResult, null);
            //}

            var bestScore = -SearchConstants.Inf;
            Move bestMove = default;
            SearchLog bestLog = default;

            var initialAlpha = alpha;
            var potentialMoves = threadState.Moves[ply];
            var moveCount = 0;
            _possibleMoves.GetAllPotentialCaptures(board, potentialMoves, ref moveCount);
            var movesEvaluated = 0;
            for (var moveIndex = 0; moveIndex < moveCount; moveIndex++)
            {
                _moveOrdering.OrderNextMove(moveIndex, potentialMoves, moveCount, ply, principalVariationMove, threadState.Killers, threadState.History);
                var move = potentialMoves[moveIndex];

                if (move.TakesPiece == ChessPiece.Empty)
                {
                    continue;
                }

                var kingSafe = _possibleMoves.IsKingSafeAfterMove(board, move);
                if (!kingSafe)
                {
                    continue;
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
                            //_state.TranspositionTable.Store(board.Key, move, depth, childScore, TranspositionTableFlags.Beta);
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
                _statistics.StoresAlpha++;
                //_state.TranspositionTable.Store(board.Key, bestMove, depth, alpha, TranspositionTableFlags.Alpha);
            }
            else
            {
                _statistics.StoresExact++;
                //_state.TranspositionTable.Store(board.Key, bestMove, depth, bestScore, TranspositionTableFlags.Exact);
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

        private bool TryProbeTranspositionTable(ZobristKey key, int depth, int alpha, int beta, ref Move bestMove, out int score, out bool exact)
        {
            score = default;
            exact = false;

            var found = _state.TranspositionTable.TryProbe(key, out var entry);
            if (!found)
            {
                _statistics.HashMiss++;
                return false;
            }

            if (entry.Key != key)
            {
                _statistics.HashCollision++;
                return false;
            }

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

        private TranspositionTableProbeResult ProbeTranspositionTable(ZobristKey key, int depth, int alpha, int beta, bool isPrincipalVariation, out TranspositionTableEntry entry)
        {
            Debug.Assert(alpha < beta);

            var found = _state.TranspositionTable.TryProbe(key, out entry);
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
