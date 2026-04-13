using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{
    enum EditingGridCellState
    {
        Empty = 0,
        Filled,
    }

    class EditingChunk
    {
        private readonly int _xLocation;
        private readonly int _yLocation;
        public int XLocation => _xLocation;
        public int YLocation => _yLocation;

        private EditingGridCellState[] _editingCells;
        private int _chunkSize;

        public EditingChunk(int chunkSize, int xLocation, int yLocation)
        {
            _xLocation = xLocation;
            _yLocation = yLocation;
            _chunkSize = chunkSize;
            _editingCells = new EditingGridCellState[chunkSize * chunkSize];
            for(int i = 0; i < _editingCells.Length; i++)
            {
                _editingCells[i] = EditingGridCellState.Empty;
            }
        }

        public EditingGridCellState GetEditingCellState(int x, int y)
        {
            return _editingCells[_chunkSize * x + y];
        }

        public void SetEditingCellState(int x, int y, EditingGridCellState state)
        {
            _editingCells[_chunkSize * x + y] = state;
        }
    }

    public class Grid : MonoBehaviour
    {
        [SerializeField]
        private float _cellSize = 4;
        public float CellSize => _cellSize;
        private Vector3 _cellCenter;

        [SerializeField]
        private int _chunkSize = 16;
        private Dictionary<(int x, int y), EditingChunk> _editingChunks = new();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
            _cellCenter = Vector3.one * 0.5f * _cellSize;
        }

        public bool AddLand(int x, int y, out Vector3 position)
        {
            ToChunkCoordinate((x, y), out (int x, int y) chunkId, out (int x, int y) chunkPos);
            if(!_editingChunks.TryGetValue(chunkId, out EditingChunk chunk))
            {
                chunk = new EditingChunk(_chunkSize, chunkId.x, chunkId.y);
                _editingChunks.Add(chunkId, chunk);
            }

            position = new Vector3(x * _cellSize, 0, y * _cellSize) + _cellCenter;

            EditingGridCellState state = chunk.GetEditingCellState(chunkPos.x, chunkPos.y);
            if (state == EditingGridCellState.Filled)
            {
                return false;
            }

            chunk.SetEditingCellState(chunkPos.x, chunkPos.y, EditingGridCellState.Filled);
            return true;
        }

        private void ToChunkCoordinate((int x, int y) pos, out (int x, int y) chunkId, out (int x, int y) chunkPos)
        {
            (bool x, bool y) isNeg = (
                pos.x < 0,
                pos.y < 0
                );
            chunkId = (
                pos.x - (isNeg.x ? _chunkSize : 0),
                pos.y - (isNeg.y ? _chunkSize : 0)
                );
            chunkId.x /= _chunkSize;
            chunkId.y /= _chunkSize;
            
            chunkPos = (
                (isNeg.x ? _chunkSize : 0) + (pos.x %  _chunkSize), 
                (isNeg.y ? _chunkSize : 0) + (pos.y % _chunkSize)
                );
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
