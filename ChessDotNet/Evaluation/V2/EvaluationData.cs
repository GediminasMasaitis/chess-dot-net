using System;
using ChessDotNet.Data;

namespace ChessDotNet.Evaluation.V2
{
    public class EvaluationData
    {

        public const int KING = 0;
        public const int QUEEN = 1;
        public const int ROOK = 2;
        public const int BISHOP = 3;
        public const int KNIGHT = 4;
        public const int PAWN = 5;
        public const int PIECE_EMPTY = 6;

        public const int SORT_KING = 400000000;

        public const int NORTH = 8;
        public const int NN = (NORTH + NORTH);
        public const int SOUTH = -8;
        public const int SS = (SOUTH + SOUTH);
        public const int EAST = 1;
        public const int WEST = -1;
        public const int NE = 9;
        public const int SW = -9;
        public const int NW = 7;
        public const int SE = -7;

        public static readonly int[] inv_sq = new int[64] {
            ChessPosition.A8, ChessPosition.B8, ChessPosition.C8, ChessPosition.D8, ChessPosition.E8, ChessPosition.F8, ChessPosition.G8, ChessPosition.H8,
            ChessPosition.A7, ChessPosition.B7, ChessPosition.C7, ChessPosition.D7, ChessPosition.E7, ChessPosition.F7, ChessPosition.G7, ChessPosition.H7,
            ChessPosition.A6, ChessPosition.B6, ChessPosition.C6, ChessPosition.D6, ChessPosition.E6, ChessPosition.F6, ChessPosition.G6, ChessPosition.H6,
            ChessPosition.A5, ChessPosition.B5, ChessPosition.C5, ChessPosition.D5, ChessPosition.E5, ChessPosition.F5, ChessPosition.G5, ChessPosition.H5,
            ChessPosition.A4, ChessPosition.B4, ChessPosition.C4, ChessPosition.D4, ChessPosition.E4, ChessPosition.F4, ChessPosition.G4, ChessPosition.H4,
            ChessPosition.A3, ChessPosition.B3, ChessPosition.C3, ChessPosition.D3, ChessPosition.E3, ChessPosition.F3, ChessPosition.G3, ChessPosition.H3,
            ChessPosition.A2, ChessPosition.B2, ChessPosition.C2, ChessPosition.D2, ChessPosition.E2, ChessPosition.F2, ChessPosition.G2, ChessPosition.H2,
            ChessPosition.A1, ChessPosition.B1, ChessPosition.C1, ChessPosition.D1, ChessPosition.E1, ChessPosition.F1, ChessPosition.G1, ChessPosition.H1,
        };

        public static readonly int[] SafetyTable = new int[100] {
            0,  0,   1,   2,   3,   5,   7,   9,  12,  15,
            18,  22,  26,  30,  35,  39,  44,  50,  56,  62,
            68,  75,  82,  85,  89,  97, 105, 113, 122, 131,
            140, 150, 169, 180, 191, 202, 213, 225, 237, 248,
            260, 272, 283, 295, 307, 319, 330, 342, 354, 366,
            377, 389, 401, 412, 424, 436, 448, 459, 471, 483,
            494, 500, 500, 500, 500, 500, 500, 500, 500, 500,
            500, 500, 500, 500, 500, 500, 500, 500, 500, 500,
            500, 500, 500, 500, 500, 500, 500, 500, 500, 500,
            500, 500, 500, 500, 500, 500, 500, 500, 500, 500
        };


        public static readonly int[] seventh = new int[2] { 6, 1 };
        public static readonly int[] eighth = new int[2] { 7, 0 };
        public static readonly int[] stepFwd = new int[2] { NORTH, SOUTH };
        public static readonly int[] stepBck = new int[2] { SOUTH, NORTH };

