using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Start is called before the first frame update
    private CharacterController controller;
    public float speed = 12f;
    public float gravity = -9.81f * 2;
    public float jumpHeight = 3f;

    [Header("Hit Reaction")]
    [Tooltip("受击后禁用输入移动的时间（秒）")]
    public float hitStunTime = 0.35f;
    [Tooltip("击退水平速度（单位/秒）")]
    public float knockbackSpeed = 10f;
    [Tooltip("击飞起跳速度（单位/秒），仅在 knockUp 为 true 时使用")]
    public float knockUpVelocity = 6f;
    [Tooltip("击退速度衰减速度（越大越快停止）")]
    public float knockbackDamp = 10f;
    
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundDistance = 0.4f;

    [Header("Dash Settings")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1.0f;
    private float lastDashTime = -10f;
    private bool isDashing = false;
    private Vector3 dashVelocity;
    private PlayerState playerState;
    
    Vector3 velocity;
    private Vector3 knockbackPlanarVelocity;
    private float moveLockTimer;
    bool isGrounded;
    private bool isMoving;
    private Vector3 lastPosition = new Vector3(0f, 0f, 0f);

    public bool IsMovementLocked => moveLockTimer > 0f;
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerState = GetComponent<PlayerState>();
    }

    // Update is called once per frame
    void Update()
    {
        if (moveLockTimer > 0f)
        {
            moveLockTimer -= Time.deltaTime;
        }

        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = 0f;
        float z = 0f;
        if (!IsMovementLocked)
        {
            x = Input.GetAxis("Horizontal");
            z = Input.GetAxis("Vertical");
        }

        // 击退水平速度衰减
        knockbackPlanarVelocity = Vector3.Lerp(knockbackPlanarVelocity, Vector3.zero, knockbackDamp * Time.deltaTime);

        Vector3 inputDir = transform.right * x + transform.forward * z;

        if (!IsMovementLocked && !isDashing && Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(PerformDash(inputDir));
        }

        Vector3 planarVelocity;
        if (isDashing)
        {
            planarVelocity = dashVelocity;
        }
        else
        {
            planarVelocity = inputDir * speed + knockbackPlanarVelocity;
        }

        if (!IsMovementLocked && Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        velocity.y += gravity * Time.deltaTime;

        Vector3 motion = (planarVelocity + Vector3.up * velocity.y) * Time.deltaTime;
        controller.Move(motion);

        if (lastPosition != gameObject.transform.position && isGrounded)
        {
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
        lastPosition = gameObject.transform.position;
    }

    /// <summary>
    /// 受击击退：在 hitStunTime 内禁用输入，并按照击退速度移动（CharacterController.Move）。
    /// </summary>
    /// <param name="attackerPosition">攻击者世界坐标</param>
    /// <param name="knockUp">是否击飞</param>
    /// <param name="stunTimeOverride">可选：覆盖默认硬直时间（小于0表示使用 hitStunTime）</param>
    /// <param name="knockbackSpeedMultiplier">可选：击退强度倍率</param>
    public void ApplyHit(Vector3 attackerPosition, bool knockUp, float stunTimeOverride = -1f, float knockbackSpeedMultiplier = 1f)
    {
        Vector3 dir = transform.position - attackerPosition;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            dir.Normalize();
        }
        else
        {
            dir = -transform.forward;
        }

        moveLockTimer = Mathf.Max(moveLockTimer, stunTimeOverride >= 0f ? stunTimeOverride : hitStunTime);
        knockbackPlanarVelocity = dir * (knockbackSpeed * knockbackSpeedMultiplier);

        if (knockUp)
        {
            // 用同一套重力/垂直速度来做“击飞轨迹”
            velocity.y = Mathf.Max(velocity.y, knockUpVelocity);
        }
    }

    IEnumerator PerformDash(Vector3 dir)
    {
        isDashing = true;
        lastDashTime = Time.time;

        if (playerState != null) playerState.IsInvincible = true;

        // 如果没有输入方向，默认向前冲刺
        if (dir.sqrMagnitude < 0.01f)
        {
            dir = transform.forward;
        }
        
        dashVelocity = dir.normalized * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        if (playerState != null) playerState.IsInvincible = false;
        isDashing = false;
    }
}
