using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;
using ChessDotNet.MoveGeneration;

namespace ChessDotNet.Searching
{
    class PrincipalVariationService
    {
        public PossibleMovesService PossibleMovesService { get; set; }
        public EvaluationService EvaluationService { get; set; }

        public const int MateScore = int.MaxValue/2;

        public PrincipalVariationService(PossibleMovesService possibleMovesService, EvaluationService evaluationService)
        {
            PossibleMovesService = possibleMovesService;
            EvaluationService = evaluationService;
        }

        public int Search(int maxDepth)
        {
            throw new NotImplementedException();
        }

        public int PrincipalVariationSearch(int alpha, int beta, BitBoards bitBoards, bool forWhite, int depth)
        {
            int score;
            if (depth == 0)
            {
                score = EvaluationService.Evaluate(bitBoards, forWhite);
                return score;
            }

            // sort moves
            
            var potentialMoves = PossibleMovesService.GetAllPotentialMoves(bitBoards, forWhite);
            var i = 0;
            var doPV = true;
            for (; i < potentialMoves.Count; i++)
            {
                var potentialMove = potentialMoves[i];
                var bbAfter = PossibleMovesService.DoMoveIfKingSafe(bitBoards, potentialMove, forWhite);
                if (bbAfter == null)
                {
                    continue;
                }

                if (doPV)
                {
                    score = -PrincipalVariationSearch(-alpha, -beta, bbAfter, !forWhite, depth - 1);
                }
                else
                {
                    score = -ZeroWindowSearch(-alpha, bbAfter, !forWhite, depth - 1);
                    if (score > alpha)
                    {
                        score = -PrincipalVariationSearch(-alpha, -beta, bbAfter, !forWhite, depth - 1);
                    }
                }
                if (score == MateScore)
                {
                    return score;
                }
                if (score >= beta)
                {
                    return score;
                }
                if (score > alpha)
                {
                    alpha = score;
                    doPV = false;
                }
            }
            return alpha;
        }

        public int ZeroWindowSearch(int beta, BitBoards bitBoards, bool forWhite, int currentDepth)
        {
            var alpha = beta - 1;
            var score = int.MinValue;
            //if (currentDepth >= MaxDepth)
            {
                score = EvaluationService.Evaluate(bitBoards, forWhite);
                return score;
            }

            var potentialMoves = PossibleMovesService.GetAllPotentialMoves(bitBoards, forWhite);
            foreach (var potentialMove in potentialMoves)
            {
                var bbAfter = PossibleMovesService.DoMoveIfKingSafe(bitBoards, potentialMove, forWhite);
                if (bbAfter != null)
                {
                    score = -ZeroWindowSearch(-alpha, bbAfter, !forWhite, currentDepth + 1);
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
