//using System;
//using System.Collections.Generic;
//using System.Text;
//using ChessDotNet.Data;
//using Score = System.Int32;

//namespace ChessDotNet.Evaluation
//{
//    class eval_vector
//    {
//        public int gamePhase;   // function of piece material: 24 in opening, 0 in endgame
//        public int[] mgMob = new int[2];     // midgame mobility
//        public int[] egMob = new int[2];     // endgame mobility
//        public int[] attCnt = new int[2];    // no. of pieces attacking zone around enemy king
//        public int[] attWeight = new int[2]; // weight of attacking pieces - index to SafetyTable
//        public int[] mgTropism = new int[2]; // midgame king tropism score
//        public int[] egTropism = new int[2]; // endgame king tropism score
//        public int[] kingShield = new int[2];
//        public int[] adjustMaterial = new int[2];
//        public int[] blockages = new int[2];
//        public int[] positionalThemes = new int[2];
//    }

//    static class ec
//    {
//        public const int KING = 0;
//        public const int QUEEN = 1;
//        public const int ROOK = 2;
//        public const int BISHOP = 3;
//        public const int KNIGHT = 4;
//        public const int PAWN = 5;
//        public const int PIECE_EMPTY = 6;

//        public const int SORT_KING = 400000000;

//        public const int WHITE = 0;
//        public const int BLACK = 1;

//        public const int NORTH = 8;
//        public const int NN = (NORTH + NORTH);
//        public const int SOUTH = -8;
//        public const int SS = (SOUTH + SOUTH);
//        public const int EAST = 1;
//        public const int WEST = -1;
//        public const int NE = 9;
//        public const int SW = -9;
//        public const int NW = 7;
//        public const int SE = -7;

//        public static (byte, byte) GetColorAndPiece(int piece)
//        {
//            switch (piece)
//            {
//                case ChessPiece.WhitePawn: return (WHITE, PAWN);
//                case ChessPiece.WhiteKnight: return (WHITE, KNIGHT);
//                case ChessPiece.WhiteBishop: return (WHITE, BISHOP);
//                case ChessPiece.WhiteRook: return (WHITE, ROOK);
//                case ChessPiece.WhiteQueen: return (WHITE, QUEEN);
//                case ChessPiece.WhiteKing: return (WHITE, KING);
//                case ChessPiece.BlackPawn: return (BLACK, PAWN);
//                case ChessPiece.BlackKnight: return (BLACK, KNIGHT);
//                case ChessPiece.BlackBishop: return (BLACK, BISHOP);
//                case ChessPiece.BlackRook: return (BLACK, ROOK);
//                case ChessPiece.BlackQueen: return (BLACK, QUEEN);
//                case ChessPiece.BlackKing: return (BLACK, KING);
//            }

//            throw new Exception();
//        }
//    }

//    class s_eval_data
//    {
//        public int[] PIECE_VALUE = new int[6];
//        public int[] SORT_VALUE = new int[6];

//        /* Piece-square tables - we use size of the board representation,
//        not 0..63, to avoid re-indexing. Initialization routine, however,
//        uses 0..63 format for clarity */
//        public int[,,] mgPst = new int[6, 2, 64];
//        public int[,,] egPst = new int[6, 2, 64];

//        /* piece-square tables for pawn structure */

//        public int[,] weak_pawn = new int[2, 64]; // isolated and backward pawns are scored in the same way
//        public int[,] passed_pawn = new int[2, 64];
//        public int[,] protected_passer = new int[2, 64];

//        public int[,,] sqNearK = new int[2, 64, 64];

//        /* single values - letter p before a name signifies a penalty */

//        public int BISHOP_PAIR;
//        public int P_KNIGHT_PAIR;
//        public int P_ROOK_PAIR;
//        public int ROOK_OPEN;
//        public int ROOK_HALF;
//        public int P_BISHOP_TRAPPED_A7;
//        public int P_BISHOP_TRAPPED_A6;
//        public int P_KNIGHT_TRAPPED_A8;
//        public int P_KNIGHT_TRAPPED_A7;
//        public int P_BLOCK_CENTRAL_PAWN;
//        public int P_KING_BLOCKS_ROOK;

//        public int SHIELD_2;
//        public int SHIELD_3;
//        public int P_NO_SHIELD;

//        public int RETURNING_BISHOP;
//        public int P_C3_KNIGHT;
//        public int P_NO_FIANCHETTO;
//        public int FIANCHETTO;
//        public int TEMPO;
//        public int ENDGAME_MAT;

//        /******************************************************************************
//        *                           PAWN PCSQ                                         *
//        *                                                                             *
//        *  Unlike TSCP, CPW generally doesn't want to advance its pawns. Its piece/   *
//        *  square table for pawns takes into account the following factors:           *
//        *                                                                             *
//        *  - file-dependent component, encouraging program to capture                 *
//        *    towards the center                                                       *
//        *  - small bonus for staying on the 2nd rank                                  *
//        *  - small bonus for standing on a3/h3                                        *
//        *  - penalty for d/e pawns on their initial squares                           *
//        *  - bonus for occupying the center                                           *
//        ******************************************************************************/

//        int[] pawn_pcsq_mg = new int[64] {
//             0,   0,   0,   0,   0,   0,   0,   0,
//            -6,  -4,   1,   1,   1,   1,  -4,  -6,
//            -6,  -4,   1,   2,   2,   1,  -4,  -6,
//            -6,  -4,   2,   8,   8,   2,  -4,  -6,
//            -6,  -4,   5,  10,  10,   5,  -4,  -6,
//            -4,  -4,   1,   5,   5,   1,  -4,  -4,
//            -6,  -4,   1, -24,  -24,  1,  -4,  -6,
//             0,   0,   0,   0,   0,   0,   0,   0
//        };

