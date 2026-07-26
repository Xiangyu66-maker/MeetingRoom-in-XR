using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Backpack Input Controller")]
public sealed class BackpackInputController : MonoBehaviour
{
    [SerializeField] private BackpackUI backpackUI;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private KeyCode nextItemKey = KeyCode.E;

    private void Awake()
    {
        ResolveUI();
    }

    private void Update()
    {
        bool togglePressed = Input.GetKeyDown(toggleKey);
        bool nextItemPressed = Input.GetKeyDown(nextItemKey);

        if (!togglePressed && !nextItemPressed)
        {
            return;
        }

        if (KeypadController.HasActiveInput)
        {
            return;
        }

        ResolveUI();
        if (backpackUI == null)
        {
            Debug.LogWarning("Tab pressed, but no BackpackUI is available.", this);
            return;
        }

        if (togglePressed)
        {
            if (backpackUI.IsOpen)
            {
                backpackUI.CloseBackpack();
                Debug.Log("Backpack closed.", this);
            }
            else
            {
                backpackUI.OpenBackpack();
                Debug.Log("Backpack opened.", this);
            }

            return;
        }

        if (nextItemPressed && backpackUI.IsOpen)
        {
            backpackUI.SelectNextItem();
        }
    }

    public void Configure(BackpackUI ui)
    {
        backpackUI = ui;
    }

    public static BackpackInputController GetOrCreate(BackpackUI ui)
    {
#if UNITY_2023_1_OR_NEWER
        BackpackInputController existing = FindFirstObjectByType<BackpackInputController>();
#else
        BackpackInputController existing = FindObjectOfType<BackpackInputController>();
#endif
        if (existing != null)
        {
            existing.Configure(ui);
            return existing;
        }

        GameObject inputObject = new GameObject("Backpack Input Controller");
        BackpackInputController controller = inputObject.AddComponent<BackpackInputController>();
        controller.Configure(ui);
        return controller;
    }

    private void ResolveUI()
    {
        if (backpackUI == null)
        {
            backpackUI = BackpackUI.GetOrCreate();
        }
    }
}