        public static (byte, byte) GetColorAndPiece(int piece)
        {
            switch (piece)
            {
                case ChessPiece.WhitePawn: return (ChessPiece.White, PAWN);
                case ChessPiece.WhiteKnight: return (ChessPiece.White, KNIGHT);
                case ChessPiece.WhiteBishop: return (ChessPiece.White, BISHOP);
                case ChessPiece.WhiteRook: return (ChessPiece.White, ROOK);
                case ChessPiece.WhiteQueen: return (ChessPiece.White, QUEEN);
                case ChessPiece.WhiteKing: return (ChessPiece.White, KING);
                case ChessPiece.BlackPawn: return (ChessPiece.Black, PAWN);
                case ChessPiece.BlackKnight: return (ChessPiece.Black, KNIGHT);
                case ChessPiece.BlackBishop: return (ChessPiece.Black, BISHOP);
                case ChessPiece.BlackRook: return (ChessPiece.Black, ROOK);
                case ChessPiece.BlackQueen: return (ChessPiece.Black, QUEEN);
                case ChessPiece.BlackKing: return (ChessPiece.Black, KING);
            }

            throw new Exception();
        }

        public readonly int[] PIECE_VALUE = new int[6];
        public readonly int[] SORT_VALUE = new int[6];

        /* Piece-square tables - we use size of the board representation,
        not 0..63, to avoid re-indexing. Initialization routine, however,
        uses 0..63 format for clarity */
        public readonly int[,,] mgPst = new int[6, 2, 64];
        public readonly int[,,] egPst = new int[6, 2, 64];

        /* piece-square tables for pawn structure */

        public readonly int[,] weak_pawn = new int[2, 64]; // isolated and backward pawns are scored in the same way
        public readonly int[,] passed_pawn = new int[2, 64];
        public readonly int[,] protected_passer = new int[2, 64];

        public readonly int[,,] sqNearK = new int[2, 64, 64];

        /* single values - letter p before a name signifies a penalty */

        public const int BISHOP_PAIR = 30;
        public const int P_KNIGHT_PAIR = 8;
        public const int P_ROOK_PAIR = 16;
        public const int ROOK_OPEN = 10;
        public const int ROOK_HALF = 5;
        public const int P_BISHOP_TRAPPED_A7 = 150;
        public const int P_BISHOP_TRAPPED_A6 = 50;
        public const int P_KNIGHT_TRAPPED_A8 = 150;
        public const int P_KNIGHT_TRAPPED_A7 = 100;
        public const int P_BLOCK_CENTRAL_PAWN = 24;
        public const int P_KING_BLOCKS_ROOK = 24;

        public const int SHIELD_2 = 10;
        public const int SHIELD_3 = 5;
        public const int P_NO_SHIELD = 10;

        public const int RETURNING_BISHOP = 20;
        public const int P_C3_KNIGHT = 5;
        public const int P_NO_FIANCHETTO = 4;
        public const int FIANCHETTO = 4;
        public const int TEMPO = 10;
        public const int ENDGAME_MAT = 1300;

        /******************************************************************************
        *                           PAWN PCSQ                                         *
        *                                                                             *
        *  Unlike TSCP, CPW generally doesn't want to advance its pawns. Its piece/   *
        *  square table for pawns takes into account the following factors:           *
        *                                                                             *
        *  - file-dependent component, encouraging program to capture                 *
        *    towards the center                                                       *
        *  - small bonus for staying on the 2nd rank                                  *
        *  - small bonus for standing on a3/h3                                        *
        *  - penalty for d/e pawns on their initial squares                           *
        *  - bonus for occupying the center                                           *
        ******************************************************************************/

        readonly int[] pawn_pcsq_mg = new int[64] {
            0,   0,   0,   0,   0,   0,   0,   0,
            -6,  -4,   1,   1,   1,   1,  -4,  -6,
            -6,  -4,   1,   2,   2,   1,  -4,  -6,
            -6,  -4,   2,   8,   8,   2,  -4,  -6,
            -6,  -4,   5,  10,  10,   5,  -4,  -6,
            -4,  -4,   1,   5,   5,   1,  -4,  -4,
            -6,  -4,   1, -24,  -24,  1,  -4,  -6,
            0,   0,   0,   0,   0,   0,   0,   0
        };

