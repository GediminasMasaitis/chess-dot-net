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

namespace ChessDotNet.Searching
{
    public class SearchService
    {
        public PossibleMovesService PossibleMovesService { get; set; }
        public IEvaluationService EvaluationService { get; set; }

        private const int Inf = int.MaxValue;
        private const int MaxDepth = 64;
        private PVSResult[] PVTable { get; set; }
        private int[,] SearchKillers { get; set; }
        private int[,] SearchHistory { get; set; }

        private long FailHigh { get; set; }
        private long FailHighFirst { get; set; }
        private long NodesSearched { get; set; }

        public const int MateScore = 50000;
        public const int MateThereshold = 49000;

        public SearchService(PossibleMovesService possibleMovesService, IEvaluationService evaluationService)
        {
            PossibleMovesService = possibleMovesService;
            EvaluationService = evaluationService;
        }

        public void Clear()
        {
            PVTable = new PVSResult[MaxDepth];
            SearchKillers = new int[MaxDepth, 2];
            SearchHistory = new int[13,64];
            FailHigh = 0;
            FailHighFirst = 0;
            NodesSearched = 0;
        }

        public IList<PVSResult> Search(Board board, SearchParams searchParams = null)
        {
            searchParams = searchParams ?? new SearchParams();
            Clear();
            var sw = new Stopwatch();
            var totalTimeSpent = 0L;
            var lastIterationSpent = 0L;
            var timeRemaining = board.WhiteToMove ? searchParams.WhiteTime : searchParams.BlackTime;
            var allowedForMove = timeRemaining/30;
            if (searchParams.Infinite)
            {
                allowedForMove = 5000;
            }
            var maxDepth = searchParams.MaxDepth ?? 64;
            var i = 1;
            for (; i <= maxDepth; i++)
            {
                sw.Restart();
                var score = PrincipalVariationSearch(-Inf, Inf, board, i, 0);
                sw.Stop();

                var elapsedNow = sw.ElapsedMilliseconds;
                totalTimeSpent += elapsedNow;

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
                if (score > MateThereshold)
                {
                    break;
                }
            }
            var speed = (NodesSearched / sw.Elapsed.TotalSeconds).ToString("0");
            //Console.WriteLine();
            //Console.WriteLine("Decided to move: " + PVTable[0].Move.ToPositionString());
            //Console.WriteLine($"Nodes searched: {NodesSearched}; Time taken: {sw.ElapsedMilliseconds} ms; Speed: {speed} N/s");
            var ratio = (FailHighFirst/(double) FailHigh).ToString("0.000");
            //Console.WriteLine($"fhf={FailHighFirst}; fh={FailHigh}; fhf/fh={ratio}");
            return PVTable;
        }

        public string PrintPVTable()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < MaxDepth; i++)
            {
                if (PVTable[i] == null)
                {
                    break;
                }
                sb.Append(PVTable[i].Move.ToPositionString() + " ");
            }
            return sb.ToString();
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

