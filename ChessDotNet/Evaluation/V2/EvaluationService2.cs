using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using ChessDotNet.Common;
using ChessDotNet.Data;
using ChessDotNet.Hashing;
using ChessDotNet.MoveGeneration;
using ChessDotNet.MoveGeneration.SlideGeneration;
using Score = System.Int32;
using Position = System.Byte;
using Piece = System.Byte;

namespace ChessDotNet.Evaluation.V2
{
    public class EvaluationService2 : IEvaluationService
    {
        private readonly EvaluationData _evaluationData;
        private readonly EvalHashTable _evalTable;
        private readonly EvalHashTable _pawnTable;
        private readonly AttacksService _attacks;
        private readonly ISlideMoveGenerator _slideGenerator;
        private readonly PinDetector _pinDetector;

        private readonly EvaluationScores _evaluationScores;
        private readonly ulong[] _pawnControl;

        public EvaluationService2(EvaluationData evaluationData)
        {
            _pawnControl = new ulong[2];
            _evaluationScores = new EvaluationScores();
            _evaluationData = evaluationData;
            _evalTable = new EvalHashTable();
            _evalTable.SetSize(16 * 1024 * 1024);
            _pawnTable = new EvalHashTable();
            _pawnTable.SetSize(16 * 1024 * 1024);
            _slideGenerator = new MagicBitboardsService();
            _pinDetector = new PinDetector(_slideGenerator);
            _attacks = new AttacksService(_slideGenerator);
        }

        public int Evaluate(Board board)
        {
            if (EngineOptions.UseEvalHashTable)
            {
                var success = _evalTable.TryProbe(board.Key, out var hashScore);
                if (success)
                {
                    return hashScore;
                }

                var score = eval(board, _evaluationScores);
                _evalTable.Store(board.Key, score);
                //printEval(board, e, eb, v, score);
                return score;
            }
            else
            {
                var score = eval(board, _evaluationScores);
                _evalTable.Store(board.Key, score);
                //printEval(board, e, eb, v, score);
                return score;
            }
        }

