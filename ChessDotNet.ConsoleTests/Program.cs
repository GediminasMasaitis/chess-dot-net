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
            var arrayBoard = fact.ParseFENToArrayBoard("rnbqkbnr/pppppppp/8/2Q5/1p3R1B/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 ");
            var bitBoards = fact.ArrayBoardToBitBoards(arrayBoard);
            var forWhite = true;

            var queens = forWhite? bitBoards.WhiteQueens: bitBoards.BlackQueens;
            var rooks = forWhite? bitBoards.WhiteRooks: bitBoards.BlackRooks;
            var bishops = forWhite? bitBoards.WhiteBishops: bitBoards.BlackBishops;
            var enemies = forWhite ? bitBoards.BlackPieces : bitBoards.WhitePieces;
            var movesService = new PossibleMovesService();

            var moves = movesService.GetPossibleQueenMoves(bitBoards, forWhite).ToList();

            var dests = moves.Select(x => x.To);
            var destBoard = fact.PiecesToBitBoard(dests);
            

            Debugging.ShowBitBoard(queens, enemies, destBoard);
            Console.ReadLine();
        }
    }
}
