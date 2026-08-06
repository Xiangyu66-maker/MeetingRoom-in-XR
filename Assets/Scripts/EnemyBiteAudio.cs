using UnityEngine;

public class EnemyBiteAudio : MonoBehaviour
{
    [Header("音频配置")]
    [Tooltip("怪物音频源，挂载在怪物根物体上")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("啃咬音效列表，多个会随机播放避免单调")]
    [SerializeField] private AudioClip[] biteAudioClips;

    [Header("播放间隔")]
    [Tooltip("两次啃咬声的最小间隔（秒）")]
    [SerializeField] private float minInterval = 1.2f;
    [Tooltip("两次啃咬声的最大间隔（秒）")]
    [SerializeField] private float maxInterval = 3.5f;

    [Header("音量音高")]
    [SerializeField] private float minVolume = 0.3f;
    [SerializeField] private float maxVolume = 0.6f;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    [Header("状态控制")]
    [Tooltip("是否启用啃咬音效")]
    public bool isEnabled = true;

    private float nextPlayTime;

    private void Awake()
    {
        // 自动获取自身的AudioSource
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // 初始化第一次播放时间
        ScheduleNextBite();
    }

    private void Update()
    {
        if (!isEnabled
            || audioSource == null
            || biteAudioClips == null
            || biteAudioClips.Length == 0)
        {
            return;
        }

        if (Time.time >= nextPlayTime)
        {
            PlayRandomBite();
            ScheduleNextBite();
        }
    }

    /// <summary>
    /// 随机选一个音效播放，同时微调音量音高
    /// </summary>
    private void PlayRandomBite()
    {
        AudioClip randomClip = biteAudioClips[Random.Range(0, biteAudioClips.Length)];

        audioSource.volume = Random.Range(minVolume, maxVolume);
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        audioSource.PlayOneShot(randomClip);
    }

    /// <summary>
    /// 随机计算下一次播放的时间点
    /// </summary>
    private void ScheduleNextBite()
    {
        float randomDelay = Random.Range(minInterval, maxInterval);
        nextPlayTime = Time.time + randomDelay;
    }

    /// <summary>
    /// 暂停啃咬声（怪物眩晕时调用）
    /// </summary>
    public void PauseBiteAudio()
    {
        isEnabled = false;
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// 恢复啃咬声（眩晕结束时调用）
    /// </summary>
    public void ResumeBiteAudio()
    {
        isEnabled = true;
        ScheduleNextBite();
    }
}