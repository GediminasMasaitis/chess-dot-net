using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;
using ChessDotNet.Hashing;
using ChessDotNet.MoveGeneration;
using ChessDotNet.Protocols;
using ChessDotNet.Testing;

namespace ChessDotNet.Searching
{
    public class SearchService
    {
        public PossibleMovesService PossibleMovesService { get; set; }
        public IEvaluationService EvaluationService { get; set; }
        public IInterruptor Interruptor { get; set; }

        public event Action<SearchInfo> OnSearchInfo;

        private bool Stopped { get; set; }
        private const int Inf = int.MaxValue;
        private const int MaxDepth = 64;

        private TranspositionTable<TTEntry> TTable { get; set; }
        private int[,] SearchKillers { get; set; }
        private int[,] SearchHistory { get; set; }

        private long FailHigh { get; set; }
        private long FailHighFirst { get; set; }

        private long NullMoveCutOffs { get; set; }
        private long NodesSearched { get; set; }

        private const int MateScore = 50000;
        private const int MateThereshold = 49000;

        public SearchService(PossibleMovesService possibleMovesService, IEvaluationService evaluationService, IInterruptor interruptor)
        {
            PossibleMovesService = possibleMovesService;
            EvaluationService = evaluationService;
            Interruptor = interruptor;
            TTable = new TranspositionTable<TTEntry>(26);
        }

        public void Clear()
        {
            SearchKillers = new int[MaxDepth, 2];
            SearchHistory = new int[13,64];
            FailHigh = 0;
            FailHighFirst = 0;
            NullMoveCutOffs = 0;
            NodesSearched = 0;
            Stopped = false;
            Interruptor.Start();
        }

        public IList<TTEntry> GetPVLine(Board board)
        {
            var entries = new List<TTEntry>();

            while (true)
            {
                TTEntry entry;
                var success = TTable.TryGet(board.Key, out entry);
                if (!success)
                {
                    return entries;
                }
                entries.Add(entry);
                board = board.DoMove(entry.Move);
            }
        }

        public IList<TTEntry> Search(Board board, SearchParams searchParams = null)
        {
            searchParams = searchParams ?? new SearchParams();
            Clear();
            var sw = new Stopwatch();
            var totalTimeSpent = 0L;
            var lastIterationSpent = 0L;
            var timeRemaining = board.WhiteToMove ? searchParams.WhiteTime : searchParams.BlackTime;
            var allowedForMove = searchParams.Infinite ? int.MaxValue : timeRemaining / 30;
            var maxDepth = searchParams.MaxDepth ?? 64;
            var i = 1;
            int alpha = -Inf;
            int beta = Inf;
            const bool aspirationWindows = false;
            const int initialAspiration = 25;
            var lowAspiration = initialAspiration;
            var highAspiration = initialAspiration;
            for (; i <= maxDepth; i++)
            {
                sw.Restart();
                var score = PrincipalVariationSearch(alpha, beta, board, i, 0, true);
                
                if (aspirationWindows)
                {
                    var lastScore = score;
                    while (score <= alpha || score >= beta)
                    {
                        if (score <= alpha)
                        {
                            lowAspiration *= 4;
                            alpha = lastScore - lowAspiration;
                            score = PrincipalVariationSearch(alpha, beta, board, i, 0, true);
                        }
                        else
                        {
                            highAspiration *= 4;
                            beta = lastScore + highAspiration;
                            score = PrincipalVariationSearch(alpha, beta, board, i, 0, true);
                        }
                    }
                    lowAspiration = initialAspiration;
                    highAspiration = initialAspiration;
                    alpha = score - lowAspiration;
                    beta = score + highAspiration;
                }
                sw.Stop();

                var pvLine = GetPVLine(board);
                if (Stopped)
                {
                    return pvLine;
                }

                var elapsedNow = sw.ElapsedMilliseconds;
                totalTimeSpent += elapsedNow;

                var searchInfo = new SearchInfo();
                searchInfo.Depth = i;
                searchInfo.Score = score;
                searchInfo.NodesSearched = NodesSearched;
                searchInfo.Time = totalTimeSpent;
                searchInfo.PrincipalVariation = pvLine;
                if (score > MateThereshold)
                {
                    searchInfo.MateIn = MateScore - score;
                }

                OnSearchInfo?.Invoke(searchInfo);

                if (score > MateThereshold)
                {
                    break;
                }

                if (totalTimeSpent > allowedForMove)
                {
                    break;
                }

                if (i > 3)
                {
                    var growthFactor = (double)elapsedNow/lastIterationSpent;
                    var estimatedNextIteration = elapsedNow*growthFactor;
                    if (totalTimeSpent + estimatedNextIteration > allowedForMove)
                    {
                        break;
                    }
                }
                lastIterationSpent = elapsedNow;

                //Console.WriteLine($"Depth: {i}; Score: {score}; Searched: {NodesSearched}; {PrintPVTable()}");
            }
            var speed = (NodesSearched / sw.Elapsed.TotalSeconds).ToString("0");
            //Console.WriteLine();
            //Console.WriteLine("Decided to move: " + PVTable[0].Move.ToPositionString());
            //Console.WriteLine($"Nodes searched: {NodesSearched}; Time taken: {sw.ElapsedMilliseconds} ms; Speed: {speed} N/s");
            var ratio = (FailHighFirst/(double) FailHigh).ToString("0.000");
            //Console.WriteLine($"fhf={FailHighFirst}; fh={FailHigh}; fhf/fh={ratio}");
            return GetPVLine(board);
        }

