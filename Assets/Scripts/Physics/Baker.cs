using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Baker : MonoBehaviour
{
    [Header("BakeSetting")]
    public Vector3 boundsCenter;
    public Vector3 boundSize = new Vector3(100,50,100);
    public int maxDepth = 6;
    public float minNodeSize = 0.1f;
    public LayerMask obstacleMask = ~0;
    public Vector3Int testint = new Vector3Int();
    public SparseOctree octree;

    [Header("文件名")]
    public string name;
    public bool showgizmos= true;
    public bool showEmptyNodes= true;
    public bool showBlockedNodes= true;

    public void Bake()
    {
        obstacleMask = LayerMask.GetMask("Obstacle");
        Bounds bounds = new Bounds(boundsCenter, boundSize);
        octree = new SparseOctree();


        System.Diagnostics.Stopwatch sw= System.Diagnostics.Stopwatch.StartNew();
        octree.BuildTreeFromStatics(maxDepth, minNodeSize);
        sw.Stop();
        Debug.Log($"烘焙完成: {octree.NodeCount} 节点, 耗时 {sw.ElapsedMilliseconds}ms");
        octree.DebugLeafNodes();
    }
    public void Clear()
    {
        octree = null;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(boundsCenter, boundSize);

        if ( octree != null)
        {
            octree.DrawGizmos();
          //  foreach(var key in octree.NodeDic.Keys)
            {
          //      Debug.Log(key);
            }
      octree.DrawGizmospos(testint);
           // octree.DrawGizmosDicKey();
        }
    }
}
