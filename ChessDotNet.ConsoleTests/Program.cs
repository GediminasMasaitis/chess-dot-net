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
            //arrayBoard = fact.ParseFENToArrayBoard("8/8/8/8/8/4p3/3P4/8 w - - 0 1 ");
            var bitBoards = fact.ArrayBoardToBitBoards(arrayBoard);
            bitBoards.EnPassantFile = BitBoards.Files[3];
            var forWhite = true;

            var movesService = new PossibleMovesService();
            var perft = new Perft.Perft(movesService);
            using (var sharperClient = new SharperPerftClient(@"C:\sharper\Sharper.exe"))
            {
                var perftRunner = new PerftRunner(perft, sharperClient);
                perftRunner.OnOut += Console.Write;
                perftRunner.Test(bitBoards, true, 4);
            }
            //var moves = movesService.GetAllPossibleMoves(bitBoards, forWhite).ToList();

            //var dests = moves.Select(x => x.To);
            //var destBoard = fact.PiecesToBitBoard(dests);
            //var attackBoard = movesService.GetAttackedByQueens(bitBoards, forWhite);

            //var newBoard = bitBoards.DoMove(new Move(8, 24, ChessPiece.WhitePawn));

            //Debugging.ShowBitBoard(newBoard.WhitePieces, newBoard.BlackPieces);
            Console.ReadLine();
        }
    }
}
