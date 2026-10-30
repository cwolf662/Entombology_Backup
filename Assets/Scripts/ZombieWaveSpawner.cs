using System.Collections;
using UnityEngine;
using TMPro;

[System.Serializable]
public class ZombieSpawnEntry {
    public GameObject zombiePrefab;
    [Min(0)] public int quantity = 5;
}

[System.Serializable]
public class ZombieWave {
    public string waveName = "Wave";
    public AudioClip announcementSound;
    public ZombieSpawnEntry[] entries;
}

public class ZombieWaveSpawner : MonoBehaviour {
    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointsRoot;

    [Header("Wave Settings")]
    [SerializeField] private ZombieWave[] waves;
    [SerializeField] private float spawnInterval = 3f;

    [Header("Zombie UI")]
    [SerializeField] private TMP_Text zombiesAliveText;

    [Header("Wave Announcement")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private AudioSource waveAudioSource;
    [SerializeField] private AudioClip victoryAnnouncementSound;
    [SerializeField] private float announcementStayDuration = 2f;
    [SerializeField] private float announcementFadeDuration = 3f;

    [Header("Victory")]
    [SerializeField] private BugSelectionMenu bugSelectionMenu;
    [SerializeField] private int victoryWaveNumber = 10;

    private Transform[] spawnPoints;
    private int currentWaveIndex = -1;
    private int aliveZombies = 0;
    private Coroutine waveRoutine;
    private bool initialized;

    public int AliveZombies => aliveZombies;
    public int CurrentWave => currentWaveIndex + 1;

    private void Awake() {
        if (spawnPointsRoot == null) {
            Debug.LogError("ZombieWaveSpawner: Spawn Points Root is not assigned.");
            enabled = false;
            return;
        }

        spawnPoints = GetSpawnPoints();

        if (spawnPoints.Length == 0) {
            Debug.LogError("ZombieWaveSpawner: No spawn points were found.");
            enabled = false;
            return;
        }

        if (waves == null || waves.Length == 0) {
            Debug.LogWarning("ZombieWaveSpawner: No waves configured.");
            enabled = false;
            return;
        }

        if (waveText != null)
            waveText.gameObject.SetActive(false);

        initialized = true;
        UpdateZombieCounter();
    }

    //Multiple spawn points exist
    private Transform[] GetSpawnPoints() {
        Transform[] allChildren = spawnPointsRoot.GetComponentsInChildren<Transform>();

        if (allChildren.Length <= 1)
            return new Transform[0];

        Transform[] points = new Transform[allChildren.Length - 1];

        for (int i = 1; i < allChildren.Length; i++)
            points[i - 1] = allChildren[i];

        return points;
    }

    public void BeginWaves() {
        if (!initialized || !enabled)
            return;

        if (waveRoutine != null)
            StopCoroutine(waveRoutine);

        waveRoutine = StartCoroutine(RunWaves());
    }

    public void ResetWaves() {
        if (waveRoutine != null) {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        if (waveText != null) {
            waveText.color = new Color(
                waveText.color.r,
                waveText.color.g,
                waveText.color.b,
                0f
            );

            waveText.gameObject.SetActive(false);
        }

        ZombieHealth.DestroyAllZombies();

        aliveZombies = 0;
        currentWaveIndex = -1;

        UpdateZombieCounter();
    }

    private IEnumerator RunWaves() {
        for (int i = 0; i < waves.Length; i++) {
            currentWaveIndex = i;
            ZombieWave wave = waves[i];
            Debug.Log("Starting " + wave.waveName);

            //Display the wave announcement before spawning
            yield return StartCoroutine( ShowWaveAnnouncement(wave.waveName, wave.announcementSound) );

            if (wave.entries != null) {
                foreach (ZombieSpawnEntry entry in wave.entries) {
                    if (entry == null || entry.zombiePrefab == null || entry.quantity <= 0){
                        continue;
                    }

                    for (int j = 0; j < entry.quantity; j++) {
                        SpawnZombie(entry.zombiePrefab);

                        bool moreToSpawn =
                            j < entry.quantity - 1 ||
                            !IsLastValidEntry(wave, entry);

                        if (moreToSpawn)
                            yield return new WaitForSeconds(spawnInterval);
                    }
                }
            }

            //Wait until every zombie in this wave is dead
            while (aliveZombies > 0)
                yield return null;

            Debug.Log(wave.waveName + " cleared");

            //Check whether the tenth wave has been defeated
            if (i + 1 == victoryWaveNumber) {
                yield return StartCoroutine( ShowWaveAnnouncement("Victory!", victoryAnnouncementSound) );

                waveRoutine = null;

                if (bugSelectionMenu != null)
                    bugSelectionMenu.OnVictory();

                yield break;
            }
        }

        waveRoutine = null;
    }

    //Each wvae has its own sound assigned to it
    private IEnumerator ShowWaveAnnouncement(string message, AudioClip announcementSound) {
        if (waveText == null){
            Debug.LogWarning("Wave announcement text is not assigned.");
            yield break;
        }

        waveText.gameObject.SetActive(true);
        waveText.text = message;

        Color textColor = waveText.color;
        textColor.a = 1f;
        waveText.color = textColor;

        if (waveAudioSource != null && announcementSound != null){
            waveAudioSource.PlayOneShot(announcementSound);
        }

        yield return new WaitForSeconds(announcementStayDuration);

        float elapsed = 0f;

        while (elapsed < announcementFadeDuration) {
            elapsed += Time.deltaTime;

            float alpha = Mathf.Lerp(1f, 0f, elapsed / announcementFadeDuration);

            textColor.a = alpha;
            waveText.color = textColor;

            yield return null;
        }

        textColor.a = 0f;
        waveText.color = textColor;
        waveText.gameObject.SetActive(false);
    }

    private bool IsLastValidEntry(ZombieWave wave, ZombieSpawnEntry currentEntry) {
        int currentIndex = System.Array.IndexOf(wave.entries, currentEntry);

        for (int i = currentIndex + 1; i < wave.entries.Length; i++) {
            ZombieSpawnEntry entry = wave.entries[i];

            if (entry != null && entry.zombiePrefab != null && entry.quantity > 0) {
                return false;
            }
        }

        return true;
    }

    private void SpawnZombie(GameObject zombiePrefab) {
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject zombie = Instantiate(
            zombiePrefab,
            spawnPoint.position,
            Quaternion.identity);

        ZombieHealth health = zombie.GetComponent<ZombieHealth>();

        if (health != null){
            health.SetWaveSpawner(this);
            aliveZombies++;
            UpdateZombieCounter();
        }
    }

    //Subtract from zombie count
    public void NotifyZombieDied() {
        aliveZombies = Mathf.Max(0, aliveZombies - 1);
        UpdateZombieCounter();
    }

    //Update zombie count text
    private void UpdateZombieCounter() {
        if (zombiesAliveText != null)
            zombiesAliveText.text = "Zombies: " + aliveZombies;
    }
}
