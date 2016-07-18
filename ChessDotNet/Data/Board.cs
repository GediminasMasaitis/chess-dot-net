using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChessDotNet.Evaluation;
using ChessDotNet.Hashing;

namespace ChessDotNet.Data
{
    public class Board
    {
        public bool WhiteToMove { get; set; }

        public bool[] CastlingPermissions { get; set; }

        public ulong WhitePieces { get; private set; }
        public ulong BlackPieces { get; private set; }
        public ulong EmptySquares { get; private set; }
        public ulong AllPieces { get; private set; }

        public ulong[] BitBoard { get; set; }
        public int[] ArrayBoard { get; set; }
        public int EnPassantFileIndex { get; set; }
        public ulong EnPassantFile { get; set; }
        public ulong Key { get; set; }

        public HistoryEntry[] History { get; set; }
        public int LastTookPieceHistoryIndex { get; set; }

        public int[] PieceCounts { get; set; }
        public int WhiteMaterial { get; set; }
        public int BlackMaterial { get; set; }

        public static ulong AllBoard { get; }
        public static ulong KnightSpan { get; private set; }
        public static int KnightSpanPosition { get; private set; }
        public static ulong KingSpan { get; private set; }
        public static int KingSpanPosition { get; private set; }
        public static IReadOnlyList<ulong> Files { get; }
        public static IReadOnlyList<ulong> Ranks { get; }
        public static IReadOnlyList<ulong> Diagonals { get; private set; }
        public static IReadOnlyList<ulong> Antidiagonals { get; private set; }
        public static ulong KingSide { get; set; }
        public static ulong QueenSide { get; set; }

        public static ulong WhiteQueenSideCastleMask { get; }
        public static ulong WhiteKingSideCastleMask { get; }
        public static ulong BlackQueenSideCastleMask { get; }
        public static ulong BlackKingSideCastleMask { get; }

        public static ulong WhiteQueenSideCastleAttackMask { get; }
        public static ulong WhiteKingSideCastleAttackMask { get; }
        public static ulong BlackKingSideCastleAttackMask { get; }
        public static ulong BlackQueenSideCastleAttackMask { get; }

        public Board()
        {
            //History = new List<HistoryEntry>(128);
            //ArrayBoard = new int[64];
            //BitBoard = new ulong[13];
            EnPassantFileIndex = -1;
        }

