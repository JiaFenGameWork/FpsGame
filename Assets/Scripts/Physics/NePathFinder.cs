using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;
using static NavigationGrid; // 引用你的 NavCell 定义

public class NePathFinder
{
    private Terrain terrain;
    private readonly NavMeshAsset _nav;
    private readonly float _maxStepHeight; // 最大上坡高度
    private readonly float _maxDropHeight; // 最大下坡高度
    private readonly float _cellSize;
    public DebugContext LastDebugData;
    public class DebugContext
    {
        public List<PathNode> VisitedNodes = new List<PathNode>(); // 已经搜索过的点 (ClosedSet)
        public List<Vector3> FailedEdges = new List<Vector3>();    // 尝试过但失败的边（用于画线）
        public Vector3 StartSnapPos;
        public Vector3 EndSnapPos;
        public string FailureReason;
    }
    // 节点类，实现堆接口
    public class PathNode : IHeapItem<PathNode>
    {
        public Vector3Int GridIndex;
        public Vector3 WorldPosition; // 缓存精确的世界坐标（包含 NavCell.height）
        public PathNode Parent;

        public float G;
        public float H;
        public float F => G + H;

        public bool Closed; // 替代 ClosedSet HashSet，性能更高
        int heapIndex;

        public PathNode(Vector3Int gridIndex, Vector3 worldPos)
        {
            GridIndex = gridIndex;
            WorldPosition = worldPos;
        }

        public int HeapIndex
        {
            get => heapIndex;
            set => heapIndex = value;
        }

        public int CompareTo(PathNode other)
        {
            int compare = F.CompareTo(other.F);
            if (compare == 0)
            {
                compare = H.CompareTo(other.H);
            }
            return -compare; // MinHeap 需要反转比较结果
        }
    }

    public NePathFinder(NavMeshAsset nav, Terrain terrain,float maxStepHeight = 0.6f, float maxDropHeight = 2.0f)
    {
        this.terrain = terrain;
        _nav = nav;
        _cellSize = nav.cellSize;
        _maxStepHeight = maxStepHeight;
        _maxDropHeight = maxDropHeight;
    }

    /// <summary>
    /// 核心寻路方法
    /// </summary>
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        LastDebugData = new DebugContext();
        // 1. 坐标转换与吸附
        Vector3Int startGrid = WorldToGrid(startPos);
        Vector3Int targetGrid = WorldToGrid(targetPos);
        UnityEngine.Debug.Log("开始寻路");
        // 如果仍无效，直接返回
        if (!_nav.bounds.Contains(startGrid) || !_nav.bounds.Contains(targetGrid))
        {
            UnityEngine.Debug.LogWarning("起点或终点在地图边界外");
            return null;
        }
        if (IsBlocked(targetGrid))
        {
            UnityEngine.Debug.LogWarning("起点和终点被阻挡");
            return null;
        }
        // 2. 初始化数据结构
        // 预估堆大小：格子总数的一小部分，或者硬编码一个足够大的数
        MinHeap<PathNode> openSet = new MinHeap<PathNode>(2048);
        Dictionary<Vector3Int, PathNode> allNodes = new Dictionary<Vector3Int, PathNode>();
        LastDebugData.StartSnapPos = startGrid;
        LastDebugData.EndSnapPos = GetMixedWorldPos(targetGrid);
        PathNode startNode = new PathNode(startGrid, GetCellWorldPos(startGrid));
        startNode.G = 0;
        startNode.H = Vector3.Distance(startNode.WorldPosition, targetPos);

        openSet.Add(startNode);
        allNodes.Add(startGrid, startNode);

        int iterations = 0;
        int maxIterations = 5000; // 防止死循环