        readonly int[] pawn_pcsq_eg = new int[64] {
            0,   0,   0,   0,   0,   0,   0,   0,
            -6,  -4,   1,   1,   1,   1,  -4,  -6,
            -6,  -4,   1,   2,   2,   1,  -4,  -6,
            -6,  -4,   2,   8,   8,   2,  -4,  -6,
            -6,  -4,   5,  10,  10,   5,  -4,  -6,
            -4,  -4,   1,   5,   5,   1,  -4,  -4,
            -6,  -4,   1, -24,  -24,  1,  -4,  -6,
            0,   0,   0,   0,   0,   0,   0,   0
        };

        /******************************************************************************
        *    KNIGHT PCSQ                                                              *
        *                                                                             *
        *   - centralization bonus                                                    *
        *   - rim and back rank penalty, including penalty for not being developed    *
        ******************************************************************************/

        readonly int[] knight_pcsq_mg = new int[] {
            -8,  -8,  -8,  -8,  -8,  -8,  -8,  -8,
            -8,   0,   0,   0,   0,   0,   0,  -8,
            -8,   0,   4,   6,   6,   4,   0,  -8,
            -8,   0,   6,   8,   8,   6,   0,  -8,
            -8,   0,   6,   8,   8,   6,   0,  -8,
            -8,   0,   4,   6,   6,   4,   0,  -8,
            -8,   0,   1,   2,   2,   1,   0,  -8,
            -16, -12,  -8,  -8,  -8,  -8, -12,  -16
        };

        readonly int[] knight_pcsq_eg = new int[64] {
            -8,  -8,  -8,  -8,  -8,  -8,  -8,  -8,
            -8,   0,   0,   0,   0,   0,   0,  -8,
            -8,   0,   4,   6,   6,   4,   0,  -8,
            -8,   0,   6,   8,   8,   6,   0,  -8,
            -8,   0,   6,   8,   8,   6,   0,  -8,
            -8,   0,   4,   6,   6,   4,   0,  -8,
            -8,   0,   1,   2,   2,   1,   0,  -8,
            -16, -12,  -8,  -8,  -8,  -8, -12,  -16
        };

        /******************************************************************************
        *                BISHOP PCSQ                                                  *
        *                                                                             *
        *   - centralization bonus, smaller than for knight                           *
        *   - penalty for not being developed                                         *
        *   - good squares on the own half of the board                               *
        ******************************************************************************/

        readonly int[] bishop_pcsq_mg = new int[64] {
            -4,  -4,  -4,  -4,  -4,  -4,  -4,  -4,
            -4,   0,   0,   0,   0,   0,   0,  -4,
            -4,   0,   2,   4,   4,   2,   0,  -4,
            -4,   0,   4,   6,   6,   4,   0,  -4,
            -4,   0,   4,   6,   6,   4,   0,  -4,
            -4,   1,   2,   4,   4,   2,   1,  -4,
            -4,   2,   1,   1,   1,   1,   2,  -4,
            -4,  -4, -12,  -4,  -4, -12,  -4,  -4
        };

        readonly int[] bishop_pcsq_eg = new int[64] {
            -4,  -4,  -4,  -4,  -4,  -4,  -4,  -4,
            -4,   0,   0,   0,   0,   0,   0,  -4,
            -4,   0,   2,   4,   4,   2,   0,  -4,
            -4,   0,   4,   6,   6,   4,   0,  -4,
            -4,   0,   4,   6,   6,   4,   0,  -4,
            -4,   1,   2,   4,   4,   2,   1,  -4,
            -4,   2,   1,   1,   1,   1,   2,  -4,
            -4,  -4, -12,  -4,  -4, -12,  -4,  -4
        };

