//See AntAttack.cs for comments detailing some of the shared code all attack scripts have.
//Special abilities functions will still have their own comments here, though

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpiderAttack : MonoBehaviour
{
    [Header("Bite")]
    [SerializeField] private float biteDamage = 10f;
    [SerializeField] private float biteRadius = 0.8f;
    [SerializeField] private Transform bitePoint;
    [SerializeField] private GameObject biteRangeVisual;
    [SerializeField] private float biteCooldown = 0.25f;

    [Header("Poison")]
    [SerializeField] private float poisonDamage = 10f;
    [SerializeField] private float poisonTickInterval = 0.5f;
    [SerializeField] private int poisonTickCount = 5;

    [Header("Web Special")]
    [SerializeField] private GameObject webPrefab;
    [SerializeField] private float webCooldown = 5f;

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

    private PlayerSoundEffects soundEffects;
    private float biteCooldownRemaining;
    private float webCooldownRemaining;
    private float biteIndicatorTimer;

    private void Awake() { soundEffects = GetComponent<PlayerSoundEffects>();}

    private void Start() {
        if (biteRangeVisual != null)
            biteRangeVisual.SetActive(false);

        if (biteCooldownSlider != null){
            biteCooldownSlider.minValue = 0f;
            biteCooldownSlider.maxValue = 1f;
            biteCooldownSlider.value = 1f;
        }

        if (specialCooldownSlider != null){
            specialCooldownSlider.minValue = 0f;
            specialCooldownSlider.maxValue = 1f;
            specialCooldownSlider.value = 1f;
        }

        if (specialSymbol != null && specialSymbolSprite != null)
            specialSymbol.sprite = specialSymbolSprite;
    }

    
    private void Update()
    {
        biteCooldownRemaining =
            Mathf.Max(0f, biteCooldownRemaining - Time.deltaTime);

        webCooldownRemaining =
            Mathf.Max(0f, webCooldownRemaining - Time.deltaTime);

        if (biteIndicatorTimer > 0f){
            biteIndicatorTimer -= Time.deltaTime;

            if (biteIndicatorTimer <= 0f &&
                biteRangeVisual != null)
            {
                biteRangeVisual.SetActive(false);
            }
        }

        if (biteCooldownSlider != null){
            biteCooldownSlider.value = biteCooldown <= 0f
                ? 1f
                : 1f - biteCooldownRemaining / biteCooldown;
        }

        if (specialCooldownSlider != null){
            specialCooldownSlider.value = webCooldown <= 0f
                ? 1f
                : 1f - webCooldownRemaining / webCooldown;
        }

        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            biteCooldownRemaining <= 0f){
            Bite();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame &&webCooldownRemaining <= 0f)
        {PlaceWeb();}
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

        if (biteRangeVisual != null)
        {
            biteRangeVisual.SetActive(true);
            biteIndicatorTimer = indicatorDuration;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            bitePoint.position,
            biteRadius,
            zombieLayers);

        foreach (Collider2D hit in hits)
        {
            ZombieHealth zombie =
                hit.GetComponentInParent<ZombieHealth>();

            if (zombie == null)
                continue;

            zombie.TakeDamage(biteDamage);

            if (!zombie.isDead)
                StartCoroutine(ApplyPoison(zombie));
        }
    }

    //Applies poison over time to the zombie after being bit
    private IEnumerator ApplyPoison(ZombieHealth zombie){
        for (int i = 0; i < poisonTickCount; i++) {
            yield return new WaitForSeconds(poisonTickInterval);

            if (zombie == null || zombie.isDead)
                yield break;

            zombie.TakePoisonDamage(poisonDamage);
        }
    }

    //Places web that slows enemies. Details on web functionality is in PlayerWebScript
    private void PlaceWeb(){
        if (webPrefab == null) {
            Debug.LogWarning( "SpiderAttack: No web prefab assigned.");
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        //Ensures player is alive before doing anything
        if (playerObject != null) {
            PlayerHealth playerHealth =
                playerObject.GetComponent<PlayerHealth>();

            if (playerHealth != null && playerHealth.IsDead)
                return;
        }

        Instantiate(
            webPrefab,
            transform.position,
            Quaternion.identity);

        if (soundEffects != null)
            soundEffects.PlaySpecialAttack();

        webCooldownRemaining = webCooldown;
    }

    public void ConfigureCooldownUI(GameObject biteBar, Slider biteSlider, GameObject specialBar, Slider specialSlider, Image symbol) {
        biteCooldownBar = biteBar;
        biteCooldownSlider = biteSlider;
        specialCooldownBar = specialBar;
        specialCooldownSlider = specialSlider;
        specialSymbol = symbol;
    }

    private void OnDrawGizmosSelected() {
        if (bitePoint == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(bitePoint.position, biteRadius);
    }
}
