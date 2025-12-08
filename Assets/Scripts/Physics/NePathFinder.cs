using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;

public class NePathFinder
{
    private OctreeAsset _octree;

    // 高度差代价系数：高度差越大，代价越高
    public float _heightCostMultiplier = 2.0f;

    // 最大可通行高度差：超过此值视为不可通行（用于阻挡高墙）
    public float _maxTraversableHeight = 1.5f;

    // 最大可跨越的单步高度差（用于台阶/斜坡判断）
    public float _maxStepHeight = 0.5f;
    public float CharacterHeight = 0f;
    public List<Vector3> debugPathPoints = new List<Vector3>();


    public NePathFinder(OctreeAsset octree, float hcost, float characterHeight ,float maxTraversableHeight = 1.5f, float maxStepHeight = 0.5f)
    {
        _octree = octree;
        _heightCostMultiplier = hcost;
        _maxTraversableHeight = maxTraversableHeight;
        _maxStepHeight = maxStepHeight;
        CharacterHeight = characterHeight;
    }

    public class PathNode
    {
        public Vector3Int GridIndex;
        public Vector3 WorldPosition;
        public PathNode Parent;
        public float G;
        public float H;
        public float F => G + H;

        public PathNode( Vector3 pos,float minSize)
        {

            WorldPosition = pos;
            GridIndex = Vector3Int.RoundToInt(pos/ minSize);
        }
    }
    Stopwatch swTotal = new Stopwatch();
    Stopwatch swSort = new Stopwatch();
    Stopwatch swGetNeighbors = new Stopwatch();
    Stopwatch swMoveCost = new Stopwatch();
    int sortCalls = 0;
    int neighborCalls = 0;
    int moveCostCalls = 0;

    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        debugPathPoints.Clear();

        if (SparseOctree.FindNodeXYZ(_octree,startPos) != null)
        {
            UnityEngine.Debug.LogError("起点被node卡住啦");
        }
        float arrivalThreshold = _octree.MinSize*3f;

        List<PathNode> openList = new List<PathNode>();
        Dictionary<Vector3Int, PathNode> allNodes = new Dictionary<Vector3Int, PathNode>();
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        PathNode startPathNode = new PathNode(startPos,_octree.MinSize);

        //加进去
        startPathNode.G = 0;
        startPathNode.H = CalculateHeuristic(startPos, targetPos);
        openList.Add(startPathNode);
        allNodes[startPathNode.GridIndex] = startPathNode;
        int maxIterations = 10000;
        int iterations = 0;

        swTotal.Start();
        while (openList.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            swSort.Start();
            openList.Sort((a, b) => a.F.CompareTo(b.F));
            swSort.Stop();
            sortCalls++;
            PathNode current = openList[0];
            openList.RemoveAt(0);

            if (closedSet.Contains(current.GridIndex))
                continue;

            closedSet.Add(current.GridIndex);

            // 使用距离判断到达终点
            float distToTarget = Vector3.Distance(current.WorldPosition,targetPos);

            if (distToTarget < 3f)
            {
                List<Vector3> path = RetracePath(current, targetPos);
                debugPathPoints = path;
                swTotal.Stop();
                UnityEngine.Debug.Log($"=== 寻路性能分析 ===\n" +
       $"总耗时: {swTotal.ElapsedMilliseconds}ms\n" +
       $"排序: {swSort.ElapsedMilliseconds}ms ({sortCalls}次) - {100.0 * swSort.ElapsedTicks / swTotal.ElapsedTicks:F1}%\n" +
       $"获取邻居: {swGetNeighbors.ElapsedMilliseconds}ms ({neighborCalls}次) - {100.0 * swGetNeighbors.ElapsedTicks / swTotal.ElapsedTicks:F1}%\n" +
       $"移动代价: {swMoveCost.ElapsedMilliseconds}ms ({moveCostCalls}次) - {100.0 * swMoveCost.ElapsedTicks / swTotal.ElapsedTicks:F1}%");
                swTotal.Reset();
                swGetNeighbors.Reset();
                swMoveCost.Reset();
                swSort.Reset(); 
                return path;
            }

            var neighbors = GetNeighbors(current);

            foreach (var neighbor in neighbors)
            {
                if (closedSet.Contains(neighbor.GridIndex))
                    continue;
                swMoveCost.Start();
                // 计算移动代价：水平距离 + 高度差惩罚

                float costG = CalculateMovementCost(current.WorldPosition, neighbor.WorldPosition, 2);
                if (costG >= 200000f)
                {
                    continue;
                }
                float newG = current.G + costG;
                swMoveCost.Stop(); moveCostCalls++;
                if (allNodes.TryGetValue(neighbor.GridIndex, out PathNode existingNode))
                {
                    if (newG < existingNode.G)
                    {
                        existingNode.G = newG;
                        existingNode.H = CalculateHeuristic(neighbor.WorldPosition, targetPos);
                        existingNode.Parent = current;
                        existingNode.WorldPosition = neighbor.WorldPosition;

                        if (!openList.Contains(existingNode))
                        {
                            openList.Add(existingNode);
                        }
                    }
                }
                else
                {
                    neighbor.G = newG;
                    neighbor.H = CalculateHeuristic(neighbor.WorldPosition, targetPos);
                    neighbor.Parent = current;

                    openList.Add(neighbor);
                    allNodes[neighbor.GridIndex] = neighbor;
                }
            }
        }

