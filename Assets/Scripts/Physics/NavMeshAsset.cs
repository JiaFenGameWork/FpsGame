using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 格子类型标记
/// </summary>


[CreateAssetMenu(fileName = "NavMeshAsset", menuName = "Navigation/NavMesh Asset")]
public class NavMeshAsset : ScriptableObject, ISerializationCallbackReceiver
{
    #region 序列化字段

    [HideInInspector]
    [SerializeField]
    private List<Vector3Int> _keys = new List<Vector3Int>();
    [SerializeField]
    public float cellSize;
    [HideInInspector]
    [SerializeField]
    private List<NavigationGrid.NavCell> _values = new List<NavigationGrid.NavCell>();


    [SerializeField]
    public Bounds bounds;  // 可选：记录边界范围

    #endregion

    #region 运行时数据

    [NonSerialized]
    public Dictionary<Vector3Int, NavigationGrid.NavCell> cells = new Dictionary<Vector3Int, NavigationGrid.NavCell>();
    #endregion

    #region 序列化回调

    public void OnAfterDeserialize()
    {
        cells.Clear();
        for(int i = 0; i < _keys.Count; i++)
        {
            cells.Add(_keys[i], _values[i]);
        }
    }

    public void OnBeforeSerialize()
    {
        _keys.Clear();
        _values.Clear();
        if (cells != null)
        {
        foreach(var kvp in cells)
            {
                _keys.Add(kvp.Key);
                _values.Add(kvp.Value);
            }
        }

    }

}   
#endregion