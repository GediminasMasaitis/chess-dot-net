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
            var arrayBoard = fact.ParseFENToArrayBoard("rnbqkbnr/pppppppp/8/8/p6P/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 ");
            //var arrayBoard = fact.ParseFENToArrayBoard("rnbqkbnr/pppppppp/N7/2Q5/1p3R1B/4K3/PPPPPPPP/RNBQ1BNR w kq - 0 1 ");
            var bitBoards = fact.ArrayBoardToBitBoards(arrayBoard);
            bitBoards.EnPassantFile = BitBoards.Files[3];
            var forWhite = false;

            var kings = forWhite ? bitBoards.WhiteKings : bitBoards.BlackKings;
            var queens = forWhite? bitBoards.WhiteQueens: bitBoards.BlackQueens;
            var rooks = forWhite? bitBoards.WhiteRooks: bitBoards.BlackRooks;
            var bishops = forWhite? bitBoards.WhiteBishops: bitBoards.BlackBishops;
            var knights = forWhite ? bitBoards.WhiteNights : bitBoards.BlackNights;

            var myPieces = forWhite ? bitBoards.WhitePieces : bitBoards.BlackPieces;
            var enemyPieces = forWhite ? bitBoards.BlackPieces : bitBoards.WhitePieces;
            var movesService = new PossibleMovesService();

            var moves = movesService.GetPossiblePawnMoves(bitBoards, forWhite).ToList();

            var dests = moves.Select(x => x.To);
            var destBoard = fact.PiecesToBitBoard(dests);
            var attackBoard = movesService.GetAttackedByQueens(bitBoards, forWhite);
            

            Debugging.ShowBitBoard(myPieces, enemyPieces, destBoard);
            Console.ReadLine();
        }
    }
}
