using UnityEngine;
using System.Collections;

public class ZombieHealth : MonoBehaviour {
    [Header("Health")]
    [SerializeField] private float maxHealth = 50f;
    private float currentHealth;

    [Header("Death")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite infectedSplatterSprite;
    [SerializeField] private float deathSpriteZ = 4f;

    [Header("Splatter Fade")]
    [SerializeField] private float fadeDelay = 5f;
    [SerializeField] private float fadeDuration = 5f;

    [Header("Coin Drops")]
    [SerializeField] private int coinsDropped = 1;

    [Header("Damage Flash")]
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.12f;

    [Header("Poison Effect")]
    [SerializeField]
    private Color poisonFlashColor = new Color(0.3f, 0.8f, 0.3f);
    [SerializeField] private float poisonFlashDuration = 0.12f;

    private Color originalSpriteColor = Color.white;
    private Coroutine flashCoroutine;

    public bool isDead;
    private ZombieWaveSpawner waveSpawner;

    private void Awake() {
        if (spriteRenderer != null) {
            originalSpriteColor = spriteRenderer.color;
        }
        currentHealth = maxHealth;

        if (spriteRenderer == null) {
            Transform visuals = transform.Find("Visuals");

            if (visuals != null)
                spriteRenderer = visuals.GetComponent<SpriteRenderer>();
        }
    }

    private static readonly System.Collections.Generic.List<ZombieHealth> activeZombies = 
        new System.Collections.Generic.List<ZombieHealth>();

    private void OnEnable() {
        if (!activeZombies.Contains(this))
            activeZombies.Add(this);
    }

    private void OnDisable() {
        activeZombies.Remove(this);
    }

    //Zombies destroyed after each game restarts
    public static void DestroyAllZombies() {
        for (int i = activeZombies.Count - 1; i >= 0; i--) {
            if (activeZombies[i] == null)
            {
                activeZombies.RemoveAt(i);
                continue;
            }

            Destroy(activeZombies[i].gameObject);
        }

        activeZombies.Clear();
    }
    public void FlashRed() {StartFlash(damageFlashColor, damageFlashDuration);} //Regular dmg

    public void FlashPoisonGreen(){StartFlash(poisonFlashColor, poisonFlashDuration);} //Poison dmg

    private void StartFlash(Color flashColor, float duration) {
        if (isDead || spriteRenderer == null)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(
            FlashRoutine(flashColor, duration));
    }

    private IEnumerator FlashRoutine(Color flashColor, float duration) {
        spriteRenderer.color = flashColor;

        yield return new WaitForSeconds(duration);

        if (!isDead && spriteRenderer != null)
            spriteRenderer.color = originalSpriteColor;

        flashCoroutine = null;
    }

    public void TakeDamage(float damage) {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null) {
            PlayerHealth playerHealth = playerObject.GetComponent<PlayerHealth>();

            if (playerHealth != null && playerHealth.IsDead)
                return;
        }

        if (isDead || damage <= 0f)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0f) {
            Die();
            return;
        }

        FlashRed();
    }

    public void TakePoisonDamage(float damage) {
        if (isDead || damage <= 0f)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0f) {
            Die();
            return;
        }

        FlashPoisonGreen();
    }

    public void SetWaveSpawner(ZombieWaveSpawner spawner) {waveSpawner = spawner;}


    private void Die() {
        isDead = true;

        if (waveSpawner != null)
            waveSpawner.NotifyZombieDied();

        //Stop the zombie's movement and contact damage
        ZombieAI ai = GetComponent<ZombieAI>();
        if (ai != null)
            ai.enabled = false;

        ZombieContactDamage contactDamage = GetComponent<ZombieContactDamage>();
        if (contactDamage != null)
            contactDamage.enabled = false;

        ZombieSpiderAttacks spiderAttacks = GetComponent<ZombieSpiderAttacks>();

        if (spiderAttacks != null) {spiderAttacks.enabled = false;}

        //Stop physics from rotating or moving the dead zombie (they would spin like crazy before)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        //Disable colliders so the splatter no longer interacts
        //with the player or other objects.
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
            col.enabled = false;

        //Replace the living sprite with the infected splatter
        if (spriteRenderer != null && infectedSplatterSprite != null) {
            spriteRenderer.sprite = infectedSplatterSprite;

            Vector3 spritePosition = spriteRenderer.transform.position;
            spritePosition.z = deathSpriteZ;
            spriteRenderer.transform.position = spritePosition;

            StartCoroutine(FadeAndDestroy());
        }

        PlayerCoins wallet = null;
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
            wallet = player.GetComponent<PlayerCoins>();

        if (wallet != null)
            wallet.AddCoins(coinsDropped);
    }

    private IEnumerator FadeAndDestroy() {
        //Keep the splatter fully visible for 5 seconds
        yield return new WaitForSeconds(fadeDelay);

        if (spriteRenderer == null) {
            Destroy(gameObject);
            yield break;
        }

        Color originalColor = spriteRenderer.color;
        float elapsed = 0f;

        while (elapsed < fadeDuration) {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            float alpha = Mathf.Lerp(originalColor.a, 0f, progress);

            Color fadedColor = originalColor;
            fadedColor.a = alpha;
            spriteRenderer.color = fadedColor;

            yield return null;
        }

        Destroy(gameObject);
    }
}