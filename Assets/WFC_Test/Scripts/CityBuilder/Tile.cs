using System;
using UnityEngine;

namespace CityBuilder
{
    public class Tile
    {
        // Order
        // NegX NegY NegZ, NegX NegY PosZ, NegX PosY NegZ, NegX PosY PosZ
        // PosX NegY NegZ, PosX NegY PosZ, PosX PosY NegZ, PosX PosY PosZ
        ushort[] _parts = new ushort[8];

        GUID ToGuid()
        {
            uint[] guidData = new uint[4];
            Buffer.BlockCopy(_parts, 0, guidData, 0, 8);
            return new GUID(guidData[0], guidData[1], guidData[2], guidData[3]);
        }
    }

    public class TileChunk
    {
        private Tile[] _tiles;
        private readonly  int _chunkSize;
        public TileChunk(int chunkSize)
        { 
            _chunkSize = chunkSize;
            _tiles = new Tile[chunkSize * chunkSize * chunkSize];
        }

        public Tile GetTile(int x, int y, int z)
        {
            return _tiles[ChunkHelpers.GetFlattenedIndex(_chunkSize, x, y, z)];
        }
    }
}
