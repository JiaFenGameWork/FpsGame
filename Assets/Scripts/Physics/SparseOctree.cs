using System;
using System.Collections.Generic;
using UnityEngine;
[Flags]
public enum CellFlag : byte
{
    None = 0,
    Walkable = 1 << 0,      // 可行走
    Climbable = 1 << 1,     // 可攀爬（墙壁、梯子）
    Jumpable = 1 << 2,      // 可跳跃通过
    Swimmable = 1 << 3,     // 可游泳
    Dangerous = 1 << 4,     // 危险区域（减速或伤害）
    Blocked = 1 << 5,       // 完全阻挡
                            // 可扩展更多...
}

public class NavigationGrid
{
   

    [Serializable]
    public struct NavCell
    {
        public CellFlag flags;
        public float height;      // 表面高度
        public byte cost;         // 寻路代价 (1-255)

        public bool HasFlag(CellFlag flag) => (flags & flag) != 0;
    }

    // 3D空间查询（攀爬、跳跃需要）
    private Dictionary<Vector3Int, NavCell> cells = new Dictionary<Vector3Int, NavCell>();

    // 2D地面查询（普通行走）
    private Dictionary<Vector2Int, NavCell> groundCells = new Dictionary<Vector2Int, NavCell>();

    private float cellSize;
    private Bounds worldBounds;

    // Layer 配置
    private LayerMask walkableLayer;
    private LayerMask climbableLayer;
    private LayerMask jumpableLayer;
    private LayerMask swimmableLayer;
    private LayerMask dangerousLayer;
    private LayerMask blockedLayer;
    public Dictionary<Vector3Int, NavCell> Cells => cells;
    public float CellSize => cellSize;
    public int CellCount => cells.Count;
    public int GroundCellCount => groundCells.Count;
    public Bounds WorldBounds => worldBounds;

    [Serializable]
    public class BuildSettings
    {
        public float cellSize = 1f;
        public float maxWalkableSlope = 45f;
        public float maxStepHeight = 0.5f;
        public float agentHeight = 2f;
        public float agentRadius = 0.5f;

        [Header("Layer Settings")]
        public LayerMask walkableLayer = 1;        // Default layer
        public LayerMask climbableLayer = 0;
        public LayerMask jumpableLayer = 0;
        public LayerMask swimmableLayer = 0;
        public LayerMask dangerousLayer = 0;
        public LayerMask blockedLayer = 0;

