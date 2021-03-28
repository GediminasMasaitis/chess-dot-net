using System;
using System.Text;
using ChessDotNet.Data;
using ChessDotNet.Hashing;
using ChessDotNet.MoveGeneration.SlideGeneration;
using Score = System.Int32;
using Position = System.Byte;
using Piece = System.Byte;

namespace ChessDotNet.Evaluation.V2
{
    public class EvaluationService2 : IEvaluationService
    {
        private readonly EvaluationData _e;
        private readonly EvalHashTable _evalTable;

        public EvaluationService2(EvaluationData e)
        {
            _e = e;
            _evalTable = new EvalHashTable();
            _evalTable.SetSize(16 * 1024 * 1024);
        }

        public int Evaluate(Board board)
        {
            //var key = ZobristKeys.CalculateKey(board);
            var success = _evalTable.TryProbe(board.Key, out var hashScore);
            if (success)
            {
                return hashScore;
            }

            var v = new EvaluationScores();
            var eb = new EvaluationBoard(_e);
            eb.Fill(board);
            var score = eval(board, v, eb);
            _evalTable.Store(board.Key, score);
            //printEval(board, e, eb, v, score);
            return score;
        }

        private int eval(Board b, EvaluationScores v, EvaluationBoard eb)
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

            v.kingShield[ChessPiece.White] = wKingShield(b);
            v.kingShield[ChessPiece.Black] = bKingShield(b);
            blockedPieces(b, v, eb, ChessPiece.White);
            blockedPieces(b, v, eb, ChessPiece.Black);
            mgScore += (v.kingShield[ChessPiece.White] - v.kingShield[ChessPiece.Black]);

            /* tempo bonus */
            if (b.WhiteToMove) result += EvaluationData.TEMPO;
            else result -= EvaluationData.TEMPO;

            /**************************************************************************
            *  Adjusting material value for the various combinations of pieces.       *
            *  Currently it scores bishop, knight and rook pairs. The first one       *
            *  gets a bonus, the latter two - a penalty. Beside that knights lose     *
            *  value as pawns disappear, whereas rooks gain.                          *
            **************************************************************************/

            if (b.PieceCounts[ChessPiece.WhiteBishop] > 1) v.adjustMaterial[ChessPiece.White] += EvaluationData.BISHOP_PAIR;
            if (b.PieceCounts[ChessPiece.BlackBishop] > 1) v.adjustMaterial[ChessPiece.Black] += EvaluationData.BISHOP_PAIR;
            if (b.PieceCounts[ChessPiece.WhiteKnight] > 1) v.adjustMaterial[ChessPiece.White] -= EvaluationData.P_KNIGHT_PAIR;
            if (b.PieceCounts[ChessPiece.BlackKnight] > 1) v.adjustMaterial[ChessPiece.Black] -= EvaluationData.P_KNIGHT_PAIR;
            if (b.PieceCounts[ChessPiece.WhiteRook] > 1) v.adjustMaterial[ChessPiece.White] -= EvaluationData.P_ROOK_PAIR;
            if (b.PieceCounts[ChessPiece.BlackRook] > 1) v.adjustMaterial[ChessPiece.Black] -= EvaluationData.P_ROOK_PAIR;

            v.adjustMaterial[ChessPiece.White] += _e.n_adj[b.PieceCounts[ChessPiece.WhitePawn]] * b.PieceCounts[ChessPiece.WhiteKnight];
            v.adjustMaterial[ChessPiece.Black] += _e.n_adj[b.PieceCounts[ChessPiece.BlackPawn]] * b.PieceCounts[ChessPiece.BlackKnight];
            v.adjustMaterial[ChessPiece.White] += _e.r_adj[b.PieceCounts[ChessPiece.WhitePawn]] * b.PieceCounts[ChessPiece.WhiteRook];
            v.adjustMaterial[ChessPiece.Black] += _e.r_adj[b.PieceCounts[ChessPiece.BlackPawn]] * b.PieceCounts[ChessPiece.BlackRook];

            var pawnScore = getPawnScore(b, eb);
            result += pawnScore;

            /**************************************************************************
            *  Evaluate pieces                                                        *
            **************************************************************************/