        private int eval(Board b, EvaluationScores v)
        {
            int result = 0;
            int mgScore = 0;
            int egScore = 0;
            int stronger = 0;
            int weaker = 0;

            var pawnControl = _pawnControl;
            pawnControl[ChessPiece.White] = _attacks.GetAttackedByPawns(b.BitBoard[ChessPiece.WhitePawn], true);
            pawnControl[ChessPiece.Black] = _attacks.GetAttackedByPawns(b.BitBoard[ChessPiece.BlackPawn], false);

            /**************************************************************************
            *  Clear all eval data                                                    *
            **************************************************************************/

            _evaluationScores.Clear();
            
            v.gamePhase = b.PieceCounts[ChessPiece.WhiteKnight] + b.PieceCounts[ChessPiece.WhiteBishop] + 2 * b.PieceCounts[ChessPiece.WhiteRook] + 4 * b.PieceCounts[ChessPiece.WhiteQueen]
                        + b.PieceCounts[ChessPiece.BlackKnight] + b.PieceCounts[ChessPiece.BlackBishop] + 2 * b.PieceCounts[ChessPiece.BlackRook] + 4 * b.PieceCounts[ChessPiece.BlackQueen];

            /************************************************************************** 
            * add king's pawn shield score and evaluate part of piece blockage score  *
            * (the rest of the latter will be done via piece eval)                    *
            **************************************************************************/

            v.kingShield[ChessPiece.White] = wKingShield(b);
            v.kingShield[ChessPiece.Black] = bKingShield(b);
            blockedPieces(b, v, ChessPiece.White);
            blockedPieces(b, v, ChessPiece.Black);
            mgScore += (v.kingShield[ChessPiece.White] - v.kingShield[ChessPiece.Black]);

            /* tempo bonus */
            if (b.WhiteToMove)
            {
                result += EvaluationData.TEMPO;
            }
            else
            {
                result -= EvaluationData.TEMPO;
            }

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

            v.adjustMaterial[ChessPiece.White] += _evaluationData.n_adj[b.PieceCounts[ChessPiece.WhitePawn]] * b.PieceCounts[ChessPiece.WhiteKnight];
            v.adjustMaterial[ChessPiece.Black] += _evaluationData.n_adj[b.PieceCounts[ChessPiece.BlackPawn]] * b.PieceCounts[ChessPiece.BlackKnight];
            v.adjustMaterial[ChessPiece.White] += _evaluationData.r_adj[b.PieceCounts[ChessPiece.WhitePawn]] * b.PieceCounts[ChessPiece.WhiteRook];
            v.adjustMaterial[ChessPiece.Black] += _evaluationData.r_adj[b.PieceCounts[ChessPiece.BlackPawn]] * b.PieceCounts[ChessPiece.BlackRook];

            var pawnScore = getPawnScore(b, pawnControl);
            result += pawnScore;

            /**************************************************************************
            *  Evaluate pieces                                                        *
            **************************************************************************/

            EvaluatePieces(b, v, pawnControl);


            /************************************************************************** 
            *  Sum the incrementally counted material and piece/square table values   *
            **************************************************************************/

            mgScore += b.PieceMaterial[ChessPiece.White] + b.PawnMaterial[ChessPiece.White] + v.PieceSquaresMidgame[ChessPiece.White]
                      - b.PieceMaterial[ChessPiece.Black] - b.PawnMaterial[ChessPiece.Black] - v.PieceSquaresMidgame[ChessPiece.Black];
            egScore += b.PieceMaterial[ChessPiece.White] + b.PawnMaterial[ChessPiece.White] + v.PieceSquaresEndgame[ChessPiece.White]
                      - b.PieceMaterial[ChessPiece.Black] - b.PawnMaterial[ChessPiece.Black] - v.PieceSquaresEndgame[ChessPiece.Black];


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

            var whitePins = _pinDetector.GetPinned(b, ChessPiece.White, b.KingPositions[ChessPiece.White]);
            result -= whitePins.BitCount() * 5;
            var blackPins = _pinDetector.GetPinned(b, ChessPiece.Black, b.KingPositions[ChessPiece.Black]);
            result += blackPins.BitCount() * 5;

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

            if (b.PawnMaterial[stronger] == 0)
            {

                if (b.PieceMaterial[stronger] < 400)
                {
                    return 0;
                }

                if (b.PawnMaterial[weaker] == 0 && (b.PieceMaterial[stronger] == 2 * EvaluationData.PIECE_VALUE[ChessPiece.Knight]))
                {
                    return 0;
                }

                if
                (
                    b.PieceMaterial[stronger] == EvaluationData.PIECE_VALUE[ChessPiece.Rook]
                    && b.PieceMaterial[weaker] == EvaluationData.PIECE_VALUE[ChessPiece.Knight] // TODO FIXED
                )
                {
                    result /= 2;
                }

                if
                (
                    b.PieceMaterial[stronger] == EvaluationData.PIECE_VALUE[ChessPiece.Rook]
                    && b.PieceMaterial[weaker] == EvaluationData.PIECE_VALUE[ChessPiece.Bishop]
                )
                {
                    result /= 2;
                }

                if
                (
                    b.PieceMaterial[stronger] == EvaluationData.PIECE_VALUE[ChessPiece.Rook] + EvaluationData.PIECE_VALUE[ChessPiece.Knight]
                    && b.PieceMaterial[weaker] == EvaluationData.PIECE_VALUE[ChessPiece.Rook]
                )
                {
                    result /= 2;
                }

                if
                (
                    b.PieceMaterial[stronger] == EvaluationData.PIECE_VALUE[ChessPiece.Rook] + EvaluationData.PIECE_VALUE[ChessPiece.Bishop]
                    && b.PieceMaterial[weaker] == EvaluationData.PIECE_VALUE[ChessPiece.Rook]
                )
                {
                    result /= 2;
                }
            }

            /**************************************************************************
            *  Finally return the score relative to the side to move.                 *
            **************************************************************************/

            if (b.ColorToMove == ChessPiece.Black)
            {
                result = -result;
            }

            //tteval_save(result);
            return result;
        }

