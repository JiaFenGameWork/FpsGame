using System.Collections.Generic;
using UnityEngine;

public class PathDebugger : MonoBehaviour
{
    [Header("Dependencies")]
    public NavMeshAsset NavAsset;
    public Terrain TerrainObj;

    [Header("Test Coordinates")]
    // Ĭ�������㱨��������
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
        StartPos = this.transform.position;
        if (NavAsset == null) return;

        _finder = new NePathFinder(NavAsset, TerrainObj, MaxStep, MaxDrop);

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        StartCoroutine(_finder.FindPathAsy(StartPos, EndPos, (path) => _currentPath = path));

        sw.Stop();
        Debug.Log($"Path Calculation took: {sw.ElapsedMilliseconds}ms. Result Count: {(_currentPath != null ? _currentPath.Count : 0)}");
    }
    void Ondone(List<Vector3> path)
    {
        if(path != null)
        {
            _currentPath = path;
        }else
        {
            _currentPath = null;
        }
    }
    private void OnDrawGizmos()
    {
        if (_finder == null || _finder.LastDebugData == null) return;

        var data = _finder.LastDebugData;

        // 1. �������յ������λ�� (��ɫ��)
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(data.StartSnapPos, 0.3f);
        Gizmos.DrawSphere(data.EndSnapPos, 0.3f);

        // ��ʵ�����������յ� (��ɫX)
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(StartPos, Vector3.one * 0.2f);
        Gizmos.DrawWireCube(EndPos, Vector3.one * 0.2f);

        // 2. ���ѷ��ʵĽڵ� (��ɫС����) - ����չʾ������Χ��ɢ��������
        if (ShowVisited)
        {
            Gizmos.color = new Color(1, 0.92f, 0.016f, 0.3f); // ��͸����
            foreach (var node in data.VisitedNodes)
            {
                Gizmos.DrawCube(node.WorldPosition, Vector3.one * NavAsset.cellSize * 0.8f);
            }
        }

        // 3. ��ʧ�ܵı� (��ɫ��) - ����չʾ���ﱻ�߶����ƿ�ס��
        if (ShowFailedEdges)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < data.FailedEdges.Count; i += 2)
            {
                Vector3 from = data.FailedEdges[i];
                Vector3 to = data.FailedEdges[i + 1];
                //��΢̧��һ�����ⱻ�����ڵ�
                Gizmos.DrawLine(from + Vector3.up * 0.2f, to + Vector3.up * 0.2f);
            }
        }

        // 4. ������·�� (��ɫ����)
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