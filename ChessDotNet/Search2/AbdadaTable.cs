namespace ChessDotNet.Search2
{
    public class AbdadaTable
    {
        private readonly ulong[,] _table;

        private const int TableSize = 32768;
        private const int TableWays = 4;
        private const int DeferDepth = 3;

        public AbdadaTable()
        {
            _table = new ulong[TableSize, TableWays];
        }

        public bool DeferMove(ulong move_hash, int depth)
        {
            if (depth < DeferDepth) // note 1
            {
                return false;
            }

            var index = move_hash & (TableSize - 1);

            for (var i = 0; i < TableWays; i++)  // note 2
            {
                if (_table[index, i] == move_hash)
                {
                    return true;
                }
            }
            return false;
        }

        public void StartingSearch(ulong move_hash, int depth)
        {
            if (depth < DeferDepth)
            {
                return;
            }

            var index = move_hash & (TableSize - 1);
            for (var i = 0; i < TableWays; i++)
            {
                if (_table[index, i] == 0)
                {
                    _table[index, i] = move_hash;
                    return;
                }

                if (_table[index, i] == move_hash) // note 3.1
                {
                    return;
                }
            }
            _table[index, 0] = move_hash;
        }

        public void FinishedSearch(ulong move_hash, int depth)
        {
            if (depth < DeferDepth)
            {
                return;
            }

            var index = move_hash & (TableSize - 1);
            for (var i = 0; i < TableWays; i++)
            {
                if (_table[index, i] == move_hash) // note 3.2
                {
                    _table[index, i] = 0;
                }
            }

        }
    }
}