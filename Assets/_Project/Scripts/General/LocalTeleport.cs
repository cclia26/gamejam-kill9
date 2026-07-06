using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LocalTeleport : MonoBehaviour
{
    [SerializeField] private Transform targetPoint;
    [SerializeField] private bool requireInteract = true;
    [SerializeField] private DoorLock requiredDoor;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private Collider2D targetCameraBounds;
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float blackHoldDuration = 0.05f;

    private Transform player;
    private bool isTeleporting;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        player = other.attachedRigidbody != null
            ? other.attachedRigidbody.transform
            : other.transform;

        if (!requireInteract)
        {
            Teleport();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Transform exitingPlayer = other.attachedRigidbody != null
            ? other.attachedRigidbody.transform
            : other.transform;

        if (player == exitingPlayer)
        {
            player = null;
        }
    }

    private void Update()
    {
        if (!requireInteract || player == null)
        {
            return;
        }

        if (Input.GetButtonDown("Interact"))
        {
            Teleport();
        }
    }

    private void Teleport()
    {
        if (!CanTeleport() || isTeleporting)
        {
            return;
        }

        if (fadeOverlay == null)
        {
            PerformTeleport(player);
            return;
        }

        StartCoroutine(TeleportRoutine(player));
    }

    private IEnumerator TeleportRoutine(Transform teleportingPlayer)
    {
        isTeleporting = true;
        SetFadeOverlayActive(true, 0f);

        yield return FadeTo(1f);
        PerformTeleport(teleportingPlayer);

        if (blackHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(blackHoldDuration);
        }

        yield return FadeTo(0f);

        SetFadeOverlayActive(false, 0f);
        isTeleporting = false;
    }

    private void PerformTeleport(Transform teleportingPlayer)
    {
        teleportingPlayer.position = targetPoint.position;

        Rigidbody2D rb = teleportingPlayer.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        if (cameraFollow != null && targetCameraBounds != null)
        {
            cameraFollow.SetBounds(targetCameraBounds);
            cameraFollow.SnapToTarget();
        }
        else if (cameraFollow != null)
        {
            cameraFollow.SnapToTarget();
        }
    }

    private bool CanTeleport()
    {
        if (targetPoint == null || player == null)
        {
            return false;
        }

        return requiredDoor == null || requiredDoor.open;
    }

    private void SetFadeOverlayActive(bool active, float alpha)
    {
        Color color = fadeOverlay.color;
        color.a = alpha;
        fadeOverlay.color = color;
        fadeOverlay.gameObject.SetActive(active);
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        Color startColor = fadeOverlay.color;
        float startAlpha = startColor.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Color color = fadeOverlay.color;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            fadeOverlay.color = color;

            yield return null;
        }

        Color finalColor = fadeOverlay.color;
        finalColor.a = targetAlpha;
        fadeOverlay.color = finalColor;
    }
}