//        int[] pawn_pcsq_eg = new int[64] {
//             0,   0,   0,   0,   0,   0,   0,   0,
//            -6,  -4,   1,   1,   1,   1,  -4,  -6,
//            -6,  -4,   1,   2,   2,   1,  -4,  -6,
//            -6,  -4,   2,   8,   8,   2,  -4,  -6,
//            -6,  -4,   5,  10,  10,   5,  -4,  -6,
//            -4,  -4,   1,   5,   5,   1,  -4,  -4,
//            -6,  -4,   1, -24,  -24,  1,  -4,  -6,
//             0,   0,   0,   0,   0,   0,   0,   0
//        };

//        /******************************************************************************
//        *    KNIGHT PCSQ                                                              *
//        *                                                                             *
//        *   - centralization bonus                                                    *
//        *   - rim and back rank penalty, including penalty for not being developed    *
//        ******************************************************************************/

//        int[] knight_pcsq_mg = new int[] {
//            -8,  -8,  -8,  -8,  -8,  -8,  -8,  -8,
//            -8,   0,   0,   0,   0,   0,   0,  -8,
//            -8,   0,   4,   6,   6,   4,   0,  -8,
//            -8,   0,   6,   8,   8,   6,   0,  -8,
//            -8,   0,   6,   8,   8,   6,   0,  -8,
//            -8,   0,   4,   6,   6,   4,   0,  -8,
//            -8,   0,   1,   2,   2,   1,   0,  -8,
//           -16, -12,  -8,  -8,  -8,  -8, -12,  -16
//        };

//        int[] knight_pcsq_eg = new int[64] {
//            -8,  -8,  -8,  -8,  -8,  -8,  -8,  -8,
//            -8,   0,   0,   0,   0,   0,   0,  -8,
//            -8,   0,   4,   6,   6,   4,   0,  -8,
//            -8,   0,   6,   8,   8,   6,   0,  -8,
//            -8,   0,   6,   8,   8,   6,   0,  -8,
//            -8,   0,   4,   6,   6,   4,   0,  -8,
//            -8,   0,   1,   2,   2,   1,   0,  -8,
//           -16, -12,  -8,  -8,  -8,  -8, -12,  -16
//        };

//        /******************************************************************************
//        *                BISHOP PCSQ                                                  *
//        *                                                                             *
//        *   - centralization bonus, smaller than for knight                           *
//        *   - penalty for not being developed                                         *
//        *   - good squares on the own half of the board                               *
//        ******************************************************************************/

//        int[] bishop_pcsq_mg = new int[64] {
//            -4,  -4,  -4,  -4,  -4,  -4,  -4,  -4,
//            -4,   0,   0,   0,   0,   0,   0,  -4,
//            -4,   0,   2,   4,   4,   2,   0,  -4,
//            -4,   0,   4,   6,   6,   4,   0,  -4,
//            -4,   0,   4,   6,   6,   4,   0,  -4,
//            -4,   1,   2,   4,   4,   2,   1,  -4,
//            -4,   2,   1,   1,   1,   1,   2,  -4,
//            -4,  -4, -12,  -4,  -4, -12,  -4,  -4
//        };

//        int[] bishop_pcsq_eg = new int[64] {
//            -4,  -4,  -4,  -4,  -4,  -4,  -4,  -4,
//            -4,   0,   0,   0,   0,   0,   0,  -4,
//            -4,   0,   2,   4,   4,   2,   0,  -4,
//            -4,   0,   4,   6,   6,   4,   0,  -4,
//            -4,   0,   4,   6,   6,   4,   0,  -4,
//            -4,   1,   2,   4,   4,   2,   1,  -4,
//            -4,   2,   1,   1,   1,   1,   2,  -4,
//            -4,  -4, -12,  -4,  -4, -12,  -4,  -4
//        };

//        /******************************************************************************
//        *                        ROOK PCSQ                                            *
//        *                                                                             *
//        *    - bonus for 7th and 8th ranks                                            *
//        *    - penalty for a/h columns                                                *
//        *    - small centralization bonus                                             *
//        ******************************************************************************/

//        int[] rook_pcsq_mg = new int[64] {
//             5,   5,   5,   5,   5,   5,   5,   5,
//            -5,   0,   0,   0,   0,   0,   0,  -5,
//            -5,   0,   0,   0,   0,   0,   0,  -5,
//            -5,   0,   0,   0,   0,   0,   0,  -5,
//            -5,   0,   0,   0,   0,   0,   0,  -5,
//            -5,   0,   0,   0,   0,   0,   0,  -5,
//            -5,   0,   0,   0,   0,   0,   0,  -5,
//             0,   0,   0,   2,   2,   0,   0,   0
//        };

//        int[] rook_pcsq_eg = new int[64] {
//             5,   5,   5,   5,   5,   5,   5,   5,
//            -5,   0,   0,   0,   0,   0,   0,  -5,
//            -5,   0,   0,   0,   0,   0,   0,  -5,
//            -5,   0,   0,   0,   0,   0,   0,  -5,
//            -5,   0,   0,   0,   0,   0,   0,  -5,
//            -5,   0,   0,   0,   0,   0,   0,  -5,
//            -5,   0,   0,   0,   0,   0,   0,  -5,
//             0,   0,   0,   2,   2,   0,   0,   0
//        };

//        /******************************************************************************
//        *                     QUEEN PCSQ                                              *
//        *                                                                             *
//        * - small bonus for centralization in the endgame                             *
//        * - penalty for staying on the 1st rank, between rooks in the midgame         *
//        ******************************************************************************/

//        int[] queen_pcsq_mg = new int[64] {
//             0,   0,   0,   0,   0,   0,   0,   0,
//             0,   0,   1,   1,   1,   1,   0,   0,
//             0,   0,   1,   2,   2,   1,   0,   0,
//             0,   0,   2,   3,   3,   2,   0,   0,
//             0,   0,   2,   3,   3,   2,   0,   0,
//             0,   0,   1,   2,   2,   1,   0,   0,
//             0,   0,   1,   1,   1,   1,   0,   0,
//            -5,  -5,  -5,  -5,  -5,  -5,  -5,  -5
//        };

