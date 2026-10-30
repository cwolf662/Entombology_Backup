//Admitedly, I had to heavily use AI for this, as I didn't have much time left to do this project.
//I am familiar with A* and AI algorithms though, I did take that class, so I do understand most of this code.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
public class ZombieAI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Pathfinding")]
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private float pathRefreshInterval = 0.3f;
    [SerializeField] private int searchMargin = 20;
    [SerializeField] private int maxSearchIterations = 10000;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stopDistance = 0.6f;

    [Header("Facing")]
    [SerializeField] private Transform visuals;
    [SerializeField] private float spriteAngleOffset = -90f;

    [Header("Walking Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite step1Sprite;
    [SerializeField] private Sprite step2Sprite;
    [SerializeField] private float animationFrameTime = 0.15f;

    [Header("Slow Effects")]
    [SerializeField, Range(0f, 1f)]
    private float webSlowMultiplier = 0.5f;
    private int activeWebSlowCount = 0;
    private Rigidbody2D rb;

    private readonly List<Vector3Int> path = new List<Vector3Int>();

    private static readonly Vector3Int[] directions = {
        new Vector3Int( 1,  0, 0),
        new Vector3Int(-1,  0, 0),
        new Vector3Int( 0,  1, 0),
        new Vector3Int( 0, -1, 0),
        new Vector3Int( 1,  1, 0),
        new Vector3Int( 1, -1, 0),
        new Vector3Int(-1,  1, 0),
        new Vector3Int(-1, -1, 0)
    };

    private int pathIndex;
    private float pathRefreshTimer;
    private Vector3Int lastTargetCell;
    private bool hasTargetCell;

    private bool isMoving;
    private float animationTimer;
    private bool showStep2;

    private static readonly System.Collections.Generic.List<ZombieAI> activeZombies = new System.Collections.Generic.List<ZombieAI>();

    private void OnEnable()
    {
        if (!activeZombies.Contains(this))
            activeZombies.Add(this);
    }

    private void OnDisable()
    {
        activeZombies.Remove(this);
    }

    public static void UpdateAllPlayerTargets(Transform newTarget)
    {
        for (int i = activeZombies.Count - 1; i >= 0; i--)
        {
            if (activeZombies[i] == null)
            {
                activeZombies.RemoveAt(i);
                continue;
            }

            activeZombies[i].SetPlayerTarget(newTarget);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetPlayerTarget(Transform newTarget)
    {
        player = newTarget;

        //Discard any path calculated for the previous player
        path.Clear();
        pathIndex = 0;
        hasTargetCell = false;
        pathRefreshTimer = 0f;
    }
    private void Start()
    {
        //Find the player if no reference was assigned
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        //Find the WallTilemap if no reference was assigned
        if (wallTilemap == null)
        {
            GameObject wallObject = GameObject.Find("WallTilemap");

            if (wallObject != null)
                wallTilemap = wallObject.GetComponent<Tilemap>();
        }

        if (player == null)
            Debug.LogError("ZombieAI: Player could not be found.", this);

        if (wallTilemap == null)
            Debug.LogError("ZombieAI: WallTilemap could not be found.", this);
    }

    private void Update()
    {
        FacePlayer();
        UpdateWalkingAnimation();
    }

    private void FixedUpdate()
    {
        isMoving = false;

        //Reacquire the player if the previous player was destroyed
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                SetPlayerTarget(playerObject.transform);
            }
        }

        if (player == null)
            return;

        Vector2 toPlayer = (Vector2)player.position - rb.position;

        if (toPlayer.magnitude <= stopDistance)
            return;

        if (wallTilemap == null)
            return;

        pathRefreshTimer -= Time.fixedDeltaTime;

        Vector3Int targetCell = wallTilemap.WorldToCell(player.position);

        if (pathRefreshTimer <= 0f || !hasTargetCell || targetCell != lastTargetCell || path.Count == 0)
        {
            FindPathToPlayer();

            pathRefreshTimer = pathRefreshInterval;
            lastTargetCell = targetCell;
            hasTargetCell = true;
        }

        FollowPath();
    }

    private void FacePlayer()
    {
        if (player == null || visuals == null)
            return;

        Vector2 direction = (Vector2)player.position - (Vector2)visuals.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        visuals.rotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);
    }

    private void UpdateWalkingAnimation()
    {
        if (spriteRenderer == null || step1Sprite == null || step2Sprite == null)
            return;

        if (!isMoving)
        {
            animationTimer = 0f;
            showStep2 = false;
            spriteRenderer.sprite = step1Sprite;
            return;
        }

        animationTimer += Time.deltaTime;

        if (animationTimer >= animationFrameTime)
        {
            animationTimer = 0f;
            showStep2 = !showStep2;
        }

        spriteRenderer.sprite = showStep2 ? step2Sprite : step1Sprite;
    }

    private void FindPathToPlayer()
    {
        path.Clear();
        pathIndex = 0;

        Vector3Int start = wallTilemap.WorldToCell(rb.position);

        Vector3Int goal = wallTilemap.WorldToCell(player.position);

        if (IsBlocked(start) || IsBlocked(goal))
            return;

        List<Vector3Int> openSet = new List<Vector3Int>();

        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();

        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();

        Dictionary<Vector3Int, float> gScore = new Dictionary<Vector3Int, float>();

        Dictionary<Vector3Int, float> fScore = new Dictionary<Vector3Int, float>();

        int minX = Mathf.Min(start.x, goal.x) - searchMargin;
        int maxX = Mathf.Max(start.x, goal.x) + searchMargin;
        int minY = Mathf.Min(start.y, goal.y) - searchMargin;
        int maxY = Mathf.Max(start.y, goal.y) + searchMargin;

        openSet.Add(start);
        gScore[start] = 0f;
        fScore[start] = Heuristic(start, goal);

        int iterations = 0;

        while (openSet.Count > 0 && iterations < maxSearchIterations)
        {
            iterations++;

            Vector3Int current = openSet[0];
            float lowestScore = GetScore(fScore, current);

            for (int i = 1; i < openSet.Count; i++)
            {
                float score = GetScore(fScore, openSet[i]);

                if (score < lowestScore)
                {
                    lowestScore = score;
                    current = openSet[i];
                }
            }

            if (current == goal)
            {
                ReconstructPath(cameFrom, current);
                return;
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Vector3Int direction in directions)
            {
                Vector3Int neighbor = current + direction;

                if (neighbor.x < minX || neighbor.x > maxX || neighbor.y < minY || neighbor.y > maxY)
                    continue;

                if (IsBlocked(neighbor) || closedSet.Contains(neighbor))
                    continue;

                //Prevent diagonal movement through wall corners
                if (direction.x != 0 && direction.y != 0)
                {
                    Vector3Int sideX = current + new Vector3Int(direction.x, 0, 0);

                    Vector3Int sideY = current + new Vector3Int(0, direction.y, 0);

                    if (IsBlocked(sideX) || IsBlocked(sideY))
                        continue;
                }

                float stepCost = direction.x != 0 && direction.y != 0
                    ? 1.41421356f
                    : 1f;

                float tentativeG = GetScore(gScore, current) + stepCost;

                if (tentativeG >= GetScore(gScore, neighbor))
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] =
                    tentativeG + Heuristic(neighbor, goal);

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
            }
        }

        //No route was found, try again later
    }

    private bool IsBlocked(Vector3Int cell)
    {
        return wallTilemap.HasTile(cell);
    }

    private float Heuristic(Vector3Int a, Vector3Int b)
    {
        float dx = Mathf.Abs(a.x - b.x);
        float dy = Mathf.Abs(a.y - b.y);

        return Mathf.Max(dx, dy) + (1.41421356f - 1f) * Mathf.Min(dx, dy);
    }

    private float GetScore(Dictionary<Vector3Int, float> scores, Vector3Int cell)
    {
        return scores.TryGetValue(cell, out float score)
            ? score
            : float.PositiveInfinity;
    }

    private void ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        path.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();

        //The zombie is already inside the starting cell
        pathIndex = path.Count > 1 ? 1 : 0;
    }

    private void FollowPath()
    {
        if (path.Count == 0 || pathIndex >= path.Count)
            return;

        Vector2 destination = wallTilemap.GetCellCenterWorld(path[pathIndex]);

        Vector2 difference = destination - rb.position;

        //Move on to the next cell once this one is reached
        if (difference.magnitude < 0.08f)
        {
            pathIndex++;

            if (pathIndex >= path.Count)
                return;

            destination = wallTilemap.GetCellCenterWorld(path[pathIndex]);

            difference = destination - rb.position;
        }

        if (difference.sqrMagnitude < 0.001f)
            return;

        float currentSpeed = moveSpeed;
        if (activeWebSlowCount > 0)
        {
            currentSpeed *= webSlowMultiplier;
        }

        Vector2 movement = difference.normalized * currentSpeed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + movement);
        isMoving = true;
    }

    public void AddWebSlow() { activeWebSlowCount++; }

    public void RemoveWebSlow() { activeWebSlowCount = Mathf.Max(0, activeWebSlowCount - 1); }
}