        void EvaluatePieces(Board b, EvaluationScores v, ulong[] pawnControl)
        {
            for (Piece color = 0; color <= 1; color++)
            {
                var pawn = ChessPiece.Pawn + color;
                var pawns = b.BitBoard[pawn];
                while (pawns != 0)
                {
                    var pos = pawns.BitScanForward();
                    v.PieceSquaresMidgame[color] += _evaluationData.mgPst[pawn][pos];
                    v.PieceSquaresEndgame[color] += _evaluationData.egPst[pawn][pos];
                    pawns &= pawns - 1;
                }

                var knight = ChessPiece.Knight + color;
                var knights = b.BitBoard[knight];
                while (knights != 0)
                {
                    var pos = knights.BitScanForward();
                    EvalKnight(b, v, pos, color, pawnControl);
                    v.PieceSquaresMidgame[color] += _evaluationData.mgPst[knight][pos];
                    v.PieceSquaresEndgame[color] += _evaluationData.egPst[knight][pos];
                    knights &= knights - 1;
                }

                var bishop = ChessPiece.Bishop + color;
                var bishops = b.BitBoard[bishop];
                while (bishops != 0)
                {
                    var pos = bishops.BitScanForward();
                    EvalBishop(b, v, pos, color, pawnControl);
                    v.PieceSquaresMidgame[color] += _evaluationData.mgPst[bishop][pos];
                    v.PieceSquaresEndgame[color] += _evaluationData.egPst[bishop][pos];
                    bishops &= bishops - 1;
                }

                var rook = ChessPiece.Rook + color;
                var rooks = b.BitBoard[rook];
                while (rooks != 0)
                {
                    var pos = rooks.BitScanForward();
                    EvalRook(b, v, pos, color);
                    v.PieceSquaresMidgame[color] += _evaluationData.mgPst[rook][pos];
                    v.PieceSquaresEndgame[color] += _evaluationData.egPst[rook][pos];
                    rooks &= rooks - 1;
                }

                var queen = ChessPiece.Queen + color;
                var queens = b.BitBoard[queen];
                while (queens != 0)
                {
                    var pos = queens.BitScanForward();
                    EvalQueen(b, v, pos, color);
                    v.PieceSquaresMidgame[color] += _evaluationData.mgPst[queen][pos];
                    v.PieceSquaresEndgame[color] += _evaluationData.egPst[queen][pos];
                    queens &= queens - 1;
                }

                v.PieceSquaresMidgame[color] += _evaluationData.mgPst[ChessPiece.King + color][b.KingPositions[color]];
                v.PieceSquaresEndgame[color] += _evaluationData.egPst[ChessPiece.King + color][b.KingPositions[color]];
            }
        }

