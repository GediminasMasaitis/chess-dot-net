﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChessDotNet.Data;

namespace ChessDotNet.Search2
{
    public class PrincipalVariationTable
    {
        public Move[][] Moves { get; set; }
        public int _currentDepth;
        public int _searchedDepth;

        public PrincipalVariationTable()
        {
            Moves = new Move[SearchConstants.MaxDepth][];
            for (var i = 1; i < SearchConstants.MaxDepth; i++)
            {
                Moves[i] = new Move[SearchConstants.MaxDepth];
            }
        }

        public void Clear()
        {
            for (var i = 1; i < _currentDepth; i++)
            {
                var depthMoves = Moves[i];
                Array.Clear(depthMoves, 0, depthMoves.Length);
            }

            _currentDepth = 0;
        }

        public void SetCurrentDepth(int depth)
        {
            _currentDepth = depth;
        }

        public void SetSearchedDepth(int depth)
        {
            _searchedDepth = depth;
        }

        public void SetBestMove(int ply, Move move)
        {
            Moves[_currentDepth][ply] = move;
        }

        public IList<Move> GetPrincipalVariation()
        {
            //Console.WriteLine($"Getting for {_searchedDepth}");
            var moves = new List<Move>();
            foreach (var move in Moves[_searchedDepth])
            {
                if (move.From == move.To)
                {
                    break;
                }
                moves.Add(move);
                
            }
            return moves;
        }

        public PrincipalVariationTable Clone()
        {
            var newTable = new PrincipalVariationTable();
            newTable._currentDepth = _currentDepth;
            newTable._searchedDepth = _searchedDepth;
            for (int i = 1; i < Moves.Length; i++)
            {
                newTable.Moves[i] = Moves[i].ToArray();
            }

            return newTable;
        }
    }
}