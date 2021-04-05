using System.Runtime.InteropServices;
using ChessDotNet.Data;
using ChessDotNet.Fen;

namespace ChessDotNet.Evaluation.Nnue
{
    public class NnueNnueCpuClient : INnueClient
    {
        public bool RequiresManagedData => false;

        private const string DllPath = "NnueCpu.dll";

        static NnueNnueCpuClient()
        {
            nncpu_init("C:/Temp/nn-62ef826d1a6d.nnue");
        }

        public int Evaluate(NnuePosition pos)
        {
            var result = nncpu_evaluate(pos.Player, pos.Pieces, pos.Squares);
            return result;
        }

        public int EvaluateFen(Board board)
        {
            var fen = new FenSerializerService().SerializeToFen(board);
            var fenResult = nncpu_evaluate_fen(fen);
            return fenResult;
        }

        [DllImport(DllPath)]
        private static extern void nncpu_init(string fileName);

        [DllImport(DllPath)]
        private static extern int nncpu_evaluate_fen(string fen);

        [DllImport(DllPath)]
        private static extern int nncpu_evaluate(int player, int[] pieces, int[] squares);
    }
}