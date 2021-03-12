using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ChessDotNet.Evaluation;
using ChessDotNet.Fen;
using ChessDotNet.Hashing;
using ChessDotNet.Testing;

using Bitboard = System.UInt64;
using ZobristKey = System.UInt64;
using Position = System.Byte;
using Piece = System.Byte;
using TTFlag = System.Byte;

namespace ChessDotNet.Data
{
    [Flags]
    public enum CastlingPermission2 : byte
    {
        None = 0,

        WhiteQueen = 1 << 0,
        WhiteKing = 1 << 1,

        BlackQueen = 1 << 2,
        BlackKing = 1 << 3,
        
        All = WhiteKing | WhiteQueen | BlackKing | BlackQueen
    }

    public struct UndoMove
    {
        public Move Move { get; set; }
        public CastlingPermission2 CastlingPermission { get; set; }
        public int EnPassantFileIndex { get; set; }
        public int EnPassantRankIndex { get; set; }
        public int FiftyMoveRule { get; set; }
        public ZobristKey Key { get; set; }

        //public UndoMove(Move move, CastlingPermission2 castlingPermission, int enPassantFileIndex, ulong key)
        //{
        //    Move = move;
        //    CastlingPermission = castlingPermission;
        //    EnPassantFileIndex = enPassantFileIndex;
        //    Key = key;
        //}
    }

    public class Board
    {
        public bool WhiteToMove { get; set; }

        public bool[] CastlingPermissions { get; set; }
        public CastlingPermission2 CastlingPermissions2 { get; set; }
        public UndoMove[] History2 { get; set; }
        public int HistoryDepth { get; set; }


        public Bitboard WhitePieces { get; private set; }
        public Bitboard BlackPieces { get; private set; }
        public Bitboard EmptySquares { get; private set; }
        public Bitboard AllPieces { get; private set; }

        public Bitboard[] BitBoard { get; set; }
        public Piece[] ArrayBoard { get; set; }
        public int EnPassantFileIndex { get; set; }
        public int EnPassantRankIndex { get; set; }
        public Bitboard EnPassantFile { get; set; }
        public ZobristKey Key { get; set; }

        public HistoryEntry[] History { get; set; }
        public int LastTookPieceHistoryIndex { get; set; }

        public int[] PieceCounts { get; set; }
        public int WhiteMaterial { get; set; }
        public int BlackMaterial { get; set; }

        private static CastlingPermission2[] CastleRevocationTable { get; set; }

        public Board()
        {
            //History = new List<HistoryEntry>(128);
            //ArrayBoard = new int[64];
            //BitBoard = new ulong[13];
            EnPassantFileIndex = -1;
            EnPassantRankIndex = -1;
            History2 = new UndoMove[20];
            HistoryDepth = 0;
            CastlingPermissions2 = CastlingPermission2.None;
        }

        static Board()
        {
            CastleRevocationTable = new CastlingPermission2[64];
            for (Position i = 0; i < 64; i++)
            {
                CastlingPermission2 permission = CastlingPermission2.All;
                switch (i)
                {
                    case 0:
                        permission &= ~CastlingPermission2.WhiteQueen;
                        break;
                    case 4:
                        permission &= ~CastlingPermission2.WhiteQueen;
                        permission &= ~CastlingPermission2.WhiteKing;
                        break;
                    case 7:
                        permission &= ~CastlingPermission2.WhiteKing;
                        break;
                    case 56:
                        permission &= ~CastlingPermission2.BlackQueen;
                        break;
                    case 60:
                        permission &= ~CastlingPermission2.BlackQueen;
                        permission &= ~CastlingPermission2.BlackKing;
                        break;
                    case 63:
                        permission &= ~CastlingPermission2.BlackKing;
                        break;
                }

                CastleRevocationTable[i] = permission;
            }
        }

        public void SyncBitBoardsToArrayBoard()
        {
            for (var i = 0; i < 64; i++)
            {
                if ((EmptySquares & (1UL << i)) != 0)
                {
                    ArrayBoard[i] = ChessPiece.Empty;
                }
                else if ((BitBoard[ChessPiece.WhitePawn] & (1UL << i)) != 0)
                {
                    ArrayBoard[i] = ChessPiece.WhitePawn;
                }
                else if ((BitBoard[ChessPiece.BlackPawn] & (1UL << i)) != 0)
                {
                    ArrayBoard[i] = ChessPiece.BlackPawn;
                }

                else if ((BitBoard[ChessPiece.WhiteKnight] & (1UL << i)) != 0)
                {
                    ArrayBoard[i] = ChessPiece.WhiteKnight;
                }
                else if ((BitBoard[ChessPiece.BlackKnight] & (1UL << i)) != 0)
                {
                    ArrayBoard[i] = ChessPiece.BlackKnight;
                }

                else if ((BitBoard[ChessPiece.WhiteBishop] & (1UL << i)) != 0)
                {
                    ArrayBoard[i] = ChessPiece.WhiteBishop;
                }
                else if ((BitBoard[ChessPiece.BlackBishop] & (1UL << i)) != 0)
                {
                    ArrayBoard[i] = ChessPiece.BlackBishop;
                }

                else if ((BitBoard[ChessPiece.WhiteRook] & (1UL << i)) != 0)
                {
                    ArrayBoard[i] = ChessPiece.WhiteRook;
                }
                else if ((BitBoard[ChessPiece.BlackRook] & (1UL << i)) != 0)
                {
                    ArrayBoard[i] = ChessPiece.BlackRook;
                }

                else if ((BitBoard[ChessPiece.WhiteQueen] & (1UL << i)) != 0)
                {
                    ArrayBoard[i] = ChessPiece.WhiteQueen;
                }
                else if ((BitBoard[ChessPiece.BlackQueen] & (1UL << i)) != 0)
                {
                    ArrayBoard[i] = ChessPiece.BlackQueen;
                }

                else if ((BitBoard[ChessPiece.WhiteKing] & (1UL << i)) != 0)
                {
                    ArrayBoard[i] = ChessPiece.WhiteKing;
                }
                else//else if ((BitBoard[ChessPiece.BlackKing] & (1UL << i)) != 0)
                {
                    ArrayBoard[i] = ChessPiece.BlackKing;
                }
            }
        }

