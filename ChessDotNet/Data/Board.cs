using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ChessDotNet.Evaluation;
using ChessDotNet.Evaluation.V2;
using ChessDotNet.Fen;
using ChessDotNet.Hashing;
using ChessDotNet.Testing;
using Newtonsoft.Json;
using Bitboard = System.UInt64;
using ZobristKey = System.UInt64;
using Position = System.Byte;
using Piece = System.Byte;
using Score = System.Int32;
using TTFlag = System.Byte;

namespace ChessDotNet.Data
{
    public class Board
    {
        public byte ColorToMove { get; set; }

        public bool WhiteToMove
        {
            get => ColorToMove == ChessPiece.White;
            set => ColorToMove = value ? ChessPiece.White : ChessPiece.Black;
        }

        public CastlingPermission CastlingPermissions { get; set; }
        public UndoMove[] History2 { get; set; }
        public int HistoryDepth { get; set; }


        public Bitboard WhitePieces { get; set; }
        public Bitboard BlackPieces { get; set; }
        public Bitboard EmptySquares { get; set; }
        public Bitboard AllPieces { get; set; }

        public Bitboard[] BitBoard { get; set; }
        public Piece[] ArrayBoard { get; set; }
        public int EnPassantFileIndex { get; set; }
        public int EnPassantRankIndex { get; set; }
        public Bitboard EnPassantFile { get; set; }
        public ZobristKey Key { get; set; }
        //public ZobristKey Key2 { get; set; }
        public ZobristKey PawnKey { get; set; }
        public int LastTookPieceHistoryIndex { get; set; }

        public int[] PieceCounts { get; set; }
        public Score[] Material { get; set; }
        public Position[] KingPositions { get; set; }
        public Score[] PawnMaterial { get; set; }
        public Score[] PieceMaterial { get; set; }

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
            TestBoard(this, clone);
            var entry = clone.History2[HistoryDepth - 1];
            var move = entry.Move;
            clone.UndoMove(false);
            clone.DoMove2(move, false);
            TestBoard(this, clone);
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
            PawnKey = history.PawnKey;
            LastTookPieceHistoryIndex = history.FiftyMoveRule;

            var originalColorToMove = ColorToMove;
            WhiteToMove = !WhiteToMove;


            if (move.NullMove)
            {
                SyncExtraBitBoards();
                //Key2 = ZobristKeys2.CalculateKey(this);
                // TODO check
                return;
            }

            // FROM
            var fromPosBitBoard = 1UL << move.From;
            //Piece promotedPiece = move.PawnPromoteTo.HasValue ? move.PawnPromoteTo.Value : move.Piece;
            ArrayBoard[move.From] = move.Piece;
            BitBoard[move.Piece] |= fromPosBitBoard;