        while (openSet.Count > 0)
        {
            iterations++;
            if (iterations > maxIterations) break;

            PathNode current = openSet.RemoveFirst();
            current.Closed = true;
            LastDebugData.VisitedNodes.Add(current);
            // 到达判断 (使用 Grid 距离更稳定，或者用距离阈值)
            if (current.GridIndex == targetGrid || Vector3.Distance(current.WorldPosition, targetPos) < _cellSize)
            {
                return RetracePath(startNode, current, targetPos);
            }

            // 获取邻居
            foreach (Vector3Int neighborGrid in GetNeighbors(current.GridIndex))
            {
                // 如果是障碍物，直接跳过
                if (_nav.cells.TryGetValue(neighborGrid, out NavCell cell))
                {
                    if (cell.flags == CellFlag.Blocked) continue;
                }

                // 2. 获取高度：如果格子存在用格子高度，不存在用 Terrain 高度
                Vector3 neighborWorldPos = GetMixedWorldPos(neighborGrid);

                // --- 核心修改结束 ---

                // 3. 物理/高度检查
                // 即使依靠物理系统，寻路也应该避免让角色走悬崖，否则物理系统会让角色卡住或滑落
                float heightDiff = neighborWorldPos.y - current.WorldPosition.y;
                bool isWalkable = true;
                // 简单的坡度限制（可选：如果你希望完全由物理决定，可以注释掉这两行）
                if (heightDiff > _maxStepHeight) isWalkable = false; // 太高爬不上去
                if (heightDiff < -_maxDropHeight) isWalkable = false;// 太深不敢跳
                if (!isWalkable)
                {
                    LastDebugData.FailedEdges.Add(current.WorldPosition);
                    LastDebugData.FailedEdges.Add(neighborWorldPos);
                    continue;
                }

                if (!allNodes.TryGetValue(neighborGrid, out PathNode neighborNode))
                {
                    neighborNode = new PathNode(neighborGrid, neighborWorldPos);
                    allNodes[neighborGrid] = neighborNode;
                }

                if (neighborNode.Closed) continue;

                float distanceCost = Vector3.Distance(current.WorldPosition, neighborWorldPos);
                float newG = current.G + distanceCost;

                if (newG < neighborNode.G || !openSet.Contains(neighborNode))
                {
                    neighborNode.G = newG;
                    neighborNode.H = Vector3.Distance(neighborWorldPos, targetPos);
                    neighborNode.Parent = current;

                    if (!openSet.Contains(neighborNode))
                        openSet.Add(neighborNode);
                    else
                        openSet.UpdateItem(neighborNode);
                }
            }
        }
        if (string.IsNullOrEmpty(LastDebugData.FailureReason))
            LastDebugData.FailureReason = "OpenSet Empty (无路可走)";

