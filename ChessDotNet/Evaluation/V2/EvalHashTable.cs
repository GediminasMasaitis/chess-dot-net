using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ChessDotNet.Evaluation.V2
{
    public class EvalHashTable
    {
        private uint _size;
        private EvalHashTableEntry[] _entries;

        public void SetSize(uint bytes)
        {
            var entrySize = (uint)Marshal.SizeOf<EvalHashTableEntry>();
            var newSize = bytes / entrySize;
            if (_size == newSize)
            {
                return;
            }

            _size = newSize;
            _entries = new EvalHashTableEntry[newSize];
        }

        public void Store(UInt64 key, int score)
        {
            var index = GetTableIndex(key);
            var entry = new EvalHashTableEntry(key, score);
            _entries[index] = entry;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryProbe(UInt64 key, out int score)
        {
            //entry = default; return false;
            var index = GetTableIndex(key);
            var entry = _entries[index];
            var success = entry.Key == key;
            score = entry.Score;
            return success;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetTableIndex(UInt64 key)
        {
            var index = key % _size;
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
    }
}