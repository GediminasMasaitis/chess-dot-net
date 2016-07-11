using ChessDotNet.Data;

namespace ChessDotNet.Perft
{
    public struct MoveAndNodes
    {
        public MoveAndNodes(string move, int nodes, Move? engineMove = null)
        {
            Move = move;
            Nodes = nodes;
            EngineMove = engineMove;
        }

        public string Move { get; }
        public int Nodes { get; }
        public Move? EngineMove { get; set; }

        public override string ToString()
        {
            return $"Move: {Move}, Nodes: {Nodes}";
        }
    }
}