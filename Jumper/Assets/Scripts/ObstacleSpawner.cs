using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject obstaclePrefab;
    public float spawnXPosition = 15f;
    public float destroyXPosition = -10f;

    [Header("Speed Range")]
    public float minSpeed = 4f;
    public float maxSpeed = 12f;

    [Header("Spawn Timing")]
    public float minSpawnInterval = 0.8f;
    public float maxSpawnInterval = 2.5f;

    [Header("References")]
    public JumperAgent agent;

    private List<Obstacle> activeObstacles = new List<Obstacle>();
    private Coroutine spawnCoroutine;

    // Reset de spawner bij het begin van elke episode
    public void ResetSpawner()
    {
        // Stop de huidige spawn loop
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        // Verwijder alle actieve obstakels
        foreach (var obstacle in activeObstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle.gameObject);
            }
        }
        activeObstacles.Clear();

        // Start opnieuw met spawnen
        spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        // Even wachten voor het eerste obstakel verschijnt
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            SpawnObstacle();

            // Willekeurige tijd tussen obstakels (maakt het moeilijker voor de agent)
            float interval = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(interval);
        }
    }

    private void SpawnObstacle()
    {
        if (obstaclePrefab == null) return;

        // Spawn rechts van de agent op de grond
        Vector3 spawnPos = new Vector3(spawnXPosition, 0.5f, 0f);
        GameObject obstacleObj = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity, transform);

        Obstacle obstacle = obstacleObj.GetComponent<Obstacle>();
        if (obstacle == null)
        {
            obstacle = obstacleObj.AddComponent<Obstacle>();
        }

        // Geef elk obstakel een willekeurige snelheid
        float speed = Random.Range(minSpeed, maxSpeed);
        obstacle.Initialize(speed, destroyXPosition, this);

        activeObstacles.Add(obstacle);
    }

    // Haal een obstakel uit de lijst als het vernietigd wordt
    public void RemoveObstacle(Obstacle obstacle)
    {
        activeObstacles.Remove(obstacle);
    }

    // Laat de agent weten dat hij een obstakel heeft ontweken
    public void NotifyObstaclePassed()
    {
        if (agent != null)
        {
            agent.ObstaclePassed();
        }
    }

    // Geeft de dichtstbijzijnde obstakels terug (gebruikt voor de observaties van de agent)
    public Obstacle[] GetNearestObstacles(Vector3 position, int count)
    {
        return activeObstacles
            .Where(o => o != null && o.transform.localPosition.x > position.x - 1f)
            .OrderBy(o => o.transform.localPosition.x - position.x)
            .Take(count)
            .ToArray();
    }
}
