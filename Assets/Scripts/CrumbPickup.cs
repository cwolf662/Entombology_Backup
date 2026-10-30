using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CrumbPickup : MonoBehaviour {
    [SerializeField] private float healingAmount = 25f;

    //Sets up crumb once created
    private void Awake() {
        Collider2D pickupCollider = GetComponent<Collider2D>();
        pickupCollider.isTrigger = true;
    }

    //When collided with, give player health
    private void OnTriggerEnter2D(Collider2D other){
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (playerHealth.IsDead)
            return;

        playerHealth.Heal(healingAmount);

        Destroy(gameObject);
    }
}