        /******************************************************************************
        *                        ROOK PCSQ                                            *
        *                                                                             *
        *    - bonus for 7th and 8th ranks                                            *
        *    - penalty for a/h columns                                                *
        *    - small centralization bonus                                             *
        ******************************************************************************/

        readonly int[] rook_pcsq_mg = new int[64] {
            5,   5,   5,   5,   5,   5,   5,   5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            0,   0,   0,   2,   2,   0,   0,   0
        };

        readonly int[] rook_pcsq_eg = new int[64] {
            5,   5,   5,   5,   5,   5,   5,   5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            0,   0,   0,   2,   2,   0,   0,   0
        };

        /******************************************************************************
        *                     QUEEN PCSQ                                              *
        *                                                                             *
        * - small bonus for centralization in the endgame                             *
        * - penalty for staying on the 1st rank, between rooks in the midgame         *
        ******************************************************************************/

        readonly int[] queen_pcsq_mg = new int[64] {
            0,   0,   0,   0,   0,   0,   0,   0,
            0,   0,   1,   1,   1,   1,   0,   0,
            0,   0,   1,   2,   2,   1,   0,   0,
            0,   0,   2,   3,   3,   2,   0,   0,
            0,   0,   2,   3,   3,   2,   0,   0,
            0,   0,   1,   2,   2,   1,   0,   0,
            0,   0,   1,   1,   1,   1,   0,   0,
            -5,  -5,  -5,  -5,  -5,  -5,  -5,  -5
        };

        readonly int[] queen_pcsq_eg = new int[64] {
            0,   0,   0,   0,   0,   0,   0,   0,
            0,   0,   1,   1,   1,   1,   0,   0,
            0,   0,   1,   2,   2,   1,   0,   0,
            0,   0,   2,   3,   3,   2,   0,   0,
            0,   0,   2,   3,   3,   2,   0,   0,
            0,   0,   1,   2,   2,   1,   0,   0,
            0,   0,   1,   1,   1,   1,   0,   0,
            -5,  -5,  -5,  -5,  -5,  -5,  -5,  -5
        };

        readonly int[] king_pcsq_mg = new int[64] {
            -40, -30, -50, -70, -70, -50, -30, -40,
            -30, -20, -40, -60, -60, -40, -20, -30,
            -20, -10, -30, -50, -50, -30, -10, -20,
            -10,   0, -20, -40, -40, -20,   0, -10,
            0,  10, -10, -30, -30, -10,  10,   0,
            10,  20,   0, -20, -20,   0,  20,  10,
            30,  40,  20,   0,   0,  20,  40,  30,
            40,  50,  30,  10,  10,  30,  50,  40
        };

        readonly int[] king_pcsq_eg = new int[64] {
            -72, -48, -36, -24, -24, -36, -48, -72,
            -48, -24, -12,   0,   0, -12, -24, -48,
            -36, -12,   0,  12,  12,   0, -12, -36,
            -24,   0,  12,  24,  24,  12,   0, -24,
            -24,   0,  12,  24,  24,  12,   0, -24,
            -36, -12,   0,  12,  12,   0, -12, -36,
            -48, -24, -12,   0,   0, -12, -24, -48,
            -72, -48, -36, -24, -24, -36, -48, -72
        };

        /******************************************************************************
        *                     WEAK PAWNS PCSQ                                         *
        *                                                                             *
        *  Current version of CPW-engine does not differentiate between isolated and  *
        *  backward pawns, using one  generic  cathegory of  weak pawns. The penalty  *
        *  is bigger in the center, on the assumption that weak central pawns can be  *
        *  attacked  from many  directions. If the penalty seems too low, please note *
        *  that being on a semi-open file will come into equation, too.               *
        ******************************************************************************/

