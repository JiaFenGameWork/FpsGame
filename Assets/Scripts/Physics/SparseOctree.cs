using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class SparseOctree
{
    #region 八叉树基本节点类
    [System.Serializable]
    public class Octreenode
    {

        public Vector3 center;
        public float halfSize;
        public bool isLeaf;
        public int[] ChildIndices;
        public List<int> triangleIndices;
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
    [SerializeField] private Dictionary<Vector3Int, int> nodeDic;

    Vector3[] cacheVertices;
    int[] cachedTriangles;
    public float MinSize => MinNodeSize;
    public int NodeCount => octreenodes.Count;
    public Octreenode[] Allnodes => octreenodes.ToArray();
    public Dictionary<Vector3Int, int> NodeDic => nodeDic;
    public Bounds WorldBounds => worldBounds;

    public void BuildTreeFromStatics(int maxDepth, float minSize)
    {
        MeshFilter[] filters = UnityEngine.Object.FindObjectsOfType<MeshFilter>();
        nodeDic = new Dictionary<Vector3Int, int>();
        List<Vector3> combineVertices = new List<Vector3>();
        List<int> combinedIndices = new List<int>();

        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        int vertexOffset = 0;

        foreach (MeshFilter mf in filters)
        {
            if (!mf.gameObject.isStatic || mf.sharedMesh == null) continue;

            Mesh m = mf.sharedMesh;
            Transform t = mf.transform;

            Vector3[] localvertice = m.vertices;
            int[] localTris = m.triangles;

            for (int i = 0; i < localvertice.Length; i++)
            {
                Vector3 worldPt = t.TransformPoint(localvertice[i]);
                combineVertices.Add(worldPt);

                min = Vector3.Min(worldPt, min);
                max = Vector3.Max(worldPt, max);
            }
            for (int i = 0; i < localTris.Length; i++)
            {
                combinedIndices.Add(localTris[i] + vertexOffset);
            }
            vertexOffset += localvertice.Length;
        }
        if (combinedIndices.Count == 0)
        {
            UnityEngine.Debug.LogWarning("No static meshes found to build Octree.");
            return;
        }

        Bounds totalbounds = new Bounds();
        totalbounds.SetMinMax(min, max);

        BuildTreeFromData(combineVertices.ToArray(), combinedIndices.ToArray(), totalbounds, maxDepth, minSize);
    }

    private void BuildTreeFromData(Vector3[] vertices, int[] triangles, Bounds totalbounds, int maxDepth, float minSize)
    {
        this.worldBounds = totalbounds;
        this.MaxDepth = maxDepth;
        this.MinNodeSize = minSize;
        this.cacheVertices = vertices;
        this.cachedTriangles = triangles;

        octreenodes.Clear();

        List<int> alltris = new List<int>();
        for (int i = 0; i < cachedTriangles.Length / 3; i++)
        {
            alltris.Add(i);
        }
        float a = MathF.Max(worldBounds.extents.x, worldBounds.extents.y);
        float maxsize = MathF.Max(a, worldBounds.extents.z);
       // Debug.Log(maxsize);

        rootIndex = BuildNode(worldBounds.center, 128, 0, alltris);
        Debug.Log($"Octree Built. Nodes: {octreenodes.Count}, Triangles: {cachedTriangles.Length / 3}");
    }
    public Vector3Int MulAndFloorVector3(Vector3 a, float m)
    {
        Vector3Int v = Vector3Int.FloorToInt(a * m);
        return v;
    }
    public static Octreenode FindNodeXYZ(OctreeAsset data, Vector3 xyz)
    {
        xyz.x /= data.MinSize;
        xyz.y /= data.MinSize;
        xyz.z /= data.MinSize;
        Vector3Int V3int = Vector3Int.FloorToInt(xyz);
        if ((data.nodeDic.TryGetValue(V3int, out int index)))
        {
            return data.octreenodes[index];
        }
        return null;
    }
    public static bool FindNodeFirst(OctreeAsset data,Vector3 pos,Vector3 raydir,float dis)
    {
        float f = 0;
        Vector3 p = pos;
        while (f<dis)
        {
            p += raydir * f;
            Vector3Int V3int = Vector3Int.FloorToInt(p);
            if (data.nodeDic.TryGetValue(V3int,out int index))
            {
                return true;
            }
            f += data.MinSize;

        }
        return false;
    }
    public static float FindNodeAxis(OctreeAsset data,Vector3 pos, Vector3 raydir, float minrange, float maxrange,float thresold)
    {
        float dis = 0;
        for (float f = minrange; f < maxrange + data.MinSize; f += data.MinSize)
        {
            Vector3 p = pos + raydir * f;
            Vector3Int V3int = Vector3Int.FloorToInt(p);
            if ((data.nodeDic.TryGetValue(V3int, out int index)))
            {

                  dis = data.octreenodes[index].center.y;
                if (dis > thresold)
                {
                    return 99999999999f;
                }
            }
        }
        return dis;
    }
    private int BuildNode(Vector3 center, float halfsize, int depth, List<int> alltris)
    {
        Bounds nodeBounds = new Bounds(center, Vector3.one * halfsize * 2);

        List<int> intersectingTriangles = new List<int>();
        foreach (int triIdx in alltris)
        {
            if (IsTriangleInAABB(triIdx, nodeBounds))
            {
                intersectingTriangles.Add(triIdx);
            }
        }

        if (intersectingTriangles.Count == 0)
        {
            return -1;
        }

        // 修复：确保判断条件符合你的预期
        bool shouldBeLeaf = (halfsize * 2f <= MinNodeSize) || (depth >= MaxDepth);

        if (shouldBeLeaf)
        {
            var leaf = new Octreenode
            {
                center = center,
                halfSize = halfsize,
                isLeaf = true,
                ChildIndices = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 },
                triangleIndices = intersectingTriangles
            };
           // Debug.Log($"add cemter{Quantize(leaf.center)},halfsize:{halfsize}");
            int nodeIdx = octreenodes.Count;
            octreenodes.Add(leaf);
            nodeDic[Quantize(leaf.center)] = nodeIdx;
            return nodeIdx;
        }

        var branch = new Octreenode
        {
            center = center,
            halfSize = halfsize,
            isLeaf = false,
            ChildIndices = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }
        };

        float childHalf = halfsize * 0.5f;
        bool hasChild = false;

        // 修复：按照 GetChildIdx 的位顺序遍历 (z, y, x)
        for (int i = 0; i < 8; i++)
        {
            int zSign = (i & 1) == 0 ? -1 : 1;
            int ySign = (i & 2) == 0 ? -1 : 1;
            int xSign = (i & 4) == 0 ? -1 : 1;

            Vector3 newCenter = center + new Vector3(childHalf * xSign, childHalf * ySign, childHalf * zSign);
            int childIdx = BuildNode(newCenter, childHalf, depth + 1, intersectingTriangles);

            branch.ChildIndices[i] = childIdx;
            if (childIdx != -1) hasChild = true;
        }

        if (!hasChild) return -1;

        int branchIdx = octreenodes.Count;
        octreenodes.Add(branch);
        return branchIdx;
    }

    private bool IsTriangleInAABB(int triIdx, Bounds nodeBounds)
    {
        Vector3 vertice1 = cacheVertices[cachedTriangles[triIdx * 3]];
        Vector3 vertice2 = cacheVertices[cachedTriangles[triIdx * 3 + 1]];
        Vector3 vertice3 = cacheVertices[cachedTriangles[triIdx * 3 + 2]];

        float minX = Mathf.Min(vertice1.x, Mathf.Min(vertice2.x, vertice3.x));
        float maxX = Mathf.Max(vertice1.x, Mathf.Max(vertice2.x, vertice3.x));
        if (minX > nodeBounds.max.x || maxX < nodeBounds.min.x) return false;
        float minY = Mathf.Min(vertice1.y, Mathf.Min(vertice2.y, vertice3.y));
        float maxY = Mathf.Max(vertice1.y, Mathf.Max(vertice2.y, vertice3.y));
        if (minY > nodeBounds.max.y || maxY < nodeBounds.min.y) return false;
        float minZ = Mathf.Min(vertice1.z, Mathf.Min(vertice2.z, vertice3.z));
        float maxZ = Mathf.Max(vertice1.z, Mathf.Max(vertice2.z, vertice3.z));
        if (minZ > nodeBounds.max.z || maxZ < nodeBounds.min.z) return false;

        return true;
    }
    public Octreenode GetLeafNode(Vector3 pos, int index)
    {
        if (index < 0 || index > octreenodes.Count) return null;
        Octreenode node = octreenodes[index];
        if (node.isLeaf) return node;
        int ChildIdx = GetChildIdx(node.center, pos);
        int ChildNodeIdx = node.ChildIndices[ChildIdx];
        if (ChildNodeIdx < 0) return node;
        return GetLeafNode(pos, ChildNodeIdx);
    }
    public Octreenode GetLeafNode(Vector3 pos)
    {
        return GetLeafNode(pos, rootIndex);
    }
    int GetChildIdx(Vector3 center, Vector3 point)
    {
        int index = 0;
        if (point.z > center.z) index |= 1;
        if (point.y > center.y) index |= 2;
        if (point.x > center.x) index |= 4;
        return index;
    }


    public void DrawGizmos()
    {
        if (octreenodes == null) return;
        foreach (var node in octreenodes)
        {
            if (!node.isLeaf) continue;
            if (node.triangleIndices != null && node.triangleIndices.Count > 0)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawWireCube(node.center, Vector3.one * node.halfSize * 2f);
            }
        }

    }
  Vector3Int Quantize(Vector3 position)
{
    const float epsilon = 1e-4f;
    return Vector3Int.FloorToInt((position / MinNodeSize) + new Vector3(epsilon, epsilon, epsilon));
}

