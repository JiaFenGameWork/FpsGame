using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TracingBullet : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("目标物体")]
    public Transform target;

    [Tooltip("发射后多少秒开始启动制导")]
    public float delayBeforeHoming = 1.0f;

    [Tooltip("子弹飞行的速度")]
    public float flyingSpeed = 50f;

    [Tooltip("转向目标的灵敏度 (重装坦克建议设低一点，如 2-5)")]
    public float turnSpeed = 5f;

    [Header("扰动设置")]
    [Tooltip("随机扰乱的强度 (值越大抖动越厉害)")]
    public float noiseStrength = 5f;
    
    [Tooltip("扰乱变化的频率 (值越大抖动频率越快)")]
    public float noiseFrequency = 10f;
    public GameObject HitSplash;
    private Rigidbody rb;
    private float timeSinceStart = 0f;
    private bool isHomingActive = false;
    private Vector3 randomOffset;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Start()
    {

        // 初始化随机种子，避免所有子弹抖动轨迹完全一样
        randomOffset = new Vector3(Random.value, Random.value, Random.value) * 100f;
    }

    void FixedUpdate()
    {
        timeSinceStart += Time.fixedDeltaTime;

        // 1. 检查是否达到启动时间
        if (timeSinceStart >= delayBeforeHoming)
        {
            if (!isHomingActive)
            {
                // 刚启动制导时的逻辑（例如可以播放一个推进器特效）
                isHomingActive = true;
                rb.useGravity = false; // 启动制导后通常取消重力影响，防止掉地上
            }
            
            HandleHomingBehavior();
        }
        else
        {
            // 2. 未启动前：保持物理惯性并让弹头朝向飞行方向
            if (rb.velocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(rb.velocity);
            }
        }
        if((transform.position-target.position).sqrMagnitude < 0.1f)
        {
            Destroy();
        }
    }
    void Destroy()
    {
        Instantiate(HitSplash, transform.position, transform.rotation);
    
        Destroy(gameObject);
    }
    void HandleHomingBehavior()
    {
        if (target == null) return;

        // --- A. 计算基础导向方向 ---
        Vector3 directionToTarget = (target.position - transform.position).normalized;

        // --- B. 计算随机扰动 (使用 Perlin Noise 获得平滑的抖动) ---
        // Perlin Noise 比纯 Random 更自然，像是有气流干扰或推进器不稳定
        float time = Time.time * noiseFrequency;
        float noiseX = Mathf.PerlinNoise(time + randomOffset.x, 0) - 0.5f;
        float noiseY = Mathf.PerlinNoise(0, time + randomOffset.y) - 0.5f;
        float noiseZ = Mathf.PerlinNoise(time + randomOffset.z, time + randomOffset.z) - 0.5f;

        Vector3 noiseVector = new Vector3(noiseX, noiseY, noiseZ) * noiseStrength;

        // --- C. 混合方向 (目标方向 + 扰动) ---
        // 随着距离目标越近，可以考虑减小扰动（可选），这里保持全程扰动
        Vector3 finalDirection = directionToTarget + noiseVector;

        // --- D. 执行转向 (RotateTowards 模拟有限的转向能力) ---
        // 获取当前的飞行方向
        Vector3 currentVelocity = rb.velocity;
        
        // 计算新的速度向量：从当前速度 -> 转向 -> 最终混合方向
        Vector3 newVelocity = Vector3.RotateTowards(
            currentVelocity, 
            finalDirection.normalized, 
            turnSpeed * Time.fixedDeltaTime, 
            0.0f
        );

        // --- E. 应用速度 ---
        rb.velocity = newVelocity.normalized * flyingSpeed;

        // 让子弹视觉朝向速度方向
        transform.rotation = Quaternion.LookRotation(rb.velocity);
    }
    public void Launch(Vector3 velocity)
    {
        rb.useGravity = false;
        rb.velocity = velocity;
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            Destroy();
        }
        var state = other.gameObject.GetComponent<CharacterState>();
        if (state != null)
        {
            Destroy();
        }
    }
    // 用于外部脚本设置目标（例如坦克发射时调用）
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

}