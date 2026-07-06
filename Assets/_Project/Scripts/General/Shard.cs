using UnityEngine;

public class Shard : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDestroyOnPlayer(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDestroyOnPlayer(collision.gameObject);
    }

    private void TryDestroyOnPlayer(GameObject other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}