        public void SyncArrayBoardToBitBoards()
        {
            for (var i = 0; i < 64; i++)
            {
                var piece = ArrayBoard[i];
                var squareMask = 1UL << i;
                switch (piece)
                {
                    case ChessPiece.Empty:
                        break;

                    case ChessPiece.WhitePawn:
                        BitBoard[ChessPiece.WhitePawn] |= squareMask;
                        break;
                    case ChessPiece.BlackPawn:
                        BitBoard[ChessPiece.BlackPawn] |= squareMask;
                        break;

                    case ChessPiece.WhiteKnight:
                        BitBoard[ChessPiece.WhiteKnight] |= squareMask;
                        break;
                    case ChessPiece.BlackKnight:
                        BitBoard[ChessPiece.BlackKnight] |= squareMask;
                        break;

                    case ChessPiece.WhiteBishop:
                        BitBoard[ChessPiece.WhiteBishop] |= squareMask;
                        break;
                    case ChessPiece.BlackBishop:
                        BitBoard[ChessPiece.BlackBishop] |= squareMask;
                        break;

                    case ChessPiece.WhiteRook:
                        BitBoard[ChessPiece.WhiteRook] |= squareMask;
                        break;
                    case ChessPiece.BlackRook:
                        BitBoard[ChessPiece.BlackRook] |= squareMask;
                        break;

                    case ChessPiece.WhiteQueen:
                        BitBoard[ChessPiece.WhiteQueen] |= squareMask;
                        break;
                    case ChessPiece.BlackQueen:
                        BitBoard[ChessPiece.BlackQueen] |= squareMask;
                        break;

                    case ChessPiece.WhiteKing:
                        BitBoard[ChessPiece.WhiteKing] |= squareMask;
                        break;
                    case ChessPiece.BlackKing:
                        BitBoard[ChessPiece.BlackKing] |= squareMask;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(piece), piece, null);
                }
            }
            SyncExtraBitBoards();
        }

        public void SyncExtraBitBoards()
        {
            WhitePieces = BitBoard[ChessPiece.WhitePawn] 
                | BitBoard[ChessPiece.WhiteKnight]
                | BitBoard[ChessPiece.WhiteBishop] 
                | BitBoard[ChessPiece.WhiteRook]
                | BitBoard[ChessPiece.WhiteQueen]
                | BitBoard[ChessPiece.WhiteKing];

            BlackPieces = BitBoard[ChessPiece.BlackPawn]
                | BitBoard[ChessPiece.BlackKnight]
                | BitBoard[ChessPiece.BlackBishop]
                | BitBoard[ChessPiece.BlackRook]
                | BitBoard[ChessPiece.BlackQueen]
                | BitBoard[ChessPiece.BlackKing];

            AllPieces = WhitePieces | BlackPieces;
            EmptySquares = ~AllPieces;
        }