        [Header("Cost Settings")]
        public byte normalCost = 1;
        public byte climbCost = 3;
        public byte jumpCost = 2;
        public byte swimCost = 4;
        public byte dangerCost = 10;
    }
    private void ExpandBlockedXZOneCell(bool use8Neighbors = true)
    {
        if (cells.Count == 0) return;

        // 先收集所有“需要被变成Blocked”的key，避免边遍历边修改导致连锁扩散
        HashSet<Vector3Int> toBlock = new HashSet<Vector3Int>();

        // 4邻域 or 8邻域
        Vector3Int[] offsets4 =
        {
        new Vector3Int( 1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int( 0, 0, 1),
        new Vector3Int( 0, 0,-1),
    };

        Vector3Int[] offsets8 =
        {
        new Vector3Int( 1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int( 0, 0, 1),
        new Vector3Int( 0, 0,-1),
        new Vector3Int( 1, 0, 1),
        new Vector3Int( 1, 0,-1),
        new Vector3Int(-1, 0, 1),
        new Vector3Int(-1, 0,-1),
    };

        var offsets = use8Neighbors ? offsets8 : offsets4;

        foreach (var kv in cells)
        {
            if (kv.Value.flags != CellFlag.Blocked) continue;

            Vector3Int p = kv.Key;

            // 扩充一格：同一y层的邻居
            foreach (var off in offsets)
            {
                var n = p + off;
                toBlock.Add(n);
            }
        }

        // 应用：把这些格子标记为Blocked，并移除Walkable（如果存在）
        foreach (var key in toBlock)
        {
            if (cells.TryGetValue(key, out var c))
            {
                c.flags |= CellFlag.Blocked;
                c.flags &= ~CellFlag.Walkable;
                cells[key] = c;
            }
            else
            {
                // 如果希望“空气”也变成blocked占位（通常不建议），可以创建新cell：
                 cells[key] = new NavCell { flags = CellFlag.Blocked, height = 0, cost = 0 };

                // 更常见做法：只“污染”已有体素，不凭空创建
            }
        }
    }
    public void Build(BuildSettings settings)
    {
        cellSize = settings.cellSize;
        walkableLayer = settings.walkableLayer;
        climbableLayer = settings.climbableLayer;
        jumpableLayer = settings.jumpableLayer;
        swimmableLayer = settings.swimmableLayer;
        dangerousLayer = settings.dangerousLayer;
        blockedLayer = settings.blockedLayer;

        cells.Clear();
        groundCells.Clear();

        float cosMaxSlope = Mathf.Cos(settings.maxWalkableSlope * Mathf.Deg2Rad);

        // 收集所有静态物体
        MeshFilter[] filters = UnityEngine.Object.FindObjectsOfType<MeshFilter>();

        Vector3 min = Vector3.one * float.MaxValue;
        Vector3 max = Vector3.one * float.MinValue;

        // 按 Layer 分类收集三角形
        List<TriangleData> allTriangles = new List<TriangleData>();

        foreach (MeshFilter mf in filters)
        {
            if (!mf.gameObject.isStatic || mf.sharedMesh == null) continue;

            int layer = mf.gameObject.layer;
            CellFlag flag = GetFlagFromLayer(layer, settings);

            if (flag == CellFlag.None) continue; // 不相关的层跳过

            byte cost = GetCostFromFlag(flag, settings);

            Mesh m = mf.sharedMesh;
            Transform t = mf.transform;
            Vector3[] verts = m.vertices;
            int[] tris = m.triangles;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v0 = t.TransformPoint(verts[tris[i]]);
                Vector3 v1 = t.TransformPoint(verts[tris[i + 1]]);
                Vector3 v2 = t.TransformPoint(verts[tris[i + 2]]);
                Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

                allTriangles.Add(new TriangleData
                {
                    v0 = v0,
                    v1 = v1,
                    v2 = v2,
                    normal = normal,
                    flag = flag,
                    cost = cost
                });

                min = Vector3.Min(min, Vector3.Min(v0, Vector3.Min(v1, v2)));
                max = Vector3.Max(max, Vector3.Max(v0, Vector3.Max(v1, v2)));
            }
        }

        worldBounds = new Bounds();
        worldBounds.SetMinMax(min - Vector3.one * cellSize, max + Vector3.one * cellSize);

        Debug.Log($"Processing {allTriangles.Count} triangles...");

        // 体素化
        foreach (var tri in allTriangles)
        {
            VoxelizeTriangle(tri, cosMaxSlope, settings);
        }

        // 后处理：清理头顶有障碍的点
        //CleanupBlockedCells(settings);

        // 生成地面层（2D）
        GenerateGroundLayer();
        //ErodeGroundByRadius(settings);
        ExpandBlockedXZOneCell();
        Debug.Log($"Build complete: {cells.Count} 3D cells, {groundCells.Count} ground cells");
    }

    private struct TriangleData
    {
        public Vector3 v0, v1, v2, normal;
        public CellFlag flag;
        public byte cost;
    }

    private CellFlag GetFlagFromLayer(int layer, BuildSettings settings)
    {
        int layerMask = 1 << layer;

        if ((settings.blockedLayer & layerMask) != 0)
            return CellFlag.Blocked;
        if ((settings.climbableLayer & layerMask) != 0)
            return CellFlag.Climbable;
        if ((settings.jumpableLayer & layerMask) != 0)
            return CellFlag.Jumpable;
        if ((settings.swimmableLayer & layerMask) != 0)
            return CellFlag.Swimmable;
        if ((settings.dangerousLayer & layerMask) != 0)
            return CellFlag.Dangerous | CellFlag.Walkable;
        if ((settings.walkableLayer & layerMask) != 0)
            return CellFlag.Walkable;

        return CellFlag.None;
    }

    private byte GetCostFromFlag(CellFlag flag, BuildSettings settings)
    {
        if (flag.HasFlag(CellFlag.Dangerous)) return settings.dangerCost;
        if (flag.HasFlag(CellFlag.Swimmable)) return settings.swimCost;
        if (flag.HasFlag(CellFlag.Climbable)) return settings.climbCost;
        if (flag.HasFlag(CellFlag.Jumpable)) return settings.jumpCost;
        return settings.normalCost;
    }

    private void VoxelizeTriangle(TriangleData tri, float cosMaxSlope, BuildSettings settings)
    {
        Vector3 triMin = Vector3.Min(tri.v0, Vector3.Min(tri.v1, tri.v2));
        Vector3 triMax = Vector3.Max(tri.v0, Vector3.Max(tri.v1, tri.v2));

        float padding = cellSize * 0.5f;
        int minX = Mathf.FloorToInt((triMin.x - padding) / cellSize);
        int maxX = Mathf.CeilToInt((triMax.x + padding) / cellSize);
        int minY = Mathf.FloorToInt((triMin.y - padding) / cellSize);
        int maxY = Mathf.CeilToInt((triMax.y + padding) / cellSize);
        int minZ = Mathf.FloorToInt((triMin.z - padding) / cellSize);
        int maxZ = Mathf.CeilToInt((triMax.z + padding) / cellSize);

        CellFlag flag = tri.flag;
        bool ignoreTriangle = false;
        // 特殊处理：可行走表面需要检查坡度
        if (flag == CellFlag.Walkable)
        {
            if ( tri.normal.y >= cosMaxSlope)
            {
                flag = CellFlag.Walkable;
            }
            else if (tri.normal.y < -0.1f)
            {
                ignoreTriangle = true;
            }
            else
            {
                flag = CellFlag.Blocked;
            }
        }
        Vector3 boxExtents = Vector3.one * (cellSize * 0.5f);

        // 特殊处理：攀爬面应该是接近垂直的
        if (flag == CellFlag.Climbable)
        {
            if (Mathf.Abs(tri.normal.y) > 0.3f)
            {
                // 不够陡，当作普通地面
                flag = CellFlag.Walkable;
            }
        }
        if (ignoreTriangle) return;
        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    Vector3 cellCenter = new Vector3(
                        (x + 0.5f) * cellSize,
                        (y + 0.5f) * cellSize,
                        (z + 0.5f) * cellSize
                    );
                    if (!TestAABBTriangleOverlap(cellCenter, boxExtents, tri.v0, tri.v1, tri.v2))
                        continue;

                    Vector3Int key = new Vector3Int(x, y, z);

                    if (cells.TryGetValue(key, out NavCell existing))
                    {
                        existing.flags |= flag;
                        // 合并标志，保留更高优先级
                        if (flag == CellFlag.Walkable)
                        {
                            // 如果之前没有地面，或者新地面比旧地面高，更新高度
                            if (!existing.HasFlag(CellFlag.Walkable) || cellCenter.y > existing.height)
                            {
                                existing.height = cellCenter.y; // 这里可以用更精确的交点高度，但中心点通常够用
                            }
                        }
              
                        existing.cost = Math.Max(existing.cost, tri.cost);
                        cells[key] = existing;
                    }
                    else
                    {
                        cells[key] = new NavCell
                        {
                            flags = flag,
                            height = cellCenter.y,
                            cost = tri.cost
                        };
                    }
                }
    }
    private static bool TestAABBTriangleOverlap(Vector3 boxCenter, Vector3 boxExtents, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        // 将三角形顶点转换到以 boxCenter 为原点的空间
        v0 -= boxCenter;
        v1 -= boxCenter;
        v2 -= boxCenter;

        // 1. 测试 AABB 的 3 个轴 (x, y, z)
        // 也就是测试三角形的 AABB 是否与 Box 的 AABB 相交
        float min, max;

        // X 轴
        min = Mathf.Min(v0.x, Mathf.Min(v1.x, v2.x));
        max = Mathf.Max(v0.x, Mathf.Max(v1.x, v2.x));
        if (min > boxExtents.x || max < -boxExtents.x) return false;

        // Y 轴
        min = Mathf.Min(v0.y, Mathf.Min(v1.y, v2.y));
        max = Mathf.Max(v0.y, Mathf.Max(v1.y, v2.y));
        if (min > boxExtents.y || max < -boxExtents.y) return false;

        // Z 轴
        min = Mathf.Min(v0.z, Mathf.Min(v1.z, v2.z));
        max = Mathf.Max(v0.z, Mathf.Max(v1.z, v2.z));
        if (min > boxExtents.z || max < -boxExtents.z) return false;

        // 2. 测试三角形法线 (平面测试)
        Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
        float planeDist = Vector3.Dot(normal, v0); // 因为已经平移过，这就是原点到平面的距离
        // 计算 Box 在法线方向上的投影半径
        float r = boxExtents.x * Mathf.Abs(normal.x) + boxExtents.y * Mathf.Abs(normal.y) + boxExtents.z * Mathf.Abs(normal.z);

        // 检查 Box 中心到平面的距离是否大于投影半径
        // 注意：这里 v0 是相对于 boxCenter 的，所以 dot(normal, v0) 其实就是平面方程的 d
        // 实际上我们需要比较的是原点(boxCenter)到平面的距离。
        // 平面方程: N dot P = d.  d = dot(normal, v0_original).
        // 在相对空间中，平面方程是 dot(normal, p) = dot(normal, v0).
        // 原点到平面距离是 dot(normal, v0).
        if (Mathf.Abs(Vector3.Dot(normal, v0)) > r) return false;

        // 3. 测试 9 条交叉轴 (Edge Cross Products)
        Vector3[] edges = new Vector3[] { v1 - v0, v2 - v1, v0 - v2 };
        Vector3[] axes = new Vector3[] { Vector3.right, Vector3.up, Vector3.forward };

        for (int i = 0; i < 3; i++) // edges
        {
            for (int j = 0; j < 3; j++) // box axes
            {
                Vector3 axis = Vector3.Cross(axes[j], edges[i]);
                if (axis == Vector3.zero) continue; // 平行，忽略

                // 投影三角形
                float p0 = Vector3.Dot(v0, axis);
                float p1 = Vector3.Dot(v1, axis);
                float p2 = Vector3.Dot(v2, axis);

                min = Mathf.Min(p0, Mathf.Min(p1, p2));
                max = Mathf.Max(p0, Mathf.Max(p1, p2));

                // 投影 Box (半径)
                r = boxExtents.x * Mathf.Abs(axis.x) + boxExtents.y * Mathf.Abs(axis.y) + boxExtents.z * Mathf.Abs(axis.z);

                if (min > r || max < -r) return false;
            }
        }

        return true;
    }
   

