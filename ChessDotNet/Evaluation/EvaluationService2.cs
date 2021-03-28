using System;
using System.Collections.Generic;
using System.Text;
using ChessDotNet.Data;
using ChessDotNet.MoveGeneration.SlideGeneration;
using ChessDotNet.Search2;
using Score = System.Int32;
using Position = System.Byte;
using Piece = System.Byte;

namespace ChessDotNet.Evaluation
{
    class eval_vector
    {
        public int gamePhase;   // function of piece material: 24 in opening, 0 in endgame
        public int[] mgMob = new int[2];     // midgame mobility
        public int[] egMob = new int[2];     // endgame mobility
        public int[] attCnt = new int[2];    // no. of pieces attacking zone around enemy king
        public int[] attWeight = new int[2]; // weight of attacking pieces - index to SafetyTable
        public int[] mgTropism = new int[2]; // midgame king tropism score
        public int[] egTropism = new int[2]; // endgame king tropism score
        public int[] kingShield = new int[2];
        public int[] adjustMaterial = new int[2];
        public int[] blockages = new int[2];
        public int[] positionalThemes = new int[2];
    }

    static class ec
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

        public static int[] inv_sq = new int[64] {
            ChessPosition.A8, ChessPosition.B8, ChessPosition.C8, ChessPosition.D8, ChessPosition.E8, ChessPosition.F8, ChessPosition.G8, ChessPosition.H8,
            ChessPosition.A7, ChessPosition.B7, ChessPosition.C7, ChessPosition.D7, ChessPosition.E7, ChessPosition.F7, ChessPosition.G7, ChessPosition.H7,
            ChessPosition.A6, ChessPosition.B6, ChessPosition.C6, ChessPosition.D6, ChessPosition.E6, ChessPosition.F6, ChessPosition.G6, ChessPosition.H6,
            ChessPosition.A5, ChessPosition.B5, ChessPosition.C5, ChessPosition.D5, ChessPosition.E5, ChessPosition.F5, ChessPosition.G5, ChessPosition.H5,
            ChessPosition.A4, ChessPosition.B4, ChessPosition.C4, ChessPosition.D4, ChessPosition.E4, ChessPosition.F4, ChessPosition.G4, ChessPosition.H4,
            ChessPosition.A3, ChessPosition.B3, ChessPosition.C3, ChessPosition.D3, ChessPosition.E3, ChessPosition.F3, ChessPosition.G3, ChessPosition.H3,
            ChessPosition.A2, ChessPosition.B2, ChessPosition.C2, ChessPosition.D2, ChessPosition.E2, ChessPosition.F2, ChessPosition.G2, ChessPosition.H2,
            ChessPosition.A1, ChessPosition.B1, ChessPosition.C1, ChessPosition.D1, ChessPosition.E1, ChessPosition.F1, ChessPosition.G1, ChessPosition.H1,
        };

        public static int[] SafetyTable = new int[100] {
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


        public static int[] seventh = new int[2] { 6, 1 };
        public static int[] eighth = new int[2] { 7, 0 };
        public static int[] stepFwd = new int[2] { NORTH, SOUTH };
        public static int[] stepBck = new int[2] { SOUTH, NORTH };

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
    }

    class s_eval_data
    {
        public int[] PIECE_VALUE = new int[6];
        public int[] SORT_VALUE = new int[6];

        /* Piece-square tables - we use size of the board representation,
        not 0..63, to avoid re-indexing. Initialization routine, however,
        uses 0..63 format for clarity */
        public int[,,] mgPst = new int[6, 2, 64];
        public int[,,] egPst = new int[6, 2, 64];

        /* piece-square tables for pawn structure */

        public int[,] weak_pawn = new int[2, 64]; // isolated and backward pawns are scored in the same way
        public int[,] passed_pawn = new int[2, 64];
        public int[,] protected_passer = new int[2, 64];

        public int[,,] sqNearK = new int[2, 64, 64];

        /* single values - letter p before a name signifies a penalty */

        public int BISHOP_PAIR;
        public int P_KNIGHT_PAIR;
        public int P_ROOK_PAIR;
        public int ROOK_OPEN;
        public int ROOK_HALF;
        public int P_BISHOP_TRAPPED_A7;
        public int P_BISHOP_TRAPPED_A6;
        public int P_KNIGHT_TRAPPED_A8;
        public int P_KNIGHT_TRAPPED_A7;
        public int P_BLOCK_CENTRAL_PAWN;
        public int P_KING_BLOCKS_ROOK;

        public int SHIELD_2;
        public int SHIELD_3;
        public int P_NO_SHIELD;

        public int RETURNING_BISHOP;
        public int P_C3_KNIGHT;
        public int P_NO_FIANCHETTO;
        public int FIANCHETTO;
        public int TEMPO;
        public int ENDGAME_MAT;

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

        int[] pawn_pcsq_mg = new int[64] {
             0,   0,   0,   0,   0,   0,   0,   0,
            -6,  -4,   1,   1,   1,   1,  -4,  -6,
            -6,  -4,   1,   2,   2,   1,  -4,  -6,
            -6,  -4,   2,   8,   8,   2,  -4,  -6,
            -6,  -4,   5,  10,  10,   5,  -4,  -6,
            -4,  -4,   1,   5,   5,   1,  -4,  -4,
            -6,  -4,   1, -24,  -24,  1,  -4,  -6,
             0,   0,   0,   0,   0,   0,   0,   0
        };

