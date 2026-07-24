using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/GPT Vision Interaction Manager")]
public sealed class GptVisionInteractionManager : MonoBehaviour
{
    private enum AiProvider
    {
        OpenAIResponses,
        GeminiOpenAICompatible
    }

    public static GptVisionInteractionManager Instance { get; private set; }

    [Header("Provider")]
    [SerializeField] private AiProvider provider = AiProvider.OpenAIResponses;
    [SerializeField] private string openAiModel = "gpt-5.4-mini";
    [SerializeField] private string geminiModel = "gemini-3.5-flash";
    [SerializeField] private string openAiApiKeyEnvironmentVariable = "OPENAI_API_KEY";
    [SerializeField] private string geminiApiKeyEnvironmentVariable = "GEMINI_API_KEY";

    [Header("Vision Capture")]
    [SerializeField] private int captureResolution = 768;
    [SerializeField] private string imageDetail = "high"; // OpenAI: low, high, auto. Gemini compatibility ignores this.
    [SerializeField] private int maxOutputTokens = 220;
    [SerializeField] private float fieldOfView = 38f;
    [SerializeField] private float framingPadding = 1.35f;
    [SerializeField] private bool showDebugOverlay = true;

    [TextArea(5, 12)]
    [SerializeField] private string instructions =
        "You are the visual recognition AI for a Unity meeting-room escape prototype. " +
        "You receive a screenshot of the object the player is interacting with. " +
        "First identify the visible object from the image, then give one natural Simplified Chinese feedback sentence for the escape-room context. " +
        "Do not rely only on object_id. If the image is unclear, say it is unclear. " +
        "Do not directly reveal the final keypad password. Output format: Visual recognition: ... Feedback: ...";

    private const string OpenAIResponsesEndpoint = "https://api.openai.com/v1/responses";
    private const string GeminiChatCompletionsEndpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";

    private Camera captureCamera;
    private bool requestInFlight;
    private string lastText = "";

    private string ActiveProviderName => provider == AiProvider.OpenAIResponses ? "OpenAI" : "Gemini";
    private string ActiveModel => provider == AiProvider.OpenAIResponses ? openAiModel : geminiModel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CreateCaptureCamera();
    }

    public void AnalyzeObject(GameObject target, string objectId, string description)
    {
        if (target == null || requestInFlight)
        {
            return;
        }

        StartCoroutine(AnalyzeRoutine(target, objectId, description));
    }

    private IEnumerator AnalyzeRoutine(GameObject target, string objectId, string description)
    {
        requestInFlight = true;
        lastText = $"{ActiveProviderName} VLM is observing...";

        byte[] pngBytes = CaptureTarget(target);
        if (pngBytes == null || pngBytes.Length == 0)
        {
            lastText = "No usable object image was captured.";
            requestInFlight = false;
            yield break;
        }

        string apiKey = LoadApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            lastText = $"Missing {GetApiKeyEnvironmentVariable()} environment variable. Set it and restart Unity.";
            requestInFlight = false;
            yield break;
        }

        string base64 = Convert.ToBase64String(pngBytes);
        string prompt =
            $"Unity scene metadata: object_id={objectId}, description={description}.\n" +
            "Identify the object from the image first, then give the player one short Simplified Chinese feedback or gentle hint for the meeting-room escape scene.";

        string endpoint = GetEndpoint();
        string body = BuildRequestJson(prompt, base64);
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

        Debug.Log($"Sending VLM request to {ActiveProviderName}, model={ActiveModel}.", this);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler.text;
            if (request.result != UnityWebRequest.Result.Success)
            {
                lastText = $"{ActiveProviderName} request failed: {request.responseCode} {request.error}\n{ExtractErrorMessage(responseText)}";
            }
            else
            {
                lastText = ExtractAssistantText(responseText);
            }
        }

        requestInFlight = false;
    }

    private string LoadApiKey()
    {
        string variable = GetApiKeyEnvironmentVariable();
        string key = Environment.GetEnvironmentVariable(variable);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        if (string.IsNullOrWhiteSpace(key))
        {
            key = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.User);
        }