        public void UndoMove()
        {
            var history = History2[HistoryDepth - 1];
            var move = history.Move;

            EnPassantFileIndex = history.EnPassantFileIndex;
            EnPassantRankIndex = history.EnPassantRankIndex;
            EnPassantFile = EnPassantFileIndex >= 0 ? BitboardConstants.Files[history.EnPassantFileIndex] : 0;
            CastlingPermissions2 = history.CastlingPermission;
            Key = history.Key;

            //var whiteToMove = WhiteToMove;
            WhiteToMove = !WhiteToMove;


            if (move.NullMove)
            {
                SyncExtraBitBoards();
                // TODO check
                return;
            }

            // FROM
            var fromPosBitBoard = 1UL << move.From;
            //Piece promotedPiece = move.PawnPromoteTo.HasValue ? move.PawnPromoteTo.Value : move.Piece;
            ArrayBoard[move.From] = move.Piece;
            BitBoard[move.Piece] |= fromPosBitBoard;

            Piece promotedPiece;
            if (move.PawnPromoteTo.HasValue)
            {
                promotedPiece = move.PawnPromoteTo.Value;
                PieceCounts[move.Piece]++;
                PieceCounts[promotedPiece]--;
                if (WhiteToMove)
                {
                    WhiteMaterial += EvaluationService.Weights[ChessPiece.WhitePawn];
                    WhiteMaterial -= EvaluationService.Weights[promotedPiece];
                }
                else
                {
                    BlackMaterial += EvaluationService.Weights[ChessPiece.BlackPawn];
                    BlackMaterial -= EvaluationService.Weights[promotedPiece];
                }
            }
            else
            {
                promotedPiece = move.Piece;
            }

            // TO
            var toPosBitBoard = 1UL << move.To;
            BitBoard[promotedPiece] &= ~toPosBitBoard;
            if (move.EnPassant)
            {
                ArrayBoard[move.To] = ChessPiece.Empty;
            }
            else
            {
                ArrayBoard[move.To] = move.TakesPiece;
            }

            // TAKES
            if (move.TakesPiece > 0)
            {
                if (!move.EnPassant)
                {
                    BitBoard[move.TakesPiece] |= toPosBitBoard;
                }
                //LastTookPieceHistoryIndex = History.Length; // TODO
                PieceCounts[move.TakesPiece]++;
                if (WhiteToMove)
                {
                    BlackMaterial += EvaluationService.Weights[move.TakesPiece];
                }
                else
                {
                    WhiteMaterial += EvaluationService.Weights[move.TakesPiece];
                }
            }
            else
            {
                LastTookPieceHistoryIndex = LastTookPieceHistoryIndex;
            }

            // EN PASSANT
            if (move.EnPassant)
            {
                int killedPawnPos;
                if (move.Piece == ChessPiece.WhitePawn)
                {
                    killedPawnPos = move.To - 8;
                }
                else
                {
                    killedPawnPos = move.To + 8;
                }

                var killedPawnBitBoard = 1UL << killedPawnPos;

                BitBoard[move.TakesPiece] |= killedPawnBitBoard;
                ArrayBoard[killedPawnPos] = move.TakesPiece;

                //BitBoard[move.TakesPiece] &= ~killedPawnBitBoard;
                //ArrayBoard[killedPawnPos] = ChessPiece.Empty;
                //Key ^= ZobristKeys.ZPieces[killedPawnPos, move.TakesPiece];
            }

            if (move.Castle)
            {
                var kingSide = move.To % 8 > 3;
                var isWhite = move.Piece == ChessPiece.WhiteKing;
                var castlingRookPos = (kingSide ? 7 : 0) + (isWhite ? 0 : 56);
                var castlingRookNewPos = (move.From + move.To) / 2;
                var rookPiece = isWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook;

                ArrayBoard[castlingRookPos] = rookPiece;
                ArrayBoard[castlingRookNewPos] = ChessPiece.Empty;
                BitBoard[rookPiece] |= (1UL << castlingRookPos);
                BitBoard[rookPiece] &= ~(1UL << castlingRookNewPos);
            }

            SyncCastleTo1();
            SyncExtraBitBoards();
        }

        public void SyncCastleTo2()
        {
            CastlingPermission2 value = CastlingPermission2.None;
            if (CastlingPermissions[CastlePermission.WhiteKingSide])
            {
                value |= CastlingPermission2.WhiteKing;
            }
            if (CastlingPermissions[CastlePermission.WhiteQueenSide])
            {
                value |= CastlingPermission2.WhiteQueen;
            }
            if (CastlingPermissions[CastlePermission.BlackKingSide])
            {
                value |= CastlingPermission2.BlackKing;
            }
            if (CastlingPermissions[CastlePermission.BlackQueenSide])
            {
                value |= CastlingPermission2.BlackQueen;
            }

            CastlingPermissions2 = value;
        }

        public void SyncCastleTo1()
        {
            CastlingPermissions[CastlePermission.WhiteQueenSide] = (CastlingPermissions2 & CastlingPermission2.WhiteQueen) != CastlingPermission2.None;
            CastlingPermissions[CastlePermission.WhiteKingSide] = (CastlingPermissions2 & CastlingPermission2.WhiteKing) != CastlingPermission2.None;
            CastlingPermissions[CastlePermission.BlackQueenSide] = (CastlingPermissions2 & CastlingPermission2.BlackQueen) != CastlingPermission2.None;
            CastlingPermissions[CastlePermission.BlackKingSide] = (CastlingPermissions2 & CastlingPermission2.BlackKing) != CastlingPermission2.None;
        }

