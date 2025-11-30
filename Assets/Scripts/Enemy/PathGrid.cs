using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathGrid
{
    private PathNode3D[,,] grid;
}
public class PathNode3D
{
    public int x, y, z;
    public int gCost;
    public int hCost;
    public int fCost=>gCost+hCost;

    public bool IsWalkable;
    public PathNode3D parent;

    public PathNode3D(int x, int y, int z, bool isWalkable)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        IsWalkable = isWalkable;
    }
}