        int[] pawn_pcsq_eg = new int[64] {
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

        int[] knight_pcsq_mg = new int[] {
            -8,  -8,  -8,  -8,  -8,  -8,  -8,  -8,
            -8,   0,   0,   0,   0,   0,   0,  -8,
            -8,   0,   4,   6,   6,   4,   0,  -8,
            -8,   0,   6,   8,   8,   6,   0,  -8,
            -8,   0,   6,   8,   8,   6,   0,  -8,
            -8,   0,   4,   6,   6,   4,   0,  -8,
            -8,   0,   1,   2,   2,   1,   0,  -8,
           -16, -12,  -8,  -8,  -8,  -8, -12,  -16
        };

        int[] knight_pcsq_eg = new int[64] {
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

        int[] bishop_pcsq_mg = new int[64] {
            -4,  -4,  -4,  -4,  -4,  -4,  -4,  -4,
            -4,   0,   0,   0,   0,   0,   0,  -4,
            -4,   0,   2,   4,   4,   2,   0,  -4,
            -4,   0,   4,   6,   6,   4,   0,  -4,
            -4,   0,   4,   6,   6,   4,   0,  -4,
            -4,   1,   2,   4,   4,   2,   1,  -4,
            -4,   2,   1,   1,   1,   1,   2,  -4,
            -4,  -4, -12,  -4,  -4, -12,  -4,  -4
        };

        int[] bishop_pcsq_eg = new int[64] {
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

        int[] rook_pcsq_mg = new int[64] {
             5,   5,   5,   5,   5,   5,   5,   5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
            -5,   0,   0,   0,   0,   0,   0,  -5,
             0,   0,   0,   2,   2,   0,   0,   0
        };

        int[] rook_pcsq_eg = new int[64] {
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

        int[] queen_pcsq_mg = new int[64] {
             0,   0,   0,   0,   0,   0,   0,   0,
             0,   0,   1,   1,   1,   1,   0,   0,
             0,   0,   1,   2,   2,   1,   0,   0,
             0,   0,   2,   3,   3,   2,   0,   0,
             0,   0,   2,   3,   3,   2,   0,   0,
             0,   0,   1,   2,   2,   1,   0,   0,
             0,   0,   1,   1,   1,   1,   0,   0,
            -5,  -5,  -5,  -5,  -5,  -5,  -5,  -5
        };

        int[] queen_pcsq_eg = new int[64] {
             0,   0,   0,   0,   0,   0,   0,   0,
             0,   0,   1,   1,   1,   1,   0,   0,
             0,   0,   1,   2,   2,   1,   0,   0,
             0,   0,   2,   3,   3,   2,   0,   0,
             0,   0,   2,   3,   3,   2,   0,   0,
             0,   0,   1,   2,   2,   1,   0,   0,
             0,   0,   1,   1,   1,   1,   0,   0,
            -5,  -5,  -5,  -5,  -5,  -5,  -5,  -5
        };

        int[] king_pcsq_mg = new int[64] {
           -40, -30, -50, -70, -70, -50, -30, -40,
           -30, -20, -40, -60, -60, -40, -20, -30,
           -20, -10, -30, -50, -50, -30, -10, -20,
           -10,   0, -20, -40, -40, -20,   0, -10,
             0,  10, -10, -30, -30, -10,  10,   0,
            10,  20,   0, -20, -20,   0,  20,  10,
            30,  40,  20,   0,   0,  20,  40,  30,
            40,  50,  30,  10,  10,  30,  50,  40
        };

        int[] king_pcsq_eg = new int[64] {
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

        int[] weak_pawn_pcsq = new int[64] {
             0,   0,   0,   0,   0,   0,   0,   0,
           -10, -12, -14, -16, -16, -14, -12, -10,
           -10, -12, -14, -16, -16, -14, -12, -10,
           -10, -12, -14, -16, -16, -14, -12, -10,
           -10, -12, -14, -16, -16, -14, -12, -10,
           -10, -12, -14, -16, -16, -14, -12, -10,
           -10, -12, -14, -16, -16, -14, -12, -10,
             0,   0,   0,   0,   0,   0,   0,   0
        };

        int[] passed_pawn_pcsq = new int[64] {
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
        public int[] n_adj = new int[9] { -20, -16, -12, -8, -4, 0, 4, 8, 12 };
        public int[] r_adj = new int[9] { 15, 12, 9, 6, 3, 0, -3, -6, -9 };

        public int[][] vector = new int[5][];

        public s_eval_data()
        {
            setBasicValues();
            setSquaresNearKing();
            setPcsq();
            correctValues();
        }

        void setBasicValues()
        {
            vector[0] = new int[8] {ec.SW, ec.SOUTH, ec.SE, ec.WEST, ec.EAST, ec.NW, ec.NORTH, ec.NE};
            vector[1] = new int[8] {ec.SW, ec.SOUTH, ec.SE, ec.WEST, ec.EAST, ec.NW, ec.NORTH, ec.NE};
            vector[2] = new int[8] {ec.SOUTH, ec.WEST, ec.EAST, ec.NORTH, 0, 0, 0, 0};
            vector[3] = new int[8] {ec.SW, ec.SE, ec.NW, ec.NE, 0, 0, 0, 0};
            vector[4] = new int[8] { -17, -15, -18, -14, 14, 18, 31, 33 };

            /********************************************************************************
            *  We use material values by IM Larry Kaufman with additional + 10 for a Bishop *
            *  and only +30 for a Bishop pair 	                                            *
            ********************************************************************************/

            PIECE_VALUE[ec.KING] = 0;
            PIECE_VALUE[ec.QUEEN] = 975;
            PIECE_VALUE[ec.ROOK] = 500;
            PIECE_VALUE[ec.BISHOP] = 335;
            PIECE_VALUE[ec.KNIGHT] = 325;
            PIECE_VALUE[ec.PAWN] = 100;

            BISHOP_PAIR = 30;
            P_KNIGHT_PAIR = 8;
            P_ROOK_PAIR = 16;

            /*************************************************
            * Values used for sorting captures are the same  *
            * as normal piece values, except for a king.     *
            *************************************************/

            for (int i = 0; i < 6; ++i)
            {
                SORT_VALUE[i] = PIECE_VALUE[i];
            }
            SORT_VALUE[ec.KING] = ec.SORT_KING;

            /* trapped and blocked pieces */
            P_KING_BLOCKS_ROOK = 24;
            P_BLOCK_CENTRAL_PAWN = 24;
            P_BISHOP_TRAPPED_A7 = 150;
            P_BISHOP_TRAPPED_A6 = 50;
            P_KNIGHT_TRAPPED_A8 = 150;
            P_KNIGHT_TRAPPED_A7 = 100;

            /* minor penalties */
            P_C3_KNIGHT = 5;
            P_NO_FIANCHETTO = 4;

            /* king's defence */
            SHIELD_2 = 10;
            SHIELD_3 = 5;
            P_NO_SHIELD = 10;

            /* minor bonuses */
            ROOK_OPEN = 10;
            ROOK_HALF = 5;
            RETURNING_BISHOP = 20;
            FIANCHETTO = 4;
            TEMPO = 10;

            ENDGAME_MAT = 1300;
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
                    if (j == i + ec.NORTH || j == i + ec.SOUTH
                                          || j == i + ec.EAST || j == i + ec.WEST
                                          || j == i + ec.NW || j == i + ec.NE
                                          || j == i + ec.SW || j == i + ec.SE)
                    {

                        sqNearK[ChessPiece.White, i, j] = 1;
                        sqNearK[ChessPiece.Black, i, j] = 1;
                    }

                    /* squares in front of the white king ring */
                    if (j == i + ec.NORTH + ec.NORTH
                        || j == i + ec.NORTH + ec.NE
                        || j == i + ec.NORTH + ec.NW)
                        sqNearK[ChessPiece.White, i, j] = 1;

                    /* squares in front og the black king ring */
                    if (j == i + ec.SOUTH + ec.SOUTH
                        || j == i + ec.SOUTH + ec.SE
                        || j == i + ec.SOUTH + ec.SW)
                        sqNearK[ChessPiece.White, i, j] = 1; // TODO: BLACK??
                }
            }
        }

        void setPcsq()
        {

            for (int i = 0; i < 64; ++i)
            {

                weak_pawn[ChessPiece.White, ec.inv_sq[i]] = weak_pawn_pcsq[i];
                weak_pawn[ChessPiece.Black, i] = weak_pawn_pcsq[i];
                passed_pawn[ChessPiece.White, ec.inv_sq[i]] = passed_pawn_pcsq[i];
                passed_pawn[ChessPiece.Black, i] = passed_pawn_pcsq[i];

                /* protected passers are slightly stronger than ordinary passers */

                protected_passer[ChessPiece.White, ec.inv_sq[i]] = (passed_pawn_pcsq[i] * 10) / 8;
                protected_passer[ChessPiece.Black, i] = (passed_pawn_pcsq[i] * 10) / 8;

                /* now set the piece/square tables for each color and piece type */

                mgPst[ec.PAWN, ChessPiece.White, ec.inv_sq[i]] = pawn_pcsq_mg[i];
                mgPst[ec.PAWN, ChessPiece.Black, i] = pawn_pcsq_mg[i];
                mgPst[ec.KNIGHT, ChessPiece.White, ec.inv_sq[i]] = knight_pcsq_mg[i];
                mgPst[ec.KNIGHT, ChessPiece.Black, i] = knight_pcsq_mg[i];
                mgPst[ec.BISHOP, ChessPiece.White, ec.inv_sq[i]] = bishop_pcsq_mg[i];
                mgPst[ec.BISHOP, ChessPiece.Black, i] = bishop_pcsq_mg[i];
                mgPst[ec.ROOK, ChessPiece.White, ec.inv_sq[i]] = rook_pcsq_mg[i];
                mgPst[ec.ROOK, ChessPiece.Black, i] = rook_pcsq_mg[i];
                mgPst[ec.QUEEN, ChessPiece.White, ec.inv_sq[i]] = queen_pcsq_mg[i];
                mgPst[ec.QUEEN, ChessPiece.Black, i] = queen_pcsq_mg[i];
                mgPst[ec.KING, ChessPiece.White, ec.inv_sq[i]] = king_pcsq_mg[i];
                mgPst[ec.KING, ChessPiece.Black, i] = king_pcsq_mg[i];

                egPst[ec.PAWN, ChessPiece.White, ec.inv_sq[i]] = pawn_pcsq_eg[i];
                egPst[ec.PAWN, ChessPiece.Black, i] = pawn_pcsq_eg[i];
                egPst[ec.KNIGHT, ChessPiece.White, ec.inv_sq[i]] = knight_pcsq_eg[i];
                egPst[ec.KNIGHT, ChessPiece.Black, i] = knight_pcsq_eg[i];
                egPst[ec.BISHOP, ChessPiece.White, ec.inv_sq[i]] = bishop_pcsq_eg[i];
                egPst[ec.BISHOP, ChessPiece.Black, i] = bishop_pcsq_eg[i];
                egPst[ec.ROOK, ChessPiece.White, ec.inv_sq[i]] = rook_pcsq_eg[i];
                egPst[ec.ROOK, ChessPiece.Black, i] = rook_pcsq_eg[i];
                egPst[ec.QUEEN, ChessPiece.White, ec.inv_sq[i]] = queen_pcsq_eg[i];
                egPst[ec.QUEEN, ChessPiece.Black, i] = queen_pcsq_eg[i];
                egPst[ec.KING, ChessPiece.White, ec.inv_sq[i]] = king_pcsq_eg[i];
                egPst[ec.KING, ChessPiece.Black, i] = king_pcsq_eg[i];
            }
        }

        void correctValues()
        {
            if (PIECE_VALUE[ec.BISHOP] == PIECE_VALUE[ec.KNIGHT])
                ++PIECE_VALUE[ec.BISHOP];
        }
    }

    class EvaluationBoard
    {
        //U8 pieces[128];
        byte[] color = new byte[64];
        //char stm;        // side to move: 0 = white,  1 = black
        //char castle;     // 1 = shortW, 2 = longW, 4 = shortB, 8 = longB
        //char ep;         // en passant square
        //U8 ply;
        //U64 hash;
        //U64 phash;
        //int rep_index;
        //U64 rep_stack[1024];
        //S8 king_loc[2];
        public int[] pcsq_mg = new int[2];
        public int[] pcsq_eg = new int[2];
        public int[] piece_material = new int[2];
        public int[] pawn_material = new int[2];
        public byte[,] piece_cnt = new byte[2, 6];
        public byte[,] pawns_on_file = new byte[2, 8];
        public byte[,] pawns_on_rank = new byte[2, 8];
        public byte[,] pawn_ctrl = new byte[2, 64];

        private readonly s_eval_data e;

        public EvaluationBoard(s_eval_data evalData)
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
                (var color, var convertedPiece) = ec.GetColorAndPiece(piece);
                fillSq(color, convertedPiece, i);
            }
        }

        void fillSq(byte color, byte piece, int sq)
        {

            // place a piece on the board
            //b.pieces[sq] = piece;
            this.color[sq] = color;

            // update king location
            //if (piece == KING)
            //    b.king_loc[color] = sq;

            /**************************************************************************
            * Pawn structure changes slower than piece position, which allows reusing *
            * some data, both in pawn and piece evaluation. For that reason we do     *
            * some extra work here, expecting to gain extra speed elsewhere.          *
            **************************************************************************/

            if (piece == ec.PAWN)
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
                    if (col < 7) pawn_ctrl[ChessPiece.White, sq + ec.NE]++;
                    if (col > 0) pawn_ctrl[ChessPiece.White, sq + ec.NW]++;
                }
                else
                {
                    if (col < 7) pawn_ctrl[ChessPiece.Black, sq + ec.SE]++;
                    if (col > 0) pawn_ctrl[ChessPiece.Black, sq + ec.SW]++;
                }
            }
            else
            {
                // update piece material
                piece_material[color] += e.PIECE_VALUE[piece];
            }

            // update piece counter
            piece_cnt[color, piece]++;

            // update piece-square value
            pcsq_mg[color] += e.mgPst[piece, color, sq];
            pcsq_eg[color] += e.egPst[piece, color, sq];

            // update hash key
            //b.hash ^= zobrist.piecesquare[piece][color][sq];
        }
    }

