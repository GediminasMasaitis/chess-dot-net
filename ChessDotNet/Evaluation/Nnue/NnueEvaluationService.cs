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
            _position.pieces[0] = 1;
            _position.pieces[1] = 7;
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
            //var json = JsonConvert.SerializeObject(_position);
            return result;
        }

        private NnueNnueData[] _datas = new NnueNnueData[3] { new NnueNnueData(), new NnueNnueData(), new NnueNnueData()};

        private void UpdateCurrentPosition(Board board)
        {
            _position.player = board.ColorToMove;
            _position.squares[0] = board.KingPositions[ChessPiece.White];
            _position.squares[1] = board.KingPositions[ChessPiece.Black];

            var currentIndex = 2;
            for (int i = 0; i < 64; i++)
            {
                var piece = board.ArrayBoard[i];
                if (piece == ChessPiece.Empty || piece == ChessPiece.WhiteKing || piece == ChessPiece.BlackKing)
                {
                    continue;
                }

                int pieceNum;
                switch (piece)
                {
                    case ChessPiece.WhiteQueen: pieceNum = 2; break;
                    case ChessPiece.WhiteRook: pieceNum = 3; break;
                    case ChessPiece.WhiteBishop: pieceNum = 4; break;
                    case ChessPiece.WhiteKnight: pieceNum = 5; break;
                    case ChessPiece.WhitePawn: pieceNum = 6; break;

                    case ChessPiece.BlackQueen: pieceNum = 8; break;
                    case ChessPiece.BlackRook: pieceNum = 9; break;
                    case ChessPiece.BlackBishop: pieceNum = 10; break;
                    case ChessPiece.BlackKnight: pieceNum = 11; break;
                    case ChessPiece.BlackPawn: pieceNum = 12; break;
                    default: throw new Exception();
                }

                _position.pieces[currentIndex] = pieceNum;
                _position.squares[currentIndex] = i;
                currentIndex++;
            }

            for (; currentIndex < 33; currentIndex++)
            {
                _position.pieces[currentIndex] = 0;
                _position.squares[currentIndex] = 0;
            }

            _position.nnue[0] = null;
            _position.nnue[1] = null;
            _position.nnue[2] = null;

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
                _position.nnue[nnueEntryIndex] = _datas[nnueEntryIndex];
                _position.nnue[nnueEntryIndex].accumulator = board.NnueData.Accumulators[boardIndex];
                _position.nnue[nnueEntryIndex].dirtyPiece = board.NnueData.Dirty[boardIndex];
            }
        }
    }
}
