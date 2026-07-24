using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[DisallowMultipleComponent]
[AddComponentMenu("AI/Camera Prompt Sender")]
public sealed class CameraPromptSender : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField] private string endpointUrl = "http://127.0.0.1:8000/guide";

    [Header("Task Context")]
    [TextArea(3, 10)]
    [SerializeField] private string currentTask = "Help the player solve the Meeting-Room escape puzzle by inspecting the whiteboard clue, searching chair and seat-card clues for the door password, then using the keypad to open the locked door.";
    [SerializeField] private KeyCode sendKey = KeyCode.G;
    [SerializeField] private bool sendOnStart;

    [Header("Adaptive Guidance")]
    [SerializeField] private MeetingRoomAdaptiveGuide adaptiveGuide;
    [SerializeField] private bool useAdaptiveGuideContext = true;

    [Header("Player Source")]
    [SerializeField] private Transform playerTransform;

    [Header("Camera Image")]
    [SerializeField] private Camera captureCamera;
    [SerializeField] private bool includeCameraImage = true;
    [SerializeField] private int captureWidth = 512;
    [SerializeField] private int captureHeight = 288;
    [Range(1, 100)]
    [SerializeField] private int jpgQuality = 70;

    [Header("Fallback Target Object Sent To FastAPI")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private string targetId = "whiteboard_01";
    [SerializeField] private string targetName = "Whiteboard";
    [SerializeField] private string targetType = "puzzle_clue";
    [SerializeField] private string targetTag = "Untagged";
    [SerializeField] private Vector3 targetPosition = new Vector3(0f, 1.5f, 4f);
    [SerializeField] private string targetRole = "target";
    [SerializeField] private string targetState = "idle";

    [Header("Debug")]
    [SerializeField] private bool logRequest = true;
    [SerializeField] private bool logResponse = true;
    [SerializeField] private bool logVisionText = true;

    private bool isSending;

    private void Awake()
    {
        ResolveDefaults();
    }

    private void Start()
    {
        if (sendOnStart)
        {
            SendPrompt();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(sendKey))
        {
            SendPrompt();
        }
    }

    [ContextMenu("Send Prompt Now")]
    public void SendPrompt()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (isSending)
        {
            Debug.LogWarning("AI guide request is already in progress.", this);
            return;
        }

        StartCoroutine(PostGuideRequest());
    }

    private IEnumerator PostGuideRequest()
    {
        isSending = true;

        string imageBase64 = includeCameraImage ? CaptureCameraImageBase64() : null;
        string payload = BuildGuideJson(imageBase64);
        byte[] body = Encoding.UTF8.GetBytes(payload);

        using (UnityWebRequest request = new UnityWebRequest(endpointUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            request.timeout = 60;

            if (logRequest)
            {
                Debug.Log($"Sending AI guide request to {endpointUrl}. image_base64 included: {!string.IsNullOrEmpty(imageBase64)}", this);
            }

            yield return request.SendWebRequest();

            long statusCode = request.responseCode;
            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

#if UNITY_2020_2_OR_NEWER
            bool failed = request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.DataProcessingError;
#else
            bool failed = request.isNetworkError || request.isHttpError;
#endif

            if (failed)
            {
                Debug.LogError($"AI guide request failed. HTTP {statusCode}: {request.error}\n{responseText}", this);
            }
            else
            {
                GuideResponse guideResponse = JsonUtility.FromJson<GuideResponse>(responseText);
                if (guideResponse != null && !string.IsNullOrWhiteSpace(guideResponse.instruction))
                {
                    Debug.Log($"AI guide instruction: {guideResponse.instruction}", this);
                }

                if (guideResponse != null && logVisionText && !string.IsNullOrWhiteSpace(guideResponse.vision_text))
                {
                    Debug.Log($"AI vision text: {guideResponse.vision_text}", this);
                }

                if (guideResponse != null)
                {
                    MeetingRoomAdaptiveGuide.NotifyAiInstruction(guideResponse.instruction, guideResponse.vision_text, guideResponse.vision_diagnosis);
                }

                if (logResponse)
                {
                    Debug.Log($"AI guide response. HTTP {statusCode}:\n{responseText}", this);
                }
            }
        }

        isSending = false;
    }

    private string BuildGuideJson(string imageBase64)
    {
        Transform playerSource = ResolvePlayerTransform();
        Vector3 playerPosition = playerSource != null ? playerSource.position : transform.position;
        string taskContext = BuildTaskContext();
        SceneObject[] sceneObjects = BuildSceneObjects();

        GuideRequest payload = new GuideRequest
        {
            player_position = ToPosition(playerPosition),
            current_task = taskContext,
            objects = sceneObjects,
            image_base64 = imageBase64,
        };

        return JsonUtility.ToJson(payload);
    }

    private string BuildTaskContext()
    {
        if (useAdaptiveGuideContext && adaptiveGuide != null)
        {
            return adaptiveGuide.BuildAiTaskContext(currentTask);
        }

        return currentTask;
    }

    private SceneObject[] BuildSceneObjects()
    {
        List<SceneObject> sceneObjects = new List<SceneObject>();

        if (useAdaptiveGuideContext && adaptiveGuide != null)
        {
            List<MeetingRoomAdaptiveGuide.GuideObjectState> adaptiveObjects = adaptiveGuide.GetGuideObjectsForAi();
            foreach (MeetingRoomAdaptiveGuide.GuideObjectState adaptiveObject in adaptiveObjects)
            {
                if (adaptiveObject == null || string.IsNullOrWhiteSpace(adaptiveObject.id))
                {
                    continue;
                }

                sceneObjects.Add(new SceneObject
                {
                    id = adaptiveObject.id,
                    name = string.IsNullOrWhiteSpace(adaptiveObject.name) ? adaptiveObject.id : adaptiveObject.name,
                    type = adaptiveObject.type,
                    tag = "Untagged",
                    position = ToPosition(adaptiveObject.position),
                    role = adaptiveObject.role,
                    state = adaptiveObject.state,
                });
            }
        }

        if (sceneObjects.Count == 0)
        {
            Transform objectTransform = targetTransform != null ? targetTransform : transform;
            Vector3 objectPosition = targetTransform != null ? targetTransform.position : targetPosition;
            string objectName = targetTransform != null && !string.IsNullOrWhiteSpace(targetTransform.name) ? targetTransform.name : targetName;

            sceneObjects.Add(new SceneObject
            {
                id = targetId,
                name = objectName,
                type = targetType,
                tag = targetTag,
                position = ToPosition(objectPosition),
                role = targetRole,
                state = targetState,
            });
        }

        return sceneObjects.ToArray();
    }

    private string CaptureCameraImageBase64()
    {
        Camera sourceCamera = captureCamera != null ? captureCamera : GetComponent<Camera>();
        if (sourceCamera == null)
        {
            Debug.LogWarning("No camera is available for image capture. Sending request without image_base64.", this);
            return null;
        }

        int width = Mathf.Max(16, captureWidth);
        int height = Mathf.Max(16, captureHeight);
        int quality = Mathf.Clamp(jpgQuality, 1, 100);

        RenderTexture previousTarget = sourceCamera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);

        try
        {
            sourceCamera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            sourceCamera.Render();
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            byte[] jpgBytes = texture.EncodeToJPG(quality);
            return Convert.ToBase64String(jpgBytes);
        }
        finally
        {
            sourceCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Destroy(renderTexture);
            Destroy(texture);
        }
    }

    private void ResolveDefaults()
    {
        if (captureCamera == null)
        {
            captureCamera = GetComponent<Camera>();
        }

        if (adaptiveGuide == null)
        {
            adaptiveGuide = GetComponent<MeetingRoomAdaptiveGuide>();
        }

        if (adaptiveGuide == null)
        {
            adaptiveGuide = FindFirstObjectByType<MeetingRoomAdaptiveGuide>();
        }
    }

    private Transform ResolvePlayerTransform()
    {
        if (playerTransform != null)
        {
            return playerTransform;
        }

        return transform.root != transform ? transform.root : transform;
    }

    private static Position ToPosition(Vector3 value)
    {
        return new Position
        {
            x = value.x,
            y = value.y,
            z = value.z,
        };
    }

    [Serializable]
    private sealed class GuideRequest
    {
        public Position player_position;
        public string current_task;
        public SceneObject[] objects;
        public string image_base64;
    }

    [Serializable]
    private sealed class SceneObject
    {
        public string id;
        public string name;
        public string type;
        public string tag;
        public Position position;
        public string role;
        public string state;
    }

    [Serializable]
    private sealed class Position
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    private sealed class GuideResponse
    {
        public string instruction;
        public string model;
        public string status;
        public string vision_text;
        public MeetingRoomAdaptiveGuide.VisionDiagnosis vision_diagnosis;
    }
}


