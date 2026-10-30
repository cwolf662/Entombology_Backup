using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderWeb : MonoBehaviour{
    [Header("Lifetime")]
    [SerializeField] private float fullVisibilityDuration = 5f;
    [SerializeField] private float fadeDuration = 5f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer webRenderer;

    private readonly HashSet<ZombieAI> slowedZombies = new HashSet<ZombieAI>();

    private void Awake(){
        if (webRenderer == null)
            webRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start() { StartCoroutine(LifetimeRoutine());}

    //Slows zombies that touch web 
    private void OnTriggerEnter2D(Collider2D other) {
        ZombieAI zombie = other.GetComponentInParent<ZombieAI>();

        if (zombie == null || !slowedZombies.Add(zombie))
            return;

        zombie.AddWebSlow();
    }

    //Speeds zombies back up after they escape the web
    private void OnTriggerExit2D(Collider2D other) {
        ZombieAI zombie = other.GetComponentInParent<ZombieAI>();

        if (zombie == null || !slowedZombies.Remove(zombie))
            return;

        zombie.RemoveWebSlow();
    }

    //Web stays, then fades away, before being destroyed
    private IEnumerator LifetimeRoutine(){
        yield return new WaitForSeconds(fullVisibilityDuration);

        if (webRenderer == null) {
            Destroy(gameObject);
            yield break;
        }

        Color startColor = webRenderer.color;
        float elapsed = 0f;

        while (elapsed < fadeDuration) {
            elapsed += Time.deltaTime;

            float alpha = Mathf.Lerp(
                startColor.a, 0f,
                elapsed / fadeDuration);

            Color color = startColor;
            color.a = alpha;
            webRenderer.color = color;

            yield return null;
        }

        Destroy(gameObject);
    }

    //Treats zombies in the web when it got destroyed as freed zombies
    private void OnDestroy(){
        foreach (ZombieAI zombie in slowedZombies) {
            if (zombie != null)
                zombie.RemoveWebSlow();
        }

        slowedZombies.Clear();
    }
}
