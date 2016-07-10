using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Perft;

namespace ChessDotNet.ConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var fact = new BoardFactory();
            var arrayBoard = fact.ParseFENToArrayBoard("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 ");
            arrayBoard = fact.ParseFENToArrayBoard("rnbqk1nr/ppp2ppp/8/3Np3/8/8/PPPPPPPP/R1BQKBNR b KQkq - 1 2 ");
            var bitBoards = fact.ArrayBoardToBitBoards(arrayBoard);
            bitBoards.EnPassantFile = BitBoards.Files[3];
            var forWhite = false;

            var movesService = new PossibleMovesService(bitBoards);

            /*var perft = new Perft.Perft(movesService);
            using (var sharperClient = new SharperPerftClient(@"C:\sharper\Sharper.exe"))
            {
                var perftRunner = new PerftRunner(perft, sharperClient);
                perftRunner.OnOut += Console.Write;
                perftRunner.Test(bitBoards, true, 4);
            }*/

            var moves = movesService.GetPossibleKingMoves(forWhite).ToList();

            var dests = moves.Select(x => x.To);
            var destBoard = fact.PiecesToBitBoard(dests);
            var attackBoard = movesService.GetAllAttacked(true);

            //var newBoard = bitBoards.DoMove(new Move(8, 24, ChessPiece.WhitePawn));

            Debugging.ShowBitBoard(bitBoards.BlackKings, bitBoards.WhitePieces, destBoard);
            Console.ReadLine();
        }
    }
}
