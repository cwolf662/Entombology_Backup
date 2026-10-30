//The zombie spider uses this instead of the regular zombie ContactDamage since it has the zombie web ability

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ZombieSpiderAttacks : MonoBehaviour
{
    [Header("Contact Damage")]
    [SerializeField] private float contactDamage = 10f;

    [Header("Web Attack")]
    [SerializeField] private GameObject webPrefab;
    [SerializeField] private float webPlacementInterval = 10f;

    private void Start() {StartCoroutine(PlaceWebsRoutine());}

    private void OnCollisionStay2D(Collision2D collision) {
        PlayerHealth playerHealth = collision.collider.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null) {
            playerHealth.TakeDamage(contactDamage);
        }
    }

    private IEnumerator PlaceWebsRoutine() {
        while (true) {
            yield return new WaitForSeconds(webPlacementInterval);

            ZombieHealth health = GetComponent<ZombieHealth>();

            if (health == null || health.isDead)
                yield break;

            if (webPrefab == null) {
                Debug.LogWarning("ZombieSpiderAttacks: No web prefab assigned.", this);
                continue;
            }

            Instantiate(
                webPrefab,
                transform.position,
                Quaternion.identity);
        }
    }
}
