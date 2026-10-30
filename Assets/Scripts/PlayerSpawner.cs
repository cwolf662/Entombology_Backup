using UnityEngine;

public class PlayerSpawner : MonoBehaviour {
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("UI")]
    [SerializeField] private PlayerUIReferences playerUI;

    [Header("Camera")]
    [SerializeField] private CameraFollow cameraFollow;

    private GameObject currentPlayer;

    //Sets up the player each round, prefabs were unable to have multiple gameobjects assigned to their fields
    public void SpawnPlayer(GameObject prefabToSpawn){
        if (prefabToSpawn == null || playerSpawnPoint == null ||playerUI == null){
            Debug.LogError("PlayerSpawner: Assign the player prefab, spawn point, and UI.");
            return;
        }

        if (currentPlayer != null)
            Destroy(currentPlayer);

        currentPlayer = Instantiate(
            prefabToSpawn,
            playerSpawnPoint.position,
            playerSpawnPoint.rotation);

        currentPlayer.tag = "Player";

        PlayerMovement movement =
            currentPlayer.GetComponent<PlayerMovement>();

        if (movement != null){
            movement.ConfigureStaminaUI(
                playerUI.staminaSlider,
                playerUI.staminaFillImage);

            if (playerUI.staminaBar != null)
                playerUI.staminaBar.SetActive(movement.canSprint);
        }
        else if (playerUI.staminaBar != null){
            playerUI.staminaBar.SetActive(false);
        }

        PlayerHealth health = currentPlayer.GetComponent<PlayerHealth>();

        if (health != null) {
            health.ConfigureHealthUI(
                playerUI.healthSlider,
                playerUI.healthFillImage);
        }

        PlayerCoins coins = currentPlayer.GetComponent<PlayerCoins>();

        if (coins != null)
            coins.ConfigureCoinsUI(playerUI.coinsText);

        AntBiteAttack antAttack = currentPlayer.GetComponent<AntBiteAttack>();

        BeeAttack beeAttack = currentPlayer.GetComponent<BeeAttack>();

        SpiderAttack spiderAttack = currentPlayer.GetComponent<SpiderAttack>();

        bool hasSpecialAttack = beeAttack != null || spiderAttack != null;

        //Configures bars depending on the bug
        if (antAttack != null) {
            antAttack.ConfigureCooldownUI(
                playerUI.biteCooldownSlider);
        }

        if (beeAttack != null) {
            beeAttack.ConfigureCooldownUI(
                playerUI.biteCooldownBar,
                playerUI.biteCooldownSlider,
                playerUI.specialCooldownBar,
                playerUI.specialCooldownSlider,
                playerUI.specialSymbol);
        }

        if (spiderAttack != null) {
            spiderAttack.ConfigureCooldownUI(
                playerUI.biteCooldownBar,
                playerUI.biteCooldownSlider,
                playerUI.specialCooldownBar,
                playerUI.specialCooldownSlider,
                playerUI.specialSymbol);
        }

        //Restore gameplay UI after coming from the menu
        SetGameplayUIVisible(true);

        if (playerUI.staminaBar != null) {
            playerUI.staminaBar.SetActive(movement != null && movement.canSprint);
        }

        if (playerUI.specialCooldownBar != null)
            playerUI.specialCooldownBar.SetActive(hasSpecialAttack);

        if (playerUI.specialSymbol != null) {
            playerUI.specialSymbol.gameObject.SetActive(hasSpecialAttack);
        }

        //Follow the newly spawned bug
        if (cameraFollow == null && Camera.main != null) {
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        if (cameraFollow != null)
            cameraFollow.SetTarget(currentPlayer.transform);

        //Ensure any existing zombies target the newly spawned player
        ZombieAI.UpdateAllPlayerTargets(currentPlayer.transform);
    }

    public void SetGameplayUIVisible(bool visible) {
        if (playerUI == null)
            return;

        if (playerUI.healthSlider != null)
            playerUI.healthSlider.gameObject.SetActive(visible);

        if (playerUI.biteCooldownBar != null)
            playerUI.biteCooldownBar.SetActive(visible);

        if (playerUI.zombiesAliveText != null)
            playerUI.zombiesAliveText.gameObject.SetActive(visible);

        if (playerUI.staminaBar != null) {
            PlayerMovement movement = currentPlayer != null
                ? currentPlayer.GetComponent<PlayerMovement>()
                : null;

            playerUI.staminaBar.SetActive(visible && movement != null && movement.canSprint);
        }

        bool hasSpecial = false;

        if (currentPlayer != null) {
            hasSpecial = currentPlayer.GetComponent<BeeAttack>() != null || currentPlayer.GetComponent<SpiderAttack>() != null;
        }

        if (playerUI.specialCooldownBar != null)
            playerUI.specialCooldownBar.SetActive(visible && hasSpecial);

        if (playerUI.specialSymbol != null)
            playerUI.specialSymbol.gameObject.SetActive(visible && hasSpecial);

        //Intentionally do not hide playerUI.coinsText, we want the player to see it during the menu
    }

    public GameObject GetCurrentPlayer() {return currentPlayer;}
}