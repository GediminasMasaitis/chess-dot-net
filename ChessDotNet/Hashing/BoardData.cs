using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Data;

namespace ChessDotNet.Hashing
{
    interface IZobristKeyed
    {
        ulong Key { get; }
    }

    class BoardData : IZobristKeyed
    {
        public ulong Key { get; }
        public Board Board { get; }
    }
}