    public class EvaluationService2 : IEvaluationService
    {
        public int Evaluate(Board board)
        {
            var v = new eval_vector();
            var e = new s_eval_data();
            var eb = new EvaluationBoard(e);
            eb.Fill(board);
            var score = eval(board, v, e, eb, true);
            printEval(board, e, eb, v, score);
            return score;
        }

        private int eval(Board b, eval_vector v, s_eval_data e, EvaluationBoard eb, bool use_hash)
        {
            int result = 0, mgScore = 0, egScore = 0;
            int stronger, weaker;

            /**************************************************************************
            *  Clear all eval data                                                    *
            **************************************************************************/

            v.gamePhase = b.PieceCounts[ChessPiece.WhiteKnight] + b.PieceCounts[ChessPiece.WhiteBishop] + 2 * b.PieceCounts[ChessPiece.WhiteRook] + 4 * b.PieceCounts[ChessPiece.WhiteQueen]
                        + b.PieceCounts[ChessPiece.BlackKnight] + b.PieceCounts[ChessPiece.BlackBishop] + 2 * b.PieceCounts[ChessPiece.BlackRook] + 4 * b.PieceCounts[ChessPiece.BlackQueen];

            for (int side = 0; side <= 1; side++)
            {
                v.mgMob[side] = 0;
                v.egMob[side] = 0;
                v.attCnt[side] = 0;
                v.attWeight[side] = 0;
                v.mgTropism[side] = 0;
                v.egTropism[side] = 0;
                v.adjustMaterial[side] = 0;
                v.blockages[side] = 0;
                v.positionalThemes[side] = 0;
                v.kingShield[side] = 0;
            }

            /************************************************************************** 
            *  Sum the incrementally counted material and piece/square table values   *
            **************************************************************************/

            mgScore = eb.piece_material[ChessPiece.White] + eb.pawn_material[ChessPiece.White] + eb.pcsq_mg[ChessPiece.White]
                    - eb.piece_material[ChessPiece.Black] - eb.pawn_material[ChessPiece.Black] - eb.pcsq_mg[ChessPiece.Black];
            egScore = eb.piece_material[ChessPiece.White] + eb.pawn_material[ChessPiece.White] + eb.pcsq_eg[ChessPiece.White]
                    - eb.piece_material[ChessPiece.Black] - eb.pawn_material[ChessPiece.Black] - eb.pcsq_eg[ChessPiece.Black];

            /************************************************************************** 
            * add king's pawn shield score and evaluate part of piece blockage score  *
            * (the rest of the latter will be done via piece eval)                    *
            **************************************************************************/

            v.kingShield[ChessPiece.White] = wKingShield(b, e);
            v.kingShield[ChessPiece.Black] = bKingShield(b, e);
            blockedPieces(b, e, v, eb, ChessPiece.White);
            blockedPieces(b, e, v, eb, ChessPiece.Black);
            mgScore += (v.kingShield[ChessPiece.White] - v.kingShield[ChessPiece.Black]);

            /* tempo bonus */
            if (b.WhiteToMove) result += e.TEMPO;
            else result -= e.TEMPO;

            /**************************************************************************
            *  Adjusting material value for the various combinations of pieces.       *
            *  Currently it scores bishop, knight and rook pairs. The first one       *
            *  gets a bonus, the latter two - a penalty. Beside that knights lose     *
            *  value as pawns disappear, whereas rooks gain.                          *
            **************************************************************************/

            if (b.PieceCounts[ChessPiece.WhiteBishop] > 1) v.adjustMaterial[ChessPiece.White] += e.BISHOP_PAIR;
            if (b.PieceCounts[ChessPiece.BlackBishop] > 1) v.adjustMaterial[ChessPiece.Black] += e.BISHOP_PAIR;
            if (b.PieceCounts[ChessPiece.WhiteKnight] > 1) v.adjustMaterial[ChessPiece.White] -= e.P_KNIGHT_PAIR;
            if (b.PieceCounts[ChessPiece.BlackKnight] > 1) v.adjustMaterial[ChessPiece.Black] -= e.P_KNIGHT_PAIR;
            if (b.PieceCounts[ChessPiece.WhiteRook] > 1) v.adjustMaterial[ChessPiece.White] -= e.P_ROOK_PAIR;
            if (b.PieceCounts[ChessPiece.BlackRook] > 1) v.adjustMaterial[ChessPiece.Black] -= e.P_ROOK_PAIR;

            v.adjustMaterial[ChessPiece.White] += e.n_adj[b.PieceCounts[ChessPiece.WhitePawn]] * b.PieceCounts[ChessPiece.WhiteKnight];
            v.adjustMaterial[ChessPiece.Black] += e.n_adj[b.PieceCounts[ChessPiece.BlackPawn]] * b.PieceCounts[ChessPiece.BlackKnight];
            v.adjustMaterial[ChessPiece.White] += e.r_adj[b.PieceCounts[ChessPiece.WhitePawn]] * b.PieceCounts[ChessPiece.WhiteRook];
            v.adjustMaterial[ChessPiece.Black] += e.r_adj[b.PieceCounts[ChessPiece.BlackPawn]] * b.PieceCounts[ChessPiece.BlackRook];

            var pawnScore = getPawnScore(b, eb, e);
            result += pawnScore;

            /**************************************************************************
            *  Evaluate pieces                                                        *
            **************************************************************************/

            for (Piece sq = 0; sq < 64; sq++)
            {
                var originalPiece = b.ArrayBoard[sq];
                if (originalPiece != ChessPiece.Empty)
                {
                    (var color, var piece) = ec.GetColorAndPiece(originalPiece);
                    switch (piece)
                    {
                        case ec.PAWN: // pawns are evaluated separately
                            break;
                        case ec.KNIGHT:
                            EvalKnight(b, e, eb, v, sq, color);
                            break;
                        case ec.BISHOP:
                            EvalBishop(b, e, eb, v, sq, color);
                            break;
                        case ec.ROOK:
                            EvalRook(b, e, eb, v, sq, color);
                            break;
                        case ec.QUEEN:
                            EvalQueen(b, e, eb, v, sq, color);
                            break;
                        case ec.KING:
                            break;
                    }
                }
            }

            /**************************************************************************
            *  Merge  midgame  and endgame score. We interpolate between  these  two  *
            *  values, using a gamePhase value, based on remaining piece material on  *
            *  both sides. With less pieces, endgame score becomes more influential.  *
            **************************************************************************/

            mgScore += (v.mgMob[ChessPiece.White] - v.mgMob[ChessPiece.Black]);
            egScore += (v.egMob[ChessPiece.White] - v.egMob[ChessPiece.Black]);
            mgScore += (v.mgTropism[ChessPiece.White] - v.mgTropism[ChessPiece.Black]);
            egScore += (v.egTropism[ChessPiece.White] - v.egTropism[ChessPiece.Black]);
            if (v.gamePhase > 24) v.gamePhase = 24;
            int mgWeight = v.gamePhase;
            int egWeight = 24 - mgWeight;
            result += ((mgScore * mgWeight) + (egScore * egWeight)) / 24;

            /**************************************************************************
            *  Add phase-independent score components.                                *
            **************************************************************************/

            result += (v.blockages[ChessPiece.White] - v.blockages[ChessPiece.Black]);
            result += (v.positionalThemes[ChessPiece.White] - v.positionalThemes[ChessPiece.Black]);
            result += (v.adjustMaterial[ChessPiece.White] - v.adjustMaterial[ChessPiece.Black]);

            /**************************************************************************
            *  Merge king attack score. We don't apply this value if there are less   *
            *  than two attackers or if the attacker has no queen.                    *
            **************************************************************************/

            if (v.attCnt[ChessPiece.White] < 2 || b.PieceCounts[ChessPiece.WhiteQueen] == 0) v.attWeight[ChessPiece.White] = 0;
            if (v.attCnt[ChessPiece.Black] < 2 || b.PieceCounts[ChessPiece.BlackQueen] == 0) v.attWeight[ChessPiece.Black] = 0;
            result += ec.SafetyTable[v.attWeight[ChessPiece.White]];
            result -= ec.SafetyTable[v.attWeight[ChessPiece.Black]];

            /**************************************************************************
            *  Low material correction - guarding against an illusory material advan- *
            *  tage. Full blown program should have more such rules, but the current  *
            *  set ought to be useful enough. Please note that our code  assumes      *
            *  different material values for bishop and  knight.                      *
            *                                                                         *
            *  - a single minor piece cannot win                                      *
            *  - two knights cannot checkmate bare king                               *
            *  - bare rook vs minor piece is drawish                                  *
            *  - rook and minor vs rook is drawish                                    *
            **************************************************************************/

            if (result > 0)
            {
                stronger = ChessPiece.White;
                weaker = ChessPiece.Black;
            }
            else
            {
                stronger = ChessPiece.Black;
                weaker = ChessPiece.White;
            }

            if (eb.pawn_material[stronger] == 0)
            {

                if (eb.piece_material[stronger] < 400)
                {
                    return 0;
                }

                if (eb.pawn_material[weaker] == 0 && (eb.piece_material[stronger] == 2 * e.PIECE_VALUE[ec.KNIGHT]))
                {
                    return 0;
                }

                if (eb.piece_material[stronger] == e.PIECE_VALUE[ec.ROOK]
                    && eb.piece_material[weaker] == e.PIECE_VALUE[ec.BISHOP]) result /= 2;

                if (eb.piece_material[stronger] == e.PIECE_VALUE[ec.ROOK]
                        && eb.piece_material[weaker] == e.PIECE_VALUE[ec.BISHOP]) result /= 2;

                if (eb.piece_material[stronger] == e.PIECE_VALUE[ec.ROOK] + e.PIECE_VALUE[ec.BISHOP]
                        && eb.piece_material[stronger] == e.PIECE_VALUE[ec.ROOK]) result /= 2;

                if (eb.piece_material[stronger] == e.PIECE_VALUE[ec.ROOK] + e.PIECE_VALUE[ec.KNIGHT]
                        && eb.piece_material[stronger] == e.PIECE_VALUE[ec.ROOK]) result /= 2;
            }

            /**************************************************************************
            *  Finally return the score relative to the side to move.                 *
            **************************************************************************/

            if (b.ColorToMove == ChessPiece.Black) result = -result;

            //tteval_save(result);
            return result;
        }

