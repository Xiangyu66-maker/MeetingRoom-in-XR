using UnityEngine;
#if UNITY_EDITOR
using System.Collections;
#endif

public class PlayerHideState : MonoBehaviour
{
    public static bool IsHidden { get; private set; }

    public static void SetHidden(bool hidden)
    {
        IsHidden = hidden;
        Debug.Log("Player hidden state: " + IsHidden);
    }

    public void SetHideState(bool hidden)
    {
        SetHidden(hidden);
    }

    private void OnDisable()
    {
        IsHidden = false;
    }
}

#if UNITY_EDITOR
/// <summary>
/// Gives the Meta XR Simulator a sensible standing height when it reports an
/// identity head pose. The whole tracking space is moved so the headset and
/// controllers keep their tracked relationship. Editor-only, so Quest builds
/// keep their real tracked height.
/// </summary>
internal static class EditorHeadsetHeightFallback
{
    private const float StandingEyeHeight = 1.6f;
    private const float MinimumValidEyeHeight = 0.3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var host = new GameObject(nameof(EditorHeadsetHeightFallback));
        Object.DontDestroyOnLoad(host);
        host.hideFlags = HideFlags.HideAndDontSave;
        host.AddComponent<HeightFallbackRunner>();
    }

    private sealed class HeightFallbackRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig == null || rig.trackingSpace == null || rig.centerEyeAnchor == null)
            {
                yield break;
            }

            if (rig.centerEyeAnchor.position.y >= MinimumValidEyeHeight)
            {
                yield break;
            }

            float heightCorrection = StandingEyeHeight - rig.centerEyeAnchor.position.y;
            rig.trackingSpace.position += Vector3.up * heightCorrection;

            Debug.Log(
                $"Meta XR Simulator reported a zero-height head pose. " +
                $"Raised the complete tracking space by {heightCorrection:0.00} m.");
        }
    }
}
#endif