//        int[] queen_pcsq_eg = new int[64] {
//             0,   0,   0,   0,   0,   0,   0,   0,
//             0,   0,   1,   1,   1,   1,   0,   0,
//             0,   0,   1,   2,   2,   1,   0,   0,
//             0,   0,   2,   3,   3,   2,   0,   0,
//             0,   0,   2,   3,   3,   2,   0,   0,
//             0,   0,   1,   2,   2,   1,   0,   0,
//             0,   0,   1,   1,   1,   1,   0,   0,
//            -5,  -5,  -5,  -5,  -5,  -5,  -5,  -5
//        };

//        int[] king_pcsq_mg = new int[64] {
//           -40, -30, -50, -70, -70, -50, -30, -40,
//           -30, -20, -40, -60, -60, -40, -20, -30,
//           -20, -10, -30, -50, -50, -30, -10, -20,
//           -10,   0, -20, -40, -40, -20,   0, -10,
//             0,  10, -10, -30, -30, -10,  10,   0,
//            10,  20,   0, -20, -20,   0,  20,  10,
//            30,  40,  20,   0,   0,  20,  40,  30,
//            40,  50,  30,  10,  10,  30,  50,  40
//        };

//        int[] king_pcsq_eg = new int[64] {
//           -72, -48, -36, -24, -24, -36, -48, -72,
//           -48, -24, -12,   0,   0, -12, -24, -48,
//           -36, -12,   0,  12,  12,   0, -12, -36,
//           -24,   0,  12,  24,  24,  12,   0, -24,
//           -24,   0,  12,  24,  24,  12,   0, -24,
//           -36, -12,   0,  12,  12,   0, -12, -36,
//           -48, -24, -12,   0,   0, -12, -24, -48,
//           -72, -48, -36, -24, -24, -36, -48, -72
//        };

//        /******************************************************************************
//        *                     WEAK PAWNS PCSQ                                         *
//        *                                                                             *
//        *  Current version of CPW-engine does not differentiate between isolated and  *
//        *  backward pawns, using one  generic  cathegory of  weak pawns. The penalty  *
//        *  is bigger in the center, on the assumption that weak central pawns can be  *
//        *  attacked  from many  directions. If the penalty seems too low, please note *
//        *  that being on a semi-open file will come into equation, too.               *
//        ******************************************************************************/

//        int[] weak_pawn_pcsq = new int[64] {
//             0,   0,   0,   0,   0,   0,   0,   0,
//           -10, -12, -14, -16, -16, -14, -12, -10,
//           -10, -12, -14, -16, -16, -14, -12, -10,
//           -10, -12, -14, -16, -16, -14, -12, -10,
//           -10, -12, -14, -16, -16, -14, -12, -10,
//           -10, -12, -14, -16, -16, -14, -12, -10,
//           -10, -12, -14, -16, -16, -14, -12, -10,
//             0,   0,   0,   0,   0,   0,   0,   0
//        };

//        int[] passed_pawn_pcsq = new int[64] {
//             0,   0,   0,   0,   0,   0,   0,   0,
//           140, 140, 140, 140, 140, 140, 140, 140,
//            92,  92,  92,  92,  92,  92,  92,  92,
//            56,  56,  56,  56,  56,  56,  56,  56,
//            32,  32,  32,  32,  32,  32,  32,  32,
//            20,  20,  20,  20,  20,  20,  20,  20,
//            20,  20,  20,  20,  20,  20,  20,  20,
//             0,   0,   0,   0,   0,   0,   0,   0
//        };

//        public s_eval_data()
//        {
//            setBasicValues();
//            setSquaresNearKing();
//            setPcsq();
//            correctValues();
//        }

//        void setBasicValues()
//        {

//            /********************************************************************************
//            *  We use material values by IM Larry Kaufman with additional + 10 for a Bishop *
//            *  and only +30 for a Bishop pair 	                                            *
//            ********************************************************************************/

//            PIECE_VALUE[ec.KING] = 0;
//            PIECE_VALUE[ec.QUEEN] = 975;
//            PIECE_VALUE[ec.ROOK] = 500;
//            PIECE_VALUE[ec.BISHOP] = 335;
//            PIECE_VALUE[ec.KNIGHT] = 325;
//            PIECE_VALUE[ec.PAWN] = 100;

//            BISHOP_PAIR = 30;
//            P_KNIGHT_PAIR = 8;
//            P_ROOK_PAIR = 16;

//            /*************************************************
//            * Values used for sorting captures are the same  *
//            * as normal piece values, except for a king.     *
//            *************************************************/

//            for (int i = 0; i < 6; ++i)
//            {
//                SORT_VALUE[i] = PIECE_VALUE[i];
//            }
//            SORT_VALUE[ec.KING] = ec.SORT_KING;

//            /* trapped and blocked pieces */
//            P_KING_BLOCKS_ROOK = 24;
//            P_BLOCK_CENTRAL_PAWN = 24;
//            P_BISHOP_TRAPPED_A7 = 150;
//            P_BISHOP_TRAPPED_A6 = 50;
//            P_KNIGHT_TRAPPED_A8 = 150;
//            P_KNIGHT_TRAPPED_A7 = 100;

//            /* minor penalties */
//            P_C3_KNIGHT = 5;
//            P_NO_FIANCHETTO = 4;

//            /* king's defence */
//            SHIELD_2 = 10;
//            SHIELD_3 = 5;
//            P_NO_SHIELD = 10;

//            /* minor bonuses */
//            ROOK_OPEN = 10;
//            ROOK_HALF = 5;
//            RETURNING_BISHOP = 20;
//            FIANCHETTO = 4;
//            TEMPO = 10;

//            ENDGAME_MAT = 1300;
//        }

//        void setSquaresNearKing()
//        {
//            for (int i = 0; i < 64; ++i)
//            {
//                for (int j = 0; j < 64; ++j)
//                {
//                    sqNearK[ec.WHITE, i, j] = 0;
//                    sqNearK[ec.BLACK, i, j] = 0;

