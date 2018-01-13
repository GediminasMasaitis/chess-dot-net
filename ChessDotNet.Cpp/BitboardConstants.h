#pragma once

#include "Stdafx.h"

#pragma unmanaged
namespace bitboard_constants
{
	std::array<Bitboard, 8> files;
	std::array<Bitboard, 8> ranks;
	std::array<Bitboard, 15> diagonals;
	std::array<Bitboard, 15> antidiagonals;

	void init_bitboard_constants()
	{
		diagonals =
		{
			0x1UL,
			0x102UL,
			0x10204UL,
			0x1020408UL,
			0x102040810UL,
			0x10204081020UL,
			0x1020408102040UL,
			0x102040810204080UL,
			0x204081020408000UL,
			0x408102040800000UL,
			0x810204080000000UL,
			0x1020408000000000UL,
			0x2040800000000000UL,
			0x4080000000000000UL,
			0x8000000000000000UL
		};

		antidiagonals =
		{
			0x80UL,
			0x8040UL,
			0x804020UL,
			0x80402010UL,
			0x8040201008UL,
			0x804020100804UL,
			0x80402010080402UL,
			0x8040201008040201UL,
			0x4020100804020100UL,
			0x2010080402010000UL,
			0x1008040201000000UL,
			0x804020100000000UL,
			0x402010000000000UL,
			0x201000000000000UL,
			0x100000000000000UL
		};

		for(auto i = 0; i < 8; i++)
		{
			Bitboard rank = 0ULL;
			for(auto j = 0; j < 8; j++)
			{
				rank |= 1ULL << (i * 8) << j;
			}
			ranks[i] = rank;
		}

		for(auto i = 0; i < 8; i++)
		{
			Bitboard file = 0ULL;
			for(auto j = 0; j < 8; j++)
			{
				file |= 1ULL << i << (j * 8);
			}
			files[i] = file;
		}
	}
}