        public int CalculateMoveScore(Board currentBoard, Move move, int currentDepth)
        {
            var moveKey = move.Key;
            var isCurrentPV = currentDepth > 0 && PVTable[currentDepth - 1] != null && PVTable[currentDepth - 1].Board.Key == currentBoard.Key;
            if (isCurrentPV)
            {
                if (PVTable[currentDepth] != null && PVTable[currentDepth].Move.Key == moveKey)
                {
                    return 20000000;
                }
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

        public void SortNextMove(int currentIndex, Board currentBoard, IList<Move> moves, int currentDepth)
        {
            //return;
            var bestScore = -Inf;
            var bestScoreIndex = -1;
            for (var i = currentIndex; i < moves.Count; i++)
            {
                var score = CalculateMoveScore(currentBoard, moves[i], currentDepth);
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

        public int PrincipalVariationSearch(int alpha, int beta, Board board, int depth, int currentDepth)
        {
            int score;
            var quiessence = currentDepth >= depth;
            /*if (currentDepth == depth)
            {
                NodesSearched++;
                score = EvaluationService.Evaluate(board);
                return score;
            }*/


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

            if (quiessence)
            {
                score = EvaluationService.Evaluate(board);
                //return score;
                if (score >= beta)
                {
                    return beta;
                }
                if (score > alpha)
                {
                    alpha = score;
                }
            }

            // sort moves
            var oldAlpha = alpha;
            var validMoves = 0;
            var bestMove = 0;
            var doPV = true;

            var potentialMoves = PossibleMovesService.GetAllPotentialMoves(board);
            if (quiessence)
            {
                potentialMoves = potentialMoves.Where(x => x.TakesPiece > 0).ToList();
            }
            //Console.WriteLine(potentialMoves.Count);
            var bbsAfter = new Board[potentialMoves.Count];
            for (var i = 0; i < potentialMoves.Count; i++)
            {
                SortNextMove(i, board, potentialMoves, currentDepth);
                var potentialMove = potentialMoves[i];
                var bbAfter = PossibleMovesService.DoMoveIfKingSafe(board, potentialMove);
                if (bbAfter == null)
                {
                    continue;
                }
                bbsAfter[i] = bbAfter;
                validMoves++;
                if (doPV)
                {
                    score = -PrincipalVariationSearch(-beta, -alpha, bbAfter, depth, currentDepth + 1);
                }
                else
                {
                    score = -ZeroWindowSearch(-alpha, bbAfter, depth, currentDepth + 1);
                    if (score > alpha)
                    {
                        score = -PrincipalVariationSearch(-beta, -alpha, bbAfter, depth, currentDepth + 1);
                    }
                }
                /*if (score > MateThereshold)
                {
                    break;
                }*/
                if (score > alpha)
                {
                    if (score >= beta)
                    {
                        if (validMoves == 1)
                        {
                            FailHighFirst++;
                        }
                        FailHigh++;
                        //Console.WriteLine("Beta cutoff, score " + score + ", beta: " + beta);
                        if (potentialMove.TakesPiece == 0)
                        {
                            SearchKillers[currentDepth, 1] = SearchKillers[currentDepth, 0];
                            SearchKillers[currentDepth, 0] = potentialMove.Key;
                        }
                        return beta;
                    }
                    alpha = score;
                    bestMove = i;

                    SearchHistory[potentialMove.Piece, potentialMove.To] += depth - currentDepth;

                    //doPV = false;
                }
            }

            if (depth == currentDepth + 1 && validMoves == 0)
            {
                var enemyAttacks = PossibleMovesService.AttacksService.GetAllAttacked(board, !board.WhiteToMove);
                var myKing = board.WhiteToMove ? board.BitBoard[ChessPiece.WhiteKing] : board.BitBoard[ChessPiece.BlackKing];
                if ((enemyAttacks & myKing) != 0)
                {
                    return -MateScore + currentDepth;
                }
                else
                {
                    return 0;
                }
            }

            if (alpha != oldAlpha)
            {
                PVTable[currentDepth] = new PVSResult(alpha, bbsAfter[bestMove], potentialMoves[bestMove]);
                //Console.WriteLine("Alpha changed from " + oldAlpha + " to " + alpha + " at depth " + currentDepth + ". PV: " + PrintPVTable());
            }

            return alpha;
        }

        public int ZeroWindowSearch(int beta, Board board, int depth, int currentDepth)
        {
            var alpha = beta - 1;
            var score = int.MinValue;
            if (currentDepth == depth)
            {
                score = EvaluationService.Evaluate(board);
                return score;
            }

            var potentialMoves = PossibleMovesService.GetAllPotentialMoves(board);
            foreach (var potentialMove in potentialMoves)
            {
                var bbAfter = PossibleMovesService.DoMoveIfKingSafe(board, potentialMove);
                if (bbAfter != null)
                {
                    score = -ZeroWindowSearch(-alpha, bbAfter, depth, currentDepth + 1);
                }

                if (score >= beta)
                {
                    return score;
                }
            }
            return alpha;
        }
    }
}
