using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class JumperAgent : Agent
{
    [Header("Jump Settings")]
    public float jumpForce = 7f;
    public float groundY = 0.5f;
    public float groundCheckThreshold = 0.05f;

    [Header("References")]
    public ObstacleSpawner spawner;

    private Rigidbody rb;
    private bool isGrounded;
    private int obstaclesPassed;
    private int totalJumps;
    private int episodeSteps;

    // Voor het loggen van custom stats naar TensorBoard
    private StatsRecorder statsRecorder;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        statsRecorder = Academy.Instance.StatsRecorder;

        if (spawner == null)
        {
            Debug.LogError("Spawner is niet ingesteld! Sleep de ObstacleSpawner naar het Spawner veld in de Inspector.");
        }
    }

    public override void OnEpisodeBegin()
    {
        // Log de stats van de vorige episode
        if (CompletedEpisodes > 0)
        {
            statsRecorder.Add("Custom/Obstacles Dodged", obstaclesPassed);
            statsRecorder.Add("Custom/Jumps Per Episode", totalJumps);
            statsRecorder.Add("Custom/Episode Length (steps)", episodeSteps);
        }

        // Zet de agent terug naar de startpositie
        transform.localPosition = new Vector3(0f, groundY, 0f);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        isGrounded = true;
        obstaclesPassed = 0;
        totalJumps = 0;
        episodeSteps = 0;

        // Verwijder oude obstakels en begin opnieuw
        if (spawner != null)
        {
            spawner.ResetSpawner();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Y positie van de agent
        sensor.AddObservation(transform.localPosition.y / 3f);

        // Y snelheid van de agent (positief = omhoog, negatief = omlaag)
        sensor.AddObservation(rb.linearVelocity.y / 10f);

        // Staat de agent op de grond?
        sensor.AddObservation(isGrounded ? 1f : 0f);

        // Info over de 3 dichtstbijzijnde obstakels
        Obstacle[] nearestObstacles = new Obstacle[0];
        if (spawner != null)
        {
            nearestObstacles = spawner.GetNearestObstacles(transform.localPosition, 3);
        }

        for (int i = 0; i < 3; i++)
        {
            if (nearestObstacles != null && i < nearestObstacles.Length && nearestObstacles[i] != null)
            {
                // Afstand, hoogte en snelheid van het obstakel
                float relativeX = (nearestObstacles[i].transform.localPosition.x - transform.localPosition.x) / 20f;
                sensor.AddObservation(relativeX);
                sensor.AddObservation(nearestObstacles[i].transform.localPosition.y / 3f);
                sensor.AddObservation(nearestObstacles[i].speed / 15f);
            }
            else
            {
                // Geen obstakel = default waarden
                sensor.AddObservation(1f);
                sensor.AddObservation(0f);
                sensor.AddObservation(0f);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        episodeSteps++;
        int jumpAction = actions.DiscreteActions[0];

        // Springen alleen als de agent op de grond staat
        if (jumpAction == 1 && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            totalJumps++;
        }

        // Kleine straf per stap zodat de agent niet te lang wacht
        AddReward(-0.001f);
    }

    // Zodat je zelf kan testen met de spatiebalk
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0;

        if (Input.GetKey(KeyCode.Space))
        {
            discreteActions[0] = 1;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Terug op de grond = weer kunnen springen
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }

        // Geraakt door obstakel = straf en episode stopt
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            statsRecorder.Add("Custom/Hit by Obstacle", 1f);
            AddReward(-1.0f);
            EndEpisode();
        }
    }

    // Wordt aangeroepen door de ObstacleSpawner als de agent een obstakel ontweek
    public void ObstaclePassed()
    {
        obstaclesPassed++;
        AddReward(1.0f);
    }
}