    /// <summary>
    /// 检查点是否在三角形内部或边缘附近
    /// </summary>
  
   
    private void GenerateGroundLayer()
    {
        groundCells.Clear();

        // 从3D cells生成2D地面层：对同一列(x,z)选最低的可走高度
        foreach (var kvp in cells)
        {
            if (!kvp.Value.HasFlag(CellFlag.Walkable)) continue;

            Vector2Int xzKey = new Vector2Int(kvp.Key.x, kvp.Key.z);

            if (groundCells.TryGetValue(xzKey, out var existing))
            {
                // 保留最低的可行走点（更像“地面层”）
                if (kvp.Value.height < existing.height)
                    groundCells[xzKey] = kvp.Value;
            }
            else
            {
                groundCells[xzKey] = kvp.Value;
            }
        }
    }
    #region 查询接口

    public bool IsWalkable(Vector3 position)
    {
        Vector2Int key = new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
        return groundCells.ContainsKey(key);
    }

    public bool TryGetGroundCell(Vector3 position, out NavCell cell)
    {
        Vector2Int key = new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
        return groundCells.TryGetValue(key, out cell);
    }

    public bool TryGetCell(Vector3 position, out NavCell cell)
    {
        Vector3Int key = Vector3Int.FloorToInt(position / cellSize);
        return cells.TryGetValue(key, out cell);
    }

