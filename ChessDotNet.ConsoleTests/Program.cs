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
            var arrayBoard = fact.ParseFENToArrayBoard("rnbqkbnr/pppp1ppp/8/2pPp3/8/8/PPP1PPPP/RNBQKBNR w KQkq - 0 1 ");
            //var arrayBoard = fact.ParseFENToArrayBoard("rnbqkbnr/pppppppp/N7/2Q5/1p3R1B/4K3/PPPPPPPP/RNBQ1BNR w kq - 0 1 ");
            var bitBoards = fact.ArrayBoardToBitBoards(arrayBoard);
            bitBoards.EnPassantFile = BitBoards.Files[4];
            var forWhite = true;

            var kings = forWhite ? bitBoards.WhiteKings : bitBoards.BlackKings;
            var queens = forWhite? bitBoards.WhiteQueens: bitBoards.BlackQueens;
            var rooks = forWhite? bitBoards.WhiteRooks: bitBoards.BlackRooks;
            var bishops = forWhite? bitBoards.WhiteBishops: bitBoards.BlackBishops;
            var knights = forWhite ? bitBoards.WhiteNights : bitBoards.BlackNights;
            var enemies = forWhite ? bitBoards.BlackPieces : bitBoards.WhitePieces;
            var movesService = new PossibleMovesService();

            var moves = movesService.GetPossibleMoves(bitBoards, forWhite).ToList();

            var dests = moves.Select(x => x.To);
            var destBoard = fact.PiecesToBitBoard(dests);
            

            Debugging.ShowBitBoard(bitBoards.WhitePieces, bitBoards.BlackPieces, destBoard);
            Console.ReadLine();
        }
    }
}
