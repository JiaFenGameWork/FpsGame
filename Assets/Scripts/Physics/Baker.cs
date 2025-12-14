using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Content;
using UnityEngine;

public class Baker : MonoBehaviour
{
    [Header("BakeSetting")]
    public NavigationGrid nav;
    public NavigationGrid.BuildSettings settings;
    [Header("ÎÄ¼þÃû")]
    public string name;
    public bool showgizmos= true;
    public bool showEmptyNodes= true;
    public bool showBlockedNodes= true;

    public void Bake()
    {
        nav = new NavigationGrid();


        System.Diagnostics.Stopwatch sw= System.Diagnostics.Stopwatch.StartNew();
        nav.Build(settings);
        sw.Stop();
    }
    public void Clear()
    {
        nav = null;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
       // Gizmos.DrawWireCube();

        if ( nav != null)
        {
            nav.DrawGizmos(true);
          //  foreach(var key in octree.NodeDic.Keys)
            {
          //      Debug.Log(key);
            }
        }
    }
}