            for (Piece sq = 0; sq < 64; sq++)
            {
                var originalPiece = b.ArrayBoard[sq];
                if (originalPiece != ChessPiece.Empty)
                {
                    (var color, var piece) = EvaluationData.GetColorAndPiece(originalPiece);
                    switch (piece)
                    {
                        case EvaluationData.PAWN: // pawns are evaluated separately
                            break;
                        case EvaluationData.KNIGHT:
                            EvalKnight(b, eb, v, sq, color);
                            break;
                        case EvaluationData.BISHOP:
                            EvalBishop(b, eb, v, sq, color);
                            break;
                        case EvaluationData.ROOK:
                            EvalRook(b, eb, v, sq, color);
                            break;
                        case EvaluationData.QUEEN:
                            EvalQueen(b, eb, v, sq, color);
                            break;
                        case EvaluationData.KING:
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
            if (v.gamePhase > 24)
            {
                v.gamePhase = 24;
            }
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
            result += EvaluationData.SafetyTable[v.attWeight[ChessPiece.White]];
            result -= EvaluationData.SafetyTable[v.attWeight[ChessPiece.Black]];

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

                if (eb.pawn_material[weaker] == 0 && (eb.piece_material[stronger] == 2 * _e.PIECE_VALUE[EvaluationData.KNIGHT]))
                {
                    return 0;
                }

                if
                (
                    eb.piece_material[stronger] == _e.PIECE_VALUE[EvaluationData.ROOK]
                    && eb.piece_material[weaker] == _e.PIECE_VALUE[EvaluationData.BISHOP]
                )
                {
                    result /= 2;
                }

                if
                (
                    eb.piece_material[stronger] == _e.PIECE_VALUE[EvaluationData.ROOK]
                    && eb.piece_material[weaker] == _e.PIECE_VALUE[EvaluationData.BISHOP]
                )
                {
                    result /= 2;
                }

                if
                (
                    eb.piece_material[stronger] == _e.PIECE_VALUE[EvaluationData.ROOK] + _e.PIECE_VALUE[EvaluationData.BISHOP]
                    && eb.piece_material[stronger] == _e.PIECE_VALUE[EvaluationData.ROOK]
                )
                {
                    result /= 2;
                }

                if
                (
                    eb.piece_material[stronger] == _e.PIECE_VALUE[EvaluationData.ROOK] + _e.PIECE_VALUE[EvaluationData.KNIGHT]
                    && eb.piece_material[stronger] == _e.PIECE_VALUE[EvaluationData.ROOK]
                )
                {
                    result /= 2;
                }
            }

            /**************************************************************************
            *  Finally return the score relative to the side to move.                 *
            **************************************************************************/

            if (b.ColorToMove == ChessPiece.Black) result = -result;

            //tteval_save(result);
            return result;
        }

        void EvalKnight(Board b, EvaluationBoard eb, EvaluationScores v, Position sq, Piece side)
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

                    if (_e.sqNearK[side ^ 1, b.KingPositions[side ^ 1], pos] != 0)
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

        void EvalBishop(Board b, EvaluationBoard eb, EvaluationScores v, Position sq, Piece side)
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
                    if (_e.sqNearK[side ^ 1, b.KingPositions[side ^ 1], pos] != 0)
                    {
                        ++att;
                    }
                }
                else if(color != side)
                {
                    mob++;
                    if (_e.sqNearK[side ^ 1, b.KingPositions[side ^ 1], pos] != 0)
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

        void EvalRook(Board b, EvaluationBoard eb, EvaluationScores v, Position sq, Piece side)
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
                sqRow == EvaluationData.seventh[side]
                && (eb.pawns_on_rank[side ^ 1, EvaluationData.seventh[side]] > 0 || (b.KingPositions[side ^ 1]) == EvaluationData.eighth[side])
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
                    v.mgMob[side] += EvaluationData.ROOK_OPEN;
                    v.egMob[side] += EvaluationData.ROOK_OPEN;
                    if (Math.Abs(sqCol - (b.KingPositions[side ^ 1] & 7)) < 2)
                    {
                        v.attWeight[side] += 1;
                    }
                }
                else
                {                                    // half open file
                    v.mgMob[side] += EvaluationData.ROOK_HALF;
                    v.egMob[side] += EvaluationData.ROOK_HALF;
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
                    if (_e.sqNearK[side ^ 1, b.KingPositions[side ^ 1], pos] != 0)
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

        void EvalQueen(Board b, EvaluationBoard eb, EvaluationScores v, Position sq, Piece side)
        {

            int att = 0;
            int mob = 0;

            var sqCol = sq & 7;
            var sqRow = sq >> 3;

            if
            (
                sqRow == EvaluationData.seventh[side]
                && (eb.pawns_on_rank[side ^ 1, EvaluationData.seventh[side]] > 0 || (b.KingPositions[side ^ 1]) == EvaluationData.eighth[side])
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
                if (isPiece(b, side, EvaluationData.KNIGHT, REL_SQ(side, ChessPosition.B1))) v.positionalThemes[side] -= 2;
                if (isPiece(b, side, EvaluationData.BISHOP, REL_SQ(side, ChessPosition.C1))) v.positionalThemes[side] -= 2;
                if (isPiece(b, side, EvaluationData.BISHOP, REL_SQ(side, ChessPosition.F1))) v.positionalThemes[side] -= 2;
                if (isPiece(b, side, EvaluationData.KNIGHT, REL_SQ(side, ChessPosition.G1))) v.positionalThemes[side] -= 2;
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
                    if (_e.sqNearK[side ^ 1, b.KingPositions[side ^ 1], pos] != 0)
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

        int wKingShield(Board b)
        {
            int result = 0;
            var kingBitboard = b.BitBoard[ChessPiece.WhiteKing];
            var kingPos = kingBitboard.BitScanForward();
            var col = kingPos & 7;

            /* king on the kingside */
            if (col > ChessFile.E)
            {
                if (b.ArrayBoard[ChessPosition.F2] == ChessPiece.WhitePawn) result += EvaluationData.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.F2] == ChessPiece.WhitePawn) result += EvaluationData.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.G2] == ChessPiece.WhitePawn) result += EvaluationData.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.G3] == ChessPiece.WhitePawn) result += EvaluationData.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.H2] == ChessPiece.WhitePawn) result += EvaluationData.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.H3] == ChessPiece.WhitePawn) result += EvaluationData.SHIELD_3;
            }

            /* king on the queenside */
            else if (col < ChessFile.D)
            {

                if (b.ArrayBoard[ChessPosition.A2] == ChessPiece.WhitePawn) result += EvaluationData.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.A3] == ChessPiece.WhitePawn) result += EvaluationData.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.B2] == ChessPiece.WhitePawn) result += EvaluationData.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.B3] == ChessPiece.WhitePawn) result += EvaluationData.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.C2] == ChessPiece.WhitePawn) result += EvaluationData.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.C3] == ChessPiece.WhitePawn) result += EvaluationData.SHIELD_3;
            }

            return result;
        }

        int bKingShield(Board b)
        {
            int result = 0;
            var kingBitboard = b.BitBoard[ChessPiece.BlackKing];
            var kingPos = kingBitboard.BitScanForward();
            var col = kingPos & 7;

            /* king on the kingside */
            if (col > ChessFile.E)
            {
                if (b.ArrayBoard[ChessPosition.F7] == ChessPiece.BlackPawn) result += EvaluationData.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.F6] == ChessPiece.BlackPawn) result += EvaluationData.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.G7] == ChessPiece.BlackPawn) result += EvaluationData.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.G6] == ChessPiece.BlackPawn) result += EvaluationData.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.H7] == ChessPiece.BlackPawn) result += EvaluationData.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.H6] == ChessPiece.BlackPawn) result += EvaluationData.SHIELD_3;
            }

            /* king on the queenside */
            else if (col < ChessFile.D)
            {
                if (b.ArrayBoard[ChessPosition.A7] == ChessPiece.BlackPawn) result += EvaluationData.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.A6] == ChessPiece.BlackPawn) result += EvaluationData.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.B7] == ChessPiece.BlackPawn) result += EvaluationData.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.B6] == ChessPiece.BlackPawn) result += EvaluationData.SHIELD_3;

                if (b.ArrayBoard[ChessPosition.C7] == ChessPiece.BlackPawn) result += EvaluationData.SHIELD_2;
                else if (b.ArrayBoard[ChessPosition.C6] == ChessPiece.BlackPawn) result += EvaluationData.SHIELD_3;
            }
            return result;
        }

        static bool isPiece(Board b, int color, int piece, int position)
        {
            var p = b.ArrayBoard[position];
            switch (p)
            {
                case ChessPiece.WhitePawn: return color == ChessPiece.White && piece == EvaluationData.PAWN;
                case ChessPiece.WhiteKnight: return color == ChessPiece.White && piece == EvaluationData.KNIGHT;
                case ChessPiece.WhiteBishop: return color == ChessPiece.White && piece == EvaluationData.BISHOP;
                case ChessPiece.WhiteRook: return color == ChessPiece.White && piece == EvaluationData.ROOK;
                case ChessPiece.WhiteQueen: return color == ChessPiece.White && piece == EvaluationData.QUEEN;
                case ChessPiece.WhiteKing: return color == ChessPiece.White && piece == EvaluationData.KING;

                case ChessPiece.BlackPawn: return color == ChessPiece.Black && piece == EvaluationData.PAWN;
                case ChessPiece.BlackKnight: return color == ChessPiece.Black && piece == EvaluationData.KNIGHT;
                case ChessPiece.BlackBishop: return color == ChessPiece.Black && piece == EvaluationData.BISHOP;
                case ChessPiece.BlackRook: return color == ChessPiece.Black && piece == EvaluationData.ROOK;
                case ChessPiece.BlackQueen: return color == ChessPiece.Black && piece == EvaluationData.QUEEN;
                case ChessPiece.BlackKing: return color == ChessPiece.Black && piece == EvaluationData.KING;
            }

            return false;
        }

        int REL_SQ(int cl, int sq)
        {
            return ((cl) == (ChessPiece.White) ? (sq) : (EvaluationData.inv_sq[sq]));
        }

        void blockedPieces(Board b, EvaluationScores v, EvaluationBoard eb, int side)
        {

            int oppo = side == 0 ? 1 : 0;

            // central pawn blocked, bishop hard to develop
            if (isPiece(b, side, EvaluationData.BISHOP, REL_SQ(side, ChessPosition.C1))
            && isPiece(b, side, EvaluationData.PAWN, REL_SQ(side, ChessPosition.D2))
            && b.ArrayBoard[REL_SQ(side, ChessPosition.D3)] != ChessPiece.Empty)
                v.blockages[side] -= EvaluationData.P_BLOCK_CENTRAL_PAWN;

            if (isPiece(b, side, EvaluationData.BISHOP, REL_SQ(side, ChessPosition.F1))
            && isPiece(b, side, EvaluationData.PAWN, REL_SQ(side, ChessPosition.E2))
            && b.ArrayBoard[REL_SQ(side, ChessPosition.E3)] != ChessPiece.Empty)
                v.blockages[side] -= EvaluationData.P_BLOCK_CENTRAL_PAWN;

            // trapped knight
            if (isPiece(b, side, EvaluationData.KNIGHT, REL_SQ(side, ChessPosition.A8))
            && (isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.A7)) || isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.C7))))
                v.blockages[side] -= EvaluationData.P_KNIGHT_TRAPPED_A8;

            if (isPiece(b, side, EvaluationData.KNIGHT, REL_SQ(side, ChessPosition.H8))
            && (isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.H7)) || isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.F7))))
                v.blockages[side] -= EvaluationData.P_KNIGHT_TRAPPED_A8;

            if (isPiece(b, side, EvaluationData.KNIGHT, REL_SQ(side, ChessPosition.A7))
            && isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.A6))
            && isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.B7)))
                v.blockages[side] -= EvaluationData.P_KNIGHT_TRAPPED_A7;

            if (isPiece(b, side, EvaluationData.KNIGHT, REL_SQ(side, ChessPosition.H7))
            && isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.H6))
            && isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.G7)))
                v.blockages[side] -= EvaluationData.P_KNIGHT_TRAPPED_A7;

            // knight blocking queenside pawns
            if (isPiece(b, side, EvaluationData.KNIGHT, REL_SQ(side, ChessPosition.C3))
            && isPiece(b, side, EvaluationData.PAWN, REL_SQ(side, ChessPosition.C2))
            && isPiece(b, side, EvaluationData.PAWN, REL_SQ(side, ChessPosition.D4))
            && !isPiece(b, side, EvaluationData.PAWN, REL_SQ(side, ChessPosition.E4)))
                v.blockages[side] -= EvaluationData.P_C3_KNIGHT;

            // trapped bishop
            if (isPiece(b, side, EvaluationData.BISHOP, REL_SQ(side, ChessPosition.A7))
            && isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.B6)))
                v.blockages[side] -= EvaluationData.P_BISHOP_TRAPPED_A7;

            if (isPiece(b, side, EvaluationData.BISHOP, REL_SQ(side, ChessPosition.H7))
            && isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.G6)))
                v.blockages[side] -= EvaluationData.P_BISHOP_TRAPPED_A7;

            if (isPiece(b, side, EvaluationData.BISHOP, REL_SQ(side, ChessPosition.B8))
            && isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.C7)))
                v.blockages[side] -= EvaluationData.P_BISHOP_TRAPPED_A7;

            if (isPiece(b, side, EvaluationData.BISHOP, REL_SQ(side, ChessPosition.G8))
            && isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.F7)))
                v.blockages[side] -= EvaluationData.P_BISHOP_TRAPPED_A7;

            if (isPiece(b, side, EvaluationData.BISHOP, REL_SQ(side, ChessPosition.A6))
            && isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.B5)))
                v.blockages[side] -= EvaluationData.P_BISHOP_TRAPPED_A6;

            if (isPiece(b, side, EvaluationData.BISHOP, REL_SQ(side, ChessPosition.H6))
            && isPiece(b, oppo, EvaluationData.PAWN, REL_SQ(side, ChessPosition.G5)))
                v.blockages[side] -= EvaluationData.P_BISHOP_TRAPPED_A6;

            // bishop on initial sqare supporting castled king
            if (isPiece(b, side, EvaluationData.BISHOP, REL_SQ(side, ChessPosition.F1))
            && isPiece(b, side, EvaluationData.KING, REL_SQ(side, ChessPosition.G1)))
                v.positionalThemes[side] += EvaluationData.RETURNING_BISHOP;

            if (isPiece(b, side, EvaluationData.BISHOP, REL_SQ(side, ChessPosition.C1))
            && isPiece(b, side, EvaluationData.KING, REL_SQ(side, ChessPosition.B1)))
                v.positionalThemes[side] += EvaluationData.RETURNING_BISHOP;

            // uncastled king blocking own rook
            if ((isPiece(b, side, EvaluationData.KING, REL_SQ(side, ChessPosition.F1)) || isPiece(b, side, EvaluationData.KING, REL_SQ(side, ChessPosition.G1)))
            && (isPiece(b, side, EvaluationData.ROOK, REL_SQ(side, ChessPosition.H1)) || isPiece(b, side, EvaluationData.ROOK, REL_SQ(side, ChessPosition.G1))))
                v.blockages[side] -= EvaluationData.P_KING_BLOCKS_ROOK;

            if ((isPiece(b, side, EvaluationData.KING, REL_SQ(side, ChessPosition.C1)) || isPiece(b, side, EvaluationData.KING, REL_SQ(side, ChessPosition.B1)))
            && (isPiece(b, side, EvaluationData.ROOK, REL_SQ(side, ChessPosition.A1)) || isPiece(b, side, EvaluationData.ROOK, REL_SQ(side, ChessPosition.B1))))
                v.blockages[side] -= EvaluationData.P_KING_BLOCKS_ROOK;
        }

        int getPawnScore(Board b, EvaluationBoard eb)
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

            result = evalPawnStructure(b, eb);
            //ttpawn_save(result);
            return result;
        }

        int evalPawnStructure(Board b, EvaluationBoard eb)
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
                        result += EvalPawn(b, eb, sq, ChessPiece.White);
                    }
                    else
                    {
                        result -= EvalPawn(b, eb, sq, ChessPiece.Black);
                    }
                }
            }

            return result;
        }

        int EvalPawn(Board b, EvaluationBoard eb, Position sq, byte side)
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

            var nextSq = sq + EvaluationData.stepFwd[side];

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

                nextSq += EvaluationData.stepFwd[side];
            }

            /**************************************************************************
            *   Another loop, going backwards and checking whether pawn has support.  *
            *   Here we can at least break out of it for speed optimization.          *
            **************************************************************************/

            nextSq = sq + EvaluationData.stepFwd[side]; // so that a pawn in a duo will not be considered weak

            while (nextSq > 0 && nextSq < 64)
            {

                if (eb.pawn_ctrl[side,nextSq] > 0)
                {
                    flagIsWeak = false;
                    break;
                }

                nextSq += EvaluationData.stepBck[side];
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
                    result += _e.protected_passer[side, sq];
                }
                else
                {
                    result += _e.passed_pawn[side, sq];
                }
            }

            /**************************************************************************
            *  Evaluate weak pawns, increasing the penalty if they are situated       *
            *  on a half-open file                                                    *
            **************************************************************************/

            if (flagIsWeak)
            {
                result += _e.weak_pawn[side, sq];
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
                step = EvaluationData.SOUTH;
            }
            else
            {
                step = EvaluationData.NORTH;
            }

            var col = sq & 7;


            if (col > 0 && isPiece(b, side, EvaluationData.PAWN, sq + EvaluationData.WEST))
            {
                return true;
            }

            if (col < 7 && isPiece(b, side, EvaluationData.PAWN, sq + EvaluationData.EAST))
            {
                return true;
            }

            if (col > 0 && isPiece(b, side, EvaluationData.PAWN, sq + step + EvaluationData.WEST))
            {
                return true;
            }

            if (col < 7 && isPiece(b, side, EvaluationData.PAWN, sq + step + EvaluationData.EAST))
            {
                return true;
            }

            return false;
        }

        void printEval(Board b, EvaluationData e, EvaluationBoard eb, EvaluationScores v, Score score)
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
            if (b.WhiteToMove)
            {
                builder.Append(EvaluationData.TEMPO);
            }
            else
            {
                builder.Append(-EvaluationData.TEMPO);
            }
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
