using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class EnemyLocomotionAnimator : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private float dampingTime = 0.12f;

    private int speedParameterHash;
    private bool hasSpeedParameter;

    private void Awake()
    {
        ResolveReferences();
        CacheSpeedParameter();
    }

    private void OnValidate()
    {
        ResolveReferences();
        CacheSpeedParameter();
    }

    private void Update()
    {
        if (targetAnimator == null || !hasSpeedParameter)
        {
            return;
        }

        float speed = 0f;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            speed = agent.velocity.magnitude;
        }

        targetAnimator.SetFloat(
            speedParameterHash,
            speed,
            dampingTime,
            Time.deltaTime
        );
    }

    private void ResolveReferences()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (targetAnimator == null)
        {
            targetAnimator = GetComponentInChildren<Animator>(true);
        }
    }

    private void CacheSpeedParameter()
    {
        hasSpeedParameter = false;

        if (targetAnimator == null || string.IsNullOrWhiteSpace(speedParameter))
        {
            return;
        }

        speedParameterHash = Animator.StringToHash(speedParameter);

        foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
        {
            if (parameter.nameHash == speedParameterHash &&
                parameter.type == AnimatorControllerParameterType.Float)
            {
                hasSpeedParameter = true;
                break;
            }
        }
    }
}
