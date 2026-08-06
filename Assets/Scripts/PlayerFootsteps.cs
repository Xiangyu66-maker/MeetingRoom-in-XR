using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("音频设置")]
    [Tooltip("脚步音频源，挂载在脚部位置")]
    [SerializeField] private AudioSource footstepAudio;

    [Tooltip("左脚脚步声")]
    [SerializeField] private AudioClip leftFootClip;

    [Tooltip("右脚脚步声")]
    [SerializeField] private AudioClip rightFootClip;

    [Header("步频参数")]
    [Tooltip("走路时单步间隔（秒），正常步行速度下的节奏")]
    [SerializeField] private float walkStepInterval = 0.45f;

    [Tooltip("最快速度下单步间隔（秒），和走路差值很小，加快不明显")]
    [SerializeField] private float runStepInterval = 0.35f;

    [Tooltip("触发脚步的最小移动速度")]
    [SerializeField] private float minMoveSpeed = 0.2f;

    [Header("音量设置")]
    [SerializeField] private float minVolume = 0.5f;
    [SerializeField] private float maxVolume = 0.75f;

    private Vector3 lastPosition;
    private float stepTimer;
    private bool isLeftFootNext;

    private void Awake()
    {
        lastPosition = transform.position;
        stepTimer = 0f;
        isLeftFootNext = true; // 默认从左脚起步
    }

    private void Update()
    {
        // 只计算水平位移，忽略跳跃、低头的Y轴变化
        Vector3 currentPosition = transform.position;
        Vector3 horizontalDelta = new Vector3(
            currentPosition.x - lastPosition.x,
            0f,
            currentPosition.z - lastPosition.z
        );

        float moveSpeed = horizontalDelta.magnitude / Time.deltaTime;

        if (moveSpeed > minMoveSpeed)
        {
            // 最大速度阈值设为5m/s，正常VR走路速度下比值很低，步频几乎不加快
            float speedRatio = Mathf.InverseLerp(minMoveSpeed, 5f, moveSpeed);
            float currentInterval = Mathf.Lerp(walkStepInterval, runStepInterval, speedRatio);

            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                PlayFootstep(speedRatio);
                stepTimer = currentInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }

        lastPosition = currentPosition;
    }

    private void PlayFootstep(float speedRatio)
    {
        if (footstepAudio == null)
        {
            return;
        }

        // 左右脚交替播放
        AudioClip targetClip = isLeftFootNext ? leftFootClip : rightFootClip;

        // 容错：其中一个音效未赋值时，自动用另一个替代
        if (targetClip == null)
        {
            targetClip = leftFootClip != null ? leftFootClip : rightFootClip;
        }

        if (targetClip == null)
        {
            return;
        }

        // 速度越快音量略大，随机微调音高消除重复单调感
        footstepAudio.volume = Mathf.Lerp(minVolume, maxVolume, speedRatio);
        footstepAudio.pitch = Random.Range(0.95f, 1.05f);

        footstepAudio.PlayOneShot(targetClip);

        // 切换下一次的脚
        isLeftFootNext = !isLeftFootNext;
    }
}