//                    /* squares constituting the ring around both kings */
//                    if (j == i + ec.NORTH || j == i + ec.SOUTH
//                                          || j == i + ec.EAST || j == i + ec.WEST
//                                          || j == i + ec.NW || j == i + ec.NE
//                                          || j == i + ec.SW || j == i + ec.SE)
//                    {

//                        sqNearK[ec.WHITE, i, j] = 1;
//                        sqNearK[ec.BLACK, i, j] = 1;
//                    }

//                    /* squares in front of the white king ring */
//                    if (j == i + ec.NORTH + ec.NORTH
//                        || j == i + ec.NORTH + ec.NE
//                        || j == i + ec.NORTH + ec.NW)
//                        sqNearK[ec.WHITE, i, j] = 1;

//                    /* squares in front og the black king ring */
//                    if (j == i + ec.SOUTH + ec.SOUTH
//                        || j == i + ec.SOUTH + ec.SE
//                        || j == i + ec.SOUTH + ec.SW)
//                        sqNearK[ec.WHITE, i, j] = 1; // TODO: BLACK??
//                }
//            }
//        }

//        void setPcsq()
//        {

//            for (int i = 0; i < 64; ++i)
//            {

//                weak_pawn[ec.WHITE, i] = weak_pawn_pcsq[i];
//                weak_pawn[ec.BLACK, i] = weak_pawn_pcsq[i];
//                passed_pawn[ec.WHITE, i] = passed_pawn_pcsq[i];
//                passed_pawn[ec.BLACK, i] = passed_pawn_pcsq[i];

//                /* protected passers are slightly stronger than ordinary passers */

//                protected_passer[ec.WHITE, i] = (passed_pawn_pcsq[i] * 10) / 8;
//                protected_passer[ec.BLACK, i] = (passed_pawn_pcsq[i] * 10) / 8;

//                /* now set the piece/square tables for each color and piece type */

//                mgPst[ec.PAWN, ec.WHITE, i] = pawn_pcsq_mg[i];
//                mgPst[ec.PAWN, ec.BLACK, i] = pawn_pcsq_mg[i];
//                mgPst[ec.KNIGHT, ec.WHITE, i] = knight_pcsq_mg[i];
//                mgPst[ec.KNIGHT, ec.BLACK, i] = knight_pcsq_mg[i];
//                mgPst[ec.BISHOP, ec.WHITE, i] = bishop_pcsq_mg[i];
//                mgPst[ec.BISHOP, ec.BLACK, i] = bishop_pcsq_mg[i];
//                mgPst[ec.ROOK, ec.WHITE, i] = rook_pcsq_mg[i];
//                mgPst[ec.ROOK, ec.BLACK, i] = rook_pcsq_mg[i];
//                mgPst[ec.QUEEN, ec.WHITE, i] = queen_pcsq_mg[i];
//                mgPst[ec.QUEEN, ec.BLACK, i] = queen_pcsq_mg[i];
//                mgPst[ec.KING, ec.WHITE, i] = king_pcsq_mg[i];
//                mgPst[ec.KING, ec.BLACK, i] = king_pcsq_mg[i];

//                egPst[ec.PAWN, ec.WHITE, i] = pawn_pcsq_eg[i];
//                egPst[ec.PAWN, ec.BLACK, i] = pawn_pcsq_eg[i];
//                egPst[ec.KNIGHT, ec.WHITE, i] = knight_pcsq_eg[i];
//                egPst[ec.KNIGHT, ec.BLACK, i] = knight_pcsq_eg[i];
//                egPst[ec.BISHOP, ec.WHITE, i] = bishop_pcsq_eg[i];
//                egPst[ec.BISHOP, ec.BLACK, i] = bishop_pcsq_eg[i];
//                egPst[ec.ROOK, ec.WHITE, i] = rook_pcsq_eg[i];
//                egPst[ec.ROOK, ec.BLACK, i] = rook_pcsq_eg[i];
//                egPst[ec.QUEEN, ec.WHITE, i] = queen_pcsq_eg[i];
//                egPst[ec.QUEEN, ec.BLACK, i] = queen_pcsq_eg[i];
//                egPst[ec.KING, ec.WHITE, i] = king_pcsq_eg[i];
//                egPst[ec.KING, ec.BLACK, i] = king_pcsq_eg[i];
//            }
//        }

//        void correctValues()
//        {
//            if (PIECE_VALUE[ec.BISHOP] == PIECE_VALUE[ec.KNIGHT])
//                ++PIECE_VALUE[ec.BISHOP];
//        }
//    }

//    class EvaluationBoard
//    {
//        //U8 pieces[128];
//        byte[] color = new byte[64];
//        //char stm;        // side to move: 0 = white,  1 = black
//        //char castle;     // 1 = shortW, 2 = longW, 4 = shortB, 8 = longB
//        //char ep;         // en passant square
//        //U8 ply;
//        //U64 hash;
//        //U64 phash;
//        //int rep_index;
//        //U64 rep_stack[1024];
//        //S8 king_loc[2];
//        public int[] pcsq_mg = new int[2];
//        public int[] pcsq_eg = new int[2];
//        public int[] piece_material = new int[2];
//        public int[] pawn_material = new int[2];
//        public byte[,] piece_cnt = new byte[2, 6];
//        public byte[,] pawns_on_file = new byte[2, 8];
//        public byte[,] pawns_on_rank = new byte[2, 8];
//        public byte[,] pawn_ctrl = new byte[2, 64];

//        private readonly s_eval_data e;

//        public EvaluationBoard(s_eval_data evalData)
//        {
//            e = evalData;
//        }

//        public void Fill(Board board)
//        {
//            for (int i = 0; i < 64; i++)
//            {
//                var piece = board.ArrayBoard[i];
//                (var color, var convertedPiece) = ec.GetColorAndPiece(piece);
//                fillSq(color, convertedPiece, i);
//            }
//        }

//        void fillSq(byte color, byte piece, int sq)
//        {

//            // place a piece on the board
//            //b.pieces[sq] = piece;
//            this.color[sq] = color;

//            // update king location
//            //if (piece == KING)
//            //    b.king_loc[color] = sq;

