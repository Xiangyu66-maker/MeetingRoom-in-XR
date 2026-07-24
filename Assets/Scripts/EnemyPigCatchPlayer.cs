using UnityEngine;

public class EnemyPigCatchPlayer : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private bool hasCaughtPlayer = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasCaughtPlayer)
        {
            return;
        }

        if (PlayerHideState.IsHidden)
        {
            Debug.Log("Pig touched player, but player is hidden.");
            return;
        }

        if (other.CompareTag(playerTag))
        {
            hasCaughtPlayer = true;

            Debug.Log("EnemyPigCatchPlayer: Pig caught the player.");

            if (PigGameOverManager.Instance != null)
            {
                PigGameOverManager.Instance.ShowGameOver();
            }
            else
            {
                Debug.LogWarning("EnemyPigCatchPlayer: PigGameOverManager not found in scene.");
            }
        }
    }
}