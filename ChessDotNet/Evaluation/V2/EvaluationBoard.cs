using ChessDotNet.Data;

namespace ChessDotNet.Evaluation.V2
{
    class EvaluationBoard
    {
        //U8 pieces[128];
        //char stm;        // side to move: 0 = white,  1 = black
        //char castle;     // 1 = shortW, 2 = longW, 4 = shortB, 8 = longB
        //char ep;         // en passant square
        //U8 ply;
        //U64 hash;
        //U64 phash;
        //int rep_index;
        //U64 rep_stack[1024];
        //S8 king_loc[2];
        public readonly int[] pcsq_mg = new int[2];
        public readonly int[] pcsq_eg = new int[2];
        public readonly int[] piece_material = new int[2];
        public readonly int[] pawn_material = new int[2];
        public readonly byte[,] pawns_on_file = new byte[2, 8];
        public readonly byte[,] pawns_on_rank = new byte[2, 8];
        public readonly byte[,] pawn_ctrl = new byte[2, 64];

        private readonly EvaluationData e;

        public EvaluationBoard(EvaluationData evalData)
        {
            e = evalData;
        }

        public void Fill(Board board)
        {
            for (int i = 0; i < 64; i++)
            {
                var piece = board.ArrayBoard[i];
                if (piece == ChessPiece.Empty)
                {
                    continue;
                }
                fillSq(piece, i);
            }
        }

        void fillSq(byte piece, int sq)
        {
            var color = (byte)(piece & ChessPiece.Color);
            var pieceNoColor = (byte)(piece & ~ChessPiece.Color);

            /**************************************************************************
            * Pawn structure changes slower than piece position, which allows reusing *
            * some data, both in pawn and piece evaluation. For that reason we do     *
            * some extra work here, expecting to gain extra speed elsewhere.          *
            **************************************************************************/

            if (pieceNoColor == ChessPiece.Pawn)
            {
                // update pawn material
                pawn_material[color] += e.PIECE_VALUE[piece];

                // update pawn hashkey
                //b.phash ^= zobrist.piecesquare[piece][color][sq];

                // update counter of pawns on a given rank and file
                var col = sq & 7;
                var row = sq >> 3;
                ++pawns_on_file[color, col];
                ++pawns_on_rank[color, row];

                // update squares controlled by pawns
                if (color == ChessPiece.White)
                {
                    if (col < 7) pawn_ctrl[ChessPiece.White, sq + EvaluationData.NE]++;
                    if (col > 0) pawn_ctrl[ChessPiece.White, sq + EvaluationData.NW]++;
                }
                else
                {
                    if (col < 7) pawn_ctrl[ChessPiece.Black, sq + EvaluationData.SE]++;
                    if (col > 0) pawn_ctrl[ChessPiece.Black, sq + EvaluationData.SW]++;
                }
            }
            else
            {
                // update piece material
                piece_material[color] += e.PIECE_VALUE[piece];
            }

            // update piece-square value
            pcsq_mg[color] += e.mgPst[piece][sq];
            pcsq_eg[color] += e.egPst[piece][sq];

            // update hash key
            //b.hash ^= zobrist.piecesquare[piece][color][sq];
        }
    }
}