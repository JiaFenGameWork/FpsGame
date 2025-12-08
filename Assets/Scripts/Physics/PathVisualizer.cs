using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    // 需要引用你的寻路器实例
    public NePathFinder targetFinder;

    // 开关，方便在Inspector里控制显示
    public bool showSearchNodes = true;
    public bool showPath = true;

    void Update()
    {
        if (targetFinder == null) return;

        // 1. 画出最终路径 (绿色粗线)
        if (showPath && targetFinder.debugPathPoints != null && targetFinder.debugPathPoints.Count > 1)
        {
            for (int i = 0; i < targetFinder.debugPathPoints.Count - 1; i++)
            {
                DrawWireCube(targetFinder.debugPathPoints[i], 2f, Color.green);
            }
        }


        // 辅助函数：画一个线框立方体
        void DrawWireCube(Vector3 center, float size, Color color)
        {
            float half = size / 2;

            // 8个顶点
            Vector3[] points = new Vector3[8];
            points[0] = center + new Vector3(-half, -half, -half);
            points[1] = center + new Vector3(half, -half, -half);
            points[2] = center + new Vector3(half, -half, half);
            points[3] = center + new Vector3(-half, -half, half);

            points[4] = center + new Vector3(-half, half, -half);
            points[5] = center + new Vector3(half, half, -half);
            points[6] = center + new Vector3(half, half, half);
            points[7] = center + new Vector3(-half, half, half);

            // 画底面
            Debug.DrawLine(points[0], points[1], color);
            Debug.DrawLine(points[1], points[2], color);
            Debug.DrawLine(points[2], points[3], color);
            Debug.DrawLine(points[3], points[0], color);

            // 画顶面
            Debug.DrawLine(points[4], points[5], color);
            Debug.DrawLine(points[5], points[6], color);
            Debug.DrawLine(points[6], points[7], color);
            Debug.DrawLine(points[7], points[4], color);

            // 画立柱
            Debug.DrawLine(points[0], points[4], color);
            Debug.DrawLine(points[1], points[5], color);
            Debug.DrawLine(points[2], points[6], color);
            Debug.DrawLine(points[3], points[7], color);
        }
    }
}