        public void DoMove2(Move move)
        {
            // history
            History2[HistoryDepth].Move = move;
            History2[HistoryDepth].Key = Key;
            History2[HistoryDepth].CastlingPermission = CastlingPermissions2;
            History2[HistoryDepth].EnPassantFileIndex = EnPassantFileIndex;
            History2[HistoryDepth].EnPassantRankIndex = EnPassantRankIndex;
            HistoryDepth++;
            
            var whiteToMove = WhiteToMove;

            WhiteToMove = !whiteToMove;
            Key ^= ZobristKeys.ZWhiteToMove;

            if (EnPassantFile != 0)
            {
                Key ^= ZobristKeys.ZEnPassant[EnPassantFileIndex];
            }

            if (move.NullMove)
            {
                SyncExtraBitBoards();
                // TODO check
                return;
            }


            // FROM
            var fromPosBitBoard = 1UL << move.From;
            ArrayBoard[move.From] = ChessPiece.Empty;
            BitBoard[move.Piece] &= ~fromPosBitBoard;
            Key ^= ZobristKeys.ZPieces[move.From, move.Piece];

            Piece promotedPiece;
            if (move.PawnPromoteTo.HasValue)
            {
                promotedPiece = move.PawnPromoteTo.Value;
                PieceCounts[move.Piece]--;
                PieceCounts[promotedPiece]++;
                if (whiteToMove)
                {
                    WhiteMaterial -= EvaluationService.Weights[ChessPiece.WhitePawn];
                    WhiteMaterial += EvaluationService.Weights[promotedPiece];
                }
                else
                {
                    BlackMaterial -= EvaluationService.Weights[ChessPiece.BlackPawn];
                    BlackMaterial += EvaluationService.Weights[promotedPiece];
                }
            }
            else
            {
                promotedPiece = move.Piece;
            }


            // TO
            var toPosBitBoard = 1UL << move.To;
            ArrayBoard[move.To] = promotedPiece;
            BitBoard[promotedPiece] |= toPosBitBoard;
            Key ^= ZobristKeys.ZPieces[move.To, promotedPiece];

            // TAKES
            if (move.TakesPiece > 0)
            {
                if (!move.EnPassant)
                {
                    BitBoard[move.TakesPiece] &= ~toPosBitBoard;
                    Key ^= ZobristKeys.ZPieces[move.To, move.TakesPiece];
                }
                //LastTookPieceHistoryIndex = History.Length; // TODO
                PieceCounts[move.TakesPiece]--;
                if (whiteToMove)
                {
                    BlackMaterial -= EvaluationService.Weights[move.TakesPiece];
                }
                else
                {
                    WhiteMaterial -= EvaluationService.Weights[move.TakesPiece];
                }
            }
            else
            {
                LastTookPieceHistoryIndex = LastTookPieceHistoryIndex;
            }

            // EN PASSANT
            if (move.EnPassant)
            {
                int killedPawnPos;
                if (move.Piece == ChessPiece.WhitePawn)
                {
                    killedPawnPos = move.To - 8;
                }
                else
                {
                    killedPawnPos = move.To + 8;
                }

                var killedPawnBitBoard = 1UL << killedPawnPos;

                BitBoard[move.TakesPiece] &= ~killedPawnBitBoard;
                ArrayBoard[killedPawnPos] = ChessPiece.Empty;
                Key ^= ZobristKeys.ZPieces[killedPawnPos, move.TakesPiece];
            }

            // PAWN DOUBLE MOVES
            if ((move.Piece == ChessPiece.WhitePawn && move.From + 16 == move.To) || (move.Piece == ChessPiece.BlackPawn && move.From - 16 == move.To))
            {
                var fileIndex = move.From % 8;
                var rankIndex = (move.To >> 3) + (whiteToMove ? -1 : 1);
                EnPassantFile = BitboardConstants.Files[fileIndex];
                EnPassantFileIndex = fileIndex;
                EnPassantRankIndex = rankIndex;
                Key ^= ZobristKeys.ZEnPassant[fileIndex];
            }
            else
            {
                EnPassantFile = 0;
                EnPassantFileIndex = -1;
                EnPassantRankIndex = -1;
            }

            // CASTLING
            if (move.Castle)
            {
                var kingSide = move.To % 8 > 3;
                var isWhite = move.Piece == ChessPiece.WhiteKing;
                var castlingRookPos = (kingSide ? 7 : 0) + (isWhite ? 0 : 56);
                var castlingRookNewPos = (move.From + move.To) / 2;
                var rookPiece = isWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook;

                ArrayBoard[castlingRookPos] = ChessPiece.Empty;
                ArrayBoard[castlingRookNewPos] = rookPiece;
                BitBoard[rookPiece] &= ~(1UL << castlingRookPos);
                BitBoard[rookPiece] |= 1UL << castlingRookNewPos;
                Key ^= ZobristKeys.ZPieces[castlingRookPos, rookPiece];
                Key ^= ZobristKeys.ZPieces[castlingRookNewPos, rookPiece];
            }

            CastlingPermissions2 &= CastleRevocationTable[move.From];
            CastlingPermissions2 &= CastleRevocationTable[move.To];

            if (move.Piece == ChessPiece.WhiteKing)
            {
                if (CastlingPermissions[CastlePermission.WhiteQueenSide])
                {
                    CastlingPermissions[CastlePermission.WhiteQueenSide] = false;
                    Key ^= ZobristKeys.ZCastle[CastlePermission.WhiteQueenSide];
                }

                if (CastlingPermissions[CastlePermission.WhiteKingSide])
                {
                    CastlingPermissions[CastlePermission.WhiteKingSide] = false;
                    Key ^= ZobristKeys.ZCastle[CastlePermission.WhiteKingSide];
                }
            }
            else if (move.Piece == ChessPiece.WhiteRook)
            {
                if (CastlingPermissions[CastlePermission.WhiteQueenSide] && move.From == 0)
                {
                    CastlingPermissions[CastlePermission.WhiteQueenSide] = false;
                    Key ^= ZobristKeys.ZCastle[CastlePermission.WhiteQueenSide];
                }

                if (CastlingPermissions[CastlePermission.WhiteKingSide] && move.From == 7)
                {
                    CastlingPermissions[CastlePermission.WhiteKingSide] = false;
                    Key ^= ZobristKeys.ZCastle[CastlePermission.WhiteKingSide];
                }
            }
            else if (move.Piece == ChessPiece.BlackKing)
            {
                if (CastlingPermissions[CastlePermission.BlackQueenSide])
                {
                    CastlingPermissions[CastlePermission.BlackQueenSide] = false;
                    Key ^= ZobristKeys.ZCastle[CastlePermission.BlackQueenSide];
                }

                if (CastlingPermissions[CastlePermission.BlackKingSide])
                {
                    CastlingPermissions[CastlePermission.BlackKingSide] = false;
                    Key ^= ZobristKeys.ZCastle[CastlePermission.BlackKingSide];
                }
            }
            else if (move.Piece == ChessPiece.BlackRook)
            {
                if (CastlingPermissions[CastlePermission.BlackQueenSide] && move.From == 56)
                {
                    CastlingPermissions[CastlePermission.BlackQueenSide] = false;
                    Key ^= ZobristKeys.ZCastle[CastlePermission.BlackQueenSide];
                }

                if (CastlingPermissions[CastlePermission.BlackKingSide] && move.From == 63)
                {
                    CastlingPermissions[CastlePermission.BlackKingSide] = false;
                    Key ^= ZobristKeys.ZCastle[CastlePermission.BlackKingSide];
                }
            }

            switch (move.To)
            {
                case 0:
                    if (CastlingPermissions[CastlePermission.WhiteQueenSide])
                    {
                        CastlingPermissions[CastlePermission.WhiteQueenSide] = false;
                        Key ^= ZobristKeys.ZCastle[CastlePermission.WhiteQueenSide];
                    }
                    break;
                case 7:
                    if (CastlingPermissions[CastlePermission.WhiteKingSide])
                    {
                        CastlingPermissions[CastlePermission.WhiteKingSide] = false;
                        Key ^= ZobristKeys.ZCastle[CastlePermission.WhiteKingSide];
                    }
                    break;
                case 56:
                    if (CastlingPermissions[CastlePermission.BlackQueenSide])
                    {
                        CastlingPermissions[CastlePermission.BlackQueenSide] = false;
                        Key ^= ZobristKeys.ZCastle[CastlePermission.BlackQueenSide];
                    }
                    break;
                case 63:
                    if (CastlingPermissions[CastlePermission.BlackKingSide])
                    {
                        CastlingPermissions[CastlePermission.BlackKingSide] = false;
                        Key ^= ZobristKeys.ZCastle[CastlePermission.BlackKingSide];
                    }
                    break;
            }

            SyncCastleTo1();
            SyncExtraBitBoards();
        }

