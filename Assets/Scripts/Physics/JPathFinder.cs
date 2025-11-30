using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JPathFinder
{
    private SparseOctree octree;

    public JPathFinder(SparseOctree oc)
    {
        this.octree = oc;
    }

    public List<Vector3> FindPath(Vector3 start, Vector3 end)
    {
        SparseOctree.Octreenode startnode = octree.GetLeafNode(start);
        SparseOctree.Octreenode endnode = octree.GetLeafNode(end);

        if (startnode == null || endnode == null)
        {
            Debug.LogWarning("起点或终点不在八叉树范围内");
            return null;
        }

        if (startnode.state != SparseOctree.Octreenode.NodeState.Empty)
        {
            Debug.LogWarning("起点被阻挡");
            return null;
        }

        if (endnode.state != SparseOctree.Octreenode.NodeState.Empty)
        {
            Debug.LogWarning("终点被阻挡");
            return null;
        }

        return FindPathAstar(startnode, endnode);
    }

    public List<Vector3> FindPathAstar(SparseOctree.Octreenode startnode, SparseOctree.Octreenode endnode)
    {
        var openSet = new List<PathNodeWrapper>();
        var closedSet = new HashSet<string>();  // 用字符串key代替Vector3
        var gScores = new Dictionary<string, float>();
        var nodeInOpen = new Dictionary<string, PathNodeWrapper>();  // 快速查找open中的节点

        string startKey = GetNodeKey(startnode);
        string endKey = GetNodeKey(endnode);

        var startWrapper = new PathNodeWrapper(startnode, 0, CalculateHeuristic(startnode, endnode), null);
        openSet.Add(startWrapper);
        gScores[startKey] = 0;
        nodeInOpen[startKey] = startWrapper;

        int maxIterations = 10000;  // 防止死循环
        int iterations = 0;

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // 找 fCost 最小的节点
            openSet.Sort((a, b) => {
                int compare = a.fCost.CompareTo(b.fCost);
                if (compare == 0)
                {
                    // fCost 相同时优先选 hCost 小的（更接近终点）
                    return a.hCost.CompareTo(b.hCost);
                }
                return compare;
            });

            var current = openSet[0];
            openSet.RemoveAt(0);

            string currentKey = GetNodeKey(current.node);
            nodeInOpen.Remove(currentKey);

            // 到达终点
            if (currentKey == endKey)
            {
                Debug.Log($"找到路径，迭代次数: {iterations}");
                return ReconstructPath(current);
            }

            closedSet.Add(currentKey);

            // 获取邻居 - 使用改进的邻居查找
            foreach (var neighbor in GetWalkableNeighborsImproved(current.node))
            {
                string neighborKey = GetNodeKey(neighbor);

                if (closedSet.Contains(neighborKey))
                {
                    continue;
                }

                float moveCost = Vector3.Distance(current.node.center, neighbor.center);
                float tentativeG = current.gCost + moveCost;

                bool isNewNode = !gScores.ContainsKey(neighborKey);
                bool isBetterPath = !isNewNode && tentativeG < gScores[neighborKey];

                if (isNewNode || isBetterPath)
                {
                    gScores[neighborKey] = tentativeG;
                    float h = CalculateHeuristic(neighbor, endnode);

                    var wrapper = new PathNodeWrapper(neighbor, tentativeG, h, current);

                    if (nodeInOpen.ContainsKey(neighborKey))
                    {
                        // 更新已存在的节点
                        openSet.Remove(nodeInOpen[neighborKey]);
                    }

                    openSet.Add(wrapper);
                    nodeInOpen[neighborKey] = wrapper;
                }
            }
        }

        Debug.LogWarning($"未找到路径，迭代次数: {iterations}");
        return null;
    }

    /// <summary>
    /// 改进的邻居查找，处理不同大小节点的情况
    /// </summary>
    private List<SparseOctree.Octreenode> GetWalkableNeighborsImproved(SparseOctree.Octreenode node)
    {
        List<SparseOctree.Octreenode> neighbors = new List<SparseOctree.Octreenode>();
        HashSet<string> addedNodes = new HashSet<string>();  // 避免重复添加

        // 6个面方向
        Vector3[] faceDirections = new Vector3[]
        {
            Vector3.right, Vector3.left,
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back
        };

        float nodeSize = node.halfSize * 2;

        foreach (var dir in faceDirections)
        {
            // 在这个方向上采样多个点，以找到所有可能的邻居
            // 对于大节点找小邻居的情况很重要
            List<Vector3> samplePoints = GetFaceSamplePoints(node, dir);

            foreach (var samplePoint in samplePoints)
            {
                // 往外偏移一点点，确保进入邻居节点
                Vector3 probePoint = samplePoint + dir * 0.01f;

                SparseOctree.Octreenode neighbor = octree.GetLeafNode(probePoint);

                if (neighbor != null && neighbor.state == SparseOctree.Octreenode.NodeState.Empty)
                {
                    string key = GetNodeKey(neighbor);
                    if (!addedNodes.Contains(key) && key != GetNodeKey(node))
                    {
                        addedNodes.Add(key);
                        neighbors.Add(neighbor);
                    }
                }
            }
        }

        // 12个边方向（对角线）
        Vector3[] edgeDirections = new Vector3[]
        {
            new Vector3(1, 1, 0).normalized,
            new Vector3(1, -1, 0).normalized,
            new Vector3(-1, 1, 0).normalized,
            new Vector3(-1, -1, 0).normalized,
            new Vector3(1, 0, 1).normalized,
            new Vector3(1, 0, -1).normalized,
            new Vector3(-1, 0, 1).normalized,
            new Vector3(-1, 0, -1).normalized,
            new Vector3(0, 1, 1).normalized,
            new Vector3(0, 1, -1).normalized,
            new Vector3(0, -1, 1).normalized,
            new Vector3(0, -1, -1).normalized,
        };

        foreach (var dir in edgeDirections)
        {
            Vector3 probePoint = node.center + dir * (node.halfSize + 0.01f);
            SparseOctree.Octreenode neighbor = octree.GetLeafNode(probePoint);

            if (neighbor != null && neighbor.state == SparseOctree.Octreenode.NodeState.Empty)
            {
                string key = GetNodeKey(neighbor);
                if (!addedNodes.Contains(key))
                {
                    // 对角线移动需要检查是否会穿墙
                    if (IsDiagonalMoveValid(node.center, neighbor.center))
                    {
                        addedNodes.Add(key);
                        neighbors.Add(neighbor);
                    }
                }
            }
        }

        return neighbors;
    }

    /// <summary>
    /// 获取节点某个面上的采样点
    /// </summary>
    private List<Vector3> GetFaceSamplePoints(SparseOctree.Octreenode node, Vector3 faceDirection)
    {
        List<Vector3> points = new List<Vector3>();

        // 面的中心点
        Vector3 faceCenter = node.center + faceDirection * node.halfSize;
        points.Add(faceCenter);

        // 根据节点大小决定采样密度
        // 对于较大的节点，需要更多采样点
        float sampleStep = node.halfSize * 0.5f;

        // 确定面的两个切向量
        Vector3 tangent1, tangent2;
        if (Mathf.Abs(faceDirection.y) > 0.9f)
        {
            tangent1 = Vector3.right;
            tangent2 = Vector3.forward;
        }
        else if (Mathf.Abs(faceDirection.x) > 0.9f)
        {
            tangent1 = Vector3.up;
            tangent2 = Vector3.forward;
        }
        else
        {
            tangent1 = Vector3.right;
            tangent2 = Vector3.up;
        }

        // 在面上添加更多采样点
        for (float u = -0.5f; u <= 0.5f; u += 0.5f)
        {
            for (float v = -0.5f; v <= 0.5f; v += 0.5f)
            {
                if (Mathf.Abs(u) < 0.1f && Mathf.Abs(v) < 0.1f) continue;  // 跳过中心点

                Vector3 offset = tangent1 * (u * node.halfSize) + tangent2 * (v * node.halfSize);
                points.Add(faceCenter + offset);
            }
        }

        return points;
    }

    /// <summary>
    /// 检查对角线移动是否有效（不穿墙）
    /// </summary>
    private bool IsDiagonalMoveValid(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float distance = dir.magnitude;

        // 射线检测
        if (Physics.Raycast(from, dir.normalized, distance))
        {
            return false;
        }

        // 额外检查：中点是否可通行
        Vector3 midPoint = (from + to) * 0.5f;
        SparseOctree.Octreenode midNode = octree.GetLeafNode(midPoint);
        if (midNode == null || midNode.state != SparseOctree.Octreenode.NodeState.Empty)
        {
            return false;
        }

        return true;
    }

    private List<Vector3> ReconstructPath(PathNodeWrapper endWrapper)
    {
        List<Vector3> path = new List<Vector3>();
        var current = endWrapper;

        while (current != null)
        {
            path.Add(current.node.center);
            current = current.parent;
        }

        path.Reverse();

        // 可选：路径平滑
        // path = SmoothPath(path);

        return path;
    }

    /// <summary>
    /// 生成节点的唯一标识符
    /// </summary>
    private string GetNodeKey(SparseOctree.Octreenode node)
    {
        // 使用中心坐标和大小生成唯一key
        return $"{node.center.x:F3}_{node.center.y:F3}_{node.center.z:F3}_{node.halfSize:F3}";
    }

    private float CalculateHeuristic(SparseOctree.Octreenode a, SparseOctree.Octreenode b)
    {
        // 欧几里得距离
        return Vector3.Distance(a.center, b.center);
    }

    /// <summary>
    /// 可选：路径平滑，移除不必要的中间点
    /// </summary>
    private List<Vector3> SmoothPath(List<Vector3> path)
    {
        if (path.Count <= 2) return path;

        List<Vector3> smoothed = new List<Vector3> { path[0] };

        int current = 0;
        while (current < path.Count - 1)
        {
            int furthest = current + 1;

            // 找到能直接到达的最远点
            for (int i = path.Count - 1; i > current + 1; i--)
            {
                Vector3 dir = path[i] - path[current];
                if (!Physics.Raycast(path[current], dir.normalized, dir.magnitude))
                {
                    furthest = i;
                    break;
                }
            }

            smoothed.Add(path[furthest]);
            current = furthest;
        }

        return smoothed;
    }

    private class PathNodeWrapper
    {
        public SparseOctree.Octreenode node;
        public float gCost;
        public float hCost;
        public float fCost => gCost + hCost;
        public PathNodeWrapper parent;

        public PathNodeWrapper(SparseOctree.Octreenode node, float g, float h, PathNodeWrapper parent)
        {
            this.node = node;
            this.gCost = g;
            this.hCost = h;
            this.parent = parent;
        }
    }
}