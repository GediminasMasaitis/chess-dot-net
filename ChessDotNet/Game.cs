using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;
using ChessDotNet.MoveGeneration;
using ChessDotNet.Searching;

namespace ChessDotNet
{
    public class Game
    {
        private HyperbolaQuintessence Hyperbola { get; set; }
        private EvaluationService Evaluation { get; set; }
        private AttacksService Attacks { get; set; }
        private PossibleMovesService Moves { get; set; }
        private SearchService Search { get; set; }

        public Game(string fen = null)
        {
            fen = fen ?? "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"; // Starting pos

            var fact = new BoardFactory();
            var board = fact.ParseFEN(fen);

            var hyperbola = new HyperbolaQuintessence();
            var evaluationService = new EvaluationService();
            var attacksService = new AttacksService(hyperbola);
            var movesService = new PossibleMovesService(attacksService, hyperbola);
            var searchService = new SearchService(movesService, evaluationService);

            Hyperbola = hyperbola;
            Evaluation = evaluationService;
            Attacks = attacksService;
            Moves = movesService;
            Search = searchService;
        }

    }
}
