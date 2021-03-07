using System;
using System.Runtime.CompilerServices;
using ChessDotNet.Data;

namespace ChessDotNet.Search2
{
    public class TranspositionTable
    {
        private readonly ulong _size;
        private readonly TranspositionTableEntry[] _entries;

        public TranspositionTable(ulong size)
        {
            _size = size;
            //var entrySize = Marshal.SizeOf<TranspositionTableEntry>();
            _entries = new TranspositionTableEntry[size];
        }

        public void Store(UInt64 key, Move move, int depth, int score, Byte flag)
        {
            var index = GetTableIndex(key);
            var existingEntry = _entries[index];

            if (existingEntry.Depth > depth && existingEntry.Key == key)
            {
                return;
            }
            
            //if (existingEntry.Key != 0 && existingEntry.Key != key)
            //{
            //    return;
            //}

            if (flag == TranspositionTableFlags.Exact || existingEntry.Key != key || depth > existingEntry.Depth - 4)
            {
                var entry = new TranspositionTableEntry(key, move, depth, score, flag);
                _entries[index] = entry;
            }
        }

        public bool TryProbe(UInt64 key, out TranspositionTableEntry entry)
        {
            //entry = default; return false;
            var index = GetTableIndex(key);
            entry = _entries[index];
            var exists = entry.Flag != TranspositionTableFlags.None;
            //var valid = entry.Key == key;
            return exists;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong GetTableIndex(UInt64 key)
        {
            var index = key % _size;
            return index;
        }

        public void Clear()
        {
            Array.Clear(_entries, 0, _entries.Length);
        }
    }
}