        UnityEngine.Debug.LogError($"Pathfinding Failed: {LastDebugData.FailureReason}. Visited Nodes: {LastDebugData.VisitedNodes.Count}");
        return null; // 没找到路径
    }

    public IEnumerator FindPathAsy(Vector3 startPos, Vector3 targetPos, Action<List<Vector3>> ondone)
    {

        LastDebugData = new DebugContext();
        // 1. 坐标转换与吸附
        Vector3Int startGrid = WorldToGrid(startPos);
        Vector3Int targetGrid = WorldToGrid(targetPos);

        // 如果仍无效，直接返回
        if (!_nav.bounds.Contains(startGrid) || !_nav.bounds.Contains(targetGrid))
        {
            UnityEngine.Debug.LogWarning("起点或终点在地图边界外");
            ondone?.Invoke(null);
            yield break;
        }
        if (IsBlocked(targetGrid))
        {
            UnityEngine.Debug.LogWarning("起点和终点被阻挡");
            ondone?.Invoke(null);
            yield break;
        }

        // 2. 初始化数据结构
        // 预估堆大小：格子总数的一小部分，或者硬编码一个足够大的数
        MinHeap<PathNode> openSet = new MinHeap<PathNode>(2048);
        Dictionary<Vector3Int, PathNode> allNodes = new Dictionary<Vector3Int, PathNode>();
        LastDebugData.StartSnapPos = startGrid;
        LastDebugData.EndSnapPos = GetMixedWorldPos(targetGrid);
        PathNode startNode = new PathNode(startGrid, startPos);
        startNode.G = 0;
        startNode.H = Vector3.Distance(startNode.WorldPosition, targetPos);

        openSet.Add(startNode);
        allNodes.Add(startGrid, startNode);

        int iterations = 0;
        int maxIterations = 5000; // 防止死循环
        int nodesProcessedThisFrame = 0;
        const int maxNodesPerFrame = 50; // 每帧最多处理50个节点，可根据性能调整
        
        while (openSet.Count > 0)
        {
            iterations++;
            if (iterations > maxIterations)
            {
                ondone?.Invoke(null);
                yield break;
            }
            
            PathNode current = openSet.RemoveFirst();
            current.Closed = true;
            LastDebugData.VisitedNodes.Add(current);
            
            // 到达判断 (使用 Grid 距离更稳定，或者用距离阈值)
            if (current.GridIndex == targetGrid || Vector2.Distance(new Vector2 (current.GridIndex.x, current.GridIndex.z), new Vector2 (targetGrid.x, targetGrid.z)) < _cellSize)
            {
                var path = RetracePath(startNode, current, targetPos);
                ondone?.Invoke(path);
                yield break;
            }

            // 获取邻居
            foreach (Vector3Int neighborGrid in GetNeighbors(current.GridIndex))
            {
                Vector3Int neighborGridY = new Vector3Int(neighborGrid.x, neighborGrid.y + (int)_cellSize, neighborGrid.z);
                // 如果是障碍物，直接跳过
                if (_nav.cells.TryGetValue(neighborGridY, out NavCell cell))
                {
                    if (cell.flags == CellFlag.Blocked) continue;
                }

                // 2. 获取高度：如果格子存在用格子高度，不存在用 Terrain 高度
                Vector3 neighborWorldPos = GetMixedWorldPos(neighborGrid);

                // --- 核心修改结束 ---

                // 3. 物理/高度检查
                // 即使依靠物理系统，寻路也应该避免让角色走悬崖，否则物理系统会让角色卡住或滑落
                float heightDiff = neighborWorldPos.y - current.WorldPosition.y;
                bool isWalkable = true;
                // 简单的坡度限制（可选：如果你希望完全由物理决定，可以注释掉这两行）
               // if (heightDiff > _maxStepHeight) isWalkable = false; // 太高爬不上去
              //  if (heightDiff < -_maxDropHeight) isWalkable = false;// 太深不敢跳
                if (!isWalkable)
                {
                    LastDebugData.FailedEdges.Add(current.WorldPosition);
                    LastDebugData.FailedEdges.Add(neighborWorldPos);
                    continue;
                }

                if (!allNodes.TryGetValue(neighborGrid, out PathNode neighborNode))
                {
                    neighborNode = new PathNode(neighborGrid, neighborWorldPos);
                    allNodes[neighborGrid] = neighborNode;
                }

                if (neighborNode.Closed) continue;

                float distanceCost = Vector3.Distance(current.WorldPosition, neighborWorldPos);
                float newG = current.G + distanceCost;

                if (newG < neighborNode.G || !openSet.Contains(neighborNode))
                {
                    neighborNode.G = newG;
                    neighborNode.H = Vector3.Distance(neighborWorldPos, targetPos);
                    neighborNode.Parent = current;

                    if (!openSet.Contains(neighborNode))
                        openSet.Add(neighborNode);
                    else
                        openSet.UpdateItem(neighborNode);
                }
            }
            
            // 每处理完一个节点后检查是否需要分帧
            nodesProcessedThisFrame++;
            if (nodesProcessedThisFrame >= maxNodesPerFrame)
            {
                nodesProcessedThisFrame = 0;
                yield return null; // 下一帧继续
            }
        }
        if (string.IsNullOrEmpty(LastDebugData.FailureReason))
            LastDebugData.FailureReason = "OpenSet Empty (无路可走)";

        UnityEngine.Debug.LogError($"Pathfinding Failed: {LastDebugData.FailureReason}. Visited Nodes: {LastDebugData.VisitedNodes.Count}");
        ondone?.Invoke(null); // 没找到路径
    }
    private Vector3 GetMixedWorldPos(Vector3Int grid)
    {
        float x = (grid.x + 0.5f) * _cellSize;
        float z = (grid.z + 0.5f) * _cellSize;
        float y = 0;

        // 策略：
        // 1. 尝试从烘焙数据获取（最快，最准确）
        if (_nav.cells.TryGetValue(grid, out NavCell cell))
        {
            y = cell.height;
        }
        // 2. 如果没有烘焙数据，从 Terrain 采样（较慢，作为回退）
        else if (terrain != null)
        {
            // SampleHeight 需要世界坐标的 X, Z
            y = terrain.SampleHeight(new Vector3(x, 0, z)) + terrain.transform.position.y;
        }
        else
        {
            // 3. 既没有烘焙也没有地形，只能假设是 0 或者射线检测（这里简单给 0）
            y = 0;
        }

        return new Vector3(x, y, z);
    }
    bool IsBlocked(Vector3Int current)
    {
        _nav.cells.TryGetValue(current, out NavCell cell);
        if(cell.flags == CellFlag.Blocked&&cell.flags != CellFlag.Walkable)
        {
            return true;
        }
        return false;
    }
    /// <summary>
    /// 获取有效的邻居（处理 3D 结构：平地、上坡、下坡）
    /// </summary>
    private IEnumerable<Vector3Int> GetNeighbors(Vector3Int centerGrid)
    {
        // 8 方向遍历
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0) continue;

                // 简单的 2D 网格邻居，不考虑 Y 轴变化 (Y轴由地形决定)
                yield return new Vector3Int(centerGrid.x + x, centerGrid.y, centerGrid.z + z);
            }
        }
    }

    private List<Vector3> RetracePath(PathNode startNode, PathNode endNode, Vector3 actualTarget)
    {
        List<Vector3> path = new List<Vector3>();
        PathNode currentNode = endNode;

        // 可以在这里把 actualTarget 加进去作为最后一个点
        path.Add(actualTarget);

        while (currentNode != startNode)
        {
            path.Add(currentNode.WorldPosition);
            currentNode = currentNode.Parent;
        }
        // path.Add(startNode.WorldPosition); // 可选：是否包含起点
        path.Reverse();
        return path;
    }

    // --- 辅助方法 ---

    private Vector3Int WorldToGrid(Vector3 pos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(pos.x / _cellSize),
            Mathf.FloorToInt(pos.y / _cellSize),
            Mathf.FloorToInt(pos.z / _cellSize)
        );
    }

    private Vector3 GetCellWorldPos(Vector3Int grid)
    {
        if (_nav.cells.TryGetValue(grid, out NavCell cell))
        {
            // X, Z 是格子中心，Y 是精确高度
            return new Vector3(
                (grid.x + 0.5f) * _cellSize,
                cell.height,
                (grid.z + 0.5f) * _cellSize
            );
        }
        return Vector3.zero;
    }
    private float GetHeuristic(Vector3Int a, Vector3Int b)
    {
        // 曼哈顿距离或欧几里得距离
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
    }
    // 简单的广度优先搜索，寻找最近的可行走格子
    private Vector3Int FindNearestWalkableCell(Vector3Int start)
    {
        if (_nav.cells.ContainsKey(start) && _nav.cells[start].flags==CellFlag.Walkable) return start;

        // 简单搜一下周围
        for (int r = 1; r <= 2; r++)
        {
            for (int x = -r; x <= r; x++)
                for (int y = -r; y <= r; y++)
                    for (int z = -r; z <= r; z++)
                    {
                        Vector3Int p = start + new Vector3Int(x, y, z);
                        if (_nav.cells.TryGetValue(p, out NavCell cell) && cell.flags==CellFlag.Walkable)
                            return p;
                    }
        }
        return start;
    }
}