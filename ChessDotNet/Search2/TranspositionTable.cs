using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ChessDotNet.Data;

namespace ChessDotNet.Search2
{
    public class TranspositionTable
    {
        public ulong _mask;
        public ulong _size;
        public TranspositionTableEntry[] _entries;
        public IList<TranspositionTableEntry> _principalVariation;

        public TranspositionTable()
        {

        }

        public void SetSize(ulong bytes)
        {
            var newSize = GetClampedSize(bytes);
            
            if (_size == newSize)
            {
                return;
            }

            _size = newSize;
            _mask = _size - 1;
            _entries = new TranspositionTableEntry[newSize];
            //_principalVariation = new TranspositionTableEntry[SearchConstants.MaxDepth];
        }

        private ulong GetClampedSize(ulong bytes)
        {
            var entrySize = (ulong)Marshal.SizeOf<TranspositionTableEntry>();
            var size = bytes / entrySize;
            //size = 1_048_576;
            if ((size & (size - 1)) == 0)
            {
                return size;
            }

            var clampedSize = 1UL;
            while (clampedSize < size)
            {
                clampedSize <<= 1;
            }
            clampedSize >>= 1;

            return clampedSize;
        }

        public void Store(UInt64 key, Move move, int depth, int score, Byte flag)
        {
            var index = GetTableIndex(key);
            var existingEntry = _entries[index];
            //var entryKey = existingEntry.Key;
            var entryKey = RestoreKey(existingEntry.PartialKey, index);

            // Cpw
            if (entryKey == key && existingEntry.Depth > depth)
            {
                return;
            }

            if (existingEntry.Flag == TranspositionTableFlags.Exact)
            {
                if (flag != TranspositionTableFlags.Exact)
                {
                    return;
                }
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

            var entry = new TranspositionTableEntry(key, move, (byte)depth, (short)score, flag/*, key2*/);
            _entries[index] = entry;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryProbe(ulong key, out TranspositionTableEntry entry, out ulong entryKey)
        {
            //entry = default; return false;
            var index = GetTableIndex(key);
            entry = _entries[index];
            var exists = entry.Flag != TranspositionTableFlags.None;
            //entryKey = entry.Key;
            entryKey = RestoreKey(entry.PartialKey, index);
            //var exists = entry.Key != 0;
            return exists;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong RestoreKey(ulong partialKey, ulong index)
        {
            return partialKey | (index & 0xFFFFUL);
        }

        public void SavePrincipalVariation(Board board)
        {
            _principalVariation = GetPrincipalVariation(board);
        }

        public IList<TranspositionTableEntry> GetSavedPrincipalVariation()
        {
            return _principalVariation;
        }

        public IList<TranspositionTableEntry> GetPrincipalVariation(Board board)
        {
            var entries = new List<TranspositionTableEntry>();
            for (var i = 0; i < SearchConstants.MaxDepth; i++)
            {
                var success = TryProbe(board.Key, out var entry, out var entryKey);
                if (!success)
                {
                    break;
                }

                if (board.Key != entryKey)
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
        private ulong GetTableIndex(UInt64 key)
        {
            //var index = key % _size;
            var index = key & _mask;
            return index;
        }

        public void Clear()
        {
            if (_entries == null)
            {
                return;
            }

            Array.Clear(_entries, 0, _entries.Length);
        }

        public TranspositionTable Clone()
        {
            var table = new TranspositionTable();
            table._size = _size;
            //table._principalVariation = _principalVariation.ToList();
            table._entries = _entries.ToArray();
            return table;
        }
    }
}