        void EvalKnight(Board b, EvaluationScores v, Position sq, Piece side, ulong[] pawnControl)
        {
            int att = 0;
            int mob = 0;

            /**************************************************************************
            *  Collect data about mobility and king attacks. This resembles move      *
            *  generation code, except that we are just incrementing the counters     *
            *  instead of adding actual moves.                                        *
            **************************************************************************/

            var jumps = BitboardConstants.KnightJumps[sq];
            var opponent = side == ChessPiece.White ? b.BlackPieces : b.WhitePieces;
            var emptyOrOpponent = (b.EmptySquares | opponent) & jumps;

            var uncontrolled = emptyOrOpponent & ~pawnControl[side ^ 1];
            mob += uncontrolled.BitCount();

            var emptyOrOpponentNearKing = emptyOrOpponent & BitboardConstants.KingExtendedJumps[side ^ 1][b.KingPositions[side ^ 1]];
            att += emptyOrOpponentNearKing.BitCount();

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

        void EvalBishop(Board b, EvaluationScores v, Position sq, Piece side, ulong[] pawnControl)
        {

            int att = 0;
            int mob = 0;

            /**************************************************************************
            *  Collect data about mobility and king attacks                           *
            **************************************************************************/
            var slide = _slideGenerator.DiagonalAntidiagonalSlide(b.AllPieces, sq);

            var emptyUncontrolled = b.EmptySquares & ~pawnControl[side ^ 1] & slide;
            mob += emptyUncontrolled.BitCount();

            var opponent = (side == ChessPiece.White ? b.BlackPieces : b.WhitePieces) & slide;
            mob += opponent.BitCount();

            var emptyOrOpponentNearKing = (b.EmptySquares | opponent) & BitboardConstants.KingExtendedJumps[side ^ 1][b.KingPositions[side ^ 1]] & slide;
            att += emptyOrOpponentNearKing.BitCount();

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

        public void EvalRook(Board b, EvaluationScores v, Position sq, Piece side)
        {

            int att = 0;
            int mob = 0;

            var sqCol = sq & 7;
            var sqRow = sq >> 3;

            /**************************************************************************
            *  Bonus for rook on the seventh rank. It is applied when there are pawns *
            *  to attack along that rank or if enemy king is cut off on 8th rank      *
            /*************************************************************************/

            //if
            //(
            //    sqRow == EvaluationData.seventh[side]
            //    && (eb.pawns_on_rank[side ^ 1, EvaluationData.seventh[side]] > 0 || (b.KingPositions[side ^ 1]) == EvaluationData.eighth[side])
            //)

            var seventh = EvaluationData.seventh[side];
            var eighth = EvaluationData.eighth[side];
            if
            (
                sqRow == seventh
                && (
                    (b.BitBoard[ChessPiece.Pawn + side ^ 1] & BitboardConstants.Ranks[seventh]) != 0
                    || (b.BitBoard[ChessPiece.King + side ^ 1] & BitboardConstants.Ranks[eighth]) != 0
                )
            )
            {
                v.mgMob[side] += 20;
                v.egMob[side] += 30;
            }

            /**************************************************************************
            *  Bonus for open and half-open files is merged with mobility score.      *
            *  Bonus for open files targetting enemy king is added to attWeight[]     *
            /*************************************************************************/
            var file = BitboardConstants.Files[sqCol];
            var ownPawnsOnFile = b.BitBoard[ChessPiece.Pawn + side] & file;
            if (ownPawnsOnFile == 0)
            {
                var opponentPawnsOnFile = b.BitBoard[ChessPiece.Pawn + side ^ 1] & file;
                if (opponentPawnsOnFile == 0) // fully open file
                {
                    v.mgMob[side] += EvaluationData.ROOK_OPEN;
                    v.egMob[side] += EvaluationData.ROOK_OPEN;
                    if (Math.Abs(sqCol - (b.KingPositions[side ^ 1] & 7)) < 2)
                    {
                        v.attWeight[side] += 1;
                    }
                }
                else // half open file
                {
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

            var slide = _slideGenerator.HorizontalVerticalSlide(b.AllPieces, sq);

            var opponent = side == ChessPiece.White ? b.BlackPieces : b.WhitePieces;
            var emptyOrOpponent = (b.EmptySquares | opponent) & slide;
            mob += emptyOrOpponent.BitCount();

            var emptyOrOpponentNearKing = emptyOrOpponent & BitboardConstants.KingExtendedJumps[side ^ 1][b.KingPositions[side ^ 1]];
            att += emptyOrOpponentNearKing.BitCount();

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

        void EvalQueen(Board b, EvaluationScores v, Position sq, Piece side)
        {

            int att = 0;
            int mob = 0;

            //var sqCol = sq & 7;
            var sqRow = sq >> 3;

            var seventh = EvaluationData.seventh[side];
            var eighth = EvaluationData.eighth[side];
            if
            (
                sqRow == seventh
                && (
                    (b.BitBoard[ChessPiece.Pawn + side ^ 1] & BitboardConstants.Ranks[seventh]) != 0
                    || (b.BitBoard[ChessPiece.King + side ^ 1] & BitboardConstants.Ranks[eighth]) != 0
                )
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
                if (isPiece(b, side, ChessPiece.Knight, REL_SQ(side, ChessPosition.B1))) v.positionalThemes[side] -= 2;
                if (isPiece(b, side, ChessPiece.Bishop, REL_SQ(side, ChessPosition.C1))) v.positionalThemes[side] -= 2;
                if (isPiece(b, side, ChessPiece.Bishop, REL_SQ(side, ChessPosition.F1))) v.positionalThemes[side] -= 2;
                if (isPiece(b, side, ChessPiece.Knight, REL_SQ(side, ChessPosition.G1))) v.positionalThemes[side] -= 2;
            }

            /**************************************************************************
            *  Collect data about mobility and king attacks                           *
            **************************************************************************/

            var slide = _slideGenerator.AllSlide(b.AllPieces, sq);

            var opponent = side == ChessPiece.White ? b.BlackPieces : b.WhitePieces;
            var emptyOrOpponent = (b.EmptySquares | opponent) & slide;
            mob += emptyOrOpponent.BitCount();

            var emptyOrOpponentNearKing = emptyOrOpponent & BitboardConstants.KingExtendedJumps[side ^ 1][b.KingPositions[side ^ 1]];
            att += emptyOrOpponentNearKing.BitCount();

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
            var kingPos = b.KingPositions[ChessPiece.White];
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
            var kingPos = b.KingPositions[ChessPiece.Black];
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

        bool isPiece(Board b, int color, int pieceNoColor, int position)
        {
            var piece = b.ArrayBoard[position];
            return (piece & ~ChessPiece.Color) == pieceNoColor && (piece & ChessPiece.Color) == color;
        }

        int REL_SQ(int color, int position)
        {
            return EvaluationData.RelativePositions[color][position];
        }

        void blockedPieces(Board b, EvaluationScores v, int side)
        {

            int oppo = side ^ 1;

            // central pawn blocked, bishop hard to develop
            if (isPiece(b, side, ChessPiece.Bishop, REL_SQ(side, ChessPosition.C1))
            && isPiece(b, side, ChessPiece.Pawn, REL_SQ(side, ChessPosition.D2))
            && b.ArrayBoard[REL_SQ(side, ChessPosition.D3)] != ChessPiece.Empty)
                v.blockages[side] -= EvaluationData.P_BLOCK_CENTRAL_PAWN;

            if (isPiece(b, side, ChessPiece.Bishop, REL_SQ(side, ChessPosition.F1))
            && isPiece(b, side, ChessPiece.Pawn, REL_SQ(side, ChessPosition.E2))
            && b.ArrayBoard[REL_SQ(side, ChessPosition.E3)] != ChessPiece.Empty)
                v.blockages[side] -= EvaluationData.P_BLOCK_CENTRAL_PAWN;

            // trapped knight
            if (isPiece(b, side, ChessPiece.Knight, REL_SQ(side, ChessPosition.A8))
            && (isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.A7)) || isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.C7))))
                v.blockages[side] -= EvaluationData.P_KNIGHT_TRAPPED_A8;

            if (isPiece(b, side, ChessPiece.Knight, REL_SQ(side, ChessPosition.H8))
            && (isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.H7)) || isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.F7))))
                v.blockages[side] -= EvaluationData.P_KNIGHT_TRAPPED_A8;

