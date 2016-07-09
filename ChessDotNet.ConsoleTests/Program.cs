using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet.ConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var fact = new BoardFactory();
            //var arrayBoard = fact.ParseFENToArrayBoard("rnbqkbnr/ppp2ppp/8/3pp3/4P3/2P5/PP1P1PPP/RNBQKBNR w KQkq - 0 3");
            var arrayBoard = fact.ParseFENToArrayBoard("8/8/7p/P7/5p1p/P1p1P1P1/2P5/8 w - - 0 1 ");
            var bitBoard = fact.ArrayBoardToBitBoards(arrayBoard);

            var movesService = new PossibleMovesService();
            var moves = movesService.GetPossibleWhitePawnMoves(bitBoard).ToList();

            var dests = moves.Select(x => x.To);
            var destBoard = fact.PiecesToBitBoard(dests);
            

            Debugging.ShowBitBoard(bitBoard.WhitePawns, bitBoard.BlackPieces, destBoard);
            Console.ReadLine();
        }
    }
}
