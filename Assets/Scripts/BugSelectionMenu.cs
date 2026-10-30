using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BugSelectionMenu : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TMP_Text coinText;

    [Header("Player and Waves")]
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private ZombieWaveSpawner waveSpawner;

    [Header("Bug Prefabs")]
    [SerializeField] private GameObject antPrefab;
    [SerializeField] private GameObject beePrefab;
    [SerializeField] private GameObject widowPrefab;

    [Header("Bug Buttons")]
    [SerializeField] private Button antButton;
    [SerializeField] private Button beeButton;
    [SerializeField] private Button widowButton;

    [Header("Button Labels")]
    [SerializeField] private TMP_Text antButtonText;
    [SerializeField] private TMP_Text beeButtonText;
    [SerializeField] private TMP_Text widowButtonText;

    [Header("Prices")]
    [SerializeField] private int beePrice = 25;
    [SerializeField] private int widowPrice = 100;

    [Header("Death")]
    [SerializeField] private float deathMenuDelay = 2f;

    private bool beeUnlocked;
    private bool widowUnlocked;
    private bool waitingForDeath;

    private void Start()
    {
        //Unlocks reset whenever the game scene starts
        beeUnlocked = false;
        widowUnlocked = false;

        if (menuPanel != null)
            menuPanel.SetActive(true);

        if (playerSpawner != null)
            playerSpawner.SetGameplayUIVisible(false);

        RefreshMenu();
        RefreshCoinText();
    }

    //If the player presses to play as an ant: (function ran via the button object in the scene)
    public void PressAnt() {StartRun(antPrefab);}

    //If the player presses to play as a bee:
    public void PressBee() {
        if (!beeUnlocked){ //Bee must be bought first
            if (!PlayerCoins.TrySpendSharedCoins(beePrice)) {
                RefreshCoinText();
                return;
            }

            beeUnlocked = true;
            RefreshMenu();
            RefreshCoinText();
            return;
        }

        StartRun(beePrefab);
    }

    //If the player presses to play as a spider:
    public void PressWidow() {
        if (!widowUnlocked) {//Bee must be bought first
            if (!PlayerCoins.TrySpendSharedCoins(widowPrice)) {
                RefreshCoinText();
                return;
            }

            widowUnlocked = true;
            RefreshMenu();
            RefreshCoinText();
            return;
        }

        StartRun(widowPrefab);
    }

    //Restart the game
    private void StartRun(GameObject chosenPrefab){
        if (chosenPrefab == null || playerSpawner == null || waveSpawner == null) {
            Debug.LogError("BugSelectionMenu: Assign all player prefabs, PlayerSpawner, and ZombieWaveSpawner.");
            return;
        }

        waitingForDeath = false;

        //Remove leftovers and reset the wave index and counter
        waveSpawner.ResetWaves();

        if (menuPanel != null)
            menuPanel.SetActive(false);

        playerSpawner.SpawnPlayer(chosenPrefab);
        playerSpawner.SetGameplayUIVisible(true);

        RefreshCoinText();

        //Start wave one only after the player has been spawned
        waveSpawner.BeginWaves();
    }

    public void OnPlayerDied() {
        if (waitingForDeath)
            return;

        waitingForDeath = true;
        StartCoroutine(ReturnToMenuAfterDeath());
    }

    //Restarts the game, but after giving a victory message
    public void OnVictory() {
        //Clear any remaining wave state
        if (waveSpawner != null)
            waveSpawner.ResetWaves();

        if (playerSpawner != null)
            playerSpawner.SetGameplayUIVisible(false);

        if (menuPanel != null)
            menuPanel.SetActive(true);

        RefreshMenu();
        RefreshCoinText();

        waitingForDeath = false;

        Debug.Log("Victory! Returning to bug selection menu.");
    }

    //Restarts the game after dying
    private IEnumerator ReturnToMenuAfterDeath() {
        yield return new WaitForSeconds(deathMenuDelay);

        if (waveSpawner != null)
            waveSpawner.ResetWaves();

        if (playerSpawner != null)
            playerSpawner.SetGameplayUIVisible(false);

        if (menuPanel != null)
            menuPanel.SetActive(true);

        RefreshMenu();
        RefreshCoinText();

        waitingForDeath = false;
    }

    //Brings the bug selection frames back
    private void RefreshMenu(){
        if (antButtonText != null)
            antButtonText.text = "Play";

        if (beeButtonText != null) {
            beeButtonText.text = beeUnlocked
                ? "Play"
                : "Buy (" + beePrice + ")";
        }

        if (widowButtonText != null) {
            widowButtonText.text = widowUnlocked
                ? "Play"
                : "Buy (" + widowPrice + ")";
        }
    }

    private void RefreshCoinText(){
        if (coinText != null)
            coinText.text = "Coins: " + PlayerCoins.GetSharedCoins();
    }
}