using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour {
    [Header("Health")]
    [SerializeField] public float maxHealth = 100f;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFillImage;

    [Header("Damage and Invincibility")]
    [SerializeField] private float invincibilityDuration = 1.5f;
    [SerializeField] private float blinkInterval = 0.1f;
    [SerializeField] private float blinkAlpha = 0.25f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private Sprite deathSplatterSprite;
    [SerializeField] private float deathSpriteZ = 4f;

    private PlayerSoundEffects soundEffects;
    private float currentHealth;
    private float invincibilityTimer;
    private float blinkTimer;
    private bool isBlinkVisible = true;
    private bool isDead;

    public bool IsDead => isDead; //Pointer to isDead
    public void ConfigureHealthUI(Slider slider,Image fillImage) {
        healthSlider = slider;
        healthFillImage = fillImage;
        UpdateHealthBar(); //Updating upon starting to refresh health after a player has died and respawned
    }

    private void Awake() {
        currentHealth = maxHealth;
        soundEffects = GetComponent<PlayerSoundEffects>();
        UpdateHealthBar(); //Updating upon starting to refresh health after a player has died and respawned
    }

    //Checks for how long to blink for, and if the player is invincible still
    private void Update() {
        if (invincibilityTimer <= 0f)
            return;

        invincibilityTimer -= Time.deltaTime;
        blinkTimer -= Time.deltaTime;

        if (blinkTimer <= 0f){
            blinkTimer = blinkInterval;
            isBlinkVisible = !isBlinkVisible;
            UpdateSpriteAlpha();
        }

        if (invincibilityTimer <= 0f){
            invincibilityTimer = 0f;
            isBlinkVisible = true;
            UpdateSpriteAlpha();
        }
    }

    //Used for the bee sting health subtraction
    public bool TryPayHealthCost(float amount){
        if (isDead || amount <= 0f)
            return false;

        // Do not allow the health cost to kill the player.
        if (currentHealth <= amount)
            return false;

        currentHealth -= amount;
        UpdateHealthBar();

        return true;
    }

    public void TakeDamage(float damage){
        if (isDead || invincibilityTimer > 0f || damage <= 0f)
            return;

        if (soundEffects != null)
            soundEffects.PlayZombieHit();

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        invincibilityTimer = invincibilityDuration;
        blinkTimer = blinkInterval;
        isBlinkVisible = false;

        UpdateHealthBar();
        UpdateSpriteAlpha();

        if (currentHealth <= 0f){
            Die();
        }
    }

    //Lets crumbs heal player if they are still alive
    public void Heal(float amount){
        if (isDead || amount <= 0f)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        UpdateHealthBar();
    }


    private void UpdateHealthBar(){
        float healthPercent = maxHealth > 0f
            ? currentHealth / maxHealth
            : 0f;

        if (healthSlider != null){
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (healthFillImage != null){
            Color lowHealthColor = Color.red;
            Color fullHealthColor = Color.green;

            healthFillImage.color = Color.Lerp( //Transition color based on health/max health ratio
                lowHealthColor,
                fullHealthColor,
                healthPercent
            );
        }
    }

    private void UpdateSpriteAlpha(){
        if (playerSpriteRenderer == null)
            return;

        Color color = playerSpriteRenderer.color;
        color.a = isBlinkVisible ? 1f : blinkAlpha;
        playerSpriteRenderer.color = color;
    }

    private void Die(){
        if (isDead)
            return;

        isDead = true;

        if (soundEffects != null)
            soundEffects.PlayDeath();

        //Stop player movement
        PlayerMovement movement = GetComponent<PlayerMovement>();

        if (movement != null)
            movement.enabled = false;

        //Stop physics movement and prevent further collisions
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null){
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        foreach (Collider2D col in colliders)
            col.enabled = false;

        //Replace the character sprite with the death splatter
        if (playerSpriteRenderer != null) {
            playerSpriteRenderer.color = Color.white;

            if (deathSplatterSprite != null)
                playerSpriteRenderer.sprite = deathSplatterSprite;

            Vector3 spritePosition = playerSpriteRenderer.transform.position;
            spritePosition.z = deathSpriteZ;
            playerSpriteRenderer.transform.position = spritePosition;
        }

        BugSelectionMenu menu = FindAnyObjectByType<BugSelectionMenu>();

        if (menu != null)
            menu.OnPlayerDied();
    }
}