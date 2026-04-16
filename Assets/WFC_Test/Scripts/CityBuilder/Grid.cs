using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityBuilder
{

    public class Grid : MonoBehaviour
    {
        [SerializeField]
        private float _cellSize = 4;
        public float CellSize => _cellSize;
        private Vector3 _cellCenter;

        [SerializeField]
        private int _chunkSize = 16;

        [SerializeField]
        private string[] _partNames;

        [SerializeField]
        private Prototype[] _prototypes;
        private Dictionary<GUID, Prototype> _registeredPrototypes;

        private Dictionary<(int x, int y, int z), EditingChunk> _editingChunks = new();
        private Dictionary<(int x, int y, int z), TileChunk> _tileChunks = new();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _cellCenter = Vector3.one * 0.5f * _cellSize;

            foreach(string partName in _partNames)
            {
                PartRegistry.Instance.RegisterPart(partName);
            }

            foreach(Prototype p in _prototypes)
            {
                if(!CreateGuid(p, out GUID guid))
                {
                    Debug.LogError("Guid could not be created!");
                    continue;
                }
                if(_registeredPrototypes.ContainsKey(guid))
                {
                    Debug.LogError("Prototype with the exact same identifying criterias already exists");
                    continue;
                }
                _registeredPrototypes.Add(guid, p);
            }
        }

        bool CreateGuid(Prototype prototype, out GUID guid)
        {
            PartRegistry instance = PartRegistry.Instance;
            ushort[] parts = new ushort[8];
            if(!instance.TryGetPartId(prototype.NegXNegYNegZ, out parts[0]))
            {
                guid = new GUID();
                return false;
            }
            if (!instance.TryGetPartId(prototype.NegXNegYPosZ, out parts[1]))
            {
                guid = new GUID();
                return false;
            }
            if (!instance.TryGetPartId(prototype.NegXPosYNegZ, out parts[2]))
            {
                guid = new GUID();
                return false;
            }
            if (!instance.TryGetPartId(prototype.NegXPosYPosZ, out parts[3]))
            {
                guid = new GUID();
                return false;
            }
            if (!instance.TryGetPartId(prototype.PosXNegYNegZ, out parts[4]))
            {
                guid = new GUID();
                return false;
            }
            if (!instance.TryGetPartId(prototype.PosXNegYPosZ, out parts[5]))
            {
                guid = new GUID();
                return false;
            }
            if (!instance.TryGetPartId(prototype.PosXPosYNegZ, out parts[6]))
            {
                guid = new GUID();
                return false;
            }
            if (!instance.TryGetPartId(prototype.PosXPosYPosZ, out parts[7]))
            {
                guid = new GUID();
                return false;
            }
            uint[] guidData = new uint[4];
            Buffer.BlockCopy(parts, 0, guidData, 0, 8);
            guid = new GUID(guidData[0], guidData[1], guidData[2], guidData[3]);
            return true;
        }

        public bool SetEditingData(int x, int y, int z, ushort dataId)
        {
            ToChunkCoordinate((x, y, z), out (int x, int y, int z) chunkId, out (int x, int y, int z) chunkPos);
            if(!_editingChunks.TryGetValue(chunkId, out EditingChunk chunk))
            {
                chunk = new EditingChunk(_chunkSize, chunkId.x, chunkId.y, chunkId.z, PartRegistry.AirId);
                _editingChunks.Add(chunkId, chunk);
            }

            // TODO: Initialize Tile Chunk
            // TODO: Update Tile Chunk

            ushort state = chunk.GetEditingCellState(chunkPos.x, chunkPos.y, chunkPos.z);
            if (state != dataId)
            {
                return false;
            }

            chunk.SetEditingCellState(chunkPos.x, chunkPos.z, dataId);
            return true;
        }

        private void ToChunkCoordinate((int x, int y, int z) pos, 
            out (int x, int y, int z) chunkId, out (int x, int y, int z) chunkPos)
        {
            (bool x, bool y, bool z) isNeg = (
                pos.x < 0,
                pos.y < 0,
                pos.z < 0
                );
            chunkId = (
                pos.x - (isNeg.x ? _chunkSize : 0),
                pos.y - (isNeg.y ? _chunkSize : 0),
                pos.z - (isNeg.z ? _chunkSize : 0)
                );
            chunkId.x /= _chunkSize;
            chunkId.y /= _chunkSize;
            
            chunkPos = (
                (isNeg.x ? _chunkSize : 0) + (pos.x %  _chunkSize), 
                (isNeg.y ? _chunkSize : 0) + (pos.y % _chunkSize),
                (isNeg.z ? _chunkSize : 0) + (pos.z % _chunkSize)
                );
        }
    }
}