        void EvalKnight(Board b, s_eval_data e, EvaluationBoard eb, eval_vector v, Position sq, Piece side)
        {
            int att = 0;
            int mob = 0;

            /**************************************************************************
            *  Collect data about mobility and king attacks. This resembles move      *
            *  generation code, except that we are just incrementing the counters     *
            *  instead of adding actual moves.                                        *
            **************************************************************************/

            var jumps = BitboardConstants.KnightJumps[sq];
            while (jumps != 0)
            {
                var pos = jumps.BitScanForward();
                var piece = b.ArrayBoard[pos];
                var color = piece & ChessPiece.Color;
                if (piece == ChessPiece.Empty || color != side)
                {
                    // we exclude mobility to squares controlled by enemy pawns
                    // but don't penalize possible captures
                    if (eb.pawn_ctrl[side ^ 1, pos] == 0)
                    {
                        ++mob;
                    }

                    if (e.sqNearK[side ^ 1, b.KingPositions[side ^ 1], pos] != 0)
                    {
                        ++att; // this knight is attacking zone around enemy king
                    }
                }
                jumps &= ~(1UL << pos);
            }
            
            /**************************************************************************
            *  Evaluate mobility. We try to do it in such a way that zero represents  *
            *  average mobility, but  our formula of doing so is a puer guess.        *
            **************************************************************************/

            v.mgMob[side] += 4 * (mob - 4);
            v.egMob[side] += 4 * (mob - 4);

            /**************************************************************************
            *  Save data about king attacks                                           *
            **************************************************************************/

            if (att > 0)
            {
                v.attCnt[side]++;
                v.attWeight[side] += 2 * att;
            }

            /**************************************************************************
            * Evaluate king tropism                                                   *
            **************************************************************************/

            int tropism = getTropism(sq, b.KingPositions[side ^ 1]);
            v.mgTropism[side] += 3 * tropism;
            v.egTropism[side] += 3 * tropism;
        }

