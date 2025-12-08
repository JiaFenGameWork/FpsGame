using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public StateMachine StateMachine;
    Transform Target;
    public float height = 0.1f;
    [Header("设置，要挂一个baker在本体上")]
    public Baker Baker;
    // 高度差代价系数：高度差越大，代价越高
    public float _heightCostMultiplier = 2.0f;
    // 最大可跨越的单步高度差（用于台阶/斜坡判断）
    public float _maxStepHeight = 0.5f;
    // 最大可通行高度差：超过此值视为不可通行（用于阻挡高墙）
    public float _maxTraversableHeight = 1.5f;
    //视野范围
    public float SightRange = 10f;
    //攻击距离
    public float AttackRange = 2.0f;
    //攻击间隔
    public float AttackDuation = 1.5f;
    //巡逻点
    public Transform[] PatrolPoints;
    public PatrolState PatrolState;
    public NePathFinder finder;
    private PathVisualizer _visualizer;
    public OctreeAsset octree;
    // Start is called before the first frame update
    private void Awake()
    {
        Target = GameObject.FindGameObjectWithTag("Player").transform;
        StateMachine = new StateMachine();
        PatrolState = new PatrolState(this);
    }
    void Start()
    {
        finder = new NePathFinder(octree, height ,_heightCostMultiplier, _maxStepHeight, _maxTraversableHeight);
        _visualizer = gameObject.AddComponent<PathVisualizer>();

        // 3. 【关键】把寻路器实例传给可视化脚本
        _visualizer.targetFinder = finder;
        StateMachine.ChangeState(PatrolState);
    }

    // Update is called once per frame
    void Update()
    {
        StateMachine.Update();
    }
    private void OnDrawGizmos()
    {
        // 1. 绘制 A* 搜索过的节点 (红色方块)
        if (finder != null && finder.debugPathPoints != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f); // 红色半透明
            foreach (var pos in finder.debugPathPoints)
            {
                if (pos != null)
                {
                    // 假设 node.size 是节点边长，node.center 是中心
                    // 如果你的 OctreeNode 定义不同，请相应调整
                    Gizmos.DrawSphere(pos, 2f );
                }
            }
        }

        // 2. 绘制 A* 最终路径 (绿色线条)
        if (finder != null && finder.debugPathPoints != null && finder.debugPathPoints.Count > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < finder.debugPathPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(finder.debugPathPoints[i], finder.debugPathPoints[i + 1]);
                Gizmos.DrawSphere(finder.debugPathPoints[i], 0.1f);
            }
        }

        // 3. (可选) 绘制当前巡逻状态的目标点
        if (PatrolState != null && PatrolState.currentPath != null)
        {
            // 这里可以画一些额外的状态信息
        }
    }
}
