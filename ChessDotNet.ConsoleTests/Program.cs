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
            var arrayBoard = fact.ParseFENToArrayBoard("rnbqkbnr/ppp2ppp/8/3pp3/4P3/2P5/PP1P1PPP/RNBQKBNR w KQkq - 0 3");
            var bitBoard = fact.ArrayBoardToBitBoard(arrayBoard);

            Debugging.ShowBitBoard(bitBoard.WhitePawns);
            Console.ReadLine();
        }
    }
}