        private Board Clone()
        {
            var clone = new Board();
            clone.WhiteToMove = WhiteToMove;
            clone.CastlingPermissions = (bool[])CastlingPermissions.Clone();
            clone.WhitePieces = WhitePieces;
            clone.BlackPieces = BlackPieces;
            clone.EmptySquares = EmptySquares;
            clone.AllPieces = AllPieces;
            clone.BitBoard = (Bitboard[])BitBoard.Clone();
            clone.ArrayBoard = (Piece[])ArrayBoard.Clone();
            clone.EnPassantFileIndex = EnPassantFileIndex;
            clone.EnPassantRankIndex = EnPassantRankIndex;
            clone.EnPassantFile = EnPassantFile;
            clone.Key = Key;
            //clone.History = board.History; // TODO
            clone.LastTookPieceHistoryIndex = LastTookPieceHistoryIndex;
            clone.PieceCounts = (int[])PieceCounts.Clone();
            clone.WhiteMaterial = WhiteMaterial;
            clone.BlackMaterial = BlackMaterial;

            clone.History2 = (UndoMove[])History2.Clone();
            clone.HistoryDepth = HistoryDepth;
            clone.CastlingPermissions2 = CastlingPermissions2;


            return clone;
        }

        private bool ExactlyEquals(Board clone)
        {
            if (clone.WhiteToMove != WhiteToMove)
            {
                return false;
            }

            if (!clone.CastlingPermissions.SequenceEqual(CastlingPermissions))
            {
                return false;
            }

            for (int i = 0; i < clone.BitBoard.Length; i++)
            {
                var bitBoard1 = BitBoard[i];
                var bitBoard2 = clone.BitBoard[i];
                if (bitBoard1 != bitBoard2)
                {
                    return false;
                }

            }

            if (clone.WhitePieces != WhitePieces)
            {
                return false;
            }

            if (clone.BlackPieces != BlackPieces)
            {
                return false;
            }

            if (clone.EmptySquares != EmptySquares)
            {
                return false;
            }

            if (clone.AllPieces != AllPieces)
            {
                return false;
            }

            for (int i = 0; i < clone.ArrayBoard.Length; i++)
            {
                var piece1 = ArrayBoard[i];
                var piece2 = clone.ArrayBoard[i];
                if (piece1 != piece2)
                {
                    return false;
                }

            }

            //if (!clone.ArrayBoard.SequenceEqual(ArrayBoard))
            //{
            //    return false;
            //}

            if (clone.EnPassantFileIndex != EnPassantFileIndex)
            {
                return false;
            }

            if (clone.EnPassantRankIndex != EnPassantRankIndex)
            {
                return false;
            }

            if (clone.EnPassantFile != EnPassantFile)
            {
                return false;
            }

            if (clone.Key != Key)
            {
                return false;
            }

            //if (board.LastTookPieceHistoryIndex != LastTookPieceHistoryIndex)
            //{
            //    return false;
            //}

            if (!clone.PieceCounts.SequenceEqual(PieceCounts))
            {
                return false;
            }

            if (clone.WhiteMaterial != WhiteMaterial)
            {
                return false;
            }

            if (clone.BlackMaterial != BlackMaterial)
            {
                return false;
            }

            if (clone.CastlingPermissions2 != CastlingPermissions2)
            {
                return false;
            }

            for (var i = 0; i < CastlingPermissions.Length; i++)
            {
                var hasPermission = CastlingPermissions[i];
                var hasPermission2 = (clone.CastlingPermissions2 & (CastlingPermission2)(1 << i)) != CastlingPermission2.None;
                if (hasPermission != hasPermission2)
                {
                    return false;
                }
            }

            //if (!clone.History2.SequenceEqual(History2))
            //{
            //    return false;
            //}

            //for (var i = 0; i < History2.Length; i++)
            //{
            //    var history1 = History2[i];
            //    var history2 = clone.History2[i];
            //    if (history1 != history2)
            //    {
            //        return false;
            //    }
            //}

            //if (clone.HistoryDepth != HistoryDepth)
            //{
            //    return false;
            //}

            //if (clone.CastlingPermissions2 != CastlingPermissions2)
            //{
            //    return false;
            //}

            return true;
        }