        void EvalBishop(Board b, s_eval_data e, EvaluationBoard eb, eval_vector v, Position sq, Piece side)
        {

            int att = 0;
            int mob = 0;

            /**************************************************************************
            *  Collect data about mobility and king attacks                           *
            **************************************************************************/
            var mb = new MagicBitboardsService();
            var slide = mb.DiagonalAntidiagonalSlide(b.AllPieces, sq);
            while (slide != 0)
            {
                var pos = slide.BitScanForward();
                var piece = b.ArrayBoard[pos];
                var color = piece & ChessPiece.Color;
                if (piece == ChessPiece.Empty)
                {
                    if (eb.pawn_ctrl[side ^ 1, pos] == 0)
                    {
                        ++mob;
                    }
                    // we exclude mobility to squares controlled by enemy pawns
                    if (e.sqNearK[side ^ 1, b.KingPositions[side ^ 1], pos] != 0)
                    {
                        ++att;
                    }
                }
                else if(color != side)
                {
                    mob++;
                    if (e.sqNearK[side ^ 1, b.KingPositions[side ^ 1], pos] != 0)
                    {
                        ++att; // this knight is attacking zone around enemy king
                    }
                }
                slide &= ~(1UL << pos);
            }

            v.mgMob[side] += 3 * (mob - 7);
            v.egMob[side] += 3 * (mob - 7);

            if (att > 0)
            {
                v.attCnt[side]++;
                v.attWeight[side] += 2 * att;
            }

            int tropism = getTropism(sq, b.KingPositions[side ^ 1]);
            v.mgTropism[side] += 2 * tropism;
            v.egTropism[side] += 1 * tropism;
        }

        void EvalRook(Board b, s_eval_data e, EvaluationBoard eb, eval_vector v, Position sq, Piece side)
        {

            int att = 0;
            int mob = 0;

            var sqCol = sq & 7;
            var sqRow = sq >> 3;

            /**************************************************************************
            *  Bonus for rook on the seventh rank. It is applied when there are pawns *
            *  to attack along that rank or if enemy king is cut off on 8th rank      *
            /*************************************************************************/

            if
            (
                sqRow == ec.seventh[side]
                && (eb.pawns_on_rank[side ^ 1, ec.seventh[side]] > 0 || (b.KingPositions[side ^ 1]) == ec.eighth[side])
            )
            {
                v.mgMob[side] += 20;
                v.egMob[side] += 30;
            }

            /**************************************************************************
            *  Bonus for open and half-open files is merged with mobility score.      *
            *  Bonus for open files targetting enemy king is added to attWeight[]     *
            /*************************************************************************/

            if (eb.pawns_on_file[side, sqCol] == 0)
            {
                if (eb.pawns_on_file[side ^ 1, sqCol] == 0)
                { // fully open file
                    v.mgMob[side] += e.ROOK_OPEN;
                    v.egMob[side] += e.ROOK_OPEN;
                    if (Math.Abs(sqCol - (b.KingPositions[side ^ 1] & 7)) < 2)
                    {
                        v.attWeight[side] += 1;
                    }
                }
                else
                {                                    // half open file
                    v.mgMob[side] += e.ROOK_HALF;
                    v.egMob[side] += e.ROOK_HALF;
                    if (Math.Abs(sqCol - (b.KingPositions[side ^ 1] & 7)) < 2)
                    {
                        v.attWeight[side] += 2;
                    }
                }
            }

            /**************************************************************************
            *  Collect data about mobility and king attacks                           *
            **************************************************************************/

            var mb = new MagicBitboardsService();
            var slide = mb.HorizontalVerticalSlide(b.AllPieces, sq);
            while (slide != 0)
            {
                var pos = slide.BitScanForward();
                var piece = b.ArrayBoard[pos];
                var color = piece & ChessPiece.Color;
                if (piece == ChessPiece.Empty || color != side)
                {
                    mob++;
                    if (e.sqNearK[side ^ 1, b.KingPositions[side ^ 1], pos] != 0)
                    {
                        ++att; // this knight is attacking zone around enemy king
                    }
                }
                slide &= ~(1UL << pos);
            }
            
            v.mgMob[side] += 2 * (mob - 7);
            v.egMob[side] += 4 * (mob - 7);

            if (att > 0)
            {
                v.attCnt[side]++;
                v.attWeight[side] += 3 * att;
            }

            int tropism = getTropism(sq, b.KingPositions[side ^ 1]);
            v.mgTropism[side] += 2 * tropism;
            v.egTropism[side] += 1 * tropism;
        }