            // PROMOTIONS
            Piece promotedPiece;
            if (move.PawnPromoteTo != ChessPiece.Empty)
            {
                promotedPiece = move.PawnPromoteTo;
                PieceCounts[move.Piece]++;
                PieceCounts[promotedPiece]--;
                Material[ColorToMove] += EvaluationService.Weights[ChessPiece.Pawn];
                Material[ColorToMove] -= EvaluationService.Weights[promotedPiece];
                PawnMaterial[ColorToMove] += EvaluationData.PIECE_VALUE[ChessPiece.Pawn];
                PieceMaterial[ColorToMove] -= EvaluationData.PIECE_VALUE[promotedPiece];
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

            // KING POS
            if (move.Piece == ChessPiece.King + ColorToMove)
            {
                KingPositions[ColorToMove] = move.From;
            }

            // TAKES
            if (move.TakesPiece > 0)
            {
                if (!move.EnPassant)
                {
                    BitBoard[move.TakesPiece] |= toPosBitBoard;
                }
                PieceCounts[move.TakesPiece]++;
                Material[originalColorToMove] += EvaluationService.Weights[move.TakesPiece];
                var takesPawn = (move.TakesPiece & ~ChessPiece.Color) == ChessPiece.Pawn;
                if (takesPawn)
                {
                    PawnMaterial[originalColorToMove] += EvaluationData.PIECE_VALUE[move.TakesPiece];
                }
                else
                {
                    PieceMaterial[originalColorToMove] += EvaluationData.PIECE_VALUE[move.TakesPiece];
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
            //Key2 = ZobristKeys2.CalculateKey(this);
        }
        
        public void TestMove(Move move)
        {
            var clone = Clone();
            TestBoard(this, clone);
            clone.DoMove2(move, false);
            clone.UndoMove(false);
            TestBoard(this, clone);
        }

        public Board DoMove(Move move)
        {
            var clone = Clone();
            clone.DoMove2(move);
            return clone;
        }

        public void DoMove2(Move move, bool test = false)
        {
            Debug.Assert(move.ColorToMove == ColorToMove || move.NullMove);

            if (test)
            {
                TestMove(move);
            }

            //var clone = Clone();
            //var newBoard = DoMove(move);

            // HISTORY
            History2[HistoryDepth].Move = move;
            History2[HistoryDepth].Key = Key;
            History2[HistoryDepth].PawnKey = PawnKey;
            History2[HistoryDepth].CastlingPermission = CastlingPermissions;
            History2[HistoryDepth].EnPassantFileIndex = EnPassantFileIndex;
            History2[HistoryDepth].EnPassantRankIndex = EnPassantRankIndex;
            History2[HistoryDepth].FiftyMoveRule = LastTookPieceHistoryIndex;
            HistoryDepth++;
            
            var originalWhiteToMove = WhiteToMove;
            var originalColorToMove = ColorToMove;

            WhiteToMove = !originalWhiteToMove;
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
                //Key2 = ZobristKeys2.CalculateKey(this);
                // TODO check
                return;
            }


            // FROM
            var fromPosBitBoard = 1UL << move.From;
            ArrayBoard[move.From] = ChessPiece.Empty;
            BitBoard[move.Piece] &= ~fromPosBitBoard;
            Key ^= ZobristKeys.ZPieces[move.From][move.Piece];

            var isPawn = (move.Piece & ~ChessPiece.Color) == ChessPiece.Pawn;
            var takesPawn = (move.TakesPiece & ~ChessPiece.Color) == ChessPiece.Pawn;
            if (isPawn)
            {
                PawnKey ^= ZobristKeys.ZPieces[move.From][move.Piece];
            }


            // PROMOTIONS
            Piece promotedPiece;
            if (move.PawnPromoteTo != ChessPiece.Empty)
            {
                promotedPiece = move.PawnPromoteTo;

                PieceCounts[move.Piece]--;
                PieceCounts[promotedPiece]++;

                Material[originalColorToMove] -= EvaluationService.Weights[ChessPiece.Pawn];
                Material[originalColorToMove] += EvaluationService.Weights[promotedPiece];

                PawnMaterial[originalColorToMove] -= EvaluationData.PIECE_VALUE[ChessPiece.Pawn];
                PieceMaterial[originalColorToMove] += EvaluationData.PIECE_VALUE[promotedPiece];
            }
            else
            {
                promotedPiece = move.Piece;
            }
            
            // TO
            var toPosBitBoard = 1UL << move.To;
            ArrayBoard[move.To] = promotedPiece;
            BitBoard[promotedPiece] |= toPosBitBoard;
            Key ^= ZobristKeys.ZPieces[move.To][promotedPiece];
            if (isPawn && move.PawnPromoteTo == ChessPiece.Empty)
            {
                PawnKey ^= ZobristKeys.ZPieces[move.To][move.Piece];
            }

            // KING POS
            if (move.Piece == (ChessPiece.King | originalColorToMove))
            {
                KingPositions[originalColorToMove] = move.To;
            }
            
            // TAKES
            if (move.TakesPiece > 0)
            {
                if (!move.EnPassant)
                {
                    BitBoard[move.TakesPiece] &= ~toPosBitBoard;
                    Key ^= ZobristKeys.ZPieces[move.To][move.TakesPiece];
                    if(takesPawn)
                    {
                        PawnKey ^= ZobristKeys.ZPieces[move.To][move.TakesPiece];
                    }
                }
                LastTookPieceHistoryIndex = HistoryDepth - 1;
                PieceCounts[move.TakesPiece]--;
                Material[ColorToMove] -= EvaluationService.Weights[move.TakesPiece];
                if (takesPawn)
                {
                    PawnMaterial[ColorToMove] -= EvaluationData.PIECE_VALUE[move.TakesPiece];
                }
                else
                {
                    PieceMaterial[ColorToMove] -= EvaluationData.PIECE_VALUE[move.TakesPiece];
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
                Key ^= ZobristKeys.ZPieces[killedPawnPos][move.TakesPiece];
                PawnKey ^= ZobristKeys.ZPieces[killedPawnPos][move.TakesPiece];
            }

            // PAWN DOUBLE MOVES
            if ((move.Piece == ChessPiece.WhitePawn && move.From + 16 == move.To) || (move.Piece == ChessPiece.BlackPawn && move.From - 16 == move.To))
            {
                var fileIndex = move.From % 8;
                var rankIndex = (move.To >> 3) + (originalWhiteToMove ? -1 : 1);
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
                Key ^= ZobristKeys.ZPieces[castlingRookPos][rookPiece];
                Key ^= ZobristKeys.ZPieces[castlingRookNewPos][rookPiece];
            }

            var originalPermissions = CastlingPermissions;
            CastlingPermissions &= CastleRevocationTable[move.From];
            CastlingPermissions &= CastleRevocationTable[move.To];
            var revoked = CastlingPermissions ^ originalPermissions;
            Key ^= ZobristKeys.ZCastle[(byte)revoked];

            Debug.Assert(Key == ZobristKeys.CalculateKey(this));

            SyncExtraBitBoards();
            //Key2 = ZobristKeys2.CalculateKey(this);
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
            //clone.Key2 = Key2;
            clone.PawnKey = PawnKey;
            //clone.History = board.History; // TODO
            clone.LastTookPieceHistoryIndex = LastTookPieceHistoryIndex;
            clone.PieceCounts = (int[])PieceCounts.Clone();
            clone.Material = (Score[])Material.Clone();
            clone.PawnMaterial = (Score[])Material.Clone();
            clone.PieceMaterial = (Score[])Material.Clone();
            clone.KingPositions = (Position[])KingPositions.Clone();
            clone.History2 = (UndoMove[])History2.Clone();
            clone.HistoryDepth = HistoryDepth;
            clone.CastlingPermissions = CastlingPermissions;


            return clone;
        }

        public void TestBoard(Board lhs, Board rhs)
        {
            var lhsKey = ZobristKeys.CalculateKey(lhs);
            var rhsKey = ZobristKeys.CalculateKey(rhs);
            Debug.Assert(lhsKey == rhsKey);
            Debug.Assert(lhs.Key == lhsKey);
            Debug.Assert(rhs.Key == rhsKey);

            var lhsPawnKey = ZobristKeys.CalculatePawnKey(lhs);
            var rhsPawnKey = ZobristKeys.CalculatePawnKey(rhs);
            Debug.Assert(lhsPawnKey == rhsPawnKey);
            Debug.Assert(lhs.PawnKey == lhsPawnKey);
            Debug.Assert(rhs.PawnKey == rhsPawnKey);

            Debug.Assert(lhs.WhiteToMove == rhs.WhiteToMove);
            Debug.Assert(lhs.ColorToMove == rhs.ColorToMove);

            for (int i = 0; i < lhs.BitBoard.Length; i++)
            {
                var bitBoard1 = lhs.BitBoard[i];
                var bitBoard2 = rhs.BitBoard[i];
                Debug.Assert(bitBoard1 == bitBoard2);
            }

            Debug.Assert(lhs.WhitePieces == rhs.WhitePieces);
            Debug.Assert(lhs.BlackPieces == rhs.BlackPieces);
            Debug.Assert(lhs.EmptySquares == rhs.EmptySquares);
            Debug.Assert(lhs.AllPieces == rhs.AllPieces);
            Debug.Assert(lhs.KingPositions[0] == rhs.KingPositions[0]);
            Debug.Assert(lhs.KingPositions[1] == rhs.KingPositions[1]);


            for (int i = 0; i < lhs.ArrayBoard.Length; i++)
            {
                var piece1 = lhs.ArrayBoard[i];
                var piece2 = rhs.ArrayBoard[i];
                Debug.Assert(piece1 == piece2);
            }

            Debug.Assert(lhs.ArrayBoard.SequenceEqual(rhs.ArrayBoard));
            Debug.Assert(lhs.EnPassantFileIndex == rhs.EnPassantFileIndex);
            Debug.Assert(lhs.EnPassantRankIndex == rhs.EnPassantRankIndex);
            Debug.Assert(lhs.EnPassantFile == rhs.EnPassantFile);
            Debug.Assert(lhs.Key == rhs.Key);
            Debug.Assert(lhs.LastTookPieceHistoryIndex == rhs.LastTookPieceHistoryIndex);
            Debug.Assert(lhs.PieceCounts.SequenceEqual(rhs.PieceCounts));
            Debug.Assert(lhs.Material.SequenceEqual(rhs.Material));
            Debug.Assert(lhs.PawnMaterial.SequenceEqual(rhs.PawnMaterial));
            Debug.Assert(lhs.PieceMaterial.SequenceEqual(rhs.PieceMaterial));
            Debug.Assert(lhs.CastlingPermissions == rhs.CastlingPermissions);
            Debug.Assert(lhs.HistoryDepth == rhs.HistoryDepth);

            for (var i = 0; i < lhs.HistoryDepth; i++)
            {
                var history1 = lhs.History2[i];
                var history2 = rhs.History2[i];
                TestUndoMove(history1, history2);
            }
        }

        private bool TestUndoMove(UndoMove lhs, UndoMove rhs)
        {
            Debug.Assert(lhs.EnPassantFileIndex == rhs.EnPassantFileIndex);
            Debug.Assert(lhs.EnPassantRankIndex == rhs.EnPassantRankIndex);
            Debug.Assert(lhs.FiftyMoveRule == rhs.FiftyMoveRule);
            Debug.Assert(lhs.Key == rhs.Key);
            Debug.Assert(lhs.CastlingPermission == rhs.CastlingPermission);
            TestMove(lhs.Move, rhs.Move);

            return true;
        }

        private void TestMove(Move lhs, Move rhs)
        {
            Debug.Assert(lhs.From == rhs.From);
            Debug.Assert(lhs.To == rhs.To);
            Debug.Assert(lhs.Piece == rhs.Piece);
            Debug.Assert(lhs.TakesPiece == rhs.TakesPiece);
            Debug.Assert(lhs.PawnPromoteTo == rhs.PawnPromoteTo);
            Debug.Assert(lhs.EnPassant == rhs.EnPassant);
            Debug.Assert(lhs.Castle == rhs.Castle);
            Debug.Assert(lhs.NullMove == rhs.NullMove);
            Debug.Assert(lhs.ColorToMove == rhs.ColorToMove);
            Debug.Assert(lhs.Castle == rhs.Castle);
            Debug.Assert(lhs.Key == rhs.Key);
            Debug.Assert(lhs.Key2 == rhs.Key2);
        }

        public void SyncMaterial()
        {
            for (Piece piece = 0; piece < ChessPiece.Count; piece++)
            {
                var color = piece & ChessPiece.Color;
                Material[color] += PieceCounts[piece] * EvaluationService.Weights[piece];
                var isPawn = (piece & ~ChessPiece.Color) == ChessPiece.Pawn;
                if (isPawn)
                {
                    PawnMaterial[color] += PieceCounts[piece] * EvaluationData.PIECE_VALUE[piece];
                }
                else
                {
                    PieceMaterial[color] += PieceCounts[piece] * EvaluationData.PIECE_VALUE[piece];
                }
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
            infos.Add("Material white: " + Material[ChessPiece.White]);
            infos.Add("Material black: " + Material[ChessPiece.Black]);
            infos.Add("Material: " + (Material[ChessPiece.White] - Material[ChessPiece.Black]));
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