public void DrawGizmospos(Vector3Int position)
{
  
    Gizmos.color = new Color(0, 1, 0, 0.3f);
        var p = Quantize(position);
        if(nodeDic.TryGetValue(p,out int nodeidx))
        {
        Gizmos.DrawSphere(octreenodes[nodeidx].center,50f);
        }
        else
        {
            Debug.Log($"当前位置{position}未找到对应的节点");
        }
    }

    public void Check()
    {
        foreach (var kv in nodeDic)
        {
            int idx = kv.Value;
            if (idx < 0 || idx >= octreenodes.Count)
                Debug.LogError($"Bad index in nodeDic: {idx}, key = {kv.Key}");
        }
        for (int i = 0; i < octreenodes.Count; i++)
        {
            var n = octreenodes[i];
            var key = Vector3Int.FloorToInt(n.center / MinNodeSize);
            if (!nodeDic.TryGetValue(key, out int idx) || idx != i)
                Debug.LogError($"Mismatch: node {i} -> key {key} -> idx {idx}");
        }
    }
    public void DebugLeafNodes()
    {
        int leafCount = 0;
        foreach (var node in octreenodes)
        {
            if (!node.isLeaf) continue;
            leafCount++;

            float nodeFullSize = node.halfSize * 2f;
            Vector3Int key = Quantize(node.center);

            Debug.Log($"Leaf: center={node.center}, halfSize={node.halfSize}, fullSize={nodeFullSize}, key={key}");

            if (leafCount > 10) break; // 只看前10个
        }

        Debug.Log($"MinNodeSize={MinNodeSize}, Total leaves={leafCount}");
    }
}

