using System;
using System.Collections.Generic;
using System.Linq;
using ChessDotNet.Common;
using ChessDotNet.Data;

namespace ChessDotNet.Init
{
    public class MagicBitboardsInitializer
    {
        public ISlideMoveGenerator OtherGenerator { get; }
        private Random RNG { get; set; }

        public MagicBitboardsInitializer(ISlideMoveGenerator otherGenerator)
        {
            OtherGenerator = otherGenerator;
        }

        public void Init()
        {
            RNG = new Random(0);
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


                UInt64 rookMask = BitboardConstants.Ranks[rank];
                rookMask |= BitboardConstants.Files[file];
                if (rank != 0) rookMask &= ~BitboardConstants.Ranks[0];
                if (rank != 7) rookMask &= ~BitboardConstants.Ranks[7];
                if (file != 0) rookMask &= ~BitboardConstants.Files[0];
                if (file != 7) rookMask &= ~BitboardConstants.Files[7];
                rookMask &= ~(1UL << pos);
                var rookEntry = InitEntry(rookMask, false, pos);
                rooks[pos] = rookEntry;

                UInt64 bishopMask = BitboardConstants.Diagonals[diagonal];
                bishopMask |= BitboardConstants.Antidiagonals[antidiagonal];
                bishopMask &= ~BitboardConstants.Ranks[0];
                bishopMask &= ~BitboardConstants.Ranks[7];
                bishopMask &= ~BitboardConstants.Files[0];
                bishopMask &= ~BitboardConstants.Files[7];
                bishopMask &= ~(1UL << pos);
                var bishopEntry = InitEntry(bishopMask, true, pos);
                bishops[pos] = bishopEntry;
            }//);
            MagicBitboards.Rooks = rooks;
            MagicBitboards.Bishops = bishops;
        }

        private MagicBitboardGenerationEntry InitEntry(UInt64 mask, bool bishop, int pos)
        {
            var entry = new MagicBitboardGenerationEntry();
            entry.Position = pos;
            entry.BlockerMask = mask;
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
            var occupancies = new UInt64[permutations];
            entry.Occupancies = occupancies;
            var moveboards = new UInt64[permutations];
            entry.Moveboards = moveboards;

            for(var i = 0; i < permutations; i++)
            {
                UInt64 occMask = 0;
                for(var j = 0; j < bits.Count; j++)
                {
                    var shouldSet = (i & (1 << j)) > 0;
                    if(shouldSet)
                    {
                        var bit = bits[j];
                        occMask |= 1UL << bit;
                    }
                }

                //mask.DumpConsole();

                occupancies[i] = occMask;
                //occMask.DumpConsole();

                var moveboard = bishop ? OtherGenerator.DiagonalAntidiagonalSlide(occMask, pos) : OtherGenerator.HorizontalVerticalSlide(occMask, pos);
                moveboards[i] = moveboard;
                //moveboard.DumpConsole();

                //Console.WriteLine("----------------------");
            }

            entry.MagicNumber = FindMagicNumber(entry);

            return entry;
        }

        private UInt64 GetRandomBitboard()
        {
            var buf = new byte[8];
            RNG.NextBytes(buf);
            var bb = BitConverter.ToUInt64(buf,0);
            return bb;
        }

        public UInt64 GetMagicNumberCandidate(MagicBitboardGenerationEntry generationEntry)
        {
            var magicNumber = GetRandomBitboard() & GetRandomBitboard() & GetRandomBitboard();
            return magicNumber;
        }

        private UInt64 FindMagicNumber(MagicBitboardGenerationEntry generationEntry)
        {
            /*for (var i = 0; i < 65; i++)
            {
                generationEntry.Occupancies[i].DumpConsole();
                generationEntry.Moveboards[i].DumpConsole();
                Console.WriteLine("------");
            }*/

            const UInt64 invalid = ~0UL;

            UInt64 magicNumber = 0UL;
            var success = false;
            ulong iterations = 0;
            while (true)
            {
                var table = Enumerable.Repeat(invalid, 1 << generationEntry.BitCount).ToArray();
                iterations++;
                magicNumber = GetMagicNumberCandidate(generationEntry);
                //magicNumber.DumpConsole();
                success = true;
                for (var i = 0; i < generationEntry.Occupancies.Count; i++)
                {
                    //if (i == 65)
                    //{
                    //   Console.WriteLine("Reached " + i);
                    //}
                    var occupancy = generationEntry.Occupancies[i];
                    var moveboard = generationEntry.Moveboards[i];
                    var multiplied = (occupancy * magicNumber);

                    //multiplied.DumpConsole();

                    var magicIndex = multiplied >> (64 - generationEntry.BitCount);
                    var magicIndexInt = (int) magicIndex;
                    //magicIndex.DumpConsole();

                    //var result = generationEntry.Moveboards[(int)magicIndex];
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

            Console.WriteLine($"{generationEntry.Position}: success in {iterations} iterations");
            return magicNumber;
        }
    }
}