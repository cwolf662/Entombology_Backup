using UnityEngine;
using UnityEngine.Tilemaps;

public class CrumbSpawner : MonoBehaviour {
    [Header("Spawn Settings")]
    [SerializeField] private GameObject crumbPrefab;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxCrumbs = 5;

    [Header("Map Tilemaps")]
    [SerializeField] private Tilemap floorTilemap;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap tunnelTilemap;

    [Header("Placement")]
    [SerializeField] private int maxSpawnAttempts = 100;

    private float spawnTimer;

    private void Update() {
        //Pause the timer while the arena already has its maximum
        if (transform.childCount >= maxCrumbs){
            spawnTimer = 0f;
            return;
        }

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval){
            spawnTimer = 0f;
            TrySpawnCrumb();
        }
    }

    //Spawns crumb randomly on the floor
    private void TrySpawnCrumb(){
        if (crumbPrefab == null || floorTilemap == null)
            return;

        BoundsInt bounds = floorTilemap.cellBounds;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++) {
            Vector3Int cell = new Vector3Int(
                Random.Range(bounds.xMin, bounds.xMax),
                Random.Range(bounds.yMin, bounds.yMax),
                0
            );

            //Only spawn on an actual floor tile
            if (!floorTilemap.HasTile(cell))
                continue;

            //Never spawn inside walls or tunnels
            if (wallTilemap != null && wallTilemap.HasTile(cell))
                continue;

            if (tunnelTilemap != null && tunnelTilemap.HasTile(cell))
                continue;

            //Avoid placing multiple crumbs on the same grid cell
            if (CrumbAlreadyInCell(cell))
                continue;

            Vector3 spawnPosition = floorTilemap.GetCellCenterWorld(cell);

            spawnPosition.z = 3f; //Keeps crumb visible

            //Parent each crumb to this spawner so its child count
            //tracks how many spawned crumbs remain
            Instantiate(
                crumbPrefab,
                spawnPosition,
                Quaternion.identity,
                transform
            );

            return;
        }
    }

    //Checks to see if a crumb already exists here, we don't want them overlapping
    private bool CrumbAlreadyInCell(Vector3Int cell) {
        foreach (Transform crumb in transform) {
            Vector3Int crumbCell =
                floorTilemap.WorldToCell(crumb.position);

            if (crumbCell == cell)
                return true;
        }

        return false;
    }
}