        readonly int[] weak_pawn_pcsq = new int[64] {
            0,   0,   0,   0,   0,   0,   0,   0,
            -10, -12, -14, -16, -16, -14, -12, -10,
            -10, -12, -14, -16, -16, -14, -12, -10,
            -10, -12, -14, -16, -16, -14, -12, -10,
            -10, -12, -14, -16, -16, -14, -12, -10,
            -10, -12, -14, -16, -16, -14, -12, -10,
            -10, -12, -14, -16, -16, -14, -12, -10,
            0,   0,   0,   0,   0,   0,   0,   0
        };

        readonly int[] passed_pawn_pcsq = new int[64] {
            0,   0,   0,   0,   0,   0,   0,   0,
            140, 140, 140, 140, 140, 140, 140, 140,
            92,  92,  92,  92,  92,  92,  92,  92,
            56,  56,  56,  56,  56,  56,  56,  56,
            32,  32,  32,  32,  32,  32,  32,  32,
            20,  20,  20,  20,  20,  20,  20,  20,
            20,  20,  20,  20,  20,  20,  20,  20,
            0,   0,   0,   0,   0,   0,   0,   0
        };

        /* adjustements of piece value based on the number of own pawns */
        public readonly int[] n_adj = new int[9] { -20, -16, -12, -8, -4, 0, 4, 8, 12 };
        public readonly int[] r_adj = new int[9] { 15, 12, 9, 6, 3, 0, -3, -6, -9 };

        //public int[][] vector = new int[5][];

        public EvaluationData()
        {
            setBasicValues();
            setSquaresNearKing();
            setPcsq();
            correctValues();
        }

        void setBasicValues()
        {
            //vector[0] = new int[8] {SW, SOUTH, SE, WEST, EAST, NW, NORTH, NE};
            //vector[1] = new int[8] {SW, SOUTH, SE, WEST, EAST, NW, NORTH, NE};
            //vector[2] = new int[8] {SOUTH, WEST, EAST, NORTH, 0, 0, 0, 0};
            //vector[3] = new int[8] {SW, SE, NW, NE, 0, 0, 0, 0};
            //vector[4] = new int[8] { -17, -15, -18, -14, 14, 18, 31, 33 };

            /********************************************************************************
            *  We use material values by IM Larry Kaufman with additional + 10 for a Bishop *
            *  and only +30 for a Bishop pair 	                                            *
            ********************************************************************************/

            PIECE_VALUE[KING] = 0;
            PIECE_VALUE[QUEEN] = 975;
            PIECE_VALUE[ROOK] = 500;
            PIECE_VALUE[BISHOP] = 335;
            PIECE_VALUE[KNIGHT] = 325;
            PIECE_VALUE[PAWN] = 100;
            
            /*************************************************
            * Values used for sorting captures are the same  *
            * as normal piece values, except for a king.     *
            *************************************************/

            for (int i = 0; i < 6; ++i)
            {
                SORT_VALUE[i] = PIECE_VALUE[i];
            }
            SORT_VALUE[KING] = SORT_KING;
        }

        void setSquaresNearKing()
        {
            for (int i = 0; i < 64; ++i)
            {
                for (int j = 0; j < 64; ++j)
                {
                    sqNearK[ChessPiece.White, i, j] = 0;
                    sqNearK[ChessPiece.Black, i, j] = 0;

                    /* squares constituting the ring around both kings */
                    if (j == i + NORTH || j == i + SOUTH
                                          || j == i + EAST || j == i + WEST
                                          || j == i + NW || j == i + NE
                                          || j == i + SW || j == i + SE)
                    {

                        sqNearK[ChessPiece.White, i, j] = 1;
                        sqNearK[ChessPiece.Black, i, j] = 1;
                    }

                    /* squares in front of the white king ring */
                    if (j == i + NORTH + NORTH
                        || j == i + NORTH + NE
                        || j == i + NORTH + NW)
                        sqNearK[ChessPiece.White, i, j] = 1;

                    /* squares in front og the black king ring */
                    if (j == i + SOUTH + SOUTH
                        || j == i + SOUTH + SE
                        || j == i + SOUTH + SW)
                        sqNearK[ChessPiece.Black, i, j] = 1; // TODO: BLACK??
                }
            }
        }