    public bool CanClimbAt(Vector3 position)
    {
        Vector3Int key = Vector3Int.FloorToInt(position / cellSize);
        return cells.TryGetValue(key, out var cell) && cell.HasFlag(CellFlag.Climbable);
    }

    public bool CanJumpThrough(Vector3 position)
    {
        Vector3Int key = Vector3Int.FloorToInt(position / cellSize);
        return cells.TryGetValue(key, out var cell) && cell.HasFlag(CellFlag.Jumpable);
    }

    // 获取某点可达的邻居（支持攀爬和跳跃）
    public List<NavNeighbor> GetReachableNeighbors(Vector3Int pos, bool canClimb, bool canJump, float maxStepHeight)
    {
        List<NavNeighbor> neighbors = new List<NavNeighbor>();

        if (!cells.TryGetValue(pos, out var currentCell))
            return neighbors;

        int stepCells = Mathf.CeilToInt(maxStepHeight / cellSize);

        // 8方向水平移动
        int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int[] dz = { -1, -1, -1, 0, 0, 1, 1, 1 };

        for (int i = 0; i < 8; i++)
        {
            // 检查同高度和上下一格
            for (int dy = -stepCells; dy <= stepCells; dy++)
            {
                Vector3Int neighborKey = pos + new Vector3Int(dx[i], dy, dz[i]);

                if (cells.TryGetValue(neighborKey, out var neighborCell))
                {
                    if (neighborCell.HasFlag(CellFlag.Walkable))
                    {
                        neighbors.Add(new NavNeighbor
                        {
                            position = neighborKey,
                            cost = neighborCell.cost,
                            moveType = MoveType.Walk
                        });
                        break; // 找到一个就跳出dy循环
                    }
                }
            }
        }

        // 攀爬移动（垂直）
        if (canClimb)
        {
            for (int dy = -1; dy <= 1; dy += 2)
            {
                Vector3Int climbKey = pos + new Vector3Int(0, dy, 0);

                if (cells.TryGetValue(climbKey, out var climbCell))
                {
                    if (climbCell.HasFlag(CellFlag.Climbable))
                    {
                        neighbors.Add(new NavNeighbor
                        {
                            position = climbKey,
                            cost = climbCell.cost,
                            moveType = MoveType.Climb
                        });
                    }
                }
            }
        }

        // 跳跃移动（穿过障碍）
        if (canJump)
        {
            for (int i = 0; i < 8; i++)
            {
                // 跳跃可以越过1-2格障碍
                for (int dist = 2; dist <= 3; dist++)
                {
                    Vector3Int jumpKey = pos + new Vector3Int(dx[i] * dist, 0, dz[i] * dist);

                    if (cells.TryGetValue(jumpKey, out var jumpCell))
                    {
                        if (jumpCell.HasFlag(CellFlag.Walkable) || jumpCell.HasFlag(CellFlag.Jumpable))
                        {
                            // 检查中间是否有可跳跃标记
                            bool canJumpOver = true;
                            for (int d = 1; d < dist; d++)
                            {
                                Vector3Int midKey = pos + new Vector3Int(dx[i] * d, 0, dz[i] * d);
                                if (cells.TryGetValue(midKey, out var midCell))
                                {
                                    if (!midCell.HasFlag(CellFlag.Jumpable) && midCell.HasFlag(CellFlag.Blocked))
                                    {
                                        canJumpOver = false;
                                        break;
                                    }
                                }
                            }

                            if (canJumpOver)
                            {
                                neighbors.Add(new NavNeighbor
                                {
                                    position = jumpKey,
                                    cost = (byte)(jumpCell.cost + dist), // 跳跃代价更高
                                    moveType = MoveType.Jump
                                });
                            }
                        }
                    }
                }
            }
        }

        return neighbors;
    }