        public Board DoMove(Move move, bool allowNull = true)
        {
#if TEST
            this.CheckBoard();
#endif
            //foreach (var pair in PiecesDict)
            var newBoard = new Board();
            newBoard.ArrayBoard = new Piece[ArrayBoard.Length]; //ArrayBoard.ToArray();
            Buffer.BlockCopy(ArrayBoard, 0, newBoard.ArrayBoard, 0, ArrayBoard.Length * sizeof(Piece));
            //Array.Copy(ArrayBoard, newBoard.ArrayBoard, ArrayBoard.Length);

            newBoard.BitBoard = new Bitboard[BitBoard.Length];//BitBoard.ToArray();
            Buffer.BlockCopy(BitBoard, 0, newBoard.BitBoard, 0, BitBoard.Length * sizeof(Bitboard));
            //Array.Copy(BitBoard, newBoard.BitBoard, BitBoard.Length);

            newBoard.CastlingPermissions = new bool[CastlePermission.Length];//CastlingPermissions.ToArray();
            Buffer.BlockCopy(CastlingPermissions, 0, newBoard.CastlingPermissions, 0, CastlingPermissions.Length * sizeof(bool));
            //Array.Copy(CastlingPermissions, newBoard.CastlingPermissions, CastlingPermissions.Length);

            newBoard.PieceCounts = new int[PieceCounts.Length];//PieceCounts.ToArray();
            Buffer.BlockCopy(PieceCounts, 0, newBoard.PieceCounts, 0, PieceCounts.Length * sizeof(int));
            //Array.Copy(PieceCounts, newBoard.PieceCounts, PieceCounts.Length);

            newBoard.WhiteMaterial = WhiteMaterial;
            newBoard.BlackMaterial = BlackMaterial;
            newBoard.Key = Key;


            var newHistory = new HistoryEntry[History.Length + 1];
            Array.Copy(History, newHistory, History.Length);
            var newEntry = new HistoryEntry(this, move);
            newHistory[newHistory.Length - 1] = newEntry;
            newBoard.History = newHistory;

            newBoard.WhiteToMove = !WhiteToMove;
            newBoard.Key ^= ZobristKeys.ZWhiteToMove;

            if (EnPassantFile != 0)
            {
                newBoard.Key ^= ZobristKeys.ZEnPassant[EnPassantFileIndex];
            }

            if (move.NullMove)
            {
                newBoard.SyncExtraBitBoards();
#if TEST
                newBoard.CheckBoard();
#endif
                return newBoard;
            }

            var toPosBitBoard = 1UL << move.To;

            newBoard.ArrayBoard[move.From] = ChessPiece.Empty;
            newBoard.BitBoard[move.Piece] &= ~(1UL << move.From);
            newBoard.Key ^= ZobristKeys.ZPieces[move.From, move.Piece];

            Piece promotedPiece;
            if (move.PawnPromoteTo.HasValue)
            {
                promotedPiece = move.PawnPromoteTo.Value;
                newBoard.PieceCounts[move.Piece]--;
                newBoard.PieceCounts[promotedPiece]++;
                if (WhiteToMove)
                {
                    newBoard.WhiteMaterial -= EvaluationService.Weights[ChessPiece.WhitePawn];
                    newBoard.WhiteMaterial += EvaluationService.Weights[promotedPiece];
                }
                else
                {
                    newBoard.BlackMaterial -= EvaluationService.Weights[ChessPiece.BlackPawn];
                    newBoard.BlackMaterial += EvaluationService.Weights[promotedPiece];
                }
            }
            else
            {
                promotedPiece = move.Piece;
            }
            newBoard.ArrayBoard[move.To] = promotedPiece;
            newBoard.BitBoard[promotedPiece] |= toPosBitBoard;
            newBoard.Key ^= ZobristKeys.ZPieces[move.To, promotedPiece];

            if (move.TakesPiece > 0)
            {
                if (!move.EnPassant)
                {
                    newBoard.BitBoard[move.TakesPiece] &= ~toPosBitBoard;
                    newBoard.Key ^= ZobristKeys.ZPieces[move.To, move.TakesPiece];
                }
                newBoard.LastTookPieceHistoryIndex = History.Length;
                newBoard.PieceCounts[move.TakesPiece]--;
                if (WhiteToMove)
                {
                    newBoard.BlackMaterial -= EvaluationService.Weights[move.TakesPiece];
                }
                else
                {
                    newBoard.WhiteMaterial -= EvaluationService.Weights[move.TakesPiece];
                }
            }
            else
            {
                newBoard.LastTookPieceHistoryIndex = LastTookPieceHistoryIndex;
            }

            // EN PASSANT
            if (move.EnPassant)
            {
                int killedPawnPos;
                if (move.Piece == ChessPiece.WhitePawn)
                {
                    killedPawnPos = move.To - 8;
                }
                else
                {
                    killedPawnPos = move.To + 8;
                }

                var killedPawnBitBoard = 1UL << killedPawnPos;

                newBoard.BitBoard[move.TakesPiece] &= ~killedPawnBitBoard;
                newBoard.ArrayBoard[killedPawnPos] = ChessPiece.Empty;
                newBoard.Key ^= ZobristKeys.ZPieces[killedPawnPos, move.TakesPiece];
            }

            // PAWN DOUBLE MOVES
            if ((move.Piece == ChessPiece.WhitePawn && move.From + 16 == move.To) || (move.Piece == ChessPiece.BlackPawn && move.From - 16 == move.To))
            {
                var fileIndex = move.From % 8;
                var rankIndex = (move.From >> 3) + (WhiteToMove ? 1 : -1);
                newBoard.EnPassantFile = BitboardConstants.Files[fileIndex];
                newBoard.EnPassantFileIndex = fileIndex;
                newBoard.EnPassantRankIndex = rankIndex;
                newBoard.Key ^= ZobristKeys.ZEnPassant[fileIndex];
            }
            else
            {
                newBoard.EnPassantFile = 0;
                newBoard.EnPassantFileIndex = -1;
            }

            // CASTLING
            // TODO: REMAKE THIS
            if (move.Castle)
            {
                var kingSide = move.To % 8 > 3;
                var isWhite = move.Piece == ChessPiece.WhiteKing;
                var castlingRookPos = (kingSide ? 7 : 0) + (isWhite ? 0 : 56);
                var castlingRookNewPos = (move.From + move.To) / 2;
                var rookPiece = isWhite ? ChessPiece.WhiteRook : ChessPiece.BlackRook;

                newBoard.ArrayBoard[castlingRookPos] = ChessPiece.Empty;
                newBoard.ArrayBoard[castlingRookNewPos] = rookPiece;
                newBoard.BitBoard[rookPiece] &= ~(1UL << castlingRookPos);
                newBoard.BitBoard[rookPiece] |= 1UL << castlingRookNewPos;
                newBoard.Key ^= ZobristKeys.ZPieces[castlingRookPos, rookPiece];
                newBoard.Key ^= ZobristKeys.ZPieces[castlingRookNewPos, rookPiece];
            }

            if (move.Piece == ChessPiece.WhiteKing)
            {
                newBoard.CastlingPermissions[CastlePermission.WhiteQueenSide] = false;
                newBoard.CastlingPermissions[CastlePermission.WhiteKingSide] = false;
            }
            else if (move.Piece == ChessPiece.WhiteRook)
            {
                newBoard.CastlingPermissions[CastlePermission.WhiteQueenSide] = CastlingPermissions[CastlePermission.WhiteQueenSide] && move.From != 0;
                newBoard.CastlingPermissions[CastlePermission.WhiteKingSide] = CastlingPermissions[CastlePermission.WhiteKingSide] && move.From != 7;
            }
            else if (move.Piece == ChessPiece.BlackKing)
            {
                newBoard.CastlingPermissions[CastlePermission.BlackQueenSide] = false;
                newBoard.CastlingPermissions[CastlePermission.BlackKingSide] = false;
            }
            else if (move.Piece == ChessPiece.BlackRook)
            {
                newBoard.CastlingPermissions[CastlePermission.BlackQueenSide] = CastlingPermissions[CastlePermission.BlackQueenSide] && move.From != 56;
                newBoard.CastlingPermissions[CastlePermission.BlackKingSide] = CastlingPermissions[CastlePermission.BlackKingSide] && move.From != 63;
            }

            switch (move.To)
            {
                case 0:
                    newBoard.CastlingPermissions[CastlePermission.WhiteQueenSide] = false;
                    break;
                case 7:
                    newBoard.CastlingPermissions[CastlePermission.WhiteKingSide] = false;
                    break;
                case 56:
                    newBoard.CastlingPermissions[CastlePermission.BlackQueenSide] = false;
                    break;
                case 63:
                    newBoard.CastlingPermissions[CastlePermission.BlackKingSide] = false;
                    break;
            }

            for (var i = 0; i < 4; i++)
            {
                if (CastlingPermissions[i] != newBoard.CastlingPermissions[i])
                {
                    newBoard.Key ^= ZobristKeys.ZCastle[i];
                }
            }

            newBoard.SyncCastleTo2();
            SyncCastleTo2();
            newBoard.SyncExtraBitBoards();


#if TEST
            newBoard.CheckBoard();
#endif
            //if (move.From == 0 && move.To == 8)
            //{
            //    Console.WriteLine(Print());
            //    var a = 123;
            //}

            //if (move.EnPassant)
            //{
            //    Console.WriteLine(Print());
            //    var a = 123;
            //}

            var clone = Clone();
            clone.DoMove2(move);

            if (!newBoard.ExactlyEquals(clone))
            {
                Console.WriteLine($"Fuck {Key}");
                Debugger.Break();
            }

            if (!clone.ExactlyEquals(newBoard))
            {
                Console.WriteLine($"Fuck {Key}");
                Debugger.Break();
            }

            clone.UndoMove();
            if (!ExactlyEquals(clone))
            {
                Console.WriteLine($"Fuck2 {Key}");
                Debugger.Break();
            }

            //if (allowNull)
            //{
            //    move = new Move(0, 0, 0);
            //    newBoard = DoMove(move, false);
            //    newBoard.SyncCastleTo2();
            //    clone.DoMove2(move);

            //    if (!newBoard.ExactlyEquals(clone))
            //    {
            //        Console.WriteLine($"Fuck {Key}");
            //        Debugger.Break();
            //    }

            //    if (!clone.ExactlyEquals(newBoard))
            //    {
            //        Console.WriteLine($"Fuck {Key}");
            //        Debugger.Break();
            //    }

            //    clone.UndoMove();
            //    if (!ExactlyEquals(clone))
            //    {
            //        Console.WriteLine($"Fuck2 {Key}");
            //        Debugger.Break();
            //    }
            //}
            

            return newBoard;
        }