        UnityEngine.Debug.LogWarning($"未找到路径! 迭代次数: {iterations}");
        return null;
    }
    private float CalculateHeuristic(Vector3 from, Vector3 to)
    {
        float distance = Vector3.Distance(from, to);

        return distance; 
    }

    /// <summary>
    /// 计算实际移动代价 G：水平距离 + 高度差代价（包含坡度惩罚）
    /// </summary>
    private float CalculateMovementCost(Vector3 from, Vector3 to,float threshold)
    {
        // 1. 基础物理距离 (走斜边)
        float distance = Vector3.Distance(from, to);
        float height =  SparseOctree.FindNodeAxis(_octree, to, Vector3.up, 0, 3,2);

        float cost = distance+height;

        return cost;
    }


    /// <summary>
    /// 获取邻居节点 - 加入高度差限制和路径可通行性检测
    /// </summary>
    private List<PathNode> GetNeighbors(PathNode current)
    {
        List<PathNode> neighbors = new List<PathNode>();
        float halfsize = _octree.MinSize/2f;
        Vector3 currentPos = current.WorldPosition;

        // 8个方向：4个正方向 + 4个对角线方向
        Vector3[] directions = {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            new Vector3(1, 0, 1).normalized,   // 右前
            new Vector3(-1, 0, 1).normalized,  // 左前
            new Vector3(1, 0, -1).normalized,  // 右后
            new Vector3(-1, 0, -1).normalized  // 左后
        };

        foreach (var dir in directions)
        {
            float Distance = _octree.MinSize + 0.1f;

            // 对角线方向需要更长的探测距离
            if (Mathf.Abs(dir.x) > 0.5f && Mathf.Abs(dir.z) > 0.5f)
            {
                Distance *= 1.414f; // sqrt(2)
            }

            Vector3 TargetPos = currentPos + dir * Distance;
            Vector3 rayStart = TargetPos + Vector3.up * 2f;
            swGetNeighbors.Start();
            bool hitGround = false;
            if (SparseOctree.FindNodeFirst(_octree, rayStart, Vector3.down, 6f))
            {
                hitGround = true;
            }
            swGetNeighbors.Stop();
            neighborCalls++;
            if (hitGround)
            {
                neighbors.Add(new PathNode(TargetPos, halfsize * 2f));
            }

        }
        return neighbors;
        }
    private List<Vector3> RetracePath(PathNode endNode, Vector3 actualTargetPos)
    {
        List<Vector3> path = new List<Vector3>();
        PathNode currentNode = endNode;

        path.Add(actualTargetPos);

        while (currentNode != null)
        {
            path.Add(currentNode.WorldPosition);
            currentNode = currentNode.Parent;
        }
        path.Reverse();
        return path;
    }
}