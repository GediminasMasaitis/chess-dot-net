using System.Runtime.InteropServices;
using ChessDotNet.Data;
using ChessDotNet.Fen;

namespace ChessDotNet.Evaluation.Nnue
{
    public class NnueExternalClient : INnueClient
    {
        public bool RequiresManagedData => false;

        static NnueExternalClient()
        {
            nnue_init("C:/Temp/nn-62ef826d1a6d.nnue");
        }

        public int Evaluate(NnuePosition pos)
        {
            var result = nnue_evaluate(pos.player, pos.pieces, pos.squares);
            return result;
        }

        public int EvaluateFen(Board board)
        {
            var fen = new FenSerializerService().SerializeToFen(board);
            var fenResult = nnue_evaluate_fen(fen);
            return fenResult;
        }

        [DllImport("NnueTest.dll")]
        private static extern void nnue_init(string fileName);

        [DllImport("NnueTest.dll")]
        private static extern int nnue_evaluate_fen(string fen);

        [DllImport("NnueTest.dll")]
        private static extern int nnue_evaluate(int player, int[] pieces, int[] squares);
    }
}