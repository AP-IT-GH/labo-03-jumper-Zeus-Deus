using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [HideInInspector] public float speed;

    private float destroyXPosition;
    private ObstacleSpawner spawner;
    private bool hasPassed = false;
    private float agentXPosition = 0f;

    // Wordt aangeroepen wanneer het obstakel gespawned wordt
    public void Initialize(float speed, float destroyX, ObstacleSpawner spawner)
    {
        this.speed = speed;
        this.destroyXPosition = destroyX;
        this.spawner = spawner;
        this.hasPassed = false;

        gameObject.tag = "Obstacle";
    }

    private void Update()
    {
        // Beweeg het obstakel naar links
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        // Check of het obstakel de agent voorbij is (dan heeft agent het ontweken)
        if (!hasPassed && transform.localPosition.x < agentXPosition - 1f)
        {
            hasPassed = true;
            if (spawner != null)
            {
                spawner.NotifyObstaclePassed();
            }
        }

        // Vernietig het obstakel als het buiten het speelveld is
        if (transform.localPosition.x < destroyXPosition)
        {
            if (spawner != null)
            {
                spawner.RemoveObstacle(this);
            }
            Destroy(gameObject);
        }
    }
}
