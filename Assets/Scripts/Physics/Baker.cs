using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Baker : MonoBehaviour
{
    [Header("BakeSetting")]
    public Vector3 boundsCenter;
    public Vector3 boundSize = new Vector3(100,50,100);
    public int maxDepth = 6;
    public float minNodeSize = 0.5f;
    public LayerMask obstacleMask = ~0;

    public SparseOctree octree;

    public bool showgizmos= true;
    public bool showEmptyNodes= true;
    public bool showBlockedNodes= true;

    public void Bake()
    {
        Bounds bounds = new Bounds(boundsCenter, boundSize);
        octree = new SparseOctree();


        System.Diagnostics.Stopwatch sw= System.Diagnostics.Stopwatch.StartNew();
        octree.BuildTree(bounds, maxDepth, minNodeSize, obstacleMask);
        sw.Stop();
        Debug.Log($"烘焙完成: {octree.NodeCount} 节点, 耗时 {sw.ElapsedMilliseconds}ms");
    }
    public void Clear()
    {
        octree = null;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(boundsCenter, boundSize);

        if (showgizmos && octree != null)
        {
            octree.DrawGizmos(showEmptyNodes, showBlockedNodes);
        }
    }

}