        void EvalQueen(Board b, s_eval_data e, EvaluationBoard eb, eval_vector v, Position sq, Piece side)
        {

            int att = 0;
            int mob = 0;

            var sqCol = sq & 7;
            var sqRow = sq >> 3;

            if
            (
                sqRow == ec.seventh[side]
                && (eb.pawns_on_rank[side ^ 1, ec.seventh[side]] > 0 || (b.KingPositions[side ^ 1]) == ec.eighth[side])
            )
            {
                v.mgMob[side] += 5;
                v.egMob[side] += 10;
            }

            /**************************************************************************
            *  A queen should not be developed too early                              *
            **************************************************************************/

            if ((side == ChessPiece.White && sqRow > 1) || (side == ChessPiece.Black && sqRow < 6))
            {
                if (isPiece(b, side, ec.KNIGHT, REL_SQ(side, ChessPosition.B1))) v.positionalThemes[side] -= 2;
                if (isPiece(b, side, ec.BISHOP, REL_SQ(side, ChessPosition.C1))) v.positionalThemes[side] -= 2;
                if (isPiece(b, side, ec.BISHOP, REL_SQ(side, ChessPosition.F1))) v.positionalThemes[side] -= 2;
                if (isPiece(b, side, ec.KNIGHT, REL_SQ(side, ChessPosition.G1))) v.positionalThemes[side] -= 2;
            }

            /**************************************************************************
            *  Collect data about mobility and king attacks                           *
            **************************************************************************/

            var mb = new MagicBitboardsService();
            var slide = mb.AllSlide(b.AllPieces, sq);
            while (slide != 0)
            {
                var pos = slide.BitScanForward();
                var piece = b.ArrayBoard[pos];
                var color = piece & ChessPiece.Color;
                if (piece == ChessPiece.Empty || color != side)
                {
                    mob++;
                    if (e.sqNearK[side ^ 1, b.KingPositions[side ^ 1], pos] != 0)
                    {
                        ++att; // this knight is attacking zone around enemy king
                    }
                }
                slide &= ~(1UL << pos);
            }

            v.mgMob[side] += 1 * (mob - 14);
            v.egMob[side] += 2 * (mob - 14);

            if (att > 0)
            {
                v.attCnt[side]++;
                v.attWeight[side] += 4 * att;
            }

            int tropism = getTropism(sq, b.KingPositions[side ^ 1]);
            v.mgTropism[side] += 2 * tropism;
            v.egTropism[side] += 4 * tropism;
        }

        int getTropism(int sq1, int sq2)
        {
            return 7 - (Math.Abs((sq1 >> 3) - (sq2 >> 3)) + Math.Abs((sq1 & 7) - (sq2 & 7)));
        }

