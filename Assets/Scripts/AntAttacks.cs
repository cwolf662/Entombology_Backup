using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AntBiteAttack : MonoBehaviour
{
    [Header("Bite Settings")]
    [SerializeField] private float biteDamage = 10f;
    [SerializeField] private float biteRadius = 0.8f;
    [SerializeField] private Transform bitePoint;
    [SerializeField] private LayerMask zombieLayers;
    [SerializeField] private float biteCooldown = 0.25f;

    [Header("Range Indicator")]
    [SerializeField] private GameObject biteRangeVisual;
    [SerializeField] private float indicatorDuration = 0.12f;

    [Header("Cooldown UI")]
    [SerializeField] private Slider cooldownSlider;

    private PlayerSoundEffects soundEffects;
    private float cooldownRemaining;
    private float indicatorTimer;
    public void ConfigureCooldownUI(Slider slider) {cooldownSlider = slider;}
    
    //From my understanding, Awake() is for internal startup,
    //whereas Start() is for interacting/getting data from outside the gameObject
    private void Awake() {soundEffects = GetComponent<PlayerSoundEffects>();} //References player sound component
    
    //Set up bite visual and bite cooldown bar
    private void Start() {
        if (biteRangeVisual != null)
            biteRangeVisual.SetActive(false);

        if (cooldownSlider != null)
        {
            cooldownSlider.minValue = 0f;
            cooldownSlider.maxValue = 1f;
            cooldownSlider.value = 1f;
        }
    }

    
    private void Update() {
        //Update the bite cooldown
        if (cooldownRemaining > 0f) {
            cooldownRemaining -= Time.deltaTime;
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining);
        }

        //The bar refills as the bite becomes available
        if (cooldownSlider != null) {
            float readyAmount = biteCooldown > 0f
                ? 1f - (cooldownRemaining / biteCooldown)
                : 1f;
            cooldownSlider.value = readyAmount;
        }

        //Hide the range indicator after blink
        if (biteRangeVisual != null && biteRangeVisual.activeSelf){
            indicatorTimer -= Time.deltaTime;

            if (indicatorTimer <= 0f)
                biteRangeVisual.SetActive(false);
        }

        //Only allow a bite when the cooldown is finished
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && cooldownRemaining <= 0f) 
            {Bite();}
    }

    private void Bite()
    {
        if (bitePoint == null) {
            Debug.LogWarning("AntBiteAttack: Bite Point is not assigned.");
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        //Ensures player is alive before biting.
        if (playerObject != null) {
            PlayerHealth playerHealth = playerObject.GetComponent<PlayerHealth>();

            if (playerHealth != null && playerHealth.IsDead)
                return;
        }

        if (soundEffects != null)
            soundEffects.PlayBite();

        //Start cooldown
        cooldownRemaining = biteCooldown;

        ShowRangeIndicator();

        //All zombies overlapping the circle will get bit
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            bitePoint.position,
            biteRadius,
            zombieLayers
        );
        foreach (Collider2D hit in hits){
            ZombieHealth zombie = hit.GetComponentInParent<ZombieHealth>();

            if (zombie != null)
                zombie.TakeDamage(biteDamage);
        }
    }

    //Bite range indicator
    private void ShowRangeIndicator()
    {
        if (biteRangeVisual == null)
            return;

        biteRangeVisual.SetActive(true);
        indicatorTimer = indicatorDuration;
    }

    private void OnDrawGizmosSelected(){
        if (bitePoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(bitePoint.position, biteRadius);//Helps visualize during development
    }
}