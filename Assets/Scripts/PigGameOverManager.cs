using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PigGameOverManager : MonoBehaviour
{
    private const string GameOverPanelName = "PigGameOverPanel";
    private const int GameOverSortingOrder = 20000;

    public static PigGameOverManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;

    private bool isGameOver = false;
    private Button restartButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate PigGameOverManager found. Disabling duplicate.", this);
            enabled = false;
            return;
        }

        Instance = this;
        ResolveUIReferences();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        // 游戏开始时正常锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowGameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        ResolveUIReferences();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("PigGameOverManager: GameOverPanel is not assigned.");
        }

        // 暂停游戏
        Time.timeScale = 0f;

        // 显示鼠标，让玩家可以点击 Restart 按钮
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        Debug.Log("Restart button clicked.");

        isGameOver = false;
        Time.timeScale = 1f;

        // 重启前恢复鼠标状态
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void ResolveUIReferences()
    {
        if (gameOverPanel == null)
        {
            gameOverPanel = FindGameOverPanel();
        }

        if (gameOverPanel == null)
        {
            Debug.LogError($"PigGameOverManager could not find {GameOverPanelName} in the active scene.", this);
            return;
        }

        Canvas canvas = gameOverPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = GameOverSortingOrder;
            canvas.transform.localScale = Vector3.one;
        }

        restartButton = gameOverPanel.GetComponentInChildren<Button>(true);
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }
        else
        {
            Debug.LogWarning("PigGameOverManager could not find a restart button under the game-over panel.", this);
        }
    }

    private static GameObject FindGameOverPanel()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        foreach (GameObject root in activeScene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate != null && candidate.name == GameOverPanelName)
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }
}