//            /**************************************************************************
//            * Pawn structure changes slower than piece position, which allows reusing *
//            * some data, both in pawn and piece evaluation. For that reason we do     *
//            * some extra work here, expecting to gain extra speed elsewhere.          *
//            **************************************************************************/

//            if (piece == ec.PAWN)
//            {
//                // update pawn material
//                pawn_material[color] += e.PIECE_VALUE[piece];

//                // update pawn hashkey
//                //b.phash ^= zobrist.piecesquare[piece][color][sq];

//                // update counter of pawns on a given rank and file
//                var col = sq & 7;
//                var row = sq >> 3;
//                ++pawns_on_file[color, col];
//                ++pawns_on_rank[color, row];

//                // update squares controlled by pawns
//                if (color == ec.WHITE)
//                {
//                    if (col < 7) pawn_ctrl[ec.WHITE, sq + ec.NE]++;
//                    if (col > 0) pawn_ctrl[ec.WHITE, sq + ec.NW]++;
//                }
//                else
//                {
//                    if (col < 7) pawn_ctrl[ec.BLACK, sq + ec.SE]++;
//                    if (col > 0) pawn_ctrl[ec.BLACK, sq + ec.SW]++;
//                }
//            }
//            else
//            {
//                // update piece material
//                piece_material[color] += e.PIECE_VALUE[piece];
//            }

//            // update piece counter
//            piece_cnt[color, piece]++;

//            // update piece-square value
//            pcsq_mg[color] += e.mgPst[piece, color, sq];
//            pcsq_eg[color] += e.egPst[piece, color, sq];

//            // update hash key
//            //b.hash ^= zobrist.piecesquare[piece][color][sq];
//        }
//    }

//    class EvaluationService2
//    {
//        int eval(Board b, int alpha, int beta, int use_hash)
//        {
//            var v = new eval_vector();
//            var e = new s_eval_data();
//            var eb = new EvaluationBoard(e);
//            eb.Fill(b);

//            int result = 0, mgScore = 0, egScore = 0;
//            int stronger, weaker;

//            /**************************************************************************
//            *  Clear all eval data                                                    *
//            **************************************************************************/

//            v.gamePhase = b.PieceCounts[ChessPiece.WhiteKnight] + b.PieceCounts[ChessPiece.WhiteBishop] + 2 * b.PieceCounts[ChessPiece.WhiteRook] + 4 * b.PieceCounts[ChessPiece.WhiteQueen]
//                        + b.PieceCounts[ChessPiece.BlackKnight] + b.PieceCounts[ChessPiece.BlackBishop] + 2 * b.PieceCounts[ChessPiece.BlackRook] + 4 * b.PieceCounts[ChessPiece.BlackQueen];

//            for (int side = 0; side <= 1; side++)
//            {
//                v.mgMob[side] = 0;
//                v.egMob[side] = 0;
//                v.attCnt[side] = 0;
//                v.attWeight[side] = 0;
//                v.mgTropism[side] = 0;
//                v.egTropism[side] = 0;
//                v.adjustMaterial[side] = 0;
//                v.blockages[side] = 0;
//                v.positionalThemes[side] = 0;
//                v.kingShield[side] = 0;
//            }

//            /************************************************************************** 
//            *  Sum the incrementally counted material and piece/square table values   *
//            **************************************************************************/

//            mgScore = eb.piece_material[ec.WHITE] + eb.pawn_material[ec.WHITE] + eb.pcsq_mg[ec.WHITE]
//                    - eb.piece_material[ec.BLACK] - eb.pawn_material[ec.BLACK] - eb.pcsq_mg[ec.BLACK];
//            egScore = eb.piece_material[ec.WHITE] + eb.pawn_material[ec.WHITE] + eb.pcsq_eg[ec.WHITE]
//                    - eb.piece_material[ec.BLACK] - eb.pawn_material[ec.BLACK] - eb.pcsq_eg[ec.BLACK];

//            /************************************************************************** 
//            * add king's pawn shield score and evaluate part of piece blockage score  *
//            * (the rest of the latter will be done via piece eval)                    *
//            **************************************************************************/

//            v.kingShield[ec.WHITE] = wKingShield(b, e);
//            v.kingShield[ec.BLACK] = bKingShield(b, e);
//            blockedPieces(WHITE);
//            blockedPieces(BLACK);
//            mgScore += (v.kingShield[WHITE] - v.kingShield[BLACK]);

//            /* tempo bonus */
//            if (b.WhiteToMove) result += e.TEMPO;
//            else result -= e.TEMPO;

//            /**************************************************************************
//            *  Adjusting material value for the various combinations of pieces.       *
//            *  Currently it scores bishop, knight and rook pairs. The first one       *
//            *  gets a bonus, the latter two - a penalty. Beside that knights lose     *
//            *  value as pawns disappear, whereas rooks gain.                          *
//            **************************************************************************/

//            if (eb.piece_cnt[WHITE, BISHOP] > 1) v.adjustMaterial[WHITE] += e.BISHOP_PAIR;
//            if (b.piece_cnt[BLACK][BISHOP] > 1) v.adjustMaterial[BLACK] += e.BISHOP_PAIR;
//            if (b.piece_cnt[WHITE][KNIGHT] > 1) v.adjustMaterial[WHITE] -= e.P_KNIGHT_PAIR;
//            if (b.piece_cnt[BLACK][KNIGHT] > 1) v.adjustMaterial[BLACK] -= e.P_KNIGHT_PAIR;
//            if (b.piece_cnt[WHITE][ROOK] > 1) v.adjustMaterial[WHITE] -= e.P_ROOK_PAIR;
//            if (b.piece_cnt[BLACK][ROOK] > 1) v.adjustMaterial[BLACK] -= e.P_ROOK_PAIR;

