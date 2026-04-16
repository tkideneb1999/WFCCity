using System.Runtime.CompilerServices;
using UnityEngine;

public class ChunkHelpers
{
    public static int GetFlattenedIndex(int chunkSize, int x, int y, int z)
    {
        return chunkSize * chunkSize * y + chunkSize * z + x;
    }
}