        int wKingShield(Board b, s_eval_data e)
        {
            int result = 0;
            var kingBitboard = b.BitBoard[ChessPiece.WhiteKing];
            var kingPos = kingBitboard.BitScanForward();
            var col = kingPos & 7;

            /* king on the kingside */
            if (col > ChessFile.E)
            {
                if (b.ArrayBoard[ChessPosition.F2] == ChessPiece.WhitePawn) result += e.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.F2] == ChessPiece.WhitePawn) result += e.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.G2] == ChessPiece.WhitePawn) result += e.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.G3] == ChessPiece.WhitePawn) result += e.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.H2] == ChessPiece.WhitePawn) result += e.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.H3] == ChessPiece.WhitePawn) result += e.SHIELD_3;
            }

            /* king on the queenside */
            else if (col < ChessFile.D)
            {

                if (b.ArrayBoard[ChessPosition.A2] == ChessPiece.WhitePawn) result += e.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.A3] == ChessPiece.WhitePawn) result += e.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.B2] == ChessPiece.WhitePawn) result += e.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.B3] == ChessPiece.WhitePawn) result += e.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.C2] == ChessPiece.WhitePawn) result += e.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.C3] == ChessPiece.WhitePawn) result += e.SHIELD_3;
            }

            return result;
        }

        int bKingShield(Board b, s_eval_data e)
        {
            int result = 0;
            var kingBitboard = b.BitBoard[ChessPiece.BlackKing];
            var kingPos = kingBitboard.BitScanForward();
            var col = kingPos & 7;

            /* king on the kingside */
            if (col > ChessFile.E)
            {
                if (b.ArrayBoard[ChessPosition.F7] == ChessPiece.BlackPawn) result += e.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.F6] == ChessPiece.BlackPawn) result += e.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.G7] == ChessPiece.BlackPawn) result += e.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.G6] == ChessPiece.BlackPawn) result += e.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.H7] == ChessPiece.BlackPawn) result += e.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.H6] == ChessPiece.BlackPawn) result += e.SHIELD_3;
            }

            /* king on the queenside */
            else if (col < ChessFile.D)
            {
                if (b.ArrayBoard[ChessPosition.A7] == ChessPiece.BlackPawn) result += e.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.A6] == ChessPiece.BlackPawn) result += e.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.B7] == ChessPiece.BlackPawn) result += e.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.B6] == ChessPiece.BlackPawn) result += e.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.C7] == ChessPiece.BlackPawn) result += e.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.C6] == ChessPiece.BlackPawn) result += e.SHIELD_3;
            }
            return result;
        }

        static bool isPiece(Board b, int color, int piece, int position)
        {
            var p = b.ArrayBoard[position];
            switch (p)
            {
                case ChessPiece.WhitePawn: return color == ChessPiece.White && piece == ec.PAWN;
                case ChessPiece.WhiteKnight: return color == ChessPiece.White && piece == ec.KNIGHT;
                case ChessPiece.WhiteBishop: return color == ChessPiece.White && piece == ec.BISHOP;
                case ChessPiece.WhiteRook: return color == ChessPiece.White && piece == ec.ROOK;
                case ChessPiece.WhiteQueen: return color == ChessPiece.White && piece == ec.QUEEN;
                case ChessPiece.WhiteKing: return color == ChessPiece.White && piece == ec.KING;

                case ChessPiece.BlackPawn: return color == ChessPiece.Black && piece == ec.PAWN;
                case ChessPiece.BlackKnight: return color == ChessPiece.Black && piece == ec.KNIGHT;
                case ChessPiece.BlackBishop: return color == ChessPiece.Black && piece == ec.BISHOP;
                case ChessPiece.BlackRook: return color == ChessPiece.Black && piece == ec.ROOK;
                case ChessPiece.BlackQueen: return color == ChessPiece.Black && piece == ec.QUEEN;
                case ChessPiece.BlackKing: return color == ChessPiece.Black && piece == ec.KING;
            }

            return false;
        }

        int REL_SQ(int cl, int sq)
        {
            return ((cl) == (ChessPiece.White) ? (sq) : (ec.inv_sq[sq]));
        }

        void blockedPieces(Board b, s_eval_data e, eval_vector v, EvaluationBoard eb, int side)
        {

            int oppo = side == 0 ? 1 : 0;

            // central pawn blocked, bishop hard to develop
            if (isPiece(b, side, ec.BISHOP, REL_SQ(side, ChessPosition.C1))
            && isPiece(b, side, ec.PAWN, REL_SQ(side, ChessPosition.D2))
            && b.ArrayBoard[REL_SQ(side, ChessPosition.D3)] != ChessPiece.Empty)
                v.blockages[side] -= e.P_BLOCK_CENTRAL_PAWN;

            if (isPiece(b, side, ec.BISHOP, REL_SQ(side, ChessPosition.F1))
            && isPiece(b, side, ec.PAWN, REL_SQ(side, ChessPosition.E2))
            && b.ArrayBoard[REL_SQ(side, ChessPosition.E3)] != ChessPiece.Empty)
                v.blockages[side] -= e.P_BLOCK_CENTRAL_PAWN;

            // trapped knight
            if (isPiece(b, side, ec.KNIGHT, REL_SQ(side, ChessPosition.A8))
            && (isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.A7)) || isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.C7))))
                v.blockages[side] -= e.P_KNIGHT_TRAPPED_A8;

            if (isPiece(b, side, ec.KNIGHT, REL_SQ(side, ChessPosition.H8))
            && (isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.H7)) || isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.F7))))
                v.blockages[side] -= e.P_KNIGHT_TRAPPED_A8;

            if (isPiece(b, side, ec.KNIGHT, REL_SQ(side, ChessPosition.A7))
            && isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.A6))
            && isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.B7)))
                v.blockages[side] -= e.P_KNIGHT_TRAPPED_A7;

            if (isPiece(b, side, ec.KNIGHT, REL_SQ(side, ChessPosition.H7))
            && isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.H6))
            && isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.G7)))
                v.blockages[side] -= e.P_KNIGHT_TRAPPED_A7;

            // knight blocking queenside pawns
            if (isPiece(b, side, ec.KNIGHT, REL_SQ(side, ChessPosition.C3))
            && isPiece(b, side, ec.PAWN, REL_SQ(side, ChessPosition.C2))
            && isPiece(b, side, ec.PAWN, REL_SQ(side, ChessPosition.D4))
            && !isPiece(b, side, ec.PAWN, REL_SQ(side, ChessPosition.E4)))
                v.blockages[side] -= e.P_C3_KNIGHT;

            // trapped bishop
            if (isPiece(b, side, ec.BISHOP, REL_SQ(side, ChessPosition.A7))
            && isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.B6)))
                v.blockages[side] -= e.P_BISHOP_TRAPPED_A7;

            if (isPiece(b, side, ec.BISHOP, REL_SQ(side, ChessPosition.H7))
            && isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.G6)))
                v.blockages[side] -= e.P_BISHOP_TRAPPED_A7;

            if (isPiece(b, side, ec.BISHOP, REL_SQ(side, ChessPosition.B8))
            && isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.C7)))
                v.blockages[side] -= e.P_BISHOP_TRAPPED_A7;

            if (isPiece(b, side, ec.BISHOP, REL_SQ(side, ChessPosition.G8))
            && isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.F7)))
                v.blockages[side] -= e.P_BISHOP_TRAPPED_A7;

            if (isPiece(b, side, ec.BISHOP, REL_SQ(side, ChessPosition.A6))
            && isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.B5)))
                v.blockages[side] -= e.P_BISHOP_TRAPPED_A6;

            if (isPiece(b, side, ec.BISHOP, REL_SQ(side, ChessPosition.H6))
            && isPiece(b, oppo, ec.PAWN, REL_SQ(side, ChessPosition.G5)))
                v.blockages[side] -= e.P_BISHOP_TRAPPED_A6;

            // bishop on initial sqare supporting castled king
            if (isPiece(b, side, ec.BISHOP, REL_SQ(side, ChessPosition.F1))
            && isPiece(b, side, ec.KING, REL_SQ(side, ChessPosition.G1)))
                v.positionalThemes[side] += e.RETURNING_BISHOP;

            if (isPiece(b, side, ec.BISHOP, REL_SQ(side, ChessPosition.C1))
            && isPiece(b, side, ec.KING, REL_SQ(side, ChessPosition.B1)))
                v.positionalThemes[side] += e.RETURNING_BISHOP;

            // uncastled king blocking own rook
            if ((isPiece(b, side, ec.KING, REL_SQ(side, ChessPosition.F1)) || isPiece(b, side, ec.KING, REL_SQ(side, ChessPosition.G1)))
            && (isPiece(b, side, ec.ROOK, REL_SQ(side, ChessPosition.H1)) || isPiece(b, side, ec.ROOK, REL_SQ(side, ChessPosition.G1))))
                v.blockages[side] -= e.P_KING_BLOCKS_ROOK;

            if ((isPiece(b, side, ec.KING, REL_SQ(side, ChessPosition.C1)) || isPiece(b, side, ec.KING, REL_SQ(side, ChessPosition.B1)))
            && (isPiece(b, side, ec.ROOK, REL_SQ(side, ChessPosition.A1)) || isPiece(b, side, ec.ROOK, REL_SQ(side, ChessPosition.B1))))
                v.blockages[side] -= e.P_KING_BLOCKS_ROOK;
        }

        int getPawnScore(Board b, EvaluationBoard eb, s_eval_data e)
        {
            int result;

            /**************************************************************************
            *  This function wraps hashing mechanism around evalPawnStructure().      *
            *  Please note  that since we use the pawn hashtable, evalPawnStructure() *
            *  must not take into account the piece position.  In a more elaborate    *
            *  program, pawn hashtable would contain only the characteristics of pawn *
            *  structure,  and scoring them in conjunction with the piece position    *
            *  would have been done elsewhere.                                        *
            **************************************************************************/

            //int probeval = ttpawn_probe();
            //if (probeval != INVALID)
            //    return probeval;

            result = evalPawnStructure(b, eb, e);
            //ttpawn_save(result);
            return result;
        }

        int evalPawnStructure(Board b, EvaluationBoard eb, s_eval_data e)
        {
            int result = 0;

            for (byte sq = 0; sq < 64; sq++)
            {
                var piece = b.ArrayBoard[sq];
                if (piece == ChessPiece.WhitePawn || piece == ChessPiece.BlackPawn)
                {
                    var color = (Position)(piece & ChessPiece.Color);
                    if (color == ChessPiece.White)
                    {
                        result += EvalPawn(b, eb, e,sq, ChessPiece.White);
                    }
                    else
                    {
                        result -= EvalPawn(b, eb, e, sq, ChessPiece.Black);
                    }
                }
            }

            return result;
        }

        int EvalPawn(Board b, EvaluationBoard eb, s_eval_data e, Position sq, byte side)
        {
            int result = 0;
            var flagIsPassed = true; // we will be trying to disprove that
            var flagIsWeak = true;   // we will be trying to disprove that
            var flagIsOpposed = false;

            /**************************************************************************
            *   We have only very basic data structures that do not update informa-   *
            *   tion about pawns incrementally, so we have to calculate everything    *
            *   here.  The loop below detects doubled pawns, passed pawns and sets    *
            *   a flag on finding that our pawn is opposed by enemy pawn.             *
            **************************************************************************/

            if (eb.pawn_ctrl[side, sq] > 0) // if a pawn is attacked by a pawn, it is not
            {
                flagIsPassed = false; // passed (not sure if it's the best decision)
            }

            var nextSq = sq + ec.stepFwd[side];

            while (nextSq > 0 && nextSq < 64)
            {
                var nextPiece = b.ArrayBoard[nextSq];
                if (nextPiece == ChessPiece.WhitePawn || nextPiece == ChessPiece.BlackPawn)
                { // either opposed by enemy pawn or doubled
                    var color = (Position)(nextPiece & ChessPiece.Color);
                    flagIsPassed = false;
                    if (color == side)
                    {
                        result -= 20;       // doubled pawn penalty
                    }
                    else
                    {
                        flagIsOpposed = true;  // flag our pawn as opposed
                    }
                }

                if (eb.pawn_ctrl[side ^ 1, nextSq] > 0)
                {
                    flagIsPassed = false;
                }

                nextSq += ec.stepFwd[side];
            }

            /**************************************************************************
            *   Another loop, going backwards and checking whether pawn has support.  *
            *   Here we can at least break out of it for speed optimization.          *
            **************************************************************************/

            nextSq = sq + ec.stepFwd[side]; // so that a pawn in a duo will not be considered weak

            while (nextSq > 0 && nextSq < 64)
            {

                if (eb.pawn_ctrl[side,nextSq] > 0)
                {
                    flagIsWeak = false;
                    break;
                }

                nextSq += ec.stepBck[side];
            }

            /**************************************************************************
            *  Evaluate passed pawns, scoring them higher if they are protected       *
            *  or if their advance is supported by friendly pawns                     *
            **************************************************************************/

            if (flagIsPassed)
            {
                var pawnSupported = isPawnSupported(b, sq, side);
                if (pawnSupported)
                {
                    result += e.protected_passer[side, sq];
                }
                else
                {
                    result += e.passed_pawn[side, sq];
                }
            }

            /**************************************************************************
            *  Evaluate weak pawns, increasing the penalty if they are situated       *
            *  on a half-open file                                                    *
            **************************************************************************/

            if (flagIsWeak)
            {
                result += e.weak_pawn[side, sq];
                if (!flagIsOpposed)
                {
                    result -= 4;
                }
            }

            return result;
        }

        bool isPawnSupported(Board b, Position sq, Piece side)
        {
            int step;
            if (side == ChessPiece.White)
            {
                step = ec.SOUTH;
            }
            else
            {
                step = ec.NORTH;
            }

            var col = sq & 7;

            //if (color == ChessPiece.White)
            //{
            //    if (col < 7) pawn_ctrl[ChessPiece.White, sq + ec.NE]++;
            //    if (col > 0) pawn_ctrl[ChessPiece.White, sq + ec.NW]++;
            //}
            //else
            //{
            //    if (col < 7) pawn_ctrl[ChessPiece.Black, sq + ec.SE]++;
            //    if (col > 0) pawn_ctrl[ChessPiece.Black, sq + ec.SW]++;
            //}

            if (col > 0 && isPiece(b, side, ec.PAWN, sq + ec.WEST))
            {
                return true;
            }

            if (col < 7 && isPiece(b, side, ec.PAWN, sq + ec.EAST))
            {
                return true;
            }

            if (col > 0 && isPiece(b, side, ec.PAWN, sq + step + ec.WEST))
            {
                return true;
            }

            if (col < 7 && isPiece(b, side, ec.PAWN, sq + step + ec.EAST))
            {
                return true;
            }

            return false;
        }

        void printEval(Board b, s_eval_data e, EvaluationBoard eb, eval_vector v, Score score)
        {
            var builder = new StringBuilder();
            builder.Append("------------------------------------------\n");
            builder.Append($"Total value (for side to move): {score}\n");
            builder.Append($"Material balance       : {eb.piece_material[ChessPiece.White] + eb.pawn_material[ChessPiece.White] - eb.piece_material[ChessPiece.Black] - eb.pawn_material[ChessPiece.Black]} \n");
            builder.Append("Material adjustement   : ");
            printEvalFactor(builder, v.adjustMaterial[ChessPiece.White], v.adjustMaterial[ChessPiece.Black]);
            builder.Append("Mg Piece/square tables : ");
            printEvalFactor(builder, eb.pcsq_mg[ChessPiece.White], eb.pcsq_mg[ChessPiece.Black]);
            builder.Append("Eg Piece/square tables : ");
            printEvalFactor(builder, eb.pcsq_eg[ChessPiece.White], eb.pcsq_eg[ChessPiece.Black]);
            builder.Append("Mg Mobility            : ");
            printEvalFactor(builder, v.mgMob[ChessPiece.White], v.mgMob[ChessPiece.Black]);
            builder.Append("Eg Mobility            : ");
            printEvalFactor(builder, v.egMob[ChessPiece.White], v.egMob[ChessPiece.Black]);
            builder.Append("Mg Tropism             : ");
            printEvalFactor(builder, v.mgTropism[ChessPiece.White], v.mgTropism[ChessPiece.Black]);
            builder.Append("Eg Tropism             : ");
            printEvalFactor(builder, v.egTropism[ChessPiece.White], v.egTropism[ChessPiece.Black]);
            //builder.Append("Pawn structure         : %d \n", evalPawnStructure());
            builder.Append("Blockages              : ");
            printEvalFactor(builder, v.blockages[ChessPiece.White], v.blockages[ChessPiece.Black]);
            builder.Append("Positional themes      : ");
            printEvalFactor(builder, v.positionalThemes[ChessPiece.White], v.positionalThemes[ChessPiece.Black]);
            builder.Append("King Shield            : ");
            printEvalFactor(builder, v.kingShield[ChessPiece.White], v.kingShield[ChessPiece.Black]);
            builder.Append("Tempo                  : ");
            if (b.WhiteToMove) builder.Append(e.TEMPO);
            else builder.Append(-e.TEMPO);
            builder.Append("\n");
            builder.Append("------------------------------------------\n");
            Console.WriteLine(builder.ToString());
        }

        void printEvalFactor(StringBuilder builder, int wh, int bl)
        {
            builder.Append($"white {wh}, black {bl}, total: {wh - bl} \n");
        }
    }
}