#endif

        return key;
    }

    private string GetApiKeyEnvironmentVariable()
    {
        return provider == AiProvider.OpenAIResponses
            ? openAiApiKeyEnvironmentVariable
            : geminiApiKeyEnvironmentVariable;
    }

    private string GetEndpoint()
    {
        return provider == AiProvider.OpenAIResponses
            ? OpenAIResponsesEndpoint
            : GeminiChatCompletionsEndpoint;
    }

    private string BuildRequestJson(string prompt, string base64Png)
    {
        return provider == AiProvider.OpenAIResponses
            ? BuildOpenAIResponsesJson(prompt, base64Png)
            : BuildGeminiChatCompletionsJson(prompt, base64Png);
    }

    private string BuildOpenAIResponsesJson(string prompt, string base64Png)
    {
        return "{" +
            $"\"model\":\"{Esc(openAiModel)}\"," +
            $"\"instructions\":\"{Esc(instructions)}\"," +
            "\"input\":[{\"role\":\"user\",\"content\":[" +
            $"{{\"type\":\"input_text\",\"text\":\"{Esc(prompt)}\"}}," +
            $"{{\"type\":\"input_image\",\"image_url\":\"data:image/png;base64,{base64Png}\",\"detail\":\"{Esc(imageDetail)}\"}}" +
            "]}]," +
            $"\"max_output_tokens\":{Mathf.Max(1, maxOutputTokens)}," +
            "\"store\":false" +
            "}";
    }

    private string BuildGeminiChatCompletionsJson(string prompt, string base64Png)
    {
        string fullPrompt = instructions + "\n\n" + prompt;
        return "{" +
            $"\"model\":\"{Esc(geminiModel)}\"," +
            $"\"max_tokens\":{Mathf.Max(1, maxOutputTokens)}," +
            "\"messages\":[{\"role\":\"user\",\"content\":[" +
            $"{{\"type\":\"text\",\"text\":\"{Esc(fullPrompt)}\"}}," +
            $"{{\"type\":\"image_url\",\"image_url\":{{\"url\":\"data:image/png;base64,{base64Png}\"}}}}" +
            "]}]" +
            "}";
    }

    private string ExtractAssistantText(string responseText)
    {
        if (provider == AiProvider.OpenAIResponses)
        {
            OpenAIResponseDto openAiResponse = JsonUtility.FromJson<OpenAIResponseDto>(responseText);
            return ExtractOpenAIText(openAiResponse);
        }

        GeminiChatCompletionDto geminiResponse = JsonUtility.FromJson<GeminiChatCompletionDto>(responseText);
        return ExtractGeminiText(geminiResponse);
    }

    private static string ExtractOpenAIText(OpenAIResponseDto response)
    {
        if (response == null)
        {
            return "OpenAI returned an empty response.";
        }

        if (!string.IsNullOrWhiteSpace(response.output_text))
        {
            return response.output_text.Trim();
        }

        if (response.output != null)
        {
            foreach (OpenAIOutputItem item in response.output)
            {
                if (item.content == null)
                {
                    continue;
                }

                foreach (OpenAIContentItem content in item.content)
                {
                    if (!string.IsNullOrWhiteSpace(content.text))
                    {
                        return content.text.Trim();
                    }
                }
            }
        }

        return "OpenAI returned no text.";
    }

    private static string ExtractGeminiText(GeminiChatCompletionDto response)
    {
        if (response == null || response.choices == null || response.choices.Length == 0)
        {
            return "Gemini returned an empty response.";
        }

        GeminiChoice firstChoice = response.choices[0];
        if (firstChoice != null && firstChoice.message != null && !string.IsNullOrWhiteSpace(firstChoice.message.content))
        {
            return firstChoice.message.content.Trim();
        }

        return "Gemini returned no text.";
    }

    private static string ExtractErrorMessage(string json)
    {
        ErrorResponseDto errorResponse = JsonUtility.FromJson<ErrorResponseDto>(json);
        if (errorResponse != null && errorResponse.error != null && !string.IsNullOrWhiteSpace(errorResponse.error.message))
        {
            return errorResponse.error.message;
        }

        return json;
    }

    private byte[] CaptureTarget(GameObject target)
    {
        if (!TryGetBounds(target, out Bounds bounds))
        {
            return null;
        }

        Camera mainCamera = Camera.main;
        Vector3 viewDirection = mainCamera != null
            ? (bounds.center - mainCamera.transform.position).normalized
            : Vector3.back;

        if (viewDirection.sqrMagnitude < 0.01f)
        {
            viewDirection = Vector3.back;
        }

        float radius = Mathf.Max(bounds.extents.magnitude, 0.25f);
        float distance = radius / Mathf.Sin(fieldOfView * Mathf.Deg2Rad * 0.5f) * framingPadding;

        captureCamera.transform.position = bounds.center - viewDirection * distance;
        captureCamera.transform.LookAt(bounds.center);
        captureCamera.fieldOfView = fieldOfView;
        captureCamera.nearClipPlane = 0.01f;
        captureCamera.farClipPlane = distance + radius * 8f;

        RenderTexture rt = new RenderTexture(captureResolution, captureResolution, 24);
        Texture2D tex = new Texture2D(captureResolution, captureResolution, TextureFormat.RGB24, false);

        RenderTexture previous = RenderTexture.active;
        captureCamera.targetTexture = rt;
        RenderTexture.active = rt;

        captureCamera.Render();
        tex.ReadPixels(new Rect(0, 0, captureResolution, captureResolution), 0, 0);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();

        captureCamera.targetTexture = null;
        RenderTexture.active = previous;
        rt.Release();

        Destroy(rt);
        Destroy(tex);

        return png;
    }

    private void CreateCaptureCamera()
    {
        GameObject cameraObject = new GameObject("GPT Vision Capture Camera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;
        captureCamera = cameraObject.AddComponent<Camera>();
        captureCamera.enabled = false;
        captureCamera.clearFlags = CameraClearFlags.Skybox;
    }

    private static bool TryGetBounds(GameObject target, out Bounds bounds)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return true;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }

            return true;
        }

        bounds = default;
        return false;
    }

    private static string Esc(string value)
    {
        return string.IsNullOrEmpty(value)
            ? ""
            : value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    private void OnGUI()
    {
        if (!showDebugOverlay || string.IsNullOrWhiteSpace(lastText))
        {
            return;
        }

        GUI.Box(new Rect(24, Screen.height - 140, Screen.width - 48, 112), $"{ActiveProviderName} Vision");
        GUI.Label(new Rect(40, Screen.height - 108, Screen.width - 80, 80), lastText);
    }

    [Serializable]
    private sealed class OpenAIResponseDto
    {
        public string output_text;
        public OpenAIOutputItem[] output;
    }

    [Serializable]
    private sealed class OpenAIOutputItem
    {
        public OpenAIContentItem[] content;
    }

    [Serializable]
    private sealed class OpenAIContentItem
    {
        public string type;
        public string text;
    }

    [Serializable]
    private sealed class GeminiChatCompletionDto
    {
        public GeminiChoice[] choices;
    }

    [Serializable]
    private sealed class GeminiChoice
    {
        public GeminiMessage message;
    }

    [Serializable]
    private sealed class GeminiMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    private sealed class ErrorResponseDto
    {
        public ApiError error;
    }

    [Serializable]
    private sealed class ApiError
    {
        public string message;
        public string type;
        public string code;
    }
}
