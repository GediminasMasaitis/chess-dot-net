using System;
using ChessDotNet.Data;
using ChessDotNet.Evaluation.Nnue.Managed;
using ChessDotNet.Evaluation.V2;

namespace ChessDotNet.Evaluation.Nnue
{
    public class NnueEvaluationService : IEvaluationService
    {
        private readonly INnueClient _client;

        private readonly EvalHashTable _evalTable;
        private readonly NnuePosition _position;

        public NnueEvaluationService(INnueClient client)
        {
            _client = client;
            _evalTable = new EvalHashTable();
            _evalTable.SetSize(16 * 1024 * 1024);

            _position = new NnuePosition(client.RequiresManagedData);
            _position.Pieces[0] = 1;
            _position.Pieces[1] = 7;
        }

        public int Evaluate(Board board)
        {
            if (EngineOptions.UseEvalHashTable)
            {
                var success = _evalTable.TryProbe(board.Key, out var hashScore);
                if (success)
                {
                    return hashScore;
                }

                var score = EvaluateInner(board);
                _evalTable.Store(board.Key, score);
                return score;
            }
            else
            {
                var score = EvaluateInner(board);
                _evalTable.Store(board.Key, score);
                return score;
            }
        }

        private int EvaluateInner(Board board)
        {
            UpdateCurrentPosition(board);
            var result = _client.Evaluate(_position);
            //result = (result * (100 - board.FiftyMoveRuleIndex)) / 100;
            result = result / 2;
            //json = JsonConvert.SerializeObject(_position);
            return result;
        }

        //private NnueNnueData[] _datas = new NnueNnueData[3] { new NnueNnueData(), new NnueNnueData(), new NnueNnueData()};

        private void UpdateCurrentPosition(Board board)
        {
            _position.Player = board.ColorToMove;
            
            SetPieces(board);

            //for (int i = board.NnueData.Ply; i >= 0; i--)
            int nnueEntryIndex = 0;
            for (; nnueEntryIndex < 3; nnueEntryIndex++)
            {
                var boardIndex = board.NnueData.Ply - nnueEntryIndex;
                if (boardIndex < 0)
                {
                    break;
                }

                //_position.nnue[nnueEntryIndex] = new NnueNnueData();
                _position.Nnue[nnueEntryIndex].accumulator = board.NnueData.Accumulators[boardIndex];
                _position.Nnue[nnueEntryIndex].dirtyPiece = board.NnueData.Dirty[boardIndex];
            }
            _position.NnueCount = nnueEntryIndex;
        }

        private void SetPieces(Board board)
        {
            _position.Squares[0] = board.KingPositions[ChessPiece.White];
            _position.Squares[1] = board.KingPositions[ChessPiece.Black];

            var currentIndex = 2;
            ulong bitboard;

            bitboard = board.BitBoard[ChessPiece.WhitePawn];
            while (bitboard != 0)
            {
                var pos = bitboard.BitScanForward();
                _position.Pieces[currentIndex] = NnueConstants.wpawn;
                _position.Squares[currentIndex] = pos;
                currentIndex++;
                bitboard &= bitboard - 1;
            }

            bitboard = board.BitBoard[ChessPiece.BlackPawn];
            while (bitboard != 0)
            {
                var pos = bitboard.BitScanForward();
                _position.Pieces[currentIndex] = NnueConstants.bpawn;
                _position.Squares[currentIndex] = pos;
                currentIndex++;
                bitboard &= bitboard - 1;
            }

            bitboard = board.BitBoard[ChessPiece.WhiteKnight];
            while (bitboard != 0)
            {
                var pos = bitboard.BitScanForward();
                _position.Pieces[currentIndex] = NnueConstants.wknight;
                _position.Squares[currentIndex] = pos;
                currentIndex++;
                bitboard &= bitboard - 1;
            }

            bitboard = board.BitBoard[ChessPiece.BlackKnight];
            while (bitboard != 0)
            {
                var pos = bitboard.BitScanForward();
                _position.Pieces[currentIndex] = NnueConstants.bknight;
                _position.Squares[currentIndex] = pos;
                currentIndex++;
                bitboard &= bitboard - 1;
            }

            bitboard = board.BitBoard[ChessPiece.WhiteBishop];
            while (bitboard != 0)
            {
                var pos = bitboard.BitScanForward();
                _position.Pieces[currentIndex] = NnueConstants.wbishop;
                _position.Squares[currentIndex] = pos;
                currentIndex++;
                bitboard &= bitboard - 1;
            }

            bitboard = board.BitBoard[ChessPiece.BlackBishop];
            while (bitboard != 0)
            {
                var pos = bitboard.BitScanForward();
                _position.Pieces[currentIndex] = NnueConstants.bbishop;
                _position.Squares[currentIndex] = pos;
                currentIndex++;
                bitboard &= bitboard - 1;
            }

            bitboard = board.BitBoard[ChessPiece.WhiteRook];
            while (bitboard != 0)
            {
                var pos = bitboard.BitScanForward();
                _position.Pieces[currentIndex] = NnueConstants.wrook;
                _position.Squares[currentIndex] = pos;
                currentIndex++;
                bitboard &= bitboard - 1;
            }

            bitboard = board.BitBoard[ChessPiece.BlackRook];
            while (bitboard != 0)
            {
                var pos = bitboard.BitScanForward();
                _position.Pieces[currentIndex] = NnueConstants.brook;
                _position.Squares[currentIndex] = pos;
                currentIndex++;
                bitboard &= bitboard - 1;
            }

            bitboard = board.BitBoard[ChessPiece.WhiteQueen];
            while (bitboard != 0)
            {
                var pos = bitboard.BitScanForward();
                _position.Pieces[currentIndex] = NnueConstants.wqueen;
                _position.Squares[currentIndex] = pos;
                currentIndex++;
                bitboard &= bitboard - 1;
            }

            bitboard = board.BitBoard[ChessPiece.BlackQueen];
            while (bitboard != 0)
            {
                var pos = bitboard.BitScanForward();
                _position.Pieces[currentIndex] = NnueConstants.bqueen;
                _position.Squares[currentIndex] = pos;
                currentIndex++;
                bitboard &= bitboard - 1;
            }

            _position.Pieces[currentIndex] = 0;
            _position.Squares[currentIndex] = 0;

            //for (; currentIndex < 33; currentIndex++)
            //{
            //    _position.pieces[currentIndex] = 0;
            //    _position.squares[currentIndex] = 0;
            //}
        }
    }
}
