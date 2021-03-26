using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChessDotNet.Common;
using ChessDotNet.Data;

using Bitboard = System.UInt64;
using Key = System.UInt64;
using Position = System.Int32;
using Piece = System.Int32;

namespace ChessDotNet.Init
{
    public class MagicBitboardsInitializer
    {
        private class MagicBitboardGenerationEntry
        {
            public int Position { get; set; }
            public bool Bishop { get; set; }
            public Bitboard BlockerMask { get; set; }
            public Bitboard MagicNumber { get; set; }
            public byte BitCount { get; set; }
            public Bitboard[] Occupancies { get; set; }
            public Bitboard[] Moveboards { get; set; }
        }

        public ISlideMoveGenerator OtherGenerator { get; }
        public IMagicNumberCandidateProvider CandidateProvider { get; }

        public MagicBitboardsInitializer(ISlideMoveGenerator otherGenerator, IMagicNumberCandidateProvider candidateProvider)
        {
            OtherGenerator = otherGenerator;
            CandidateProvider = candidateProvider;
        }

        public void Init()
        {
            var rooks = new MagicBitboardGenerationEntry[64];
            var bishops = new MagicBitboardGenerationEntry[64];
            var positions = Enumerable.Range(0, 64);
            //Parallel.ForEach(positions, pos =>
            foreach (var pos in positions)
            {
                var rank = pos / 8;
                var file = pos % 8;
                var diagonal = file + rank;
                var antidiagonal = rank - file + 7;


                Bitboard rookMask = BitboardConstants.Ranks[rank];
                rookMask |= BitboardConstants.Files[file];
                if (rank != 0) rookMask &= ~BitboardConstants.Ranks[0];
                if (rank != 7) rookMask &= ~BitboardConstants.Ranks[7];
                if (file != 0) rookMask &= ~BitboardConstants.Files[0];
                if (file != 7) rookMask &= ~BitboardConstants.Files[7];
                rookMask &= ~(1UL << pos);
                var rookEntry = InitEntry(rookMask, false, pos);
                rooks[pos] = rookEntry;

                Bitboard bishopMask = BitboardConstants.Diagonals[diagonal];
                bishopMask |= BitboardConstants.Antidiagonals[antidiagonal];
                bishopMask &= ~BitboardConstants.Ranks[0];
                bishopMask &= ~BitboardConstants.Ranks[7];
                bishopMask &= ~BitboardConstants.Files[0];
                bishopMask &= ~BitboardConstants.Files[7];
                bishopMask &= ~(1UL << pos);
                var bishopEntry = InitEntry(bishopMask, true, pos);
                bishops[pos] = bishopEntry;
            }//);
            MagicBitboards.Rooks = rooks.Select(x => new MagicBitboardEntry(x.BlockerMask, x.MagicNumber, (byte)(64 - x.BitCount), x.Moveboards)).ToArray();
            MagicBitboards.Bishops = bishops.Select(x => new MagicBitboardEntry(x.BlockerMask, x.MagicNumber, (byte)(64 - x.BitCount), x.Moveboards)).ToArray();
            //PrintBitboardArray(rooks.Select(x => x.MagicNumber).ToList());
            //PrintBitboardArray(bishops.Select(x => x.MagicNumber).ToList());
        }

        private void PrintBitboardArray(IReadOnlyList<Bitboard> bitboards)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < bitboards.Count; i++)
            {
                if (i % 8 == 0)
                {
                    sb.AppendLine();
                }
                sb.Append($"0x{bitboards[i]:X8},");
            }
            Console.WriteLine(sb);
        }

        private MagicBitboardGenerationEntry InitEntry(Bitboard mask, bool bishop, int pos)
        {
            var entry = new MagicBitboardGenerationEntry();
            entry.Position = pos;
            entry.BlockerMask = mask;
            entry.Bishop = bishop;
            var bits = new List<int>();
            for(var i = 0; i < 64; i++)
            {
                if((mask & (1UL << i)) > 0)
                {
                    bits.Add(i);
                }
            }

            var permutations = 1 << bits.Count;
            entry.BitCount = (byte)bits.Count;
            var occupancies = new Bitboard[permutations];
            entry.Occupancies = occupancies;
            var moveboards = new Bitboard[permutations];
            entry.Moveboards = moveboards;

            for(var i = 0; i < permutations; i++)
            {
                Bitboard occMask = 0;
                for(var j = 0; j < bits.Count; j++)
                {
                    var shouldSet = (i & (1 << j)) > 0;
                    if(shouldSet)
                    {
                        var bit = bits[j];
                        occMask |= 1UL << bit;
                    }
                }

                occupancies[i] = occMask;
                var moveboard = bishop ? OtherGenerator.DiagonalAntidiagonalSlide(occMask, pos) : OtherGenerator.HorizontalVerticalSlide(occMask, pos);
                moveboards[i] = moveboard;
            }

            entry.MagicNumber = FindMagicNumber(entry);

            return entry;
        }


        private Bitboard FindMagicNumber(MagicBitboardGenerationEntry generationEntry)
        {
            const Bitboard invalid = ~0UL;

            Bitboard magicNumber = 0UL;
            var success = false;
            ulong iterations = 0;
            while (true)
            {
                var table = Enumerable.Repeat(invalid, 1 << generationEntry.BitCount).ToArray();
                iterations++;
                magicNumber = CandidateProvider.GetMagicNumberCandidate(generationEntry.Position, generationEntry.Bishop);
                success = true;
                for (var i = 0; i < generationEntry.Occupancies.Length; i++)
                {
                    var occupancy = generationEntry.Occupancies[i];
                    var moveboard = generationEntry.Moveboards[i];
                    var multiplied = (occupancy * magicNumber);
                    var magicIndex = multiplied >> (64 - generationEntry.BitCount);
                    var magicIndexInt = (int) magicIndex;
                    if (table[magicIndexInt] == invalid || table[magicIndexInt] == moveboard)
                    {
                        table[magicIndexInt] = moveboard;
                    }
                    else
                    {
                        success = false;
                        break;
                    }
                }

                if (success)
                {
                    generationEntry.MagicNumber = magicNumber;
                    generationEntry.Moveboards = table;
                    break;
                }
            }

            //Console.WriteLine($"{generationEntry.Position}: success in {iterations} iterations");
            return magicNumber;
        }
    }
}