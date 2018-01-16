namespace ChessDotNet.Common
{
    public interface ISlideMoveGenerator
    {
        ulong AllSlide(ulong allPieces, int position);
        ulong DiagonalAntidiagonalSlide(ulong allPieces, int position);
        ulong HorizontalVerticalSlide(ulong allPieces, int position);
    }
}