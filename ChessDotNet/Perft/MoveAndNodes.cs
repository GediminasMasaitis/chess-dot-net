namespace ChessDotNet.Perft
{
    public struct MoveAndNodes
    {
        public MoveAndNodes(string move, int nodes)
        {
            Move = move;
            Nodes = nodes;
        }

        public string Move { get; }
        public int Nodes { get; }

        public override string ToString()
        {
            return $"Move: {Move}, Nodes: {Nodes}";
        }
    }
}