        static Board()
        {
            KnightSpan = 43234889994UL;
            KnightSpanPosition = 18;

            KingSpan = 460039UL;
            KingSpanPosition = 9;

            var files = new List<ulong>(8);
            for (var i = 0; i < 8; i++)
            {
                var file = 0UL;
                for (var j = 0; j < 8; j++)
                {
                    file |= 1UL << i << (j*8);
                }
                files.Add(file);
            }
            Files = files;

            QueenSide = Files[0] | Files[1] | Files[2] | Files[3];
            KingSide = ~QueenSide;

            var ranks = new List<ulong>(8);
            for (var i = 0; i < 8; i++)
            {
                var rank = 0UL;
                for (var j = 0; j < 8; j++)
                {
                    rank |= 1UL << (i*8) << j;
                }
                ranks.Add(rank);
            }
            Ranks = ranks;

            Diagonals = new[]
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

            Antidiagonals = new[]
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

            var queenSideCastleMask = Files[1] | Files[2] | Files[3];
            var kingSideCastleMask = Files[5] | Files[6];
            WhiteQueenSideCastleMask = queenSideCastleMask & Ranks[0];
            WhiteKingSideCastleMask = kingSideCastleMask & Ranks[0];
            BlackQueenSideCastleMask = queenSideCastleMask & Ranks[7];
            BlackKingSideCastleMask = kingSideCastleMask & Ranks[7];

            var queenSideCastleAttackMask = Files[2] | Files[3] | Files[4];
            var kingSideCastleAttackMask = Files[4] | Files[5] | Files[6];
            WhiteQueenSideCastleAttackMask = queenSideCastleAttackMask & Ranks[0];
            WhiteKingSideCastleAttackMask = kingSideCastleAttackMask & Ranks[0];
            BlackQueenSideCastleAttackMask = queenSideCastleAttackMask & Ranks[7];
            BlackKingSideCastleAttackMask = kingSideCastleAttackMask & Ranks[7];
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

        public Board DoMove(Move move)
        {
#if TEST
            CheckBoard();
#endif
            //foreach (var pair in PiecesDict)
            var newBoard = new Board();
            newBoard.ArrayBoard = ArrayBoard.ToArray();
            newBoard.BitBoard = BitBoard.ToArray();
            newBoard.CastlingPermissions = CastlingPermissions.ToArray();
            newBoard.PieceCounts = PieceCounts.ToArray();
            newBoard.WhiteMaterial = WhiteMaterial;
            newBoard.BlackMaterial = BlackMaterial;
            newBoard.Key = Key;

            var newHistory = new HistoryEntry[History.Length + 1];
            Array.Copy(History, newHistory, History.Length);
            var newEntry = new HistoryEntry(this, move);
            newHistory[newHistory.Length - 1] = newEntry;
            newBoard.History = newHistory;

            var forWhite = WhiteToMove;
            newBoard.WhiteToMove = !WhiteToMove;
            newBoard.Key ^= ZobristKeys.ZWhiteToMove;

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

            int promotedPiece;
            if (move.PawnPromoteTo.HasValue)
            {
                promotedPiece = move.PawnPromoteTo.Value;
                newBoard.PieceCounts[move.Piece]--;
                newBoard.PieceCounts[promotedPiece]++;
                if (forWhite)
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
                if (forWhite)
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
                newBoard.Key ^= ZobristKeys.ZPieces[killedPawnPos, move.Piece];
            }

            if (EnPassantFile != 0)
            {
                newBoard.Key ^= ZobristKeys.ZEnPassant[EnPassantFileIndex];
            }
            if ((move.Piece == ChessPiece.WhitePawn && move.From + 16 == move.To) || (move.Piece == ChessPiece.BlackPawn && move.From - 16 == move.To))
            {
                var fileIndex = move.From % 8;
                newBoard.EnPassantFile = Files[fileIndex];
                newBoard.EnPassantFileIndex = fileIndex;
                newBoard.Key ^= ZobristKeys.ZEnPassant[fileIndex];
            }
            else
            {
                newBoard.EnPassantFile = 0;
                newBoard.EnPassantFileIndex = -1;
            }

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

            newBoard.SyncExtraBitBoards();

#if TEST
            newBoard.CheckBoard();
#endif

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

        public void CheckBoard()
        {
            if (PieceCounts[6] != 1)
            {
                throw new Exception();
            }
            if (PieceCounts[12] != 1)
            {
                throw new Exception();
            }

            if (WhitePieces != (BitBoard[ChessPiece.WhitePawn]
                | BitBoard[ChessPiece.WhiteKnight]
                | BitBoard[ChessPiece.WhiteBishop]
                | BitBoard[ChessPiece.WhiteRook]
                | BitBoard[ChessPiece.WhiteQueen]
                | BitBoard[ChessPiece.WhiteKing]))
            {
                throw new Exception();
            }

            if(BlackPieces != (BitBoard[ChessPiece.BlackPawn]
                | BitBoard[ChessPiece.BlackKnight]
                | BitBoard[ChessPiece.BlackBishop]
                | BitBoard[ChessPiece.BlackRook]
                | BitBoard[ChessPiece.BlackQueen]
                | BitBoard[ChessPiece.BlackKing]))
            {
                throw new Exception();
            }

            if(AllPieces != (WhitePieces | BlackPieces))
            {
                throw new Exception();
            }
            if(EmptySquares != ~AllPieces)
            {
                throw new Exception();
            }
        }

        public string Print(IEvaluationService evaluationService = null)
        {
            const string separators  = "   +---+---+---+---+---+---+---+---+";
            const string fileMarkers = "     A   B   C   D   E   F   G   H  ";
            const bool useUnicodeSymbols = false;

            var infos = new List<string>();

            infos.Add("Hash key: " + Key.ToString("X").PadLeft(16, '0'));
            infos.Add("To move: " + (WhiteToMove ? "White" : "Black"));
            infos.Add("Material: " + (WhiteMaterial - BlackMaterial));
            infos.Add("White material: " + WhiteMaterial);
            infos.Add("Black material: " + BlackMaterial);
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
                sb.AppendLine(separators);
                sb.Append(" " + (i+1) + " ");

                for (var j = 0; j < 8; j++)
                {
                    var piece = ArrayBoard[i*8 + j];
                    var pieceChar = useUnicodeSymbols ? ChessPiece.ChessPieceToSymbol(piece) : ChessPiece.ChessPieceToLetter(piece);
                    sb.Append($"| {pieceChar} ");
                }
                sb.Append("|   ");
                if (infos.Count > 7-i)
                {
                    sb.Append(infos[7-i]);
                }
                sb.AppendLine();
            }
            sb.AppendLine(separators);
            sb.AppendLine(fileMarkers);
            return sb.ToString();
        }

        
    }
}