//            v.adjustMaterial[WHITE] += n_adj[b.piece_cnt[WHITE][PAWN]] * b.piece_cnt[WHITE][KNIGHT];
//            v.adjustMaterial[BLACK] += n_adj[b.piece_cnt[BLACK][PAWN]] * b.piece_cnt[BLACK][KNIGHT];
//            v.adjustMaterial[WHITE] += r_adj[b.piece_cnt[WHITE][PAWN]] * b.piece_cnt[WHITE][ROOK];
//            v.adjustMaterial[BLACK] += r_adj[b.piece_cnt[BLACK][PAWN]] * b.piece_cnt[BLACK][ROOK];

//            result += getPawnScore();

//            /**************************************************************************
//            *  Evaluate pieces                                                        *
//            **************************************************************************/

//            for (U8 row = 0; row < 8; row++)
//                for (U8 col = 0; col < 8; col++)
//                {

//                    S8 sq = SET_SQ(row, col);

//                    if (b.color[sq] != COLOR_EMPTY)
//                    {
//                        switch (b.pieces[sq])
//                        {
//                            case PAWN: // pawns are evaluated separately
//                                break;
//                            case KNIGHT:
//                                EvalKnight(sq, b.color[sq]);
//                                break;
//                            case BISHOP:
//                                EvalBishop(sq, b.color[sq]);
//                                break;
//                            case ROOK:
//                                EvalRook(sq, b.color[sq]);
//                                break;
//                            case QUEEN:
//                                EvalQueen(sq, b.color[sq]);
//                                break;
//                            case KING:
//                                break;
//                        }
//                    }
//                }

//            /**************************************************************************
//            *  Merge  midgame  and endgame score. We interpolate between  these  two  *
//            *  values, using a gamePhase value, based on remaining piece material on  *
//            *  both sides. With less pieces, endgame score becomes more influential.  *
//            **************************************************************************/

//            mgScore += (v.mgMob[WHITE] - v.mgMob[BLACK]);
//            egScore += (v.egMob[WHITE] - v.egMob[BLACK]);
//            mgScore += (v.mgTropism[WHITE] - v.mgTropism[BLACK]);
//            egScore += (v.egTropism[WHITE] - v.egTropism[BLACK]);
//            if (v.gamePhase > 24) v.gamePhase = 24;
//            int mgWeight = v.gamePhase;
//            int egWeight = 24 - mgWeight;
//            result += ((mgScore * mgWeight) + (egScore * egWeight)) / 24;

//            /**************************************************************************
//            *  Add phase-independent score components.                                *
//            **************************************************************************/

//            result += (v.blockages[WHITE] - v.blockages[BLACK]);
//            result += (v.positionalThemes[WHITE] - v.positionalThemes[BLACK]);
//            result += (v.adjustMaterial[WHITE] - v.adjustMaterial[BLACK]);

//            /**************************************************************************
//            *  Merge king attack score. We don't apply this value if there are less   *
//            *  than two attackers or if the attacker has no queen.                    *
//            **************************************************************************/

//            if (v.attCnt[WHITE] < 2 || b.piece_cnt[WHITE][QUEEN] == 0) v.attWeight[WHITE] = 0;
//            if (v.attCnt[BLACK] < 2 || b.piece_cnt[BLACK][QUEEN] == 0) v.attWeight[BLACK] = 0;
//            result += SafetyTable[v.attWeight[WHITE]];
//            result -= SafetyTable[v.attWeight[BLACK]];

//            /**************************************************************************
//            *  Low material correction - guarding against an illusory material advan- *
//            *  tage. Full blown program should have more such rules, but the current  *
//            *  set ought to be useful enough. Please note that our code  assumes      *
//            *  different material values for bishop and  knight.                      *
//            *                                                                         *
//            *  - a single minor piece cannot win                                      *
//            *  - two knights cannot checkmate bare king                               *
//            *  - bare rook vs minor piece is drawish                                  *
//            *  - rook and minor vs rook is drawish                                    *
//            **************************************************************************/

//            if (result > 0)
//            {
//                stronger = WHITE;
//                weaker = BLACK;
//            }
//            else
//            {
//                stronger = BLACK;
//                weaker = WHITE;
//            }

//            if (b.pawn_material[stronger] == 0)
//            {

//                if (b.piece_material[stronger] < 400) return 0;

//                if (b.pawn_material[weaker] == 0
//                        && (b.piece_material[stronger] == 2 * e.PIECE_VALUE[KNIGHT]))
//                    return 0;

//                if (b.piece_material[stronger] == e.PIECE_VALUE[ROOK]
//                        && b.piece_material[weaker] == e.PIECE_VALUE[BISHOP]) result /= 2;

//                if (b.piece_material[stronger] == e.PIECE_VALUE[ROOK]
//                        && b.piece_material[weaker] == e.PIECE_VALUE[BISHOP]) result /= 2;

//                if (b.piece_material[stronger] == e.PIECE_VALUE[ROOK] + e.PIECE_VALUE[BISHOP]
//                        && b.piece_material[stronger] == e.PIECE_VALUE[ROOK]) result /= 2;

//                if (b.piece_material[stronger] == e.PIECE_VALUE[ROOK] + e.PIECE_VALUE[KNIGHT]
//                        && b.piece_material[stronger] == e.PIECE_VALUE[ROOK]) result /= 2;
//            }

//            /**************************************************************************
//            *  Finally return the score relative to the side to move.                 *
//            **************************************************************************/

//            if (b.stm == BLACK) result = -result;

//            tteval_save(result);

//            return result;
//        }

//        int wKingShield(Board b, s_eval_data e)
//        {
//            int result = 0;
//            var kingBitboard = b.BitBoard[ChessPiece.WhiteKing];
//            var kingPos = kingBitboard.BitScanForward();
//            var col = kingPos & 7;

//            /* king on the kingside */
//            if (col > ChessFile.E)
//            {
//                if (b.ArrayBoard[ChessPosition.F2] == ChessPiece.WhitePawn) result += e.SHIELD_2;
//                else if (b.ArrayBoard[ChessPosition.F2] == ChessPiece.WhitePawn) result += e.SHIELD_3;