        public void SyncMaterial()
        {
            WhiteMaterial = 0;
            BlackMaterial = 0;
            for (var i = 1; i < 7; i++)
            {
                WhiteMaterial += PieceCounts[i]*EvaluationService.Weights[i];
            }
            for (var i = 7; i < 13; i++)
            {
                BlackMaterial += PieceCounts[i]*EvaluationService.Weights[i];
            }
        }

        public void SyncPiecesCount()
        {
            PieceCounts = new int[13];
            for (var i = 0; i < 64; i++)
            {
                var piece = ArrayBoard[i];
                PieceCounts[piece]++;
            }
        }

        public string Print(IEvaluationService evaluationService = null, FenSerializerService fenService = null)
        {
            const bool useUnicodeSymbols = false;
            const bool useUnicodeSeparators = true;

             const string separators     = "   +---+---+---+---+---+---+---+---+";

            const string separatorsTop     = "   ┌───┬───┬───┬───┬───┬───┬───┬───┐";
            const string separatorsMid     = "   ├───┼───┼───┼───┼───┼───┼───┼───┤";
            const string separatorsBottom  = "   └───┴───┴───┴───┴───┴───┴───┴───┘";
            const char separator = useUnicodeSeparators ? '│' : '|';;

            const string fileMarkers = "     A   B   C   D   E   F   G   H  ";
            

            var infos = new List<string>();

            infos.Add("Hash key: " + Key.ToString("X").PadLeft(16, '0'));
            infos.Add("To move: " + (WhiteToMove ? "White" : "Black"));
            infos.Add("Material: " + (WhiteMaterial - BlackMaterial));
            infos.Add("White material: " + WhiteMaterial);
            infos.Add("Black material: " + BlackMaterial);
            if (fenService != null)
            {
                var fen = fenService.SerializeToFen(this);
                infos.Add($"FEN: {fen}");
            }

            if (evaluationService != null)
            {
                var score = evaluationService.Evaluate(this);
                if (!WhiteToMove)
                {
                    score = -score;
                }
                infos.Add("Evaluation: " + score);
            }


            var sb = new StringBuilder();
            for (var i = 7; i >= 0; i--)
            {
                if (i == 7)
                {
                    sb.AppendLine(useUnicodeSeparators ? separatorsTop : separators);
                }
                else
                {
                    sb.AppendLine(useUnicodeSeparators ? separatorsMid : separators);
                }
                
                sb.Append(" " + (i+1) + " ");

                for (var j = 0; j < 8; j++)
                {
                    var piece = ArrayBoard[i*8 + j];
                    var pieceChar = useUnicodeSymbols ? ChessPiece.ChessPieceToSymbol(piece) : ChessPiece.ChessPieceToLetter(piece);
                    sb.Append($"{separator} {pieceChar} ");
                }
                sb.Append("|   ");
                if (infos.Count > 7-i)
                {
                    sb.Append(infos[7-i]);
                }
                sb.AppendLine();
            }
            sb.AppendLine(useUnicodeSeparators ? separatorsBottom : separators);
            sb.AppendLine(fileMarkers);
            return sb.ToString();
        }

        
    }
}