        public bool IsRepetition(Board board)
        {
            for (var i = board.LastTookPieceHistoryIndex; i < board.History.Length; i++)
            {
                if (board.Key == board.History[i].Board.Key)
                {
                    return true;
                }
            }
            return false;
        }

        public int CalculateMoveScore(Board currentBoard, Move move, int currentDepth, Move? pvMove)
        {
            var moveKey = move.Key;
            /*var isCurrentPV = currentDepth > 0 && PVTable[currentDepth - 1] != null && PVTable[currentDepth - 1].Board.Key == currentBoard.Key;
            if (isCurrentPV)
            {
                if (PVTable[currentDepth] != null && PVTable[currentDepth].Move.Key == moveKey)
                {
                    return 20000000;
                }
            }*/
            var isCurrentPV = pvMove.HasValue && pvMove.Value.Key == move.Key;
            if (isCurrentPV)
            {
                return 20000000;
            }
            var mvvlva = MVVLVAScoreCalculation.Scores[move.Piece,move.TakesPiece];
            if (mvvlva > 0)
            {
                return mvvlva + 10000000;
            }
            //return 0;
            if (SearchKillers[currentDepth, 0] == moveKey)
            {
                return 9000000;
            }
            if (SearchKillers[currentDepth, 1] == moveKey)
            {
                return 8000000;
            }
            //return 0;
            return SearchHistory[move.Piece, move.To];
        }

