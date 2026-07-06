using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    public const string SpawnFacingXKey = "spawnFacingX";
    public const string SpawnFacingYKey = "spawnFacingY";

    [SerializeField] private string targetSceneName;
    [SerializeField] private bool setSpawnFacingDirection;
    [SerializeField] private Vector2 spawnFacingDirection = Vector2.down;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (Input.GetButtonDown("Interact"))
        {
            GameManager.Instance.LoadScene(targetSceneName, CreatePayload());
            Debug.Log("transition");
        }
    }

    private ScenePayload CreatePayload()
    {
        ScenePayload payload = GameManager.Instance.PendingPayload;
        if (!setSpawnFacingDirection)
        {
            return payload;
        }

        payload ??= new ScenePayload();

        Vector2 facingDirection = spawnFacingDirection.sqrMagnitude > 0f
            ? spawnFacingDirection.normalized
            : Vector2.down;

        payload.SetExtra(SpawnFacingXKey, facingDirection.x);
        payload.SetExtra(SpawnFacingYKey, facingDirection.y);

        return payload;
    }
}
