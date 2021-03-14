//#define LOG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace ChessDotNet.Search2
{
    public class SearchService2
    {
        private readonly PossibleMovesService _possibleMoves;
        private readonly EvaluationService _evaluation;
        private readonly SearchState _state;
        private readonly SearchStopper _stopper;
        private readonly MoveOrderingService _moveOrdering;
        private readonly SearchStatistics _statistics;
        public event Action<SearchInfo> SearchInfo;

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
            CancellationToken token = default
        )
        {
            _stopper.NewSearch(parameters, board.WhiteToMove, token);
            _statistics.Reset();
            _state.OnNewSearch();
            var log = SearchLog.New();
            RunIterativeDeepening(board, log);
            log.PrintLastChild();
            
            //var principalVariation = _state.TranspositionTable.GetPrincipalVariation(board);
            //var moves = principalVariation.Select(entry => entry.Move).ToList();

            var moves = _state.PrincipalVariationTable.GetPrincipalVariation();
            return moves;
        }

        private void RunIterativeDeepening(Board board, SearchLog log)
        {
            const int initialDepth = 1;

            _state.PrincipalVariationTable.SetCurrentDepth(initialDepth);
            var initialSearchLog = SearchLog.New();
            var score = SearchToDepth(board, initialDepth, 0, -SearchConstants.Inf, SearchConstants.Inf, true, true, initialSearchLog);
            log.AddChild(initialSearchLog);
            if (!_stopper.IsStopped())
            {
                _state.PrincipalVariationTable.SetSearchedDepth(initialDepth);
            }

            for (var depth = initialDepth + 1; depth < SearchConstants.MaxDepth; depth++)
            {
                _state.OnIterativeDeepen();

                _state.PrincipalVariationTable.SetCurrentDepth(depth);
                var iterativeLog = SearchLog.New();
                //score = SearchToDepth(board, depth, 0, -SearchConstants.Inf, SearchConstants.Inf, true, true, iterativeLog);
                score = SearchAspirationWindow(board, depth, score, iterativeLog);
                log.AddChild(iterativeLog);
                if (!_stopper.IsStopped())
                {
                    _state.PrincipalVariationTable.SetSearchedDepth(depth);
                }

                ReportSearchInfo(board, depth, score);
                ReportCurrentState();
                if (_stopper.ShouldStopOnDepthIncrease(depth))
                {
                    return;
                }
            }
        }

        private void ReportSearchInfo(Board board, int depth, int score)
        {
            //var principalVariation = _state.TranspositionTable.GetPrincipalVariation(board);
            //var principalMoves = principalVariation.Select(entry => entry.Move).ToList();

            var principalMoves = _state.PrincipalVariationTable.GetPrincipalVariation();

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

        private void ReportCurrentState()
        {
            Console.WriteLine($"{_statistics}");
            Console.WriteLine();
        }

        private int SearchAspirationWindow(Board board, int depth, int previousScore, SearchLog log)
        {
            const int window = 50;
            var alpha = previousScore - window;
            var beta = previousScore + window;
            var windowLog = SearchLog.New();
            var windowScore = SearchToDepth(board, depth, 0, alpha, beta, true, true, windowLog);
            if (windowScore <= alpha || windowScore >= beta)
            {
                _statistics.AspirationFail++;
                var fullSearchLog = SearchLog.New();
                var fullSearchScore = SearchToDepth(board, depth, 0, -SearchConstants.Inf, SearchConstants.Inf, true, true, fullSearchLog);
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

        private int SearchToDepth(Board board, int depth, int ply, int alpha, int beta, bool nullMoveAllowed, bool isPrincipalVariation, SearchLog log)
        {
            log.AddMessage("Starting search", depth, alpha, beta);

            // STOP CHECK
            if (_stopper.ShouldStop())
            {
                var score = 0;
                log.AddMessage("Stop requested", depth, alpha, beta, score);
                return score;
            }

            // REPETITION DETECTION
            if (ply > 0)
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
            var enemyAttacks = _possibleMoves.AttacksService.GetAllAttacked(board, !board.WhiteToMove);
            var myKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            var inCheck = (enemyAttacks & myKing) != 0;

            if (inCheck)
            {
                depth++;
                log.AddMessage("Check depth extension", depth, alpha, beta);
            }

            // QUIESCENCE
            if (depth == 0)
            {
                //var evaluatedScore = _evaluation.Evaluate(board);
                //_statistics.NodesSearched++;
                log.AddMessage("Start quiescence search", depth, alpha, beta);
                var evaluatedScore = Quiescence(board, depth, ply, alpha, beta, log);
                log.AddMessage("End quiescence search", depth, alpha, beta, evaluatedScore);
                return evaluatedScore;
            }

            // PROBE TRANSPOSITION TABLE
            Move? principalVariationMove = null;
            var probeResult = ProbeTranspositionTable(board.Key, depth, alpha, beta, isPrincipalVariation, out var tableEntry);
            switch (probeResult)
            {
                case TranspositionTableProbeResult.HitCutoff:
                    log.AddMessage("Transposition cutoff", depth, alpha, beta, tableEntry.Score);
                    _state.PrincipalVariationTable.SetBestMove(ply, tableEntry.Move);
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
                    board.DoMove2(nullMove);
                    //var nullBoard = board.DoMove(nullMove);
                    var nullLog = SearchLog.New(nullMove);
                    log.AddMessage($"Start null move search, R={nullDepthReduction}", depth, alpha, beta);
                    var nullMoveScore = -SearchToDepth(board, depth - nullDepthReduction - 1, ply + 1, -beta, -beta + 1, false, false, nullLog);
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
            SearchLog bestLog = default;


            // CHILD SEARCH
            var initialAlpha = alpha;
            var potentialMoves = _state.Moves[ply];
            potentialMoves.Clear();
            _possibleMoves.GetAllPotentialMoves(board, potentialMoves);
            //_moveOrdering.CalculateMoveScores(potentialMoves, ply, principalVariationMove, _state.Killers, _state.History);
            var movesEvaluated = 0;
            var raisedAlpha = false;

            for (var moveIndex = 0; moveIndex < potentialMoves.Count; moveIndex++)
            {
                _moveOrdering.OrderNextMove(moveIndex, potentialMoves, ply, principalVariationMove, _state.Killers, _state.History);
                var move = potentialMoves[moveIndex];

                var kingSafe = _possibleMoves.IsKingSafeAfterMove(board, move);
                if (!kingSafe)
                {
                    continue;
                }

                board.DoMove2(move);
                //var childBoard = board.DoMove(move);
                var childLog = SearchLog.New(move);

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
                    childScore = -SearchToDepth(board, depth - 1, ply + 1, -alpha - 1, -alpha, true, false, childLog);
                    if (childScore > alpha)
                    {
                        childScore = -SearchToDepth(board, depth - 1, ply + 1, -beta, -alpha, true, isPrincipalVariation, childLog);
                        _statistics.PvsScoutFail++;
                    }
                    else
                    {
                        _statistics.PvsScoutSuccess++;
                    }
                }
                else
                {
                    childScore = -SearchToDepth(board, depth - 1, ply + 1, -beta, -alpha, true, isPrincipalVariation, childLog);
                }
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
                            StoreTranspositionTable(board.Key, move, depth, childScore, TranspositionTableFlags.Beta);
                            

                            if (move.TakesPiece == 0)
                            {
                                _state.Killers[ply, 1] = _state.Killers[ply, 0];
                                _state.Killers[ply, 0] = move.Key2;
                            }
                            log.AddChild(childLog);
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
                _state.PrincipalVariationTable.SetBestMove(ply, bestMove);
                StoreTranspositionTable(board.Key, bestMove, depth, bestScore, TranspositionTableFlags.Exact);
            }

            log.AddChild(bestLog);
            return alpha;
        }

        private bool IsRepetition(Board board)
        {
            for (var i = board.LastTookPieceHistoryIndex; i < board.HistoryDepth; i++)
            {
                var previousKey = board.History2[i].Key;
                if (board.Key == previousKey)
                {
                    return true;
                }
            }
            return false;
        }

        private int Quiescence(Board board, int depth, int ply, int alpha, int beta, SearchLog log)
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
            SearchLog bestLog = default;

            var initialAlpha = alpha;
            var potentialMoves = _state.Moves[ply];
            potentialMoves.Clear();
            _possibleMoves.GetAllPotentialCaptures(board, potentialMoves);
            var movesEvaluated = 0;
            for (var moveIndex = 0; moveIndex < potentialMoves.Count; moveIndex++)
            {
                _moveOrdering.OrderNextMove(moveIndex, potentialMoves, ply, principalVariationMove, _state.Killers, _state.History);
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

                var childScore = -Quiescence(board, depth - 1, ply + 1, -beta, -alpha, childLog);
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
                            _state.TranspositionTable.Store(board.Key, move, depth, childScore, TranspositionTableFlags.Beta);
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
                _state.TranspositionTable.Store(board.Key, bestMove, depth, alpha, TranspositionTableFlags.Alpha);
            }
            else
            {
                _statistics.StoresExact++;
                _state.TranspositionTable.Store(board.Key, bestMove, depth, bestScore, TranspositionTableFlags.Exact);
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
        
        private TranspositionTableProbeResult ProbeTranspositionTable(ZobristKey key, int depth, int alpha, int beta, bool isPrincipalVariation, out TranspositionTableEntry entry)
        {
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
