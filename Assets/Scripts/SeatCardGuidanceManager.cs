using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Seat Card Guidance Manager")]
public sealed class SeatCardGuidanceManager : MonoBehaviour
{
    public float clueTimeoutSeconds = 60f;
    public string[] targetSeatCardIds =
    {
        "seat_card_01",
        "seat_card_02",
        "seat_card_03",
        "seat_card_04",
    };

    public bool guidanceTriggered;
    public bool guidanceCompleted;

    [SerializeField] private SeatCardInspectionTracker inspectionTracker;
    [SerializeField] private GuidanceTrailRenderer trailRenderer;
    [SerializeField] private SimpleObjectHighlighter objectHighlighter;
    [SerializeField] private Transform playerTransform;

    private readonly List<Transform> targetTransforms = new List<Transform>();
    private readonly List<GameObject> targetObjects = new List<GameObject>();
    private GameStateManager gameStateManager;
    private float searchStartTime;
    private bool timerStarted;

    private void Awake()
    {
        EnsureLocalReferences();
        StartClueTimer();
    }

    private void Update()
    {
        if (guidanceCompleted)
        {
            return;
        }

        if (IsGameOver())
        {
            if (guidanceTriggered)
            {
                HideSeatCardGuidance();
            }

            return;
        }

        EnsureLocalReferences();

        if (inspectionTracker != null && inspectionTracker.HasInspectedAllSeatCards())
        {
            CompleteSeatCardGuidance();
            return;
        }

        if (inspectionTracker != null && inspectionTracker.HasInspectedAnySeatCard())
        {
            if (guidanceTriggered)
            {
                HideSeatCardGuidance();
            }

            return;
        }

        if (!timerStarted)
        {
            StartClueTimer();
        }

        if (!guidanceTriggered && Time.time - searchStartTime >= clueTimeoutSeconds)
        {
            ShowSeatCardGuidance();
        }
    }

    public void Configure(
        SeatCardInspectionTracker tracker,
        GuidanceTrailRenderer guidanceTrail,
        SimpleObjectHighlighter highlighter,
        Transform player,
        List<ObjectIdentity> targetIdentities)
    {
        inspectionTracker = tracker;
        trailRenderer = guidanceTrail;
        objectHighlighter = highlighter;
        playerTransform = player;

        targetTransforms.Clear();
        targetObjects.Clear();

        if (targetIdentities != null)
        {
            foreach (ObjectIdentity identity in targetIdentities)
            {
                if (identity == null)
                {
                    continue;
                }

                targetTransforms.Add(identity.transform);
                targetObjects.Add(identity.gameObject);
            }
        }

        if (inspectionTracker != null)
        {
            inspectionTracker.ConfigureTargets(targetSeatCardIds);
        }

        StartClueTimer();
    }

    public void NotifySeatCardInspected(string objectId)
    {
        EnsureLocalReferences();

        if (inspectionTracker != null)
        {
            inspectionTracker.MarkSeatCardInspected(objectId);
        }

        if (inspectionTracker != null && inspectionTracker.HasInspectedAllSeatCards())
        {
            CompleteSeatCardGuidance();
            return;
        }

        if (inspectionTracker != null && inspectionTracker.HasInspectedAnySeatCard())
        {
            HideSeatCardGuidance();
        }
    }

    public void ShowSeatCardGuidance()
    {
        if (guidanceCompleted || guidanceTriggered)
        {
            return;
        }

        EnsureLocalReferences();

        if (playerTransform == null || targetTransforms.Count == 0)
        {
            Debug.LogWarning("Seat card guidance could not be shown because player or target objects are missing.", this);
            return;
        }

        guidanceTriggered = true;

        if (trailRenderer != null)
        {
            trailRenderer.ShowTrail(playerTransform, targetTransforms);
        }

        if (objectHighlighter != null)
        {
            objectHighlighter.HighlightObjects(targetObjects);
        }

        Debug.Log("Seat card guidance triggered after 60 seconds.", this);
    }

    public void HideSeatCardGuidance()
    {
        if (!guidanceTriggered)
        {
            return;
        }

        guidanceTriggered = false;

        if (trailRenderer != null)
        {
            trailRenderer.HideTrail();
        }

        if (objectHighlighter != null)
        {
            objectHighlighter.ClearHighlight();
        }

        Debug.Log("Seat card guidance hidden after seat card inspection.", this);
    }

    public void CompleteSeatCardGuidance()
    {
        if (guidanceCompleted)
        {
            return;
        }

        guidanceCompleted = true;
        guidanceTriggered = false;

        if (trailRenderer != null)
        {
            trailRenderer.HideTrail();
        }

        if (objectHighlighter != null)
        {
            objectHighlighter.ClearHighlight();
        }

        Debug.Log("All seat cards inspected. Seat card guidance completed.", this);
    }

    private void StartClueTimer()
    {
        searchStartTime = Time.time;
        timerStarted = true;
        guidanceTriggered = false;
        Debug.Log($"Seat card guidance timer started. Timeout: {clueTimeoutSeconds:0} seconds.", this);
    }

    private void EnsureLocalReferences()
    {
        if (inspectionTracker == null)
        {
            inspectionTracker = GetComponent<SeatCardInspectionTracker>();
        }

        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<GuidanceTrailRenderer>();
        }

        if (objectHighlighter == null)
        {
            objectHighlighter = GetComponent<SimpleObjectHighlighter>();
        }

        if (gameStateManager == null)
        {
            gameStateManager = FindGameStateManager();
        }
    }

    private bool IsGameOver()
    {
        EnsureLocalReferences();
        return gameStateManager != null && gameStateManager.IsGameOver();
    }

    private static GameStateManager FindGameStateManager()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<GameStateManager>();
#else
        return FindObjectOfType<GameStateManager>();
#endif
    }
}
