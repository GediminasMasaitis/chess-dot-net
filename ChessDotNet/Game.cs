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
        private BoardFactory BoardFact { get; set; }
        private HyperbolaQuintessence Hyperbola { get; set; }
        private EvaluationService Evaluation { get; set; }
        private AttacksService Attacks { get; set; }
        private PossibleMovesService Moves { get; set; }
        private SearchService Search { get; set; }

        private Board CurrentBoard { get; set; }

        public Game()
        {

            var hyperbola = new HyperbolaQuintessence();
            var evaluationService = new EvaluationService();
            var attacksService = new AttacksService(hyperbola);
            var movesService = new PossibleMovesService(attacksService, hyperbola);
            var searchService = new SearchService(movesService, evaluationService);

            BoardFact = new BoardFactory();
            Hyperbola = hyperbola;
            Evaluation = evaluationService;
            Attacks = attacksService;
            Moves = movesService;
            Search = searchService;
        }

        public void SetPositionByFEN(string fen)
        {
            CurrentBoard = BoardFact.ParseFEN(fen);
        }

        public void SetPositionByMoves(bool startNewBoard, IEnumerable<string> moves)
        {
            if (startNewBoard)
            {
                SetStartingPos();
            }
            foreach (var moveStr in moves)
            {
                var move = Move.FromPositionString(CurrentBoard, moveStr);
                CurrentBoard = CurrentBoard.DoMove(move);
            }
        }

        public void SetStartingPos()
        {
            SetPositionByFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        }

        public string Print()
        {
            return CurrentBoard.Print();
        }

        public IList<SearchService.PVSResult> SearchMove(SearchParams searchParams)
        {
            if (CurrentBoard == null)
            {
                SetStartingPos();
            }
            var searchResult = Search.Search(CurrentBoard, searchParams);
            CurrentBoard = searchResult[0].Board;
            return searchResult;
        }
    }
}
