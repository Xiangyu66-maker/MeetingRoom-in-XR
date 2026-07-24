using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Game State Manager")]
public sealed class GameStateManager : MonoBehaviour
{
    private const float DefaultTimeLimitSeconds = 600f;

    public enum GameState
    {
        Playing,
        Success,
        Failed
    }

    public float timeLimitSeconds = 600f;
    public bool doorUnlocked;
    public GameState currentState = GameState.Playing;

    [SerializeField] private GameResultUI resultUI;
    [SerializeField] private GameTimerUI timerUI;

    private float startTime;
    private float frozenElapsedTime;

    private void Awake()
    {
        if (timeLimitSeconds <= 0f)
        {
            timeLimitSeconds = DefaultTimeLimitSeconds;
        }

        currentState = GameState.Playing;
        doorUnlocked = false;
        StartGameTimer();

        EnsureResultUI();
        EnsureTimerUI();

        if (resultUI != null)
        {
            resultUI.Hide();
        }

        Debug.Log($"GameStateManager started. Time limit: {timeLimitSeconds:0} seconds.", this);
    }

    private void Update()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        frozenElapsedTime = GetLiveElapsedTime();
        if (frozenElapsedTime >= timeLimitSeconds)
        {
            FailGame();
        }
    }

    public void NotifyDoorUnlocked()
    {
        if (doorUnlocked)
        {
            return;
        }

        doorUnlocked = true;
        Debug.Log("GameStateManager notified: door unlocked.", this);
    }

    public void TryTriggerSuccess()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        if (!doorUnlocked)
        {
            Debug.Log("Exit reached, but door is still locked.", this);
            return;
        }

        SucceedGame();
    }

    public void SucceedGame()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        frozenElapsedTime = GetLiveElapsedTime();
        currentState = GameState.Success;

        EnsureResultUI();
        if (resultUI != null)
        {
            resultUI.ShowSuccess();
        }

        Debug.Log("Game Success triggered.", this);
    }

    public void FailGame()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        frozenElapsedTime = Mathf.Min(GetLiveElapsedTime(), timeLimitSeconds);
        currentState = GameState.Failed;

        EnsureResultUI();
        if (resultUI != null)
        {
            resultUI.ShowFailure();
        }

        Debug.Log("Game Failed: time limit exceeded.", this);
    }

    public float GetElapsedTime()
    {
        if (currentState == GameState.Playing)
        {
            return GetLiveElapsedTime();
        }

        return frozenElapsedTime;
    }

    public float GetRemainingTime()
    {
        return Mathf.Clamp(timeLimitSeconds - GetElapsedTime(), 0f, Mathf.Max(0f, timeLimitSeconds));
    }

    public float GetRemainingTime01()
    {
        if (timeLimitSeconds <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(GetRemainingTime() / timeLimitSeconds);
    }

    public bool IsGameOver()
    {
        return currentState != GameState.Playing;
    }

    private void StartGameTimer()
    {
        startTime = Time.unscaledTime;
        frozenElapsedTime = 0f;
    }

    private float GetLiveElapsedTime()
    {
        return Mathf.Max(0f, Time.unscaledTime - startTime);
    }

    private void EnsureResultUI()
    {
        if (resultUI != null)
        {
            return;
        }

        resultUI = GetComponent<GameResultUI>();
        if (resultUI == null)
        {
            resultUI = FindObjectOfType<GameResultUI>();
        }

        if (resultUI == null)
        {
            resultUI = gameObject.AddComponent<GameResultUI>();
            Debug.Log("GameResultUI was missing, so GameStateManager added one for player-view result prompts.", this);
        }
    }

    private void EnsureTimerUI()
    {
        if (timerUI != null)
        {
            return;
        }

        timerUI = GetComponent<GameTimerUI>();
        if (timerUI == null)
        {
            timerUI = gameObject.AddComponent<GameTimerUI>();
            Debug.Log("GameTimerUI was missing, so GameStateManager added a player-view countdown bar.", this);
        }
    }
}
