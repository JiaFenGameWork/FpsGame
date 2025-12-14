using System.Collections.Generic;
using UnityEngine;

public class PathDebugger : MonoBehaviour
{
    [Header("Dependencies")]
    public NavMeshAsset NavAsset;
    public Terrain TerrainObj;

    [Header("Test Coordinates")]
    // 默认填入你报错的坐标
    public Vector3 StartPos = new Vector3(-0.90f, 1.92f, -36.40f);
    public Vector3 EndPos = new Vector3(-3.80f, -3.28f, -88.07f);

    [Header("Settings")]
    public float MaxStep = 0.6f;
    public float MaxDrop = 2.0f;
    public bool ShowVisited = true;
    public bool ShowFailedEdges = true;

    private NePathFinder _finder;
    private List<Vector3> _currentPath;

    [ContextMenu("Run Path Test")]
    public void RunTest()
    {
        if (NavAsset == null) return;

        _finder = new NePathFinder(NavAsset, TerrainObj, MaxStep, MaxDrop);

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        _currentPath = _finder.FindPath(StartPos, EndPos);

        sw.Stop();
        Debug.Log($"Path Calculation took: {sw.ElapsedMilliseconds}ms. Result Count: {(_currentPath != null ? _currentPath.Count : 0)}");
    }

    private void OnDrawGizmos()
    {
        if (_finder == null || _finder.LastDebugData == null) return;

        var data = _finder.LastDebugData;

        // 1. 画起点和终点的吸附位置 (蓝色球)
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(data.StartSnapPos, 0.3f);
        Gizmos.DrawSphere(data.EndSnapPos, 0.3f);

        // 画实际请求的起点终点 (红色X)
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(StartPos, Vector3.one * 0.2f);
        Gizmos.DrawWireCube(EndPos, Vector3.one * 0.2f);

        // 2. 画已访问的节点 (黄色小方块) - 这能展示搜索范围扩散到了哪里
        if (ShowVisited)
        {
            Gizmos.color = new Color(1, 0.92f, 0.016f, 0.3f); // 半透明黄
            foreach (var node in data.VisitedNodes)
            {
                Gizmos.DrawCube(node.WorldPosition, Vector3.one * NavAsset.cellSize * 0.8f);
            }
        }

        // 3. 画失败的边 (红色线) - 这能展示哪里被高度限制卡住了
        if (ShowFailedEdges)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < data.FailedEdges.Count; i += 2)
            {
                Vector3 from = data.FailedEdges[i];
                Vector3 to = data.FailedEdges[i + 1];
                //稍微抬高一点以免被地形遮挡
                Gizmos.DrawLine(from + Vector3.up * 0.2f, to + Vector3.up * 0.2f);
            }
        }

        // 4. 画最终路径 (绿色粗线)
        if (_currentPath != null && _currentPath.Count > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < _currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(_currentPath[i], _currentPath[i + 1]);
                Gizmos.DrawSphere(_currentPath[i], 0.1f);
            }
        }
    }
}