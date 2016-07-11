using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Data;
using ChessDotNet.MoveGeneration;
using ChessDotNet.Rating;

namespace ChessDotNet.Searching
{
    class PrincipalVariationService
    {
        public PossibleMovesService PossibleMovesService { get; set; }
        public RatingService RatingService { get; set; }
        public int MaxDepth { get; }

        public const int MateScore = int.MaxValue/2;

        public PrincipalVariationService(PossibleMovesService possibleMovesService, RatingService ratingService)
        {
            PossibleMovesService = possibleMovesService;
            RatingService = ratingService;
            MaxDepth = 3;
        }

        public int PrincipalVariationSearch(int alpha, int beta, BitBoards bitBoards, bool forWhite, int currentDepth)
        {
            int score;
            Move bestMove;
            if (currentDepth == MaxDepth)
            {
                score = RatingService.Evaluate(bitBoards, forWhite);
                return score;
            }

            // sort moves
            
            var potentialMoves = PossibleMovesService.GetAllPotentialMoves(bitBoards, forWhite);
            var i = 0;
            for (; i < potentialMoves.Count; i++)
            {
                var potentialMove = potentialMoves[i];
                var bbAfter = PossibleMovesService.DoMoveIfKingSafe(bitBoards, potentialMove, forWhite);
                if (bbAfter != null)
                {
                    score = -PrincipalVariationSearch(-alpha, -beta, bbAfter, !forWhite, currentDepth + 1);
                    if (score == MateScore)
                    {
                        return score;
                    }
                    if (score > alpha)
                    {
                        if (score >= beta)
                        {
                            return score;
                        }
                        alpha = score;
                    }
                    bestMove = potentialMove;
                    break;
                }
            }

            for (; i < potentialMoves.Count; i++)
            {
                var tempScore = -ZeroWindowSearch(-alpha, )
            }
        }

        public int ZeroWindowSearch(int beta, BitBoards bitBoards, bool forWhite, int currentDepth)
        {
            var alpha = beta - 1;
            var score = int.MinValue;
            if (currentDepth >= MaxDepth)
            {
                score = RatingService.Evaluate(bitBoards, forWhite);
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
