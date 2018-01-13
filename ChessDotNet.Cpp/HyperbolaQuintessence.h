// ChessDotNet.Cpp.h

#pragma once

#include "BitboardConstants.h"


namespace ChessDotNetCpp {

#pragma managed
	public ref class CppInitializer
	{
	public:
		static void Init()
		{
			bitboard_constants::init_bitboard_constants();
		}
	};

	public ref class HyperbolaQuintessenceManaged : ChessDotNet::Common::IHyperbolaQuintessence
	{
	public:
		virtual Bitboard AllSlide(Bitboard allPieces, int position)
		{
			Bitboard hv = HorizontalVerticalSlide(allPieces, position);
			Bitboard dad = DiagonalAntidiagonalSlide(allPieces, position);
			return hv | dad;
		}

		virtual Bitboard HorizontalVerticalSlide(Bitboard allPieces, int position)
		{
			Bitboard pieceBitboard = 1ULL << position;
			Bitboard horizontal = MaskedSlide(allPieces, pieceBitboard, bitboard_constants::ranks[position / 8]);
			Bitboard vertical = MaskedSlide(allPieces, pieceBitboard, bitboard_constants::files[position % 8]);
			return horizontal | vertical;
		}

		virtual Bitboard DiagonalAntidiagonalSlide(Bitboard allPieces, int position)
		{
			Bitboard pieceBitboard = 1ULL << position;
			Bitboard horizontal = MaskedSlide(allPieces, pieceBitboard, bitboard_constants::diagonals[position / 8 + position % 8]);
			Bitboard vertical = MaskedSlide(allPieces, pieceBitboard, bitboard_constants::antidiagonals[position / 8 + 7 - position % 8]);
			return horizontal | vertical;
		}

		Bitboard MaskedSlide(Bitboard allPieces, Bitboard pieceBitboard, Bitboard mask)
		{
			Bitboard left = ((allPieces & mask) - 2 * pieceBitboard);
			Bitboard right = ReverseBits(ReverseBits(allPieces & mask) - 2 * ReverseBits(pieceBitboard));
			Bitboard both = left ^ right;
			Bitboard slide = both & mask;
			return slide;
		}

		Bitboard ReverseBits(Bitboard bitboard)
		{
			const Bitboard h1 = 0x5555555555555555;
			const Bitboard h2 = 0x3333333333333333;
			const Bitboard h4 = 0x0F0F0F0F0F0F0F0F;
			const Bitboard v1 = 0x00FF00FF00FF00FF;
			const Bitboard v2 = 0x0000FFFF0000FFFF;
			bitboard = ((bitboard >> 1) & h1) | ((bitboard & h1) << 1);
			bitboard = ((bitboard >> 2) & h2) | ((bitboard & h2) << 2);
			bitboard = ((bitboard >> 4) & h4) | ((bitboard & h4) << 4);
			bitboard = ((bitboard >> 8) & v1) | ((bitboard & v1) << 8);
			bitboard = ((bitboard >> 16) & v2) | ((bitboard & v2) << 16);
			bitboard = (bitboard >> 32) | (bitboard << 32);
			return bitboard;
		}
	};

#pragma unmanaged

	Bitboard reverse_bits(Bitboard bitboard)
	{
		const Bitboard h1 = 0x5555555555555555;
		const Bitboard h2 = 0x3333333333333333;
		const Bitboard h4 = 0x0F0F0F0F0F0F0F0F;
		const Bitboard v1 = 0x00FF00FF00FF00FF;
		const Bitboard v2 = 0x0000FFFF0000FFFF;
		bitboard = ((bitboard >> 1) & h1) | ((bitboard & h1) << 1);
		bitboard = ((bitboard >> 2) & h2) | ((bitboard & h2) << 2);
		bitboard = ((bitboard >> 4) & h4) | ((bitboard & h4) << 4);
		bitboard = ((bitboard >> 8) & v1) | ((bitboard & v1) << 8);
		bitboard = ((bitboard >> 16) & v2) | ((bitboard & v2) << 16);
		bitboard = (bitboard >> 32) | (bitboard << 32);
		return bitboard;
	}

	Bitboard MaskedSlide(Bitboard allPieces, Bitboard pieceBitboard, Bitboard mask)
	{
		auto left = ((allPieces & mask) - 2 * pieceBitboard);
		auto right = reverse_bits(reverse_bits(allPieces & mask) - 2 * reverse_bits(pieceBitboard));
		auto both = left ^ right;
		auto slide = both & mask;
		return slide;
	}

	Bitboard horizontal_vertical_slide(Bitboard allPieces, int position)
	{
		auto pieceBitboard = 1ULL << position;
		auto horizontal = MaskedSlide(allPieces, pieceBitboard, bitboard_constants::ranks[position / 8]);
		auto vertical = MaskedSlide(allPieces, pieceBitboard, bitboard_constants::files[position % 8]);
		return horizontal | vertical;
	}

	Bitboard diagonal_antidiagonal_slide(Bitboard allPieces, int position)
	{
		auto pieceBitboard = 1ULL << position;
		auto horizontal = MaskedSlide(allPieces, pieceBitboard, bitboard_constants::diagonals[position / 8 + position % 8]);
		auto vertical = MaskedSlide(allPieces, pieceBitboard, bitboard_constants::antidiagonals[position / 8 + 7 - position % 8]);
		return horizontal | vertical;
	}

	Bitboard all_slide(Bitboard allPieces, int position)
	{
		auto hv = horizontal_vertical_slide(allPieces, position);
		auto dad = diagonal_antidiagonal_slide(allPieces, position);
		return hv | dad;
	}
#pragma managed
	public ref class HyperbolaQuintessenceUnmanaged : ChessDotNet::Common::IHyperbolaQuintessence
	{
	public:
		virtual Bitboard AllSlide(Bitboard allPieces, int position)
		{
			return all_slide(allPieces, position);
		}

		virtual Bitboard HorizontalVerticalSlide(Bitboard allPieces, int position)
		{
			return horizontal_vertical_slide(allPieces, position);
		}

		virtual Bitboard DiagonalAntidiagonalSlide(Bitboard allPieces, int position)
		{
			return diagonal_antidiagonal_slide(allPieces, position);
		}
	};
}
