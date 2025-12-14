using System.Collections.Generic;
using UnityEngine;

public class OctreePathSmoother
{
    private float agentRadius;
    private float maxStepHeight;

    public OctreePathSmoother(float agentRadius = 0.5f, float maxStepHeight = 0.5f)
    {
        //this.octree = octree;
        this.agentRadius = agentRadius;
        this.maxStepHeight = maxStepHeight;
    }

    public List<Vector3> Smooth(List<Vector3> path)
    {
        if(path == null ) return null;
        if (path.Count <= 2) return path;

        // 第一步：拉绳简化
        List<Vector3> simplified = StringPull(path);

        // 第二步：曲线平滑
        List<Vector3> curved = CatmullRomSmooth(simplified, 5);

        // 第三步：修正高度
        List<Vector3> heightFixed = FixHeights(curved);

        return heightFixed;
    }

    private List<Vector3> StringPull(List<Vector3> path)
    {
        List<Vector3> result = new List<Vector3>();
        result.Add(path[0]);

        int current = 0;

        while (current < path.Count - 1)
        {
            int furthest = current + 1;

            for (int i = current + 2; i < path.Count; i++)
            {
                if (CanWalkDirect(path[current], path[i]))
                {
                    furthest = i;
                }
            }

            result.Add(path[furthest]);
            current = furthest;
        }

        return result;
    }

    private bool CanWalkDirect(Vector3 from, Vector3 to)
    {
        float distance = Vector3.Distance(from, to);
        int steps = Mathf.CeilToInt(distance / (agentRadius * 0.5f));
        steps = Mathf.Max(steps, 2);

        float lastHeight = float.MinValue;

        //for (int i = 0; i <= steps; i++)
        //{
        //    float t = i / (float)steps;
        //    Vector3 samplePos = Vector3.Lerp(from, to, t);

        //    // 检查是否有有效地面
        //    if (!octree.GetGroundHeight(samplePos, out float groundHeight))
        //    {
        //        return false;
        //    }

        //    // 检查高度差
        //    if (lastHeight != float.MinValue)
        //    {
        //        float heightDiff = Mathf.Abs(groundHeight - lastHeight);
        //        if (heightDiff > maxStepHeight)
        //        {
        //            return false;
        //        }
        //    }

        //    lastHeight = groundHeight;
        //}

        return true;
    }

    private List<Vector3> CatmullRomSmooth(List<Vector3> path, int pointsPerSegment)
    {
        if (path.Count < 2) return path;

        List<Vector3> smoothed = new List<Vector3>();

        List<Vector3> extended = new List<Vector3>();
        extended.Add(path[0] * 2 - path[1]);
        extended.AddRange(path);
        extended.Add(path[path.Count - 1] * 2 - path[path.Count - 2]);

        for (int i = 1; i < extended.Count - 2; i++)
        {
            for (int j = 0; j < pointsPerSegment; j++)
            {
                float t = j / (float)pointsPerSegment;
                smoothed.Add(CatmullRom(
                    extended[i - 1],
                    extended[i],
                    extended[i + 1],
                    extended[i + 2],
                    t
                ));
            }
        }

        smoothed.Add(path[path.Count - 1]);
        return smoothed;
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private List<Vector3> FixHeights(List<Vector3> path)
    {
        List<Vector3> result = new List<Vector3>();

        //foreach (var point in path)
        //{
        //    if (octree.GetGroundHeight(point, out float groundHeight))
        //    {
        //        result.Add(new Vector3(point.x, groundHeight, point.z));
        //    }
        //    else
        //    {
        //        result.Add(point);
        //    }
        //}

        return result;
    }
}