using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterState : MonoBehaviour, IDamageable
{
    [Header("生命值设置")]
    [SerializeField] float MaxHp = 100f;
    float currentHp;

    [Header("血条设置")]
    [SerializeField] float healthBarHeight = 2f;        // 血条高度偏移
    [SerializeField] float healthBarWidth = 1f;         // 血条宽度
    [SerializeField] float healthBarDisplayTime = 1f;   // 血条显示时间
    [SerializeField] Color healthBarColor = Color.red;  // 血条颜色
    [SerializeField] Color healthBarBgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f); // 背景颜色

    // 血条相关组件
    private Canvas healthBarCanvas;
    private Image healthBarFill;
    private Image healthBarBackground;
    private Coroutine hideCoroutine;
    private Camera mainCamera;
    public float deadAnimationDuration = 1f;
    public float CurrentHealth => currentHp;
    public float MaxHealth => MaxHp;
    public bool IsDead => currentHp <= 0;
    public event Action<AttackData> OnTakeDamage;
    public event Action OnDeath;
    public Material Deadmaterial;
    public MeshRenderer renderer;
    void Awake()
    {
        currentHp = MaxHp;
        mainCamera = Camera.main;
        CreateHealthBar();
    }

    void LateUpdate()
    {
       
        // 让血条始终面向摄像机
        if (healthBarCanvas != null && mainCamera != null)
        {
            healthBarCanvas.transform.LookAt(
                healthBarCanvas.transform.position + mainCamera.transform.forward
            );
        }
    }

    /// <summary>
    /// 动态创建世界空间血条
    /// </summary>
    void CreateHealthBar()
    {
        // 创建Canvas
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = new Vector3(0, healthBarHeight, 0);
        canvasObj.transform.localRotation = Quaternion.identity;

        healthBarCanvas = canvasObj.AddComponent<Canvas>();
        healthBarCanvas.renderMode = RenderMode.WorldSpace;
        healthBarCanvas.sortingOrder = 100;

        // 设置Canvas大小
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(healthBarWidth, 0.2f*healthBarWidth);
        canvasRect.localScale = Vector3.one * 0.01f; // 缩小到合适大小

        // 创建背景
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        healthBarBackground = bgObj.AddComponent<Image>();
        healthBarBackground.color = healthBarBgColor;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // 创建血条填充
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        healthBarFill = fillObj.AddComponent<Image>();
        healthBarFill.color = healthBarColor;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0, 0.5f); // 从左边开始填充
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        // 初始隐藏血条
        healthBarCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// 更新血条显示
    /// </summary>
    void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float healthPercent = currentHp / MaxHp;
            healthBarFill.rectTransform.anchorMax = new Vector2(healthPercent, 1);
        }
    }

    /// <summary>
    /// 显示血条并在指定时间后隐藏
    /// </summary>
    void ShowHealthBar()
    {
        if (healthBarCanvas == null) return;

        // 显示血条
        healthBarCanvas.gameObject.SetActive(true);
        UpdateHealthBar();

        // 停止之前的隐藏协程
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        // 启动新的隐藏协程
        hideCoroutine = StartCoroutine(HideHealthBarAfterDelay());
    }

    IEnumerator HideHealthBarAfterDelay()
    {
        yield return new WaitForSeconds(healthBarDisplayTime);
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(false);
        }
        hideCoroutine = null;
    }

    public void TakeDamage(AttackData attackData)
    {
        currentHp -= attackData.damage;
        
        // 触发事件
        OnTakeDamage?.Invoke(attackData);
        
        // 显示血条
        ShowHealthBar();

        if (currentHp <= 0)
        {
            currentHp = 0;
        }
        Debug.Log($"take damage: {attackData.damage}");
        Debug.Log($"currentHp: {currentHp}");
    }

    public void Die()
    {
         var enemyController = this.GetComponent<BaseEnemyController>();
         if (enemyController != null)
         {
            Destroy(enemyController);
         }
        StartCoroutine(DieAnimation());
    }
    IEnumerator DieAnimation()
    {
        if (renderer == null || Deadmaterial == null)
        {
            Debug.LogWarning("DieAnimation: renderer 或 Deadmaterial 为空，直接销毁对象");
            yield return new WaitForSeconds(deadAnimationDuration);
            Destroy(gameObject);
            yield break;
        }

        // 使用 material 而不是 sharedMaterial，这会创建材质实例
        // 避免影响其他使用相同材质的对象
        renderer.material = Deadmaterial;
        
        // 缓存材质引用，避免每帧重新获取
        Material mat = renderer.material;
        
        // 确保从 0 开始溶解
        mat.SetFloat("_DissolveAmount", 0f);
        
        float dissolveDuration = deadAnimationDuration; // 溶解动画总时长
        float elapsed = 0f;
        
        while (elapsed < dissolveDuration)
        {
            float dissolveAmount = Mathf.Clamp01(elapsed / dissolveDuration);
            mat.SetFloat("_DissolveAmount", dissolveAmount);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 确保最终值为 1
        mat.SetFloat("_DissolveAmount", 1f);
        
        Destroy(gameObject);
    }
}
