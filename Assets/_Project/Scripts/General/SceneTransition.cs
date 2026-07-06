using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public const string SpawnFacingXKey = "spawnFacingX";
    public const string SpawnFacingYKey = "spawnFacingY";

    [Header("Scene")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private bool requireMouseClick;
    [SerializeField] private float interactionDistance = 3.5f;
    [SerializeField] private Vector2 interactionBoxSize = new Vector2(1.6f, 3.0f);
    [SerializeField] private Vector2 interactionBoxOffset = new Vector2(0f, 0f);
    [SerializeField] private bool setSpawnFacingDirection;
    [SerializeField] private Vector2 spawnFacingDirection = Vector2.down;

    [Header("Highlight")]
    [SerializeField] private bool enableHoverHighlight;
    [SerializeField] private Color highlightColor = new Color(1f, 0.62f, 0.28f, 0.9f);
    [SerializeField] private float highlightPadding = 0f;
    [SerializeField] private float highlightLineWidth = 0.12f;

    private bool playerInside;
    private Collider2D interactionCollider;
    private LineRenderer highlightRenderer;
    private Transform cachedPlayer;

    public void Configure(string sceneName, bool mouseClick, bool hoverHighlight)
    {
        targetSceneName = sceneName;
        requireMouseClick = mouseClick;
        interactionDistance = Mathf.Max(interactionDistance, 3.5f);
        enableHoverHighlight = hoverHighlight;
        RefreshRuntimeReferences();
    }

    private void Awake()
    {
        RefreshRuntimeReferences();
    }

    private void Update()
    {
        bool mouseOverThis = IsMouseOverInteractionArea();

        if (enableHoverHighlight)
        {
            UpdateHighlightBounds();
            SetHighlightVisible(mouseOverThis);
        }

        if (!requireMouseClick)
        {
            return;
        }

        if (mouseOverThis && IsPlayerCloseEnough() && Input.GetMouseButtonDown(0))
        {
            LoadTargetScene();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        cachedPlayer = GetRootTransform(other);
        playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInside = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        cachedPlayer = GetRootTransform(other);
        playerInside = true;

        if (!requireMouseClick && Input.GetButtonDown("Interact"))
        {
            LoadTargetScene();
        }
    }

    private void RefreshRuntimeReferences()
    {
        interactionCollider = GetComponent<BoxCollider2D>();
        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<Collider2D>();
        }

        if (enableHoverHighlight && highlightRenderer == null)
        {
            CreateHighlightRenderer();
        }

        UpdateHighlightBounds();
        SetHighlightVisible(false);
    }

    private bool IsMouseOverInteractionArea()
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            return false;
        }

        Vector3 mouseWorld3D = activeCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mouseWorld = new Vector2(mouseWorld3D.x, mouseWorld3D.y);

        if (interactionCollider != null && interactionCollider.enabled)
        {
            return interactionCollider.OverlapPoint(mouseWorld);
        }

        Vector2 center = (Vector2)transform.position + interactionBoxOffset;
        Rect interactionRect = new Rect(
            center.x - interactionBoxSize.x * 0.5f,
            center.y - interactionBoxSize.y * 0.5f,
            interactionBoxSize.x,
            interactionBoxSize.y
        );

        return interactionRect.Contains(mouseWorld);
    }

    private bool IsPlayerCloseEnough()
    {
        if (playerInside)
        {
            return true;
        }

        Transform player = ResolvePlayerTransform();
        if (player == null)
        {
            return false;
        }

        return Vector2.Distance(player.position, transform.position) <= interactionDistance;
    }

    private Transform ResolvePlayerTransform()
    {
        if (cachedPlayer != null)
        {
            return cachedPlayer;
        }

        GameObject namedPlayer = GameObject.Find("Player");
        if (namedPlayer != null)
        {
            cachedPlayer = namedPlayer.transform;
            return cachedPlayer;
        }

        Controller_Home homePlayer = FindObjectOfType<Controller_Home>();
        if (homePlayer != null)
        {
            cachedPlayer = homePlayer.transform;
            return cachedPlayer;
        }

        Controller_Empathy empathyPlayer = FindObjectOfType<Controller_Empathy>();
        if (empathyPlayer != null)
        {
            cachedPlayer = empathyPlayer.transform;
            return cachedPlayer;
        }

        Controller_Will willPlayer = FindObjectOfType<Controller_Will>();
        if (willPlayer != null)
        {
            cachedPlayer = willPlayer.transform;
            return cachedPlayer;
        }

        return null;
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            return true;
        }

        Transform root = GetRootTransform(other);

        return root.name == "Player"
            || root.GetComponent<Controller_Home>() != null
            || root.GetComponent<Controller_Empathy>() != null
            || root.GetComponent<Controller_Will>() != null;
    }

    private Transform GetRootTransform(Collider2D other)
    {
        return other.attachedRigidbody != null
            ? other.attachedRigidbody.transform
            : other.transform.root;
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            return;
        }

        ScenePayload payload = CreatePayload();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScene(targetSceneName, payload);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }

        Debug.Log("transition to " + targetSceneName);
    }

    private ScenePayload CreatePayload()
    {
        ScenePayload payload = GameManager.Instance != null
            ? GameManager.Instance.PendingPayload
            : null;

        if (!setSpawnFacingDirection)
        {
            return payload;
        }

        if (payload == null)
        {
            payload = new ScenePayload();
        }

        Vector2 facingDirection = spawnFacingDirection.sqrMagnitude > 0f
            ? spawnFacingDirection.normalized
            : Vector2.down;

        payload.SetExtra(SpawnFacingXKey, facingDirection.x);
        payload.SetExtra(SpawnFacingYKey, facingDirection.y);

        return payload;
    }

    private void CreateHighlightRenderer()
    {
        GameObject highlightObject = new GameObject("HoverHighlight");
        highlightObject.transform.SetParent(transform, false);
        highlightObject.transform.localPosition = Vector3.zero;
        highlightObject.transform.localRotation = Quaternion.identity;
        highlightObject.transform.localScale = Vector3.one;

        highlightRenderer = highlightObject.AddComponent<LineRenderer>();
        highlightRenderer.useWorldSpace = true;
        highlightRenderer.loop = true;
        highlightRenderer.positionCount = 4;
        highlightRenderer.startWidth = highlightLineWidth;
        highlightRenderer.endWidth = highlightLineWidth;
        highlightRenderer.startColor = highlightColor;
        highlightRenderer.endColor = highlightColor;
        highlightRenderer.material = new Material(Shader.Find("Sprites/Default"));

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            highlightRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            highlightRenderer.sortingOrder = spriteRenderer.sortingOrder + 10;
        }
    }

    private void UpdateHighlightBounds()
    {
        if (highlightRenderer == null)
        {
            return;
        }

        Bounds bounds;
        if (interactionCollider != null && interactionCollider.enabled)
        {
            bounds = interactionCollider.bounds;
        }
        else
        {
            Vector2 center = (Vector2)transform.position + interactionBoxOffset;
            bounds = new Bounds(center, interactionBoxSize);
        }

        float minX = bounds.min.x - highlightPadding;
        float maxX = bounds.max.x + highlightPadding;
        float minY = bounds.min.y - highlightPadding;
        float maxY = bounds.max.y + highlightPadding;
        float z = transform.position.z;

        highlightRenderer.startWidth = highlightLineWidth;
        highlightRenderer.endWidth = highlightLineWidth;
        highlightRenderer.SetPosition(0, new Vector3(minX, minY, z));
        highlightRenderer.SetPosition(1, new Vector3(minX, maxY, z));
        highlightRenderer.SetPosition(2, new Vector3(maxX, maxY, z));
        highlightRenderer.SetPosition(3, new Vector3(maxX, minY, z));
    }

    private void SetHighlightVisible(bool visible)
    {
        if (highlightRenderer != null)
        {
            highlightRenderer.enabled = visible;
        }
    }
}