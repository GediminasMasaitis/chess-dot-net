using System.Runtime.CompilerServices;
using ChessDotNet.Common;
using ChessDotNet.Data;

namespace ChessDotNet.MoveGeneration
{
    public class PinDetector
    {
        private readonly ISlideMoveGenerator _slideMoveGenerator;

        public PinDetector(ISlideMoveGenerator slideMoveGenerator)
        {
            _slideMoveGenerator = slideMoveGenerator;
        }

        public ulong GetPinned(Board board, byte color, byte pos)
        {
            var opponentColor = (byte)(color ^ 1);
            var pinned = 0UL;
            var ownPieces = color == ChessPiece.White ? board.WhitePieces : board.BlackPieces;

            var xrays = DiagonalAntidiagonalXray(board.AllPieces, ownPieces, pos);
            var pinners = xrays & (board.BitBoard[ChessPiece.Bishop | opponentColor] | board.BitBoard[ChessPiece.Queen | opponentColor]);

            while (pinners != 0)
            {
                int pinner = pinners.BitScanForward();
                pinned |= BitboardConstants.Between[pinner][pos] & ownPieces;
                pinners &= pinners - 1;
            }

            xrays = HorizontalVerticalXray(board.AllPieces, ownPieces, pos);
            pinners = xrays & (board.BitBoard[ChessPiece.Rook | opponentColor] | board.BitBoard[ChessPiece.Queen | opponentColor]);

            while (pinners != 0)
            {
                int pinner = pinners.BitScanForward();
                pinned |= BitboardConstants.Between[pinner][pos] & ownPieces;
                pinners &= pinners - 1;
            }
            return pinned;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong DiagonalAntidiagonalXray(ulong allPieces, ulong ownPieces, byte position)
        {
            var attacks = _slideMoveGenerator.DiagonalAntidiagonalSlide(allPieces, position);
            ownPieces &= attacks;
            var xrayAttacks = attacks ^ _slideMoveGenerator.DiagonalAntidiagonalSlide(allPieces ^ ownPieces, position);
            return xrayAttacks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong HorizontalVerticalXray(ulong allPieces, ulong ownPieces, byte position)
        {
            var attacks = _slideMoveGenerator.HorizontalVerticalSlide(allPieces, position);
            ownPieces &= attacks;
            var xrayAttacks = attacks ^ _slideMoveGenerator.HorizontalVerticalSlide(allPieces ^ ownPieces, position);
            return xrayAttacks;
        }
    }
}