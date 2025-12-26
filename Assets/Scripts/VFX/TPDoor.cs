using UnityEngine;
using UnityEngine.VFX;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class TPDoor : MonoBehaviour
{
    [SerializeField] private VisualEffect visualEffect;
    [SerializeField] private bool billboardYAxis = true;
    float a = 0;

    private void OnValidate()
    {
        if (visualEffect == null)
            visualEffect = GetComponent<VisualEffect>();

        UpdateVFXParameters();
    
    }
    
    private void Update()
    {
                    UpdateVFXParameters();
        // 编辑器模式下也会更新
        if (!Application.isPlaying)
        {
            
        }
        
        // Y轴 Billboard：始终面向相机（只在水平面旋转）
        if (billboardYAxis)
        {
            LookAtCameraYAxis();
        }
    }
    
    private void LookAtCameraYAxis()
    {
        Camera cam = Camera.main;
        
        #if UNITY_EDITOR
        // 编辑器模式下使用 SceneView 相机
        if (!Application.isPlaying && SceneView.lastActiveSceneView != null)
        {
            cam = SceneView.lastActiveSceneView.camera;
        }
        #endif
        
        if (cam == null) return;
        
        // 计算朝向相机的方向（忽略Y轴高度差）
        Vector3 directionToCamera = cam.transform.position - transform.position;
        directionToCamera.y = 0; // 忽略Y轴，只在水平面旋转
        
        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(directionToCamera);
        }
    }
    
    public void UpdateVFXParameters()
    {
        if (visualEffect == null) return;
        a+= Time.deltaTime;
        // Float 参数
        SetFloatIfExists("size", Mathf.Abs(Mathf.Sin(a))*0.5f+0.3f);
        
        // 标记为dirty以便编辑器保存更改
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(visualEffect);
        }
        #endif
    }
    
    #region 辅助方法
    private void SetFloatIfExists(string name, float value)
    {
        if (visualEffect.HasFloat(name))
            visualEffect.SetFloat(name, value);
    }
    
   
    #endregion
}

#if UNITY_EDITOR
[CustomEditor(typeof(TPDoor))]
public class TPDoorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        TPDoor tpDoor = (TPDoor)target;
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("刷新 VFX 参数", GUILayout.Height(30)))
        {
            tpDoor.UpdateVFXParameters();
            SceneView.RepaintAll();
        }
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.HelpBox(
            "提示：修改Inspector中的参数会自动更新VFX效果。\n" +
            "确保VFX Graph中的暴露参数名称与代码中的名称一致。",
            MessageType.Info);
    }
}
#endif