    #endregion

    #region 可视化

    public void DrawGizmos(bool showAll = false)
    {
        foreach (var kvp in cells)
        {
            Vector3 center = (Vector3)kvp.Key * cellSize + Vector3.one * cellSize * 0.5f;
            NavCell cell = kvp.Value;

            // 先画“双标记”——最重要的调试信号
            if (cell.HasFlag(CellFlag.Walkable) && cell.HasFlag(CellFlag.Blocked))
            {
                Gizmos.color = new Color(1f, 0f, 1f, 0.7f); // 紫色：矛盾格
            }
            else if (cell.HasFlag(CellFlag.Walkable))
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.2f); // 绿：Walkable
            }
            else if (cell.HasFlag(CellFlag.Climbable))
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.4f); // 黄：Climbable
            }
            else if (cell.HasFlag(CellFlag.Blocked))
            {
                if (!showAll) continue;
                Gizmos.color = new Color(1f, 0f, 0f, 0.2f); // 红：Blocked
            }
            else
            {
                continue;
            }

            Gizmos.DrawWireCube(center, Vector3.one * cellSize * 0.9f);
        }
    }

    public void DrawGroundGizmos()
    {
        foreach (var kvp in groundCells)
        {
            Vector3 pos = new Vector3(
                (kvp.Key.x + 0.5f) * cellSize,
                kvp.Value.height,
                (kvp.Key.y + 0.5f) * cellSize
            );

            Gizmos.color = kvp.Value.HasFlag(CellFlag.Dangerous)
                ? new Color(1, 0.5f, 0, 0.5f)
                : new Color(0, 1, 0, 0.5f);

            Gizmos.DrawCube(pos, new Vector3(cellSize * 0.8f, 0.1f, cellSize * 0.8f));
        }
    }

    #endregion

    public struct NavNeighbor
    {
        public Vector3Int position;
        public byte cost;
        public MoveType moveType;
    }

    public enum MoveType
    {
        Walk,
        Climb,
        Jump,
        Swim
    }
}