//                if (b.ArrayBoard[ChessPosition.G2] == ChessPiece.WhitePawn) result += e.SHIELD_2;
//                else if (b.ArrayBoard[ChessPosition.G3] == ChessPiece.WhitePawn) result += e.SHIELD_3;

//                if (b.ArrayBoard[ChessPosition.H2] == ChessPiece.WhitePawn) result += e.SHIELD_2;
//                else if (b.ArrayBoard[ChessPosition.H3] == ChessPiece.WhitePawn) result += e.SHIELD_3;
//            }

//            /* king on the queenside */
//            else if (col < ChessFile.D)
//            {

//                if (b.ArrayBoard[ChessPosition.A2] == ChessPiece.WhitePawn) result += e.SHIELD_2;
//                else if (b.ArrayBoard[ChessPosition.A3] == ChessPiece.WhitePawn) result += e.SHIELD_3;

//                if (b.ArrayBoard[ChessPosition.B2] == ChessPiece.WhitePawn) result += e.SHIELD_2;
//                else if (b.ArrayBoard[ChessPosition.B3] == ChessPiece.WhitePawn) result += e.SHIELD_3;

//                if (b.ArrayBoard[ChessPosition.C2] == ChessPiece.WhitePawn) result += e.SHIELD_2;
//                else if (b.ArrayBoard[ChessPosition.C3] == ChessPiece.WhitePawn) result += e.SHIELD_3;
//            }

//            return result;
//        }

//        int bKingShield(Board b, s_eval_data e)
//        {
//            int result = 0;
//            var kingBitboard = b.BitBoard[ChessPiece.BlackKing];
//            var kingPos = kingBitboard.BitScanForward();
//            var col = kingPos & 7;

//            /* king on the kingside */
//            if (col > ChessFile.E)
//            {
//                if (b.ArrayBoard[ChessPosition.F7] == ChessPiece.BlackPawn) result += e.SHIELD_2;
//                else if (b.ArrayBoard[ChessPosition.F6] == ChessPiece.BlackPawn) result += e.SHIELD_3;

//                if (b.ArrayBoard[ChessPosition.G7] == ChessPiece.BlackPawn) result += e.SHIELD_2;
//                else if (b.ArrayBoard[ChessPosition.G6] == ChessPiece.BlackPawn) result += e.SHIELD_3;

//                if (b.ArrayBoard[ChessPosition.H7] == ChessPiece.BlackPawn) result += e.SHIELD_2;
//                else if (b.ArrayBoard[ChessPosition.H6] == ChessPiece.BlackPawn) result += e.SHIELD_3;
//            }

//            /* king on the queenside */
//            else if (col < ChessFile.D)
//            {
//                if (b.ArrayBoard[ChessPosition.A7] == ChessPiece.BlackPawn) result += e.SHIELD_2;
//                else if (b.ArrayBoard[ChessPosition.A6] == ChessPiece.BlackPawn) result += e.SHIELD_3;

//                if (b.ArrayBoard[ChessPosition.B7] == ChessPiece.BlackPawn) result += e.SHIELD_2;
//                else if (b.ArrayBoard[ChessPosition.B6] == ChessPiece.BlackPawn) result += e.SHIELD_3;

//                if (b.ArrayBoard[ChessPosition.C7] == ChessPiece.BlackPawn) result += e.SHIELD_2;
//                else if (b.ArrayBoard[ChessPosition.C6] == ChessPiece.BlackPawn) result += e.SHIELD_3;
//            }
//            return result;
//        }

//        static int[] inv_sq = new int[64] {
//            ChessPosition.A8, ChessPosition.B8, ChessPosition.C8, ChessPosition.D8, ChessPosition.E8, ChessPosition.F8, ChessPosition.G8, ChessPosition.H8,
//            ChessPosition.A7, ChessPosition.B7, ChessPosition.C7, ChessPosition.D7, ChessPosition.E7, ChessPosition.F7, ChessPosition.G7, ChessPosition.H7,
//            ChessPosition.A6, ChessPosition.B6, ChessPosition.C6, ChessPosition.D6, ChessPosition.E6, ChessPosition.F6, ChessPosition.G6, ChessPosition.H6,
//            ChessPosition.A5, ChessPosition.B5, ChessPosition.C5, ChessPosition.D5, ChessPosition.E5, ChessPosition.F5, ChessPosition.G5, ChessPosition.H5,
//            ChessPosition.A4, ChessPosition.B4, ChessPosition.C4, ChessPosition.D4, ChessPosition.E4, ChessPosition.F4, ChessPosition.G4, ChessPosition.H4,
//            ChessPosition.A3, ChessPosition.B3, ChessPosition.C3, ChessPosition.D3, ChessPosition.E3, ChessPosition.F3, ChessPosition.G3, ChessPosition.H3,
//            ChessPosition.A2, ChessPosition.B2, ChessPosition.C2, ChessPosition.D2, ChessPosition.E2, ChessPosition.F2, ChessPosition.G2, ChessPosition.H2,
//            ChessPosition.A1, ChessPosition.B1, ChessPosition.C1, ChessPosition.D1, ChessPosition.E1, ChessPosition.F1, ChessPosition.G1, ChessPosition.H1,
//        };

//        static bool isPiece(Board b, int color, int piece, int position)
//        {
//            var p = b.ArrayBoard[position];
//            switch (p)
//            {
//                case ChessPiece.WhitePawn: return color == ec.WHITE && piece == ec.PAWN;
//                case ChessPiece.WhiteKnight: return color == ec.WHITE && piece == ec.KNIGHT;
//                case ChessPiece.WhiteBishop: return color == ec.WHITE && piece == ec.BISHOP;
//                case ChessPiece.WhiteRook: return color == ec.WHITE && piece == ec.ROOK;
//                case ChessPiece.WhiteQueen: return color == ec.WHITE && piece == ec.QUEEN;
//                case ChessPiece.WhiteKing: return color == ec.WHITE && piece == ec.KING;