        public void SortNextMove(int currentIndex, Board currentBoard, IList<Move> moves, int currentDepth, Move? pvMove)
        {
            //return;
            var bestScore = -Inf;
            var bestScoreIndex = -1;
            for (var i = currentIndex; i < moves.Count; i++)
            {
                var score = CalculateMoveScore(currentBoard, moves[i], currentDepth, pvMove);
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

        public int PrincipalVariationSearch(int alpha, int beta, Board board, int depth, int currentDepth, bool allowNullMoveSearch)
        {
#if TEST
            Test.Assert(beta > alpha);
            Test.Assert(depth >= 0);
            board.CheckBoard();
#endif
            if ((NodesSearched & 2047) == 0)
            {
                if (Interruptor.IsInterrupted())
                {
                    Stopped = true;
                    return 0;
                }
            }

            NodesSearched++;

            var isRepetition = IsRepetition(board);
            if (isRepetition && currentDepth > 0)
            {
                return 0;
            }

            if (board.History.Length - board.LastTookPieceHistoryIndex >= 100)
            {
                return 0;
            }

            if (currentDepth >= MaxDepth)
            {
                return EvaluationService.Evaluate(board);
            }

            if (depth <= 0)
            {
                // quiessence next
                return Quiessence(alpha, beta, board, currentDepth);
            }

            var enemyAttacks = PossibleMovesService.AttacksService.GetAllAttacked(board, !board.WhiteToMove);
            var myKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
            var inCheck = (enemyAttacks & myKing) != 0;

            if (inCheck)
            {
                depth++;
            }

            int score = -Inf;
            Move? pvMove = null;

            TTEntry foundEntry;
            var found = TTable.TryGet(board.Key, out foundEntry);
            if (found && foundEntry.Key == board.Key)
            {
                pvMove = foundEntry.Move;
                if (foundEntry.Depth >= depth)
                {
                    switch (foundEntry.Flag)
                    {
                        case TTFlags.Beta:
                            return beta;
                            break;
                        case TTFlags.Exact:
                            return foundEntry.Score;
                            break;
                        case TTFlags.Alpha:
                            return alpha;
                            break;
                    }
                }
            }

            const int nullMoveFactor = 2;
            if (allowNullMoveSearch && !inCheck && currentDepth > 0 && depth >= nullMoveFactor+1)
            {
                var haveBigPiece = false;
                var pieceOffset = board.WhiteToMove ? 1 : 7;
                for (var i = 1; i < 5; i++)
                {
                    if (board.PieceCounts[pieceOffset + i] > 0)
                    {
                        haveBigPiece = true;
                        break;
                    }
                }
                if (haveBigPiece)
                {
                    var nullMove = new Move(0,0,0);
                    var nullBoard = board.DoMove(nullMove);
                    score = -PrincipalVariationSearch(-beta, -beta + 1, nullBoard, depth - nullMoveFactor - 1, currentDepth + 1, false);
                    if (Stopped)
                    {
                        return 0;
                    }

                    if (score >= beta && score > -MateThereshold && score < MateThereshold)
                    {
                        NullMoveCutOffs++;
                        return beta;
                    }
                }
            }

            var potentialMoves = PossibleMovesService.GetAllPotentialMoves(board);

            var oldAlpha = alpha;
            var validMoves = 0;

            Move? bestMove = null;
            var bestScore = -Inf;
            score = -Inf;

            var bbsAfter = new Board[potentialMoves.Count];
            for (var i = 0; i < potentialMoves.Count; i++)
            {
                SortNextMove(i, board, potentialMoves, currentDepth, pvMove);
                var potentialMove = potentialMoves[i];
                var bbAfter = PossibleMovesService.DoMoveIfKingSafe(board, potentialMove);
                if (bbAfter == null)
                {
                    continue;
                }
                bbsAfter[i] = bbAfter;
                validMoves++;

                score = -PrincipalVariationSearch(-beta, -alpha, bbAfter, depth-1, currentDepth + 1, true);

                if (Stopped)
                {
                    return 0;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = potentialMove;

                    if (score > alpha)
                    {
                        if (score >= beta)
                        {
#if TEST
                            if (validMoves == 1)
                            {
                                FailHighFirst++;
                            }
                            FailHigh++;
#endif

                            if (potentialMove.TakesPiece == 0)
                            {
                                SearchKillers[currentDepth, 1] = SearchKillers[currentDepth, 0];
                                SearchKillers[currentDepth, 0] = potentialMove.Key;
                            }

                            var entry = new TTEntry(board.Key, bestMove.Value, beta, TTFlags.Beta, depth);
                            TTable.Add(board.Key, entry);

                            return beta;
                        }
                        alpha = score;

                        if (potentialMove.TakesPiece == 0)
                        {
                            SearchHistory[potentialMove.Piece, potentialMove.To] += depth;
                        }

                    }
                }
            }

            if (validMoves == 0)
            {
                if (inCheck)
                {
                    return -MateScore + currentDepth;
                }
                else
                {
                    return 0;
                }
            }

#if TEST
            Test.Assert(alpha >= oldAlpha);
#endif

            if (alpha != oldAlpha)
            {
                var entry = new TTEntry(board.Key, bestMove.Value, bestScore, TTFlags.Exact, depth);
                TTable.Add(board.Key, entry);
                //PVTable[currentDepth] = new PVSResult(alpha, bbsAfter[bestMove], potentialMoves[bestMove]);
            }
            else
            {
                var entry = new TTEntry(board.Key, bestMove.Value, alpha, TTFlags.Alpha, depth);
                TTable.Add(board.Key, entry);
            }

            return alpha;
        }

        public int Quiessence(int alpha, int beta, Board board, int currentDepth)
        {
#if TEST
            Test.Assert(beta > alpha);
            board.CheckBoard();
#endif
            if ((NodesSearched & 2047) == 0)
            {
                if (Interruptor.IsInterrupted())
                {
                    Stopped = true;
                    return 0;
                }
            }

            NodesSearched++;

            var isRepetition = IsRepetition(board);
            if (isRepetition)
            {
                return 0;
            }

            if (board.History.Length - board.LastTookPieceHistoryIndex >= 100)
            {
                return 0;
            }

            if (currentDepth >= MaxDepth)
            {
                return EvaluationService.Evaluate(board);
            }

            int score = EvaluationService.Evaluate(board);

            if (score >= beta)
            {
                return beta;
            }
            if (score > alpha)
            {
                alpha = score;
            }

            var potentialMoves = PossibleMovesService.GetAllPotentialMoves(board).Where(x => x.TakesPiece > 0).ToList();

            var oldAlpha = alpha;
            var validMoves = 0;

            Move? bestMove = null;
            var bestScore = -Inf;
            score = -Inf;

            for (var i = 0; i < potentialMoves.Count; i++)
            {
                SortNextMove(i, board, potentialMoves, currentDepth, null);
                var potentialMove = potentialMoves[i];
                var bbAfter = PossibleMovesService.DoMoveIfKingSafe(board, potentialMove);
                if (bbAfter == null)
                {
                    continue;
                }
                validMoves++;

                score = -Quiessence(-beta, -alpha, bbAfter, currentDepth + 1);

                if (Stopped)
                {
                    return 0;
                }

                if (score > alpha)
                {
                    if (score >= beta)
                    {
#if TEST
                        if (validMoves == 1)
                        {
                            FailHighFirst++;
                        }
                        FailHigh++;
#endif
                        return beta;
                    }
                    alpha = score;
                }
            }
#if TEST
            Test.Assert(alpha >= oldAlpha);
            Test.Assert(alpha < Inf);
            Test.Assert(alpha > -Inf);
#endif
            return alpha;
        }
    }
}
