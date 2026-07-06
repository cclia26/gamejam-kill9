using UnityEngine;

public class DoorLock : MonoBehaviour
{
    [SerializeField] private PressureButton button1;
    [SerializeField] private PressureButton button2;
    [SerializeField] private Sprite openDoor;
    [SerializeField] private Sprite closedDoor;

    public bool open;

    private SpriteRenderer spriteRenderer;
    private Collider2D doorCollider;
    private bool finalDoorTriggered;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        doorCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (!open && button1 != null && button2 != null && button1.pressed && button2.pressed)
        {
            open = true;
            button1.LockPressed();
            button2.LockPressed();
            if (spriteRenderer != null && openDoor != null)
            {
                spriteRenderer.sprite = openDoor;
            }
        }

        if (open && IsBigLevel2FinalDoor())
        {
            CheckFinalDoorOverlap();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTriggerBigLevel2Win(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryTriggerBigLevel2Win(other);
    }

    private void CheckFinalDoorOverlap()
    {
        if (finalDoorTriggered || doorCollider == null)
        {
            return;
        }

        Bounds b = doorCollider.bounds;
        Collider2D[] hits = Physics2D.OverlapBoxAll(b.center, b.size, transform.eulerAngles.z);
        foreach (Collider2D hit in hits)
        {
            TryTriggerBigLevel2Win(hit);
            if (finalDoorTriggered)
            {
                break;
            }
        }
    }

    private void TryTriggerBigLevel2Win(Collider2D other)
    {
        if (finalDoorTriggered || !open || !IsBigLevel2FinalDoor() || !IsPlayerCollider(other))
        {
            return;
        }

        BigLevel2DialogueController controller = FindObjectOfType<BigLevel2DialogueController>();
        if (controller == null)
        {
            Debug.LogWarning("[DoorLock] door-3 detected player, but BigLevel2DialogueController was not found.", this);
            return;
        }

        finalDoorTriggered = true;
        Debug.Log("[DoorLock] door-3 final win trigger fired.", this);
        controller.OnFinalDoorEntered();
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.CompareTag("Player"))
        {
            return true;
        }

        return other.GetComponentInParent<Controller_Empathy>() != null
            || other.GetComponentInParent<Controller_Home>() != null
            || other.GetComponentInParent<Controller_Will>() != null;
    }

    private bool IsBigLevel2FinalDoor()
    {
        return gameObject.name == "door-3";
    }
}