//                case ChessPiece.BlackPawn: return color == ec.BLACK && piece == ec.PAWN;
//                case ChessPiece.BlackKnight: return color == ec.BLACK && piece == ec.KNIGHT;
//                case ChessPiece.BlackBishop: return color == ec.BLACK && piece == ec.BISHOP;
//                case ChessPiece.BlackRook: return color == ec.BLACK && piece == ec.ROOK;
//                case ChessPiece.BlackQueen: return color == ec.BLACK && piece == ec.QUEEN;
//                case ChessPiece.BlackKing: return color == ec.BLACK && piece == ec.KING;
//            }

//            return false;
//        }

//        int REL_SQ(int cl, int sq)
//        {
//            return ((cl) == (ec.WHITE) ? (sq) : (inv_sq[sq]));
//        }

//        void blockedPieces(Board b, s_eval_data e, eval_vector v, EvaluationBoard eb, int side)
//        {

//            int oppo = side == 0 ? 1 : 0;

//            // central pawn blocked, bishop hard to develop
//            if (isPiece(side, ec.BISHOP, REL_SQ(side, ChessPosition.C1))
//            && isPiece(side, ec.PAWN, REL_SQ(side, ChessPosition.D2))
//            && eb.color[REL_SQ(side, ChessPosition.D3)] != COLOR_EMPTY)
//                v.blockages[side] -= e.P_BLOCK_CENTRAL_PAWN;

//            if (isPiece(side, BISHOP, REL_SQ(side, ChessPosition.F1))
//            && isPiece(side, PAWN, REL_SQ(side, ChessPosition.E2))
//            && b.color[REL_SQ(side, ChessPosition.E3)] != COLOR_EMPTY)
//                v.blockages[side] -= e.P_BLOCK_CENTRAL_PAWN;

//            // trapped knight
//            if (isPiece(side, KNIGHT, REL_SQ(side, ChessPosition.A8))
//            && (isPiece(oppo, PAWN, REL_SQ(side, ChessPosition.A7)) || isPiece(oppo, PAWN, REL_SQ(side, ChessPosition.C7))))
//                v.blockages[side] -= e.P_KNIGHT_TRAPPED_A8;

//            if (isPiece(side, KNIGHT, REL_SQ(side, H8))
//            && (isPiece(oppo, PAWN, REL_SQ(side, H7)) || isPiece(oppo, PAWN, REL_SQ(side, F7))))
//                v.blockages[side] -= e.P_KNIGHT_TRAPPED_A8;

//            if (isPiece(side, KNIGHT, REL_SQ(side, A7))
//            && isPiece(oppo, PAWN, REL_SQ(side, A6))
//            && isPiece(oppo, PAWN, REL_SQ(side, B7)))
//                v.blockages[side] -= e.P_KNIGHT_TRAPPED_A7;

//            if (isPiece(side, KNIGHT, REL_SQ(side, H7))
//            && isPiece(oppo, PAWN, REL_SQ(side, H6))
//            && isPiece(oppo, PAWN, REL_SQ(side, G7)))
//                v.blockages[side] -= e.P_KNIGHT_TRAPPED_A7;

//            // knight blocking queenside pawns
//            if (isPiece(side, KNIGHT, REL_SQ(side, C3))
//            && isPiece(side, PAWN, REL_SQ(side, C2))
//            && isPiece(side, PAWN, REL_SQ(side, D4))
//            && !isPiece(side, PAWN, REL_SQ(side, E4)))
//                v.blockages[side] -= e.P_C3_KNIGHT;

//            // trapped bishop
//            if (isPiece(side, BISHOP, REL_SQ(side, A7))
//            && isPiece(oppo, PAWN, REL_SQ(side, B6)))
//                v.blockages[side] -= e.P_BISHOP_TRAPPED_A7;

//            if (isPiece(side, BISHOP, REL_SQ(side, H7))
//            && isPiece(oppo, PAWN, REL_SQ(side, G6)))
//                v.blockages[side] -= e.P_BISHOP_TRAPPED_A7;

//            if (isPiece(side, BISHOP, REL_SQ(side, B8))
//            && isPiece(oppo, PAWN, REL_SQ(side, C7)))
//                v.blockages[side] -= e.P_BISHOP_TRAPPED_A7;

//            if (isPiece(side, BISHOP, REL_SQ(side, G8))
//            && isPiece(oppo, PAWN, REL_SQ(side, F7)))
//                v.blockages[side] -= e.P_BISHOP_TRAPPED_A7;

//            if (isPiece(side, BISHOP, REL_SQ(side, A6))
//            && isPiece(oppo, PAWN, REL_SQ(side, B5)))
//                v.blockages[side] -= e.P_BISHOP_TRAPPED_A6;

//            if (isPiece(side, BISHOP, REL_SQ(side, H6))
//            && isPiece(oppo, PAWN, REL_SQ(side, G5)))
//                v.blockages[side] -= e.P_BISHOP_TRAPPED_A6;

//            // bishop on initial sqare supporting castled king
//            if (isPiece(side, BISHOP, REL_SQ(side, F1))
//            && isPiece(side, KING, REL_SQ(side, G1)))
//                v.positionalThemes[side] += e.RETURNING_BISHOP;

//            if (isPiece(side, BISHOP, REL_SQ(side, C1))
//            && isPiece(side, KING, REL_SQ(side, B1)))
//                v.positionalThemes[side] += e.RETURNING_BISHOP;

//            // uncastled king blocking own rook
//            if ((isPiece(side, KING, REL_SQ(side, F1)) || isPiece(side, KING, REL_SQ(side, G1)))
//            && (isPiece(side, ROOK, REL_SQ(side, H1)) || isPiece(side, ROOK, REL_SQ(side, G1))))
//                v.blockages[side] -= e.P_KING_BLOCKS_ROOK;

//            if ((isPiece(side, KING, REL_SQ(side, C1)) || isPiece(side, KING, REL_SQ(side, B1)))
//            && (isPiece(side, ROOK, REL_SQ(side, A1)) || isPiece(side, ROOK, REL_SQ(side, B1))))
//                v.blockages[side] -= e.P_KING_BLOCKS_ROOK;
//        }
//    }
//}
