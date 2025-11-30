using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class SparseOctree
{
    #region 八叉树基本节点类
    [System.Serializable]
    public class Octreenode
    {
        public Vector3 center;
        public float halfSize;
        public bool isLeaf;
        public int[] ChildIndicces;
        public enum NodeState
        {
            Empty,
            Blocked,
            Mixed
        }
        public NodeState state;
        public Bounds GetBounds()
        {
            return new Bounds(center, Vector3.one * halfSize * 2);
        }

    }
    #endregion
    [SerializeField] private List<Octreenode> octreenodes = new List<Octreenode>();
    [SerializeField] private int rootIndex;
    [SerializeField] private int MaxDepth;
    [SerializeField] private float MinNodeSize;
    [SerializeField] private Bounds worldBounds;

    public int NodeCount => octreenodes.Count;
    public Bounds WorldBounds => worldBounds;

    public void BuildTree(Bounds bounds,int maxDepth,float minSize,LayerMask obstacleMask)
    {
        this.worldBounds = bounds;
        this.MaxDepth = maxDepth;
        this.MinNodeSize = minSize;

        octreenodes.Clear();
        rootIndex = BuildNode(bounds.center, bounds.extents.x, 0, obstacleMask);
    }
    public void TryMergeChildren(int nodeIndex)
    {
        Octreenode node = octreenodes[nodeIndex];
        if(node.isLeaf) return;

        Octreenode.NodeState? commonstate = null;

        for(int i=0;i<8; i++)
        {
            if (node.ChildIndicces[i]<0) continue;

            Octreenode child = octreenodes[node.ChildIndicces[i]];
            //有非叶子节点的情况，肯定不能合并。
            if (!child.isLeaf) return;
            //第一个的到时候给state赋值
            if (commonstate == null)
            {
                commonstate = child.state;
            }
            //有很严格的存在任意不同的状态的情况，则不能合并，只有所有节点状态相同才行
            else if (commonstate != child.state)
            {
                return;
            }
            if (commonstate.HasValue)
            {
                node.isLeaf = true;
                node.state = commonstate.Value;
                node.ChildIndicces = new int[8] { -1,-1,-1,-1,-1,-1,-1,-1};
                octreenodes[nodeIndex] = node;
            }
        }
    }
    public int BuildNode(Vector3 center, float halfsize, int depth, LayerMask layerMask)
    {
        Octreenode node = new Octreenode { center = center, halfSize = halfsize, ChildIndicces = new int[8] { -1, -1, -1, -1, -1, -1, -1, -1 } };
        Bounds bounds = node.GetBounds();

        bool hasAnyObstacle = Physics.CheckBox(center, Vector3.one * halfsize * 0.99f,Quaternion.identity, layerMask);

        if (!hasAnyObstacle)
        {
            node.isLeaf = true;
            node.state = Octreenode.NodeState.Empty;
            octreenodes.Add(node);
            return octreenodes.Count-1;
        }
        bool isFullyBlock = IsFullyblock(bounds, layerMask);
        if (isFullyBlock)
        {
            node.isLeaf = true;
            node.state = Octreenode.NodeState.Blocked;
            octreenodes.Add(node);
            return octreenodes.Count-1;
        }
        if (depth >= MaxDepth || halfsize * 2f <= MinNodeSize)
        {
            node.isLeaf = true;
            node.state = Octreenode.NodeState.Blocked;
            octreenodes.Add(node);
            return octreenodes.Count - 1;
        }
        //继续细分
        node.isLeaf = false;
        node.state = Octreenode.NodeState.Mixed;

        int nodeindex = octreenodes.Count;
        octreenodes.Add(node);

        float childHalfSize = halfsize * 0.5f;
        int childIdx = 0;
        for(int x = -1; x <= 1; x += 2)
        {
            for(int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 childCenter = center+new Vector3(x* childHalfSize, y * childHalfSize, z * childHalfSize);

                    int childNodeIndex = BuildNode(childCenter, childHalfSize, depth + 1, layerMask);
                    node.ChildIndicces[childIdx] = childNodeIndex;
                    childIdx++;
                }
            }
        }

        octreenodes[nodeindex] = node;
        return nodeindex;
    }

    private bool IsFullyblock(Bounds bounds, LayerMask layerMask)
    {
        //bound是一个立方体，这里就是将立方体的八个端点作为采样点，用Physics.CheckSphere采样以采样点位中心半径为0.01f的球形区域是否有遮挡，如果有一个点没有被遮挡，则没有被完全遮挡
        Vector3[] SamplePoints = new Vector3[]
        {bounds.center,
        bounds.center+new Vector3(bounds.extents.x,0,0)*0.9f,
        bounds.center-new Vector3(bounds.extents.x,0,0)*0.9f,
        bounds.center+new Vector3(0,bounds.extents.y,0)*0.9f,
        bounds.center-new Vector3(0,bounds.extents.y,0)*0.9f,
        bounds.center+new Vector3(0,0,bounds.extents.z)*0.9f,
        bounds.center-new Vector3(0,0,bounds.extents.z)*0.9f,

        };
        foreach (Vector3 point in SamplePoints)
        {
            if (!Physics.CheckSphere(point, 0.01f, layerMask))return false;
        }
        return true;
    }
    public bool IsWalkable(Vector3 point)
    {
        if(!worldBounds.Contains(point))
        return false;

        return IsWalkableRecursive(rootIndex, point);
    }

    private bool IsWalkableRecursive(int ind, Vector3 point)
    {
        if(ind<0||ind>=octreenodes.Count) return false;

        Octreenode node=octreenodes[ind];

        if (node.isLeaf)
        {
            return node.state == Octreenode.NodeState.Empty;
        }
        int childidx = GetChildIndex(node.center,point);
        int chilNodeidx = node.ChildIndicces[childidx];

        if(chilNodeidx<0) return true;

        return IsWalkableRecursive(chilNodeidx, point);

    }
    int GetChildIndex(Vector3 center,Vector3 point)
    {
        int index = 0;
        if (point.z > center.z) index |= 1;
        if(point.y > center.y) index |= 2;
        if(point.x > center.x) index |= 4;
        return index;
    }
    public Octreenode GetLeafNode(Vector3 point)
    {
        if (!worldBounds.Contains(point))
        {
            return null;
        }
        return GetLeafNodeRecursive(rootIndex,point);
    }

    private Octreenode GetLeafNodeRecursive(int index,Vector3 point)
    {
        if(index <0) return null;
        
        Octreenode node = octreenodes[index];
        if(node.isLeaf) {return node;}
        int childidx = GetChildIndex(node.center, point);
        return GetLeafNodeRecursive(node.ChildIndicces[childidx],point);
    }
    //查找临近节点可通行
    public List<Octreenode> getWalkableNeighbors(Octreenode node)
    {
        List<Octreenode> neighbors = new List<Octreenode>();
        float step = node.halfSize * 2;

        Vector3[] directions = new Vector3[]
        {
            Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back
        };
        foreach(var dir in directions)
        {
            Vector3 neipos = node.center+ dir * step;
            Octreenode neibor = GetLeafNode(neipos);

            if(neibor!=null&&neibor.state == Octreenode.NodeState.Empty)
            {
                neighbors.Add(neibor);
            }
        }
        Vector3[] edgeDirections = new Vector3[]
        { 
            new Vector3(1,1,0),new Vector3(1,-1,0),new Vector3(-1,1,0),new Vector3(-1,-1,0),
            new Vector3(1,0,1),new Vector3(1,0,-1),new Vector3(-1,0,1),new Vector3(-1,0,-1),
            new Vector3(0,1,1),new Vector3(0,1,-1),new Vector3(0,-1,1),new Vector3(0,-1,-1),
        };

        foreach (var dir in edgeDirections)
        {
            Vector3 neipos = node.center + dir * step;
            Octreenode neibor = GetLeafNode(neipos);
            if (neibor != null && neibor.state == Octreenode.NodeState.Empty)
            {
                neighbors.Add(neibor);
            }
        }

        return neighbors;
        }
    private bool IsDiagonalMoveVaild(Vector3 from,Vector3 to) 
    {
        Vector3 dir = to - from;
        return !Physics.Raycast(from, dir.normalized,dir.magnitude);
    }
    public void DrawGizmos(bool showEmpty = false,bool showBlocked = true)
    {
        foreach(var node in octreenodes)
        {
            if (!node.isLeaf) continue;

            if(node.state == Octreenode.NodeState.Blocked && showBlocked)
            {
                Gizmos.color = new Color(1,0,0,0.3f);
                Gizmos.DrawCube(node.center, Vector3.one * node.halfSize * 2f);

            }
            else if(node.state == Octreenode.NodeState.Empty && showEmpty)
            {
                Gizmos.color = new Color(0, 1, 0, 0.1f);
                Gizmos.DrawCube(node.center, Vector3.one * node.halfSize * 2);
            }
        }
    }
}