            if (isPiece(b, side, ChessPiece.Knight, REL_SQ(side, ChessPosition.A7))
            && isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.A6))
            && isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.B7)))
                v.blockages[side] -= EvaluationData.P_KNIGHT_TRAPPED_A7;

            if (isPiece(b, side, ChessPiece.Knight, REL_SQ(side, ChessPosition.H7))
            && isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.H6))
            && isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.G7)))
                v.blockages[side] -= EvaluationData.P_KNIGHT_TRAPPED_A7;

            // knight blocking queenside pawns
            if (isPiece(b, side, ChessPiece.Knight, REL_SQ(side, ChessPosition.C3))
            && isPiece(b, side, ChessPiece.Pawn, REL_SQ(side, ChessPosition.C2))
            && isPiece(b, side, ChessPiece.Pawn, REL_SQ(side, ChessPosition.D4))
            && !isPiece(b, side, ChessPiece.Pawn, REL_SQ(side, ChessPosition.E4)))
                v.blockages[side] -= EvaluationData.P_C3_KNIGHT;

            // trapped bishop
            if (isPiece(b, side, ChessPiece.Bishop, REL_SQ(side, ChessPosition.A7))
            && isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.B6)))
                v.blockages[side] -= EvaluationData.P_BISHOP_TRAPPED_A7;

            if (isPiece(b, side, ChessPiece.Bishop, REL_SQ(side, ChessPosition.H7))
            && isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.G6)))
                v.blockages[side] -= EvaluationData.P_BISHOP_TRAPPED_A7;

            if (isPiece(b, side, ChessPiece.Bishop, REL_SQ(side, ChessPosition.B8))
            && isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.C7)))
                v.blockages[side] -= EvaluationData.P_BISHOP_TRAPPED_A7;

            if (isPiece(b, side, ChessPiece.Bishop, REL_SQ(side, ChessPosition.G8))
            && isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.F7)))
                v.blockages[side] -= EvaluationData.P_BISHOP_TRAPPED_A7;

            if (isPiece(b, side, ChessPiece.Bishop, REL_SQ(side, ChessPosition.A6))
            && isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.B5)))
                v.blockages[side] -= EvaluationData.P_BISHOP_TRAPPED_A6;

            if (isPiece(b, side, ChessPiece.Bishop, REL_SQ(side, ChessPosition.H6))
            && isPiece(b, oppo, ChessPiece.Pawn, REL_SQ(side, ChessPosition.G5)))
                v.blockages[side] -= EvaluationData.P_BISHOP_TRAPPED_A6;

            // bishop on initial sqare supporting castled king
            if (isPiece(b, side, ChessPiece.Bishop, REL_SQ(side, ChessPosition.F1))
            && isPiece(b, side, ChessPiece.King, REL_SQ(side, ChessPosition.G1)))
                v.positionalThemes[side] += EvaluationData.RETURNING_BISHOP;

            if (isPiece(b, side, ChessPiece.Bishop, REL_SQ(side, ChessPosition.C1))
            && isPiece(b, side, ChessPiece.King, REL_SQ(side, ChessPosition.B1)))
                v.positionalThemes[side] += EvaluationData.RETURNING_BISHOP;

            // uncastled king blocking own rook
            if ((isPiece(b, side, ChessPiece.King, REL_SQ(side, ChessPosition.F1)) || isPiece(b, side, ChessPiece.King, REL_SQ(side, ChessPosition.G1)))
            && (isPiece(b, side, ChessPiece.Rook, REL_SQ(side, ChessPosition.H1)) || isPiece(b, side, ChessPiece.Rook, REL_SQ(side, ChessPosition.G1))))
                v.blockages[side] -= EvaluationData.P_KING_BLOCKS_ROOK;

            if ((isPiece(b, side, ChessPiece.King, REL_SQ(side, ChessPosition.C1)) || isPiece(b, side, ChessPiece.King, REL_SQ(side, ChessPosition.B1)))
            && (isPiece(b, side, ChessPiece.Rook, REL_SQ(side, ChessPosition.A1)) || isPiece(b, side, ChessPiece.Rook, REL_SQ(side, ChessPosition.B1))))
                v.blockages[side] -= EvaluationData.P_KING_BLOCKS_ROOK;
        }

        int getPawnScore(Board b, ulong[] pawnControl)
        {
            /**************************************************************************
            *  This function wraps hashing mechanism around evalPawnStructure().      *
            *  Please note  that since we use the pawn hashtable, evalPawnStructure() *
            *  must not take into account the piece position.  In a more elaborate    *
            *  program, pawn hashtable would contain only the characteristics of pawn *
            *  structure,  and scoring them in conjunction with the piece position    *
            *  would have been done elsewhere.                                        *
            **************************************************************************/

            if (EngineOptions.UsePawnHashTable)
            {
                var success = _pawnTable.TryProbe(b.PawnKey, out var hashScore);
                if (success)
                {
                    return hashScore;
                }

                var score = evalPawnStructure(b, pawnControl);
                _pawnTable.Store(b.PawnKey, score);
                return score;
            }
            else
            {
                var score = evalPawnStructure(b, pawnControl);
                return score;
            }
        }

        public int evalPawnStructure(Board b, ulong[] pawnControl)
        {
            int result = 0;

            var whitePawns = b.BitBoard[ChessPiece.WhitePawn];
            while (whitePawns != 0)
            {
                var sq = whitePawns.BitScanForward();
                var pawnBitboard = 1UL << sq;

                //var pawnResult = EvalPawnOld(b, eb, sq, ChessPiece.White);
                var pawnResult = EvalPawn(b, sq, ChessPiece.White, pawnControl, pawnBitboard);
                result += pawnResult;

                whitePawns &= ~pawnBitboard;
            }

            var blackPawns = b.BitBoard[ChessPiece.BlackPawn];
            while (blackPawns != 0)
            {
                var sq = blackPawns.BitScanForward();
                var pawnBitboard = 1UL << sq;

                //var pawnResult = EvalPawnOld(b, eb, sq, ChessPiece.White);
                var pawnResult = EvalPawn(b, sq, ChessPiece.Black, pawnControl, pawnBitboard);
                result -= pawnResult;

                blackPawns &= ~pawnBitboard;
            }

            return result;
        }

        int EvalPawn(Board b, byte sq, byte side, ulong[] pawnControl, ulong bitboard)
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

            // TODO: MISTAKE HERE - should be [side ^ 1]
            if ((bitboard & pawnControl[side ^ 1]) != 0)
            {
                flagIsPassed = false;
            }

            var inFront = BitboardConstants.ColumnInFront[side][sq];
            var ownPawnsInFront = inFront & b.BitBoard[ChessPiece.Pawn + side];
            result -= ownPawnsInFront.BitCount() * 20;

            var opponentPawnsInFront = inFront & b.BitBoard[ChessPiece.Pawn + (side ^ 1)];
            flagIsOpposed = opponentPawnsInFront != 0;

            var pawnControlledInFront = inFront & (pawnControl[side ^ 1] | ownPawnsInFront | opponentPawnsInFront);
            flagIsPassed &= pawnControlledInFront == 0;

            /**************************************************************************
            *   Another loop, going backwards and checking whether pawn has support.  *
            *   Here we can at least break out of it for speed optimization.          *
            **************************************************************************/

            var behind = BitboardConstants.ColumnSortOfBehind[side][sq];
            var ownControlledBehind = behind & pawnControl[side];
            flagIsWeak = ownControlledBehind == 0;

            /**************************************************************************
            *  Evaluate passed pawns, scoring them higher if they are protected       *
            *  or if their advance is supported by friendly pawns                     *
            **************************************************************************/

            if (flagIsPassed)
            {
                var pawnSupported = IsPawnSupported(b, sq, side);

                if (pawnSupported)
                {
                    result += _evaluationData.protected_passer[side][sq];
                }
                else
                {
                    result += _evaluationData.passed_pawn[side][sq];
                }
            }

            /**************************************************************************
            *  Evaluate weak pawns, increasing the penalty if they are situated       *
            *  on a half-open file                                                    *
            **************************************************************************/

            if (flagIsWeak)
            {
                result += _evaluationData.weak_pawn[side, sq];
                if (!flagIsOpposed)
                {
                    result -= 4;
                }
            }

            return result;
        }

        private bool IsPawnSupported(Board board, Position pos, Piece color)
        {
            var supportMask = BitboardConstants.PawnSupportJumps[color][pos];
            var pawns = board.BitBoard[ChessPiece.Pawn + color];
            var supported = (supportMask & pawns) != 0;
            return supported;
        }

        void printEval(Board b, EvaluationData e, EvaluationScores v, Score score)
        {
            var builder = new StringBuilder();
            builder.Append("------------------------------------------\n");
            builder.Append($"Total value (for side to move): {score}\n");
            builder.Append($"Material balance       : {b.PieceMaterial[ChessPiece.White] + b.PawnMaterial[ChessPiece.White] - b.PieceMaterial[ChessPiece.Black] - b.PawnMaterial[ChessPiece.Black]} \n");
            builder.Append("Material adjustement   : ");
            printEvalFactor(builder, v.adjustMaterial[ChessPiece.White], v.adjustMaterial[ChessPiece.Black]);
            builder.Append("Mg Piece/square tables : ");
            printEvalFactor(builder, v.PieceSquaresMidgame[ChessPiece.White], v.PieceSquaresMidgame[ChessPiece.Black]);
            builder.Append("Eg Piece/square tables : ");
            printEvalFactor(builder, v.PieceSquaresEndgame[ChessPiece.White], v.PieceSquaresEndgame[ChessPiece.Black]);
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
