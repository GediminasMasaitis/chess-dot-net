using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ChessDotNet.Common;
using ChessDotNet.Data;
using ChessDotNet.Evaluation;
using ChessDotNet.MoveGeneration;
using ChessDotNet.MoveGeneration.SlideGeneration;
using ChessDotNet.Protocols;
using ChessDotNet.Search2;
using ChessDotNet.Searching;

namespace ChessDotNet
{
    public class Game
    {
        public SearchOptions Options { get; set; }

        private BoardFactory BoardFact { get; set; }
        private ISlideMoveGenerator Hyperbola { get; set; }
        private EvaluationService Evaluation { get; set; }
        private AttacksService Attacks { get; set; }
        private PossibleMovesService Moves { get; set; }
        public SearchService2 Search { get; set; }
        private Board CurrentBoard { get; set; }


        public Game()
        {
            Options = new SearchOptions();
            var slideMoveGenerator = new MagicBitboardsService();
            var evaluationService = new EvaluationService();
            var attacksService = new AttacksService(slideMoveGenerator);
            var movesService = new PossibleMovesService(attacksService, slideMoveGenerator);
            var searchService = new SearchService2(movesService, evaluationService);

            BoardFact = new BoardFactory();
            Hyperbola = slideMoveGenerator;
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
                CurrentBoard.DoMove2(move);
            }
        }

        public void SetStartingPos()
        {
            SetPositionByFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        }

        public string Print()
        {
            return CurrentBoard.Print(Evaluation);
        }

        public IList<Move> SearchMove(SearchParameters searchParameters)
        {
            if (CurrentBoard == null)
            {
                SetStartingPos();
            }
            var searchResult = Search.Run(CurrentBoard, searchParameters, Options);
            CurrentBoard.DoMove2(searchResult[0]);
            return searchResult;
        }
    }
}
