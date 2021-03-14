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
    public enum CastlingPermission : byte
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
        public CastlingPermission CastlingPermission { get; set; }
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
        public CastlingPermission CastlingPermissions { get; set; }
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

        private static CastlingPermission[] CastleRevocationTable { get; set; }

        public Board()
        {
            //History = new List<HistoryEntry>(128);
            //ArrayBoard = new int[64];
            //BitBoard = new ulong[13];
            EnPassantFileIndex = -1;
            EnPassantRankIndex = -1;
            History2 = new UndoMove[20];
            HistoryDepth = 0;
            CastlingPermissions = CastlingPermission.None;
        }

        static Board()
        {
            CastleRevocationTable = new CastlingPermission[64];
            for (Position i = 0; i < 64; i++)
            {
                CastlingPermission permission = CastlingPermission.All;
                switch (i)
                {
                    case 0:
                        permission &= ~CastlingPermission.WhiteQueen;
                        break;
                    case 4:
                        permission &= ~CastlingPermission.WhiteQueen;
                        permission &= ~CastlingPermission.WhiteKing;
                        break;
                    case 7:
                        permission &= ~CastlingPermission.WhiteKing;
                        break;
                    case 56:
                        permission &= ~CastlingPermission.BlackQueen;
                        break;
                    case 60:
                        permission &= ~CastlingPermission.BlackQueen;
                        permission &= ~CastlingPermission.BlackKing;
                        break;
                    case 63:
                        permission &= ~CastlingPermission.BlackKing;
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

        private void TestUndoMove()
        {
            var clone = Clone();
            Debug.Assert(ExactlyEquals(this, clone));
            var entry = clone.History2[HistoryDepth - 1];
            var move = entry.Move;
            clone.UndoMove(false);
            clone.DoMove2(move, false);
            Debug.Assert(ExactlyEquals(this, clone));
        }

        public void UndoMove(bool test = false)
        {
            var history = History2[HistoryDepth - 1];
            var move = history.Move;

            if (test)
            {
                TestUndoMove();
            }

            HistoryDepth--;

            EnPassantFileIndex = history.EnPassantFileIndex;
            EnPassantRankIndex = history.EnPassantRankIndex;
            EnPassantFile = EnPassantFileIndex >= 0 ? BitboardConstants.Files[history.EnPassantFileIndex] : 0;
            CastlingPermissions = history.CastlingPermission;
            Key = history.Key;
            LastTookPieceHistoryIndex = history.FiftyMoveRule;

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

            //SyncCastleTo1();
            SyncExtraBitBoards();
        }

        //public void SyncCastleTo2()
        //{
        //    CastlingPermission2 value = CastlingPermission2.None;
        //    if (CastlingPermissions[CastlePermission.WhiteKingSide])
        //    {
        //        value |= CastlingPermission2.WhiteKing;
        //    }
        //    if (CastlingPermissions[CastlePermission.WhiteQueenSide])
        //    {
        //        value |= CastlingPermission2.WhiteQueen;
        //    }
        //    if (CastlingPermissions[CastlePermission.BlackKingSide])
        //    {
        //        value |= CastlingPermission2.BlackKing;
        //    }
        //    if (CastlingPermissions[CastlePermission.BlackQueenSide])
        //    {
        //        value |= CastlingPermission2.BlackQueen;
        //    }

        //    CastlingPermissions2 = value;
        //}

        //public void SyncCastleTo1()
        //{
        //    CastlingPermissions[CastlePermission.WhiteQueenSide] = (CastlingPermissions2 & CastlingPermission2.WhiteQueen) != CastlingPermission2.None;
        //    CastlingPermissions[CastlePermission.WhiteKingSide] = (CastlingPermissions2 & CastlingPermission2.WhiteKing) != CastlingPermission2.None;
        //    CastlingPermissions[CastlePermission.BlackQueenSide] = (CastlingPermissions2 & CastlingPermission2.BlackQueen) != CastlingPermission2.None;
        //    CastlingPermissions[CastlePermission.BlackKingSide] = (CastlingPermissions2 & CastlingPermission2.BlackKing) != CastlingPermission2.None;
        //}

        public void TestMove(Move move)
        {
            var clone = Clone();
            Debug.Assert(ExactlyEquals(this, clone));
            clone.DoMove2(move, false);
            clone.UndoMove(false);
            Debug.Assert(ExactlyEquals(this, clone));
        }

        public Board DoMove(Move move)
        {
            var clone = Clone();
            clone.DoMove2(move);
            return clone;
        }

        public void DoMove2(Move move, bool test = false)
        {
            if (test)
            {
                TestMove(move);
            }

            //var clone = Clone();
            //var newBoard = DoMove(move);

            // history
            History2[HistoryDepth].Move = move;
            History2[HistoryDepth].Key = Key;
            History2[HistoryDepth].CastlingPermission = CastlingPermissions;
            History2[HistoryDepth].EnPassantFileIndex = EnPassantFileIndex;
            History2[HistoryDepth].EnPassantRankIndex = EnPassantRankIndex;
            History2[HistoryDepth].FiftyMoveRule = LastTookPieceHistoryIndex;
            HistoryDepth++;
            
            var whiteToMove = WhiteToMove;

            WhiteToMove = !whiteToMove;
            Key ^= ZobristKeys.ZWhiteToMove;

            if (EnPassantFile != 0)
            {
                Key ^= ZobristKeys.ZEnPassant[EnPassantFileIndex];

                EnPassantFile = 0;
                EnPassantFileIndex = -1;
                EnPassantRankIndex = -1;
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
                LastTookPieceHistoryIndex = HistoryDepth - 1;
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
            
            if (move.Piece == ChessPiece.WhiteKing)
            {
                if ((CastlingPermissions & CastlingPermission.WhiteQueen) != CastlingPermission.None)
                {
                    //CastlingPermissions &= ~CastlingPermission.WhiteQueen;
                    Key ^= ZobristKeys.ZCastle[(byte)CastlingPermission.WhiteQueen];
                }

                if ((CastlingPermissions & CastlingPermission.WhiteKing) != CastlingPermission.None)
                {
                    //CastlingPermissions &= ~CastlingPermission.WhiteKing;
                    Key ^= ZobristKeys.ZCastle[(byte)CastlingPermission.WhiteKing];
                }
            }
            else if (move.Piece == ChessPiece.WhiteRook)
            {
                if ((CastlingPermissions & CastlingPermission.WhiteQueen) != CastlingPermission.None && move.From == 0)
                {
                    //CastlingPermissions &= ~CastlingPermission.WhiteQueen;
                    Key ^= ZobristKeys.ZCastle[(byte)CastlingPermission.WhiteQueen];
                }

                if ((CastlingPermissions & CastlingPermission.WhiteKing) != CastlingPermission.None && move.From == 7)
                {
                    //CastlingPermissions &= ~CastlingPermission.WhiteKing;
                    Key ^= ZobristKeys.ZCastle[(byte)CastlingPermission.WhiteKing];
                }
            }
            else if (move.Piece == ChessPiece.BlackKing)
            {
                if ((CastlingPermissions & CastlingPermission.BlackQueen) != CastlingPermission.None)
                {
                    //CastlingPermissions &= ~CastlingPermission.BlackQueen;
                    Key ^= ZobristKeys.ZCastle[(byte)CastlingPermission.BlackQueen];
                }

                if ((CastlingPermissions & CastlingPermission.BlackKing) != CastlingPermission.None)
                {
                    //CastlingPermissions &= ~CastlingPermission.BlackKing;
                    Key ^= ZobristKeys.ZCastle[(byte)CastlingPermission.BlackKing];
                }
            }
            else if (move.Piece == ChessPiece.BlackRook)
            {
                if ((CastlingPermissions & CastlingPermission.BlackQueen) != CastlingPermission.None && move.From == 56)
                {
                    //CastlingPermissions &= ~CastlingPermission.BlackQueen;
                    Key ^= ZobristKeys.ZCastle[(byte)CastlingPermission.BlackQueen];
                }

                if ((CastlingPermissions & CastlingPermission.BlackKing) != CastlingPermission.None && move.From == 63)
                {
                    //CastlingPermissions &= ~CastlingPermission.BlackKing;
                    Key ^= ZobristKeys.ZCastle[(byte)CastlingPermission.BlackKing];
                }
            }

            switch (move.To)
            {
                case 0:
                    if ((CastlingPermissions & CastlingPermission.WhiteQueen) != CastlingPermission.None)
                    {
                        //CastlingPermissions &= ~CastlingPermission.WhiteQueen;
                        Key ^= ZobristKeys.ZCastle[(byte)CastlingPermission.WhiteQueen];
                    }
                    break;
                case 7:
                    if ((CastlingPermissions & CastlingPermission.WhiteKing) != CastlingPermission.None)
                    {
                        //CastlingPermissions &= ~CastlingPermission.WhiteKing;
                        Key ^= ZobristKeys.ZCastle[(byte)CastlingPermission.WhiteKing];
                    }
                    break;
                case 56:
                    if ((CastlingPermissions & CastlingPermission.BlackQueen) != CastlingPermission.None)
                    {
                        //CastlingPermissions &= ~CastlingPermission.BlackQueen;
                        Key ^= ZobristKeys.ZCastle[(byte)CastlingPermission.BlackQueen];
                    }
                    break;
                case 63:
                    if ((CastlingPermissions & CastlingPermission.BlackKing) != CastlingPermission.None)
                    {
                        //CastlingPermissions &= ~CastlingPermission.BlackKing;
                        Key ^= ZobristKeys.ZCastle[(byte)CastlingPermission.BlackKing];
                    }
                    break;
            }

            CastlingPermissions &= CastleRevocationTable[move.From];
            CastlingPermissions &= CastleRevocationTable[move.To];

            //SyncCastleTo1();
            SyncExtraBitBoards();

            //if (!newBoard.ExactlyEquals(this))
            //{
            //    var foo = clone.DoMove(move);
            //    Console.WriteLine($"Fuck {Key}");
            //    Debugger.Break();
            //}

            //if (!this.ExactlyEquals(newBoard))
            //{
            //    Console.WriteLine($"Fuck {Key}");
            //    Debugger.Break();
            //}

            //UndoMove();

            //if (!ExactlyEquals(clone))
            //{
            //    Console.WriteLine($"Fuck2 {Key}");
            //    Debugger.Break();
            //}

            //if (!clone.ExactlyEquals(this))
            //{
            //    Console.WriteLine($"Fuck2 {Key}");
            //    Debugger.Break();
            //}
        }

        public Board Clone()
        {
            var clone = new Board();
            clone.WhiteToMove = WhiteToMove;
            //clone.CastlingPermissions = (bool[])CastlingPermissions.Clone();
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

            clone.History = History.ToArray();
            clone.History2 = (UndoMove[])History2.Clone();
            clone.HistoryDepth = HistoryDepth;
            clone.CastlingPermissions = CastlingPermissions;


            return clone;
        }

        public bool ExactlyEquals(Board lhs, Board rhs)
        {
            var lhsKey = ZobristKeys.CalculateKey(lhs);
            var rhsKey = ZobristKeys.CalculateKey(rhs);
            if (lhsKey != rhsKey)
            {
                return false;
            }
            if (lhs.Key != lhsKey)
            {
                return false;
            }
            if (rhs.Key != rhsKey)
            {
                return false;
            }

            if (lhs.WhiteToMove != rhs.WhiteToMove)
            {
                return false;
            }

            for (int i = 0; i < lhs.BitBoard.Length; i++)
            {
                var bitBoard1 = lhs.BitBoard[i];
                var bitBoard2 = rhs.BitBoard[i];
                if (bitBoard1 != bitBoard2)
                {
                    return false;
                }
            }

            if (lhs.WhitePieces != rhs.WhitePieces)
            {
                return false;
            }

            if (lhs.BlackPieces != rhs.BlackPieces)
            {
                return false;
            }

            if (lhs.EmptySquares != rhs.EmptySquares)
            {
                return false;
            }

            if (lhs.AllPieces != rhs.AllPieces)
            {
                return false;
            }

            for (int i = 0; i < lhs.ArrayBoard.Length; i++)
            {
                var piece1 = lhs.ArrayBoard[i];
                var piece2 = rhs.ArrayBoard[i];
                if (piece1 != piece2)
                {
                    return false;
                }

            }

            if (!lhs.ArrayBoard.SequenceEqual(rhs.ArrayBoard))
            {
                return false;
            }

            if (lhs.EnPassantFileIndex != rhs.EnPassantFileIndex)
            {
                return false;
            }

            if (lhs.EnPassantRankIndex != rhs.EnPassantRankIndex)
            {
                return false;
            }

            if (lhs.EnPassantFile != rhs.EnPassantFile)
            {
                return false;
            }

            if (lhs.Key != rhs.Key)
            {
                return false;
            }

            if (lhs.LastTookPieceHistoryIndex != rhs.LastTookPieceHistoryIndex)
            {
                return false;
            }

            if (!lhs.PieceCounts.SequenceEqual(rhs.PieceCounts))
            {
                return false;
            }

            if (lhs.WhiteMaterial != rhs.WhiteMaterial)
            {
                return false;
            }

            if (lhs.BlackMaterial != rhs.BlackMaterial)
            {
                return false;
            }

            if (lhs.CastlingPermissions != rhs.CastlingPermissions)
            {
                return false;
            }

            if (lhs.HistoryDepth != rhs.HistoryDepth)
            {
                return false;
            }

            //for (var i = 0; i < History2.Length; i++)
            for (var i = 0; i < lhs.HistoryDepth; i++)
            {
                var history1 = lhs.History2[i];
                var history2 = rhs.History2[i];
                if (!UndoMoveExactlyEqual(history1, history2))
                {
                    return false;
                }
            }

            return true;
        }

        private bool UndoMoveExactlyEqual(UndoMove lhs, UndoMove rhs)
        {
            if (lhs.EnPassantFileIndex != rhs.EnPassantFileIndex)
            {
                return false;
            }
            if (lhs.EnPassantRankIndex != rhs.EnPassantRankIndex)
            {
                return false;
            }
            if (lhs.FiftyMoveRule != rhs.FiftyMoveRule)
            {
                return false;
            }
            if (lhs.Key != rhs.Key)
            {
                return false;
            }
            if (lhs.CastlingPermission != rhs.CastlingPermission)
            {
                return false;
            }

            if (!MoveExactlyEqual(lhs.Move, rhs.Move))
            {
                return false;
            }

            return true;
        }

        private bool MoveExactlyEqual(Move lhs, Move rhs)
        {
            if (lhs.From != rhs.From)
            {
                return false;
            }
            if (lhs.To != rhs.To)
            {
                return false;
            }
            if (lhs.Piece != rhs.Piece)
            {
                return false;
            }
            if (lhs.TakesPiece != rhs.TakesPiece)
            {
                return false;
            }
            if (lhs.PawnPromoteTo != rhs.PawnPromoteTo)
            {
                return false;
            }
            if (lhs.EnPassant != rhs.EnPassant)
            {
                return false;
            }
            if (lhs.Castle != rhs.Castle)
            {
                return false;
            }
            if (lhs.Key != rhs.Key)
            {
                return false;
            }
            if (lhs.Key2 != rhs.Key2)
            {
                return false;
            }

            return true;
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