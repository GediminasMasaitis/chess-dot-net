namespace ChessDotNet.Evaluation.V2
{
    class EvaluationScores
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
}