//See AntAttack.cs for comments detailing some of the shared code all attack scripts have.
//Special abilities functions will still have their own comments here, though

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeeAttack : MonoBehaviour
{
    [Header("Bite")]
    [SerializeField] private float biteDamage = 10f;
    [SerializeField] private float biteRadius = 0.8f;
    [SerializeField] private Transform bitePoint;
    [SerializeField] private GameObject biteRangeVisual;
    [SerializeField] private float biteCooldown = 0.25f;

    [Header("Sting")]
    [SerializeField] private float stingDamageMultiplier = 10f;
    [SerializeField, Range(0f, 1f)]
    private float stingHealthCostPercent = 0.10f;
    [SerializeField] private float stingRadius = 0.8f;
    [SerializeField] private Transform stingPoint;
    [SerializeField] private GameObject stingRangeVisual;
    [SerializeField] private float stingCooldown = 3f;

    [Header("Shared Settings")]
    [SerializeField] private LayerMask zombieLayers;
    [SerializeField] private float indicatorDuration = 0.12f;

    [Header("Bite Cooldown UI")]
    [SerializeField] private GameObject biteCooldownBar;
    [SerializeField] private Slider biteCooldownSlider;

    [Header("Special Cooldown UI")]
    [SerializeField] private GameObject specialCooldownBar;
    [SerializeField] private Slider specialCooldownSlider;
    [SerializeField] private Image specialSymbol;
    [SerializeField] private Sprite specialSymbolSprite;

    private PlayerHealth playerHealth;
    private PlayerSoundEffects soundEffects;
    private float biteCooldownRemaining;
    private float stingCooldownRemaining;
    private float biteIndicatorTimer;
    private float stingIndicatorTimer;

    public void ConfigureCooldownUI(
    GameObject biteBar,
    Slider biteSlider,
    GameObject specialBar,
    Slider specialSlider,
    Image symbol)
    {
        biteCooldownBar = biteBar;
        biteCooldownSlider = biteSlider;

        specialCooldownBar = specialBar;
        specialCooldownSlider = specialSlider;
        specialSymbol = symbol;
    }

    private void Awake() {
        playerHealth = GetComponent<PlayerHealth>();
        soundEffects = GetComponent<PlayerSoundEffects>();
    }
    private void Start() {
        if (biteCooldownBar != null)
            biteCooldownBar.SetActive(true);

        if (biteCooldownSlider != null) {
            biteCooldownSlider.minValue = 0f;
            biteCooldownSlider.maxValue = 1f;
            biteCooldownSlider.value = 1f;
        }

        if (biteRangeVisual != null)
            biteRangeVisual.SetActive(false);

        if (stingRangeVisual != null)
            stingRangeVisual.SetActive(false);

        if (specialCooldownBar != null)
            specialCooldownBar.SetActive(true);

        if (specialCooldownSlider != null) {
            specialCooldownSlider.minValue = 0f;
            specialCooldownSlider.maxValue = 1f;
            specialCooldownSlider.value = 1f;
        }

        if (specialSymbol != null && specialSymbolSprite != null) {
            specialSymbol.sprite = specialSymbolSprite;
        }
    }

    
    private void Update()
    {
        biteCooldownRemaining =
            Mathf.Max(0f, biteCooldownRemaining - Time.deltaTime);

        stingCooldownRemaining =
            Mathf.Max(0f, stingCooldownRemaining - Time.deltaTime);

        UpdateIndicator(
            biteRangeVisual, ref biteIndicatorTimer);

        UpdateIndicator(
            stingRangeVisual, ref stingIndicatorTimer);

        if (biteCooldownSlider != null) {
            float biteReady = biteCooldown <= 0f
                ? 1f
                : 1f - biteCooldownRemaining / biteCooldown;

            biteCooldownSlider.value = biteReady;
        }

        if (specialCooldownSlider != null) {
            float ready = stingCooldown <= 0f
                ? 1f
                : 1f - stingCooldownRemaining / stingCooldown;

            specialCooldownSlider.value = ready;
        }

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame && biteCooldownRemaining <= 0f) 
        {Bite();}

        if (Mouse.current.rightButton.wasPressedThisFrame && stingCooldownRemaining <= 0f)
        {Sting();}
    }

    private void Bite()
    {
        if (bitePoint == null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            PlayerHealth playerHealth =
                playerObject.GetComponent<PlayerHealth>();

            if (playerHealth != null && playerHealth.IsDead)
                return;
        }

        if (soundEffects != null)
            soundEffects.PlayBite();

        biteCooldownRemaining = biteCooldown;
        ShowIndicator(biteRangeVisual, ref biteIndicatorTimer);

        DamageZombies(bitePoint.position, biteRadius, biteDamage);
    }

    //Special ability is to sting (behind the player bee), at the cost of some health
    private void Sting()
    {
        if (stingPoint == null || playerHealth == null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        //Ensures player is alive before doing anything
        if (playerObject != null) {
            PlayerHealth playerHealth =
                playerObject.GetComponent<PlayerHealth>();

            if (playerHealth != null && playerHealth.IsDead)
                return;
        }

        float healthCost = playerHealth.maxHealth * stingHealthCostPercent;

        //See if the player can afford a sting
        if (!playerHealth.TryPayHealthCost(healthCost))
            return;

        if (soundEffects != null)
            soundEffects.PlaySpecialAttack();

        stingCooldownRemaining = stingCooldown;
        ShowIndicator(stingRangeVisual, ref stingIndicatorTimer);

        DamageZombies(
            stingPoint.position,
            stingRadius,
            biteDamage * stingDamageMultiplier
        );
    }

    private void DamageZombies( Vector2 center, float radius, float damage) {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            center, radius, zombieLayers);

        foreach (Collider2D hit in hits) {
            ZombieHealth zombie =
                hit.GetComponentInParent<ZombieHealth>();

            if (zombie != null)
                zombie.TakeDamage(damage);
        }
    }

    private void ShowIndicator( GameObject indicator, ref float timer) {
        if (indicator == null)
            return;

        indicator.SetActive(true);
        timer = indicatorDuration;
    }

    private void UpdateIndicator(GameObject indicator, ref float timer) {
        if (indicator == null || !indicator.activeSelf)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
            indicator.SetActive(false);
    }
}