using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 性能监控器 - 在游戏中实时显示FPS和性能警告
/// 使用方法：将此脚本挂载到任意GameObject上
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    [Header("显示设置")]
    [SerializeField] private bool showFPS = true;
    [SerializeField] private bool showDetailedStats = false;
    [SerializeField] private KeyCode toggleKey = KeyCode.F1; // 按F1切换显示
    
    [Header("警告阈值")]
    [SerializeField] private float lagThreshold = 50f; // 帧时间超过50ms视为卡顿
    [SerializeField] private int maxWarnings = 10; // 最多显示10条警告
    
    [Header("UI样式")]
    [SerializeField] private int fontSize = 20;
    [SerializeField] private Color goodColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color badColor = Color.red;
    
    // FPS计算
    private float deltaTime = 0f;
    private float fps = 0f;
    private float avgFps = 0f;
    private List<float> fpsHistory = new List<float>();
    private const int FPS_HISTORY_SIZE = 60;
    
    // 卡顿检测
    private Queue<string> lagWarnings = new Queue<string>();
    private int frameCount = 0;
    private float totalFrameTime = 0f;
    
    // GC监控
    private int lastCollectCount = 0;
    private float lastGCTime = 0f;
    
    void Start()
    {
        // 确保帧率不受限制（用于测试）
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
        
        lastCollectCount = System.GC.CollectionCount(0);
    }
    
    void Update()
    {
        // 切换显示
        if (Input.GetKeyDown(toggleKey))
        {
            showDetailedStats = !showDetailedStats;
        }
        
        // 计算帧时间
        deltaTime = Time.unscaledDeltaTime;
        fps = 1f / deltaTime;
        
        // 记录历史FPS
        fpsHistory.Add(fps);
        if (fpsHistory.Count > FPS_HISTORY_SIZE)
        {
            fpsHistory.RemoveAt(0);
        }
        
        // 计算平均FPS
        float sum = 0f;
        foreach (float f in fpsHistory)
        {
            sum += f;
        }
        avgFps = sum / fpsHistory.Count;
        
        // 统计帧时间
        frameCount++;
        totalFrameTime += deltaTime * 1000f; // 转换为毫秒
        
        // 检测卡顿
        float frameTimeMs = deltaTime * 1000f;
        if (frameTimeMs > lagThreshold)
        {
            string warning = $"[帧 {Time.frameCount}] 卡顿: {frameTimeMs:F1}ms";
            AddWarning(warning);
            Debug.LogWarning(warning);
        }
        
        // 检测GC
        int currentCollectCount = System.GC.CollectionCount(0);
        if (currentCollectCount > lastCollectCount)
        {
            float gcTime = Time.realtimeSinceStartup - lastGCTime;
            string warning = $"[帧 {Time.frameCount}] GC触发 (距上次 {gcTime:F1}s)";
            AddWarning(warning);
            Debug.LogWarning(warning);
            
            lastCollectCount = currentCollectCount;
            lastGCTime = Time.realtimeSinceStartup;
        }
    }
    
    void OnGUI()
    {
        if (!showFPS) return;
        
        // 设置GUI样式
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.normal.textColor = GetFPSColor();
        style.alignment = TextAnchor.UpperLeft;
        
        // 显示FPS
        Rect rect = new Rect(10, 10, 400, 30);
        string fpsText = $"FPS: {fps:F1} (平均: {avgFps:F1})";
        GUI.Label(rect, fpsText, style);
        
        // 显示详细信息
        if (showDetailedStats)
        {
            style.fontSize = fontSize - 4;
            float yOffset = 40;
            
            // 帧时间
            float frameTimeMs = deltaTime * 1000f;
            Color frameColor = frameTimeMs < 16.67f ? goodColor : (frameTimeMs < 33.33f ? warningColor : badColor);
            style.normal.textColor = frameColor;
            rect.y = yOffset;
            GUI.Label(rect, $"帧时间: {frameTimeMs:F2}ms", style);
            yOffset += 25;
            
            // 平均帧时间
            float avgFrameTime = frameCount > 0 ? totalFrameTime / frameCount : 0f;
            rect.y = yOffset;
            GUI.Label(rect, $"平均帧时间: {avgFrameTime:F2}ms", style);
            yOffset += 25;
            
            // 内存
            style.normal.textColor = Color.white;
            rect.y = yOffset;
            float memoryMB = System.GC.GetTotalMemory(false) / 1024f / 1024f;
            GUI.Label(rect, $"托管内存: {memoryMB:F2} MB", style);
            yOffset += 25;
            
            // GC次数
            rect.y = yOffset;
            GUI.Label(rect, $"GC次数: {System.GC.CollectionCount(0)}", style);
            yOffset += 25;
            
            // 对象数量
            rect.y = yOffset;
            GUI.Label(rect, $"对象总数: {FindObjectsOfType<GameObject>().Length}", style);
            yOffset += 30;
            
            // 提示
            style.fontSize = fontSize - 6;
            style.normal.textColor = Color.gray;
            rect.y = yOffset;
            GUI.Label(rect, $"按 {toggleKey} 切换详细信息", style);
            yOffset += 30;
            
            // 显示警告
            if (lagWarnings.Count > 0)
            {
                style.normal.textColor = badColor;
                rect.y = yOffset;
                GUI.Label(rect, "=== 性能警告 ===", style);
                yOffset += 25;
                
                style.fontSize = fontSize - 6;
                foreach (string warning in lagWarnings)
                {
                    rect.y = yOffset;
                    GUI.Label(rect, warning, style);
                    yOffset += 20;
                }
            }
        }
        else
        {
            // 简化模式提示
            style.fontSize = fontSize - 6;
            style.normal.textColor = Color.gray;
            rect.y = 40;
            GUI.Label(rect, $"按 {toggleKey} 显示详细信息", style);
        }
    }
    
    private Color GetFPSColor()
    {
        if (fps >= 55f)
            return goodColor;
        else if (fps >= 30f)
            return warningColor;
        else
            return badColor;
    }
    
    private void AddWarning(string warning)
    {
        lagWarnings.Enqueue(warning);
        if (lagWarnings.Count > maxWarnings)
        {
            lagWarnings.Dequeue();
        }
    }
    
    // 公共方法：手动标记性能点
    public static void LogPerformanceMarker(string name, float timeMs)
    {
        if (timeMs > 10f) // 超过10ms就记录
        {
            Debug.Log($"[性能标记] {name}: {timeMs:F2}ms");
        }
    }
}


