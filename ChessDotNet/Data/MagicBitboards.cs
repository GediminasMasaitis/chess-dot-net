using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessDotNet.Common;
using ChessDotNet.MoveGeneration;
using Bitboard = System.UInt64;
using Key = System.UInt64;
using Position = System.Int32;
using Piece = System.Int32;

namespace ChessDotNet.Data
{
    public class MagicBitboardEntry
    {
        public Position Position { get; set; }
        public Bitboard BlockerMask { get; set; }
        public Bitboard MagicNumber { get; set; }
        public byte BitCount { get; set; }
        public IReadOnlyList<Bitboard> Occupancies { get; set; }
        public IReadOnlyList<Bitboard> Moveboards { get; set; }
    }

    public class MagicBitboards
    {
        public static IReadOnlyList<MagicBitboardEntry> Rooks { get; set; }
        public static IReadOnlyList<MagicBitboardEntry> Bishops { get; set; }
    }

    public class MagicBitboardsService : IHyperbolaQuintessence
    {
        public ulong AllSlide(ulong allPieces, int position)
        {
            var hv = HorizontalVerticalSlide(allPieces, position);
            var dad = DiagonalAntidiagonalSlide(allPieces, position);
            return hv | dad;
        }

        public ulong HorizontalVerticalSlide(ulong allPieces, int position)
        {
            return Foo(allPieces, position, MagicBitboards.Rooks);
        }

        public ulong DiagonalAntidiagonalSlide(ulong allPieces, int position)
        {
            return Foo(allPieces, position, MagicBitboards.Bishops);
        }

        private Bitboard Foo(ulong allPieces, int position, IReadOnlyList<MagicBitboardEntry> entries)
        {
            var entry = entries[position];
            var occupancy = allPieces & entry.BlockerMask;
            var index = (occupancy * entry.MagicNumber) >> (64 - entry.BitCount);
            var indexInt = (int) index;
            var moveboard = entry.Moveboards[indexInt];
            return moveboard;
        }
    }

    public class MagicBitboardsInitializer
    {
        public IHyperbolaQuintessence HyperbolaQuintessence { get; }
        private Random RNG { get; set; }

        public MagicBitboardsInitializer(IHyperbolaQuintessence hyperbolaQuintessence)
        {
            HyperbolaQuintessence = hyperbolaQuintessence;
        }

        public void Init()
        {
            RNG = new Random(0);
            var rooks = new MagicBitboardEntry[64];
            var bishops = new MagicBitboardEntry[64];
            //for(var rank = 0; rank < 8; rank++)
            var positions = Enumerable.Range(0, 64);
            //for(var pos = 0; pos < 64; pos++)
            //Parallel.ForEach(positions, pos =>
            foreach (var pos in positions)
            {
                //Position pos = rank * 8 + file;
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
            MagicBitboards.Rooks = rooks;
            MagicBitboards.Bishops = bishops;
        }

        private MagicBitboardEntry InitEntry(Bitboard mask, bool bishop, int pos)
        {
            var entry = new MagicBitboardEntry();
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

                //mask.DumpConsole();

                occupancies[i] = occMask;
                //occMask.DumpConsole();

                var moveboard = bishop ? HyperbolaQuintessence.DiagonalAntidiagonalSlide(occMask, pos) : HyperbolaQuintessence.HorizontalVerticalSlide(occMask, pos);
                moveboards[i] = moveboard;
                //moveboard.DumpConsole();

                //Console.WriteLine("----------------------");
            }

            entry.MagicNumber = GetMagicNumber(entry);

            return entry;
        }

        private Bitboard GetRandomBitboard()
        {
            var buf = new byte[8];
            RNG.NextBytes(buf);
            var bb = BitConverter.ToUInt64(buf,0);
            return bb;
        }

        private Bitboard GetMagicNumber(MagicBitboardEntry entry)
        {
            /*for (var i = 0; i < 65; i++)
            {
                entry.Occupancies[i].DumpConsole();
                entry.Moveboards[i].DumpConsole();
                Console.WriteLine("------");
            }*/

            const Bitboard invalid = ~0UL;

            Bitboard magicNumber = 0UL;
            var success = false;
            ulong iterations = 0;
            while (true)
            {
                var table = Enumerable.Repeat(invalid, 1 << entry.BitCount).ToArray();
                iterations++;
                magicNumber = GetRandomBitboard() & GetRandomBitboard() & GetRandomBitboard();
                //magicNumber.DumpConsole();
                success = true;
                for (var i = 0; i < entry.Occupancies.Count; i++)
                {
                    //if (i == 65)
                    //{
                    //   Console.WriteLine("Reached " + i);
                    //}
                    var occupancy = entry.Occupancies[i];
                    var moveboard = entry.Moveboards[i];
                    var multiplied = (occupancy * magicNumber);

                    //multiplied.DumpConsole();

                    var magicIndex = multiplied >> (64 - entry.BitCount);
                    var magicIndexInt = (int) magicIndex;
                    //magicIndex.DumpConsole();

                    //var result = entry.Moveboards[(int)magicIndex];
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
                    entry.MagicNumber = magicNumber;
                    entry.Moveboards = table;
                    break;
                }
            }

            Console.WriteLine($"{entry.Position}: success in {iterations} iterations");
            return magicNumber;
        }
    }
}