        void setPcsq()
        {

            for (int i = 0; i < 64; ++i)
            {

                weak_pawn[ChessPiece.White, inv_sq[i]] = weak_pawn_pcsq[i];
                weak_pawn[ChessPiece.Black, i] = weak_pawn_pcsq[i];
                passed_pawn[ChessPiece.White, inv_sq[i]] = passed_pawn_pcsq[i];
                passed_pawn[ChessPiece.Black, i] = passed_pawn_pcsq[i];

                /* protected passers are slightly stronger than ordinary passers */

                protected_passer[ChessPiece.White, inv_sq[i]] = (passed_pawn_pcsq[i] * 10) / 8;
                protected_passer[ChessPiece.Black, i] = (passed_pawn_pcsq[i] * 10) / 8;

                /* now set the piece/square tables for each color and piece type */

                mgPst[PAWN, ChessPiece.White, inv_sq[i]] = pawn_pcsq_mg[i];
                mgPst[PAWN, ChessPiece.Black, i] = pawn_pcsq_mg[i];
                mgPst[KNIGHT, ChessPiece.White, inv_sq[i]] = knight_pcsq_mg[i];
                mgPst[KNIGHT, ChessPiece.Black, i] = knight_pcsq_mg[i];
                mgPst[BISHOP, ChessPiece.White, inv_sq[i]] = bishop_pcsq_mg[i];
                mgPst[BISHOP, ChessPiece.Black, i] = bishop_pcsq_mg[i];
                mgPst[ROOK, ChessPiece.White, inv_sq[i]] = rook_pcsq_mg[i];
                mgPst[ROOK, ChessPiece.Black, i] = rook_pcsq_mg[i];
                mgPst[QUEEN, ChessPiece.White, inv_sq[i]] = queen_pcsq_mg[i];
                mgPst[QUEEN, ChessPiece.Black, i] = queen_pcsq_mg[i];
                mgPst[KING, ChessPiece.White, inv_sq[i]] = king_pcsq_mg[i];
                mgPst[KING, ChessPiece.Black, i] = king_pcsq_mg[i];

                egPst[PAWN, ChessPiece.White, inv_sq[i]] = pawn_pcsq_eg[i];
                egPst[PAWN, ChessPiece.Black, i] = pawn_pcsq_eg[i];
                egPst[KNIGHT, ChessPiece.White, inv_sq[i]] = knight_pcsq_eg[i];
                egPst[KNIGHT, ChessPiece.Black, i] = knight_pcsq_eg[i];
                egPst[BISHOP, ChessPiece.White, inv_sq[i]] = bishop_pcsq_eg[i];
                egPst[BISHOP, ChessPiece.Black, i] = bishop_pcsq_eg[i];
                egPst[ROOK, ChessPiece.White, inv_sq[i]] = rook_pcsq_eg[i];
                egPst[ROOK, ChessPiece.Black, i] = rook_pcsq_eg[i];
                egPst[QUEEN, ChessPiece.White, inv_sq[i]] = queen_pcsq_eg[i];
                egPst[QUEEN, ChessPiece.Black, i] = queen_pcsq_eg[i];
                egPst[KING, ChessPiece.White, inv_sq[i]] = king_pcsq_eg[i];
                egPst[KING, ChessPiece.Black, i] = king_pcsq_eg[i];
            }
        }

        void correctValues()
        {
            if (PIECE_VALUE[BISHOP] == PIECE_VALUE[KNIGHT])
                ++PIECE_VALUE[BISHOP];
        }
    }
}