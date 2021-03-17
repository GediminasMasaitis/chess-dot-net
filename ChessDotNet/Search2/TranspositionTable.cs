using System;
using System.Collections.Generic;
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

            // Cpw
            if (existingEntry.Key == key && existingEntry.Depth > depth)
            {
                return;
            }

            //if (existingEntry.Depth > depth && existingEntry.Key == key)
            //{
            //    return;
            //}
            
            // Stockfish
            /*if (flag != TranspositionTableFlags.Exact && existingEntry.Key == key && depth <= existingEntry.Depth - 4)
            {
                return;
            }*/

            var entry = new TranspositionTableEntry(key, move, depth, score, flag);
            _entries[index] = entry;
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

        public IList<TranspositionTableEntry> GetPrincipalVariation(Board board)
        {
            var entries = new List<TranspositionTableEntry>();
            for (var i = 0; i < SearchConstants.MaxDepth; i++)
            {
                var success = TryProbe(board.Key, out var entry);
                if (!success)
                {
                    break;
                }

                if (board.Key != entry.Key)
                {
                    break;
                }

                if (entry.Flag != TranspositionTableFlags.Exact)
                {
                    break;
                }

                entries.Add(entry);
                board = board.DoMove(entry.Move);
            }
            return entries;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetTableIndex(UInt64 key)
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