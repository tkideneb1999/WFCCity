using UnityEngine;

namespace CityBuilder
{
    class EditingChunk
    {
        private readonly int _xLocation;
        private readonly int _yLocation;
        private readonly int _zLocation;
        public int XLocation => _xLocation;
        public int YLocation => _yLocation;
        public int ZLocation => _zLocation;

        private ushort[] _editingCells;
        private int _chunkSize;

        public EditingChunk(int chunkSize, int xLocation, int yLocation, int zLocation, ushort airId)
        {
            _xLocation = xLocation;
            _yLocation = yLocation;
            _zLocation = zLocation;
            _chunkSize = chunkSize;
            _editingCells = new ushort[chunkSize * chunkSize * chunkSize];
            for (int i = 0; i < _editingCells.Length; i++)
            {
                _editingCells[i] = airId;
            }
        }

        public ushort GetEditingCellState(int x, int y, int z)
        {
            return _editingCells[ChunkHelpers.GetFlattenedIndex(_chunkSize, x, y, z)];
        }

        public void SetEditingCellState(int x, int y, ushort state)
        {
            _editingCells[_chunkSize * x + y] = state;
        }
    }
}
