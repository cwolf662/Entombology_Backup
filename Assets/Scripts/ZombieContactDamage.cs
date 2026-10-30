using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ZombieContactDamage : MonoBehaviour{
    [Header("Contact Damage")]
    [SerializeField] private float contactDamage = 10f;

    //Damage player on contact
    private void OnCollisionStay2D(Collision2D collision) {
        PlayerHealth playerHealth = collision.collider.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null) {
            playerHealth.TakeDamage(contactDamage);
        }
    }
}