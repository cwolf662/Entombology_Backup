using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour {
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6f;

    [Header("Stamina")]
    [SerializeField] public bool canSprint = true;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 25f;
    [SerializeField] private float staminaRegenRate = 20f;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Image staminaFillImage;
    [SerializeField] private Color normalStaminaColor = new Color(0.2f, 0.3f, 0.8f);
    [SerializeField] private Color recoveryStaminaColor = Color.red;

    [Header("Aiming")]
    [SerializeField] private Transform visuals;
    [SerializeField] private float spriteAngleOffset = -90f;

    [Header("Walking Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite step1Sprite;
    [SerializeField] private Sprite step2Sprite;
    [SerializeField] private float animationFrameTime = 0.15f;

    [Header("Slow Effects")]
    [SerializeField, Range(0f, 1f)]
    private float webSlowMultiplier = 0.5f;

    private int activeWebSlowCount = 0;

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private Camera mainCamera;

    private float currentStamina;
    private bool isSprinting;
    private bool sprintLocked;
    private float animationTimer;
    private bool showStep2;

    public void ConfigureStaminaUI(Slider slider, Image fillImage) {
        staminaSlider = slider;
        staminaFillImage = fillImage;
    }

    private void Awake(){
        if (staminaSlider != null) {
            staminaSlider.gameObject.SetActive(canSprint);
        }
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        currentStamina = maxStamina;
    }

    private void Update(){
        ReadMovementInput();
        UpdateSprint();
        UpdateStaminaBar();
        UpdateStaminaColor();
        AimAtMouse();
        UpdateWalkingAnimation();
    }

    private void FixedUpdate(){ //Using fixed update for more consistent movement
        float currentSpeed = isSprinting
            ? sprintSpeed
            : moveSpeed;

        if (activeWebSlowCount > 0){
            currentSpeed *= webSlowMultiplier;
        }

        rb.MovePosition(
            rb.position +
            movementInput * currentSpeed * Time.fixedDeltaTime
        );
    }

    //Gets player inputs, normalizing movement to avoid the ol' hypotenuse movement issue
    private void ReadMovementInput(){
        movementInput = Vector2.zero;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            movementInput.y += 1f;

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            movementInput.y -= 1f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            movementInput.x += 1f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            movementInput.x -= 1f;

        movementInput = movementInput.normalized;
    }

    //Keeps stamina input and values updated
    private void UpdateSprint(){
        if (canSprint == false)
            return;

        isSprinting = false;

        if (Keyboard.current == null)
            return;

        bool shiftHeld =
            Keyboard.current.leftShiftKey.isPressed ||
            Keyboard.current.rightShiftKey.isPressed;

        bool moving = movementInput.sqrMagnitude > 0.01f;

        //Unlock sprinting after stamina recovers to 50%
        if (sprintLocked && currentStamina >= maxStamina * 0.5f) {sprintLocked = false;}

        //Sprint only if it is not locked, shift is held,
        //the player is moving, and stamina is available
        if (!sprintLocked && shiftHeld && moving && currentStamina > 0f) {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0f, currentStamina);

            isSprinting = currentStamina > 0f;

            //Lock sprinting when stamina is depleted
            if (currentStamina <= 0f) {
                sprintLocked = true;
                isSprinting = false;
            }
        }
        else {
            //Stamina regenerates while not sprinting
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(maxStamina, currentStamina);
        }
    }

    private void UpdateStaminaColor() {
        if (staminaFillImage == null)
            return;

        staminaFillImage.color = sprintLocked
            ? recoveryStaminaColor
            : normalStaminaColor;
    }

    private void UpdateStaminaBar() {
        if (staminaSlider == null)
            return;

        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = currentStamina;
    }

    //Aims player visuals (and the biteRange child) at the mouse
    //I didn't include the entire player insect, because I used capsule hitboxes, and I figure that'd
    //cause a weird pushing if you are near a wall and rotate.
    private void AimAtMouse(){
        if (Mouse.current == null || mainCamera == null || visuals == null)
            return;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        Vector3 screenPosition = new Vector3( mouseScreenPosition.x, mouseScreenPosition.y, -mainCamera.transform.position.z);

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(screenPosition);

        Vector2 direction = mouseWorldPosition - visuals.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        visuals.rotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);
    }

    //Alternates between step1 and step2 animation sprites, because no way in hell I'd have time or skill to make proper 2d animations
    //I am getting a lot better at 3D animations, though, so we'll see about the 3D project
    private void UpdateWalkingAnimation(){
        if (spriteRenderer == null)
            return;

        if (movementInput.sqrMagnitude < 0.01f) {
            animationTimer = 0f;
            showStep2 = false;
            spriteRenderer.sprite = idleSprite;
            return;
        }

        float currentFrameTime = isSprinting
            ? animationFrameTime * 0.65f
            : animationFrameTime;

        animationTimer += Time.deltaTime;

        if (animationTimer >= currentFrameTime) {
            animationTimer = 0f;
            showStep2 = !showStep2;
        }

        spriteRenderer.sprite = showStep2 ? step2Sprite : step1Sprite;
    }

    public void AddWebSlow() {
        activeWebSlowCount++;
    }

    public void RemoveWebSlow() {
        activeWebSlowCount =
            Mathf.Max(0, activeWebSlowCount - 1);
    }
}