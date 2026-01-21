using UnityEngine;

/// <summary>
/// 技能范围基类 (ScriptableObject)
/// 所有范围类型都继承此类
/// </summary>
public abstract class SkillRange : ScriptableObject
{
    /// <summary>
    /// 检测目标点是否在范围内
    /// </summary>
    /// <param name="origin">范围中心点（世界坐标）</param>
    /// <param name="forward">朝向方向</param>
    /// <param name="target">目标点（世界坐标）</param>
    /// <returns>是否在范围内</returns>
    public abstract bool Contains(Vector3 origin, Vector3 forward, Vector3 target);

    /// <summary>
    /// 在 Scene 视图绘制范围（供编辑器调用）
    /// </summary>
    public abstract void DrawGizmos(Vector3 origin, Vector3 forward);

    /// <summary>
    /// 范围的主颜色（用于 Gizmos 绘制）
    /// </summary>
    public virtual Color GizmoColor => new Color(1f, 0.5f, 0f, 0.5f);
}

#region 基础范围类型

/// <summary>
/// 圆形范围
/// </summary>
[CreateAssetMenu(fileName = "CircleRange", menuName = "SkillRange/Circle", order = 0)]
public class CircleRange : SkillRange
{
    [Tooltip("半径")]
    public float radius = 5f;

    public override bool Contains(Vector3 origin, Vector3 forward, Vector3 target)
    {
        Vector3 delta = target - origin;
        delta.y = 0; // 忽略高度
        return delta.sqrMagnitude <= radius * radius;
    }

    public override void DrawGizmos(Vector3 origin, Vector3 forward)
    {
        DrawCircle(origin, radius, 32);
    }

    protected void DrawCircle(Vector3 center, float r, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(r, 0.1f, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * r, 0.1f, Mathf.Sin(angle) * r);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
}

/// <summary>
/// 扇形范围
/// </summary>
[CreateAssetMenu(fileName = "SectorRange", menuName = "SkillRange/Sector", order = 1)]
public class SectorRange : SkillRange
{
    [Tooltip("半径")]
    public float radius = 5f;
    [Tooltip("半角（度数，如60度扇形填30）")]
    [Range(1f, 180f)]
    public float halfAngle = 45f;

    public override bool Contains(Vector3 origin, Vector3 forward, Vector3 target)
    {
        Vector3 toTarget = target - origin;
        toTarget.y = 0;
        forward.y = 0;

        float distSqr = toTarget.sqrMagnitude;
        if (distSqr > radius * radius || distSqr < 0.0001f)
            return false;

        float angle = Vector3.Angle(forward.normalized, toTarget.normalized);
        return angle <= halfAngle;
    }

    public override void DrawGizmos(Vector3 origin, Vector3 forward)
    {
        forward.y = 0;
        forward.Normalize();

        // 计算扇形的两条边
        Quaternion leftRot = Quaternion.Euler(0, -halfAngle, 0);
        Quaternion rightRot = Quaternion.Euler(0, halfAngle, 0);
        Vector3 leftDir = leftRot * forward;
        Vector3 rightDir = rightRot * forward;

        Vector3 leftEnd = origin + leftDir * radius + Vector3.up * 0.1f;
        Vector3 rightEnd = origin + rightDir * radius + Vector3.up * 0.1f;
        Vector3 originUp = origin + Vector3.up * 0.1f;

        // 画两条边
        Gizmos.DrawLine(originUp, leftEnd);
        Gizmos.DrawLine(originUp, rightEnd);

        // 画弧线
        int segments = Mathf.CeilToInt(halfAngle * 2 / 5f); // 每5度一段
        segments = Mathf.Max(segments, 4);
        float angleStep = halfAngle * 2f / segments;
        Vector3 prevPoint = leftEnd;

        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = -halfAngle + angleStep * i;
            Quaternion rot = Quaternion.Euler(0, currentAngle, 0);
            Vector3 point = origin + rot * forward * radius + Vector3.up * 0.1f;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }

    public override Color GizmoColor => new Color(1f, 0.3f, 0f, 0.5f);
}

/// <summary>
/// 矩形范围（前方矩形区域）
/// </summary>
[CreateAssetMenu(fileName = "RectangleRange", menuName = "SkillRange/Rectangle", order = 2)]
public class RectangleRange : SkillRange
{
    [Tooltip("宽度（左右方向）")]
    public float width = 3f;
    [Tooltip("长度（前方方向）")]
    public float length = 10f;
    [Tooltip("偏移（从中心向前偏移）")]
    public float offset = 0f;

    public override bool Contains(Vector3 origin, Vector3 forward, Vector3 target)
    {
        forward.y = 0;
        forward.Normalize();

        Vector3 toTarget = target - origin;
        toTarget.y = 0;

        // 转换到本地坐标
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float localZ = Vector3.Dot(toTarget, forward); // 前方距离
        float localX = Vector3.Dot(toTarget, right);   // 左右距离

        // 检测是否在矩形内
        float halfWidth = width * 0.5f;
        return localZ >= offset && localZ <= offset + length &&
               localX >= -halfWidth && localX <= halfWidth;
    }

    public override void DrawGizmos(Vector3 origin, Vector3 forward)
    {
        forward.y = 0;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        float halfWidth = width * 0.5f;
        float y = 0.1f;

        // 四个角点
        Vector3 frontLeft = origin + forward * (offset + length) - right * halfWidth + Vector3.up * y;
        Vector3 frontRight = origin + forward * (offset + length) + right * halfWidth + Vector3.up * y;
        Vector3 backLeft = origin + forward * offset - right * halfWidth + Vector3.up * y;
        Vector3 backRight = origin + forward * offset + right * halfWidth + Vector3.up * y;

        Gizmos.DrawLine(frontLeft, frontRight);
        Gizmos.DrawLine(frontRight, backRight);
        Gizmos.DrawLine(backRight, backLeft);
        Gizmos.DrawLine(backLeft, frontLeft);
    }

    public override Color GizmoColor => new Color(0f, 1f, 0.5f, 0.5f);
}

/// <summary>
/// 环形范围（内外半径之间的环）
/// </summary>
[CreateAssetMenu(fileName = "RingRange", menuName = "SkillRange/Ring", order = 3)]
public class RingRange : SkillRange
{
    [Tooltip("内半径")]
    public float innerRadius = 3f;
    [Tooltip("外半径")]
    public float outerRadius = 8f;

    public override bool Contains(Vector3 origin, Vector3 forward, Vector3 target)
    {
        Vector3 delta = target - origin;
        delta.y = 0;
        float distSqr = delta.sqrMagnitude;
        return distSqr >= innerRadius * innerRadius && distSqr <= outerRadius * outerRadius;
    }

    public override void DrawGizmos(Vector3 origin, Vector3 forward)
    {
        DrawCircle(origin, innerRadius, 32);
        DrawCircle(origin, outerRadius, 32);
    }

    private void DrawCircle(Vector3 center, float r, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(r, 0.1f, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * r, 0.1f, Mathf.Sin(angle) * r);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }

    public override Color GizmoColor => new Color(0.5f, 0f, 1f, 0.5f);
}

/// <summary>
/// 线形范围（射线/激光类技能）
/// </summary>
[CreateAssetMenu(fileName = "LineRange", menuName = "SkillRange/Line", order = 4)]
public class LineRange : SkillRange
{
    [Tooltip("长度")]
    public float length = 15f;
    [Tooltip("宽度（粗细）")]
    public float width = 1f;

    public override bool Contains(Vector3 origin, Vector3 forward, Vector3 target)
    {
        forward.y = 0;
        forward.Normalize();

        Vector3 toTarget = target - origin;
        toTarget.y = 0;

        // 投影到前方方向
        float projLength = Vector3.Dot(toTarget, forward);
        if (projLength < 0 || projLength > length)
            return false;

        // 计算到线的垂直距离
        Vector3 projPoint = origin + forward * projLength;
        float perpDist = Vector3.Distance(new Vector3(target.x, 0, target.z),
                                           new Vector3(projPoint.x, 0, projPoint.z));
        return perpDist <= width * 0.5f;
    }

    public override void DrawGizmos(Vector3 origin, Vector3 forward)
    {
        forward.y = 0;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        float halfWidth = width * 0.5f;
        float y = 0.1f;

        Vector3 startLeft = origin - right * halfWidth + Vector3.up * y;
        Vector3 startRight = origin + right * halfWidth + Vector3.up * y;
        Vector3 endLeft = origin + forward * length - right * halfWidth + Vector3.up * y;
        Vector3 endRight = origin + forward * length + right * halfWidth + Vector3.up * y;

        Gizmos.DrawLine(startLeft, startRight);
        Gizmos.DrawLine(startRight, endRight);
        Gizmos.DrawLine(endRight, endLeft);
        Gizmos.DrawLine(endLeft, startLeft);
    }

    public override Color GizmoColor => new Color(1f, 1f, 0f, 0.5f);
}

/// <summary>
/// 点范围（单体技能，极小容差）
/// </summary>
[CreateAssetMenu(fileName = "PointRange", menuName = "SkillRange/Point", order = 5)]
public class PointRange : SkillRange
{
    [Tooltip("容差半径")]
    public float tolerance = 0.5f;

    public override bool Contains(Vector3 origin, Vector3 forward, Vector3 target)
    {
        Vector3 delta = target - origin;
        delta.y = 0;
        return delta.sqrMagnitude <= tolerance * tolerance;
    }

    public override void DrawGizmos(Vector3 origin, Vector3 forward)
    {
        // 画十字
        float size = tolerance;
        float y = 0.1f;
        Gizmos.DrawLine(origin + new Vector3(-size, y, 0), origin + new Vector3(size, y, 0));
        Gizmos.DrawLine(origin + new Vector3(0, y, -size), origin + new Vector3(0, y, size));

        // 画小圆
        DrawCircle(origin, tolerance, 16);
    }

    private void DrawCircle(Vector3 center, float r, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(r, 0.1f, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * r, 0.1f, Mathf.Sin(angle) * r);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }

    public override Color GizmoColor => new Color(1f, 0f, 0f, 0.8f);
}

#endregion

#region 布尔组合范围

/// <summary>
/// 并集范围：A 或 B（任一范围内即命中）
/// </summary>
[CreateAssetMenu(fileName = "UnionRange", menuName = "SkillRange/Boolean/Union (A or B)", order = 10)]
public class UnionRange : SkillRange
{
    [Tooltip("范围 A")]
    public SkillRange rangeA;
    [Tooltip("范围 B")]
    public SkillRange rangeB;

    public override bool Contains(Vector3 origin, Vector3 forward, Vector3 target)
    {
        bool inA = rangeA != null && rangeA.Contains(origin, forward, target);
        bool inB = rangeB != null && rangeB.Contains(origin, forward, target);
        return inA || inB;
    }

    public override void DrawGizmos(Vector3 origin, Vector3 forward)
    {
        if (rangeA != null)
        {
            Gizmos.color = rangeA.GizmoColor;
            rangeA.DrawGizmos(origin, forward);
        }
        if (rangeB != null)
        {
            Gizmos.color = rangeB.GizmoColor;
            rangeB.DrawGizmos(origin, forward);
        }
    }

    public override Color GizmoColor => new Color(0f, 1f, 0f, 0.5f);
}

/// <summary>
/// 交集范围：A 且 B（同时在两个范围内）
/// </summary>
[CreateAssetMenu(fileName = "IntersectRange", menuName = "SkillRange/Boolean/Intersect (A and B)", order = 11)]
public class IntersectRange : SkillRange
{
    [Tooltip("范围 A")]
    public SkillRange rangeA;
    [Tooltip("范围 B")]
    public SkillRange rangeB;

    public override bool Contains(Vector3 origin, Vector3 forward, Vector3 target)
    {
        bool inA = rangeA != null && rangeA.Contains(origin, forward, target);
        bool inB = rangeB != null && rangeB.Contains(origin, forward, target);
        return inA && inB;
    }

    public override void DrawGizmos(Vector3 origin, Vector3 forward)
    {
        if (rangeA != null)
        {
            Gizmos.color = rangeA.GizmoColor;
            rangeA.DrawGizmos(origin, forward);
        }
        if (rangeB != null)
        {
            Gizmos.color = new Color(rangeB.GizmoColor.r, rangeB.GizmoColor.g, rangeB.GizmoColor.b, 0.3f);
            rangeB.DrawGizmos(origin, forward);
        }
    }

    public override Color GizmoColor => new Color(0f, 0.5f, 1f, 0.5f);
}

/// <summary>
/// 差集范围：A 减 B（在A内但不在B内，即挖洞效果）
/// </summary>
[CreateAssetMenu(fileName = "SubtractRange", menuName = "SkillRange/Boolean/Subtract (A minus B)", order = 12)]
public class SubtractRange : SkillRange
{
    [Tooltip("主范围 A")]
    public SkillRange rangeA;
    [Tooltip("被挖掉的范围 B")]
    public SkillRange rangeB;

    public override bool Contains(Vector3 origin, Vector3 forward, Vector3 target)
    {
        bool inA = rangeA != null && rangeA.Contains(origin, forward, target);
        bool inB = rangeB != null && rangeB.Contains(origin, forward, target);
        return inA && !inB;
    }

    public override void DrawGizmos(Vector3 origin, Vector3 forward)
    {
        // 主范围用实色
        if (rangeA != null)
        {
            Gizmos.color = rangeA.GizmoColor;
            rangeA.DrawGizmos(origin, forward);
        }
        // 被减范围用虚线颜色（红色表示"减去"）
        if (rangeB != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
            rangeB.DrawGizmos(origin, forward);
        }
    }

    public override Color GizmoColor => new Color(1f, 0.5f, 0f, 0.5f);
}

#endregion


