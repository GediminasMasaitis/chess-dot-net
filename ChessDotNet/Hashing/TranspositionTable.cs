using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessDotNet.Hashing
{
    class TranspositionTable<TData>
        where TData:class
    {
        private TData[] Table { get; }
        public uint TableSize { get; }

        public TranspositionTable(int size)
        {
            TableSize = 1 << 16;
            Table = new TData[TableSize];
        }

        public void Add(ulong key, TData data)
        {
            Table[key%TableSize] = data;
        }

        public bool TryGet(ulong key, out TData data)
        {
            data = Table[key%TableSize];
            return data != default(TData);
        }

        public bool Contains(ulong key)
        {
            return Table[key%TableSize] != default(TData);
        }
    }
}
