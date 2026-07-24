using UnityEngine;

public class PlayerHideState : MonoBehaviour
{
    // 猪的两个脚本通过 PlayerHideState.IsHidden 读取这个状态
    public static bool IsHidden { get; private set; } = false;

    // 兼容之前可能使用的 isHiding 写法
    public bool isHiding
    {
        get => IsHidden;
        set => IsHidden = value;
    }

    private void Awake()
    {
        // 每次进入或重新加载场景时，默认玩家没有躲藏
        IsHidden = false;
    }

    // 兼容新版 LockerHideSpot 的调用
    public void SetHideState(bool state)
    {
        IsHidden = state;
        Debug.Log("Player hidden state: " + IsHidden);
    }

    // 兼容旧版 LockerHideSpot 的调用
    public static void SetHidden(bool hidden)
    {
        IsHidden = hidden;
        Debug.Log("Player hidden state: " + IsHidden);
    }
}