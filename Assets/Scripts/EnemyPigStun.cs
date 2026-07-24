using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyPigStun : MonoBehaviour
{
    [Header("Knockback Settings")]
    [SerializeField] private float knockbackDistance = 2f;
    [SerializeField] private float knockbackDuration = 0.25f;
    [SerializeField] private float knockbackHeight = 0.35f;

    private EnemyPigChase chaseScript;
    private EnemyPigCatchPlayer catchScript;
    private NavMeshAgent agent;
    private Animator pigAnimator;

    private Coroutine stunCoroutine;
    private bool isStunned;

    private bool chaseWasEnabled;
    private bool catchWasEnabled;
    private bool agentWasEnabled;
    private bool agentWasStopped;
    private float previousAnimatorSpeed = 1f;

    private void Awake()
    {
        chaseScript = GetComponent<EnemyPigChase>();
        catchScript = GetComponent<EnemyPigCatchPlayer>();
        agent = GetComponent<NavMeshAgent>();
        pigAnimator = GetComponentInChildren<Animator>();

        if (pigAnimator != null)
        {
            previousAnimatorSpeed = pigAnimator.speed;
        }
    }

    public void Stun(float duration, Vector3 shotDirection)
    {
        if (duration <= 0f)
        {
            return;
        }

        // 第一次被击中时保存猪原来的状态
        if (!isStunned)
        {
            chaseWasEnabled =
                chaseScript != null && chaseScript.enabled;

            catchWasEnabled =
                catchScript != null && catchScript.enabled;

            if (agent != null)
            {
                agentWasEnabled = agent.enabled;

                if (agent.enabled && agent.isOnNavMesh)
                {
                    agentWasStopped = agent.isStopped;
                }
            }

            if (pigAnimator != null)
            {
                previousAnimatorSpeed = pigAnimator.speed;
            }
        }

        // 眩晕期间再次中枪，会重新计算时间并再次击退
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }

        stunCoroutine = StartCoroutine(
            StunRoutine(duration, shotDirection)
        );
    }

    private IEnumerator StunRoutine(
        float duration,
        Vector3 shotDirection)
    {
        isStunned = true;

        // 停止追逐
        if (chaseScript != null)
        {
            chaseScript.enabled = false;
        }

        // 眩晕时不能抓玩家
        if (catchScript != null)
        {
            catchScript.enabled = false;
        }

        // 暂停动画
        if (pigAnimator != null)
        {
            pigAnimator.speed = 0f;
        }

        // 停止并暂时关闭 NavMeshAgent
        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        // 只保留水平方向，避免猪沿着上下方向乱飞
        Vector3 knockbackDirection = shotDirection;
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude < 0.001f)
        {
            knockbackDirection = -transform.forward;
        }

        knockbackDirection.Normalize();

        Vector3 startPosition = transform.position;
        Vector3 wantedPosition =
            startPosition +
            knockbackDirection * knockbackDistance;

        /*
         * 检查后方是不是NavMesh边缘或障碍。
         * 避免猪被击退到墙里或地图外。
         */
        Vector3 targetPosition = wantedPosition;

        if (NavMesh.Raycast(
                startPosition,
                wantedPosition,
                out NavMeshHit navHit,
                NavMesh.AllAreas))
        {
            targetPosition =
                navHit.position -
                knockbackDirection * 0.15f;
        }

        float elapsedTime = 0f;

        while (elapsedTime < knockbackDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsedTime / knockbackDuration
            );

            // 让运动开始快、结束慢
            float smoothT =
                1f - Mathf.Pow(1f - t, 3f);

            Vector3 newPosition = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothT
            );

            // 增加一个很小的腾空弧线
            newPosition.y +=
                Mathf.Sin(t * Mathf.PI) *
                knockbackHeight;

            transform.position = newPosition;

            yield return null;
        }

        // 保证最后落回NavMesh附近
        if (NavMesh.SamplePosition(
                targetPosition,
                out NavMeshHit landingHit,
                2f,
                NavMesh.AllAreas))
        {
            transform.position = landingHit.position;
        }
        else
        {
            transform.position = targetPosition;
        }

        Debug.Log(
            "Pig was knocked back and stunned for " +
            duration +
            " seconds."
        );

        // 击退过程也算在眩晕时间内
        float remainingStunTime =
            Mathf.Max(0f, duration - knockbackDuration);

        yield return new WaitForSeconds(remainingStunTime);

        RestorePig();

        isStunned = false;
        stunCoroutine = null;

        Debug.Log("Pig recovered from stun.");
    }

    private void RestorePig()
    {
        // 先恢复NavMeshAgent
        if (agent != null && agentWasEnabled)
        {
            if (NavMesh.SamplePosition(
                    transform.position,
                    out NavMeshHit hit,
                    2f,
                    NavMesh.AllAreas))
            {
                transform.position = hit.position;
            }

            agent.enabled = true;

            if (agent.isOnNavMesh)
            {
                agent.Warp(transform.position);
                agent.isStopped = agentWasStopped;
            }
        }

        // 再恢复追逐与抓人，避免脚本访问关闭的Agent
        if (chaseScript != null)
        {
            chaseScript.enabled = chaseWasEnabled;
        }

        if (catchScript != null)
        {
            catchScript.enabled = catchWasEnabled;
        }

        if (pigAnimator != null)
        {
            pigAnimator.speed = previousAnimatorSpeed;
        }
    }
}