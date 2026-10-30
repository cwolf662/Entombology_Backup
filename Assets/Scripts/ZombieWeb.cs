//Very similar to the player web, just configured to only affect the player

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ZombieWeb : MonoBehaviour {
    [Header("Lifetime")]
    [SerializeField] private float fullVisibilityDuration = 2.5f;
    [SerializeField] private float fadeDuration = 2.5f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer webRenderer;

    private readonly HashSet<PlayerMovement> slowedPlayers =
        new HashSet<PlayerMovement>();

    private void Awake() {
        if (webRenderer == null)
            webRenderer = GetComponent<SpriteRenderer>();

        Collider2D webCollider = GetComponent<Collider2D>();
        webCollider.isTrigger = true;
    }

    private void Start() {StartCoroutine(LifetimeRoutine());}

    private void OnTriggerEnter2D(Collider2D other) {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();

        if (player == null || !slowedPlayers.Add(player))
            return;

        player.AddWebSlow();
    }

    private void OnTriggerExit2D(Collider2D other) {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();

        if (player == null || !slowedPlayers.Remove(player))
            return;

        player.RemoveWebSlow();
    }

    private IEnumerator LifetimeRoutine() {
        yield return new WaitForSeconds(fullVisibilityDuration);

        if (webRenderer == null) {
            Destroy(gameObject);
            yield break;
        }

        Color originalColor = webRenderer.color;
        float elapsed = 0f;

        while (elapsed < fadeDuration) {
            elapsed += Time.deltaTime;

            float alpha = Mathf.Lerp(originalColor.a,0f,Mathf.Clamp01(elapsed / fadeDuration));

            Color fadedColor = originalColor;
            fadedColor.a = alpha;
            webRenderer.color = fadedColor;

            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy() {
        foreach (PlayerMovement player in slowedPlayers) {
            if (player != null)
                player.RemoveWebSlow();
        }

        slowedPlayers.Clear();
    }
}