using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEditor.Timeline.Actions;
using UnityEngine;

public class SolderAgent : Agent
{
    public List<GameObject> enemies = new List<GameObject>();
    public List<GameObject> checkpoints = new List<GameObject>();
    public float rayCastLength = 60f;
    public Vector3 startingPosition = new Vector3(-2923.3f, 0f, 293.6f);
    public CharacterController characterController;
    public float speed = 10f;
    public Animator animator;
    private bool isWalking;

    private int currentEnemyIndex = 0;
    private bool enemyKilled = false;
    private int currentCheckpointGroup = 0;
    private int currentCheckpointIndex = 0;
    private bool collidedWithEnemy = false;
    private object vectorAction;

    public override void Initialize()
    {
        // Initialize the list of enemies and checkpoints
        foreach (GameObject Enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            enemies.Add(Enemy);
        }

        // Add checkpoints
        foreach (GameObject CheckToEnemy in GameObject.FindGameObjectsWithTag("CheckToEnemy"))
        {
            checkpoints.Add(CheckToEnemy);
        }

        // Activate only the initial checkpoints (1, 2, and 3)
        ActivateCheckpoints(0, 3);

    }


    public override void OnEpisodeBegin()
    {
        // Reposition the agent at a designated starting position
        transform.position = startingPosition;
        transform.rotation = Quaternion.Euler(Vector3.zero);

        // Reset the list of enemies to include all available enemies in the scene
        enemies.Clear();
        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            enemies.Add(enemy);
        }
        // Reposition agent, enemies, and checkpoints
        currentEnemyIndex = 0;
        enemyKilled = false;
        currentCheckpointGroup = 0;
        currentCheckpointIndex = 0;

        //animations
        isWalking = false;

        //enemy COLLISION
        collidedWithEnemy = false;

        // Activate only the initial checkpoints (1, 2, and 3)
        ActivateCheckpoints(0, 3);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // The position of the agent
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.y);

        sensor.AddObservation(transform.position);

        // Raycast to detect enemies, walls, and checkpoints
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, rayCastLength))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag("Enemy"))
            {
                sensor.AddObservation(hitObject.transform.position);
                float distanceToEnemy = Vector3.Distance(transform.position, hitObject.transform.position);
                sensor.AddObservation(distanceToEnemy);
            }
            if (hitObject.CompareTag("Wall"))
            {
                sensor.AddObservation(hitObject.transform.position);
                float distanceToWall = Vector3.Distance(transform.position, hitObject.transform.position);
                sensor.AddObservation(distanceToWall);
            }
            if (hitObject.CompareTag("Poison"))
            {
                sensor.AddObservation(hitObject.transform.position);
                float distanceToPoison = Vector3.Distance(transform.position, hitObject.transform.position);
                sensor.AddObservation(distanceToPoison);
            }
            if (hitObject.CompareTag("CheckToEnemy"))
            {
                sensor.AddObservation(hitObject.transform.position);
                float distanceCheckToEnemy = Vector3.Distance(transform.position, hitObject.transform.position);
                sensor.AddObservation(distanceCheckToEnemy);
            }
            if (hitObject.CompareTag("CheckPoint"))
            {
                sensor.AddObservation(hitObject.transform.position);
                float distanceToCheckPoint = Vector3.Distance(transform.position, hitObject.transform.position);
                sensor.AddObservation(distanceToCheckPoint);
            }
        }

        // Information about the current enemy
        if (currentEnemyIndex < enemies.Count)
        {
            sensor.AddObservation(enemies[currentEnemyIndex].transform.position);
            float distanceToCurrentEnemy = Vector3.Distance(transform.position, enemies[currentEnemyIndex].transform.position);
            sensor.AddObservation(distanceToCurrentEnemy);
        }
        else
        {
            // If all enemies are eliminated, provide no observations for the enemy
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && !enemyKilled)
        {
            AddReward(10f);
            // Eliminate the enemy from the list and the scene
            enemies.RemoveAt(currentEnemyIndex);
            Destroy(collision.gameObject);

            // Check if all enemies have been eliminated
            if (enemies.Count == 0)
            {
                //EndEpisode();// Turn This back Onn
            }
            else
            {
                // Move to the next enemy
                currentEnemyIndex++;
                enemyKilled = true;

                // Activate the next set of checkpoints (4, 5, 6) when an enemy is killed
                ActivateCheckpoints(currentCheckpointGroup * 3 + 3, currentCheckpointGroup * 3 + 6);
                currentCheckpointGroup++;
            }
        }
        if (collision.collider.tag == "Wall")
        {
            AddReward(-1);
            //EndEpisode();// I have not decided on this
        }
        if (collision.collider.tag == "Poison")
        {
            AddReward(-5);
            EndEpisode();
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var actionTaken = actions.ContinuousActions;

        float moveDirection = actionTaken[0]; // Control forward and backward movement
        float turnDirection = actionTaken[1]; // Control turning

        // Calculate forward and backward movement
        float moveSpeed = moveDirection * speed;
        transform.Translate(Vector3.forward * moveSpeed * Time.fixedDeltaTime);

        // Calculate rotation based on turnDirection (e.g., -1 for left, 0 for no turn, 1 for right)
        float rotation = turnDirection * 180; // You can adjust the factor to control the turn rate

        // Check the direction of the enemy
        if (currentEnemyIndex < enemies.Count)
        {
            Vector3 enemyDirection = enemies[currentEnemyIndex].transform.position - transform.position;
            float angleToEnemy = Vector3.SignedAngle(transform.forward, enemyDirection, Vector3.up);

            // Apply rotation based on the angle to the enemy
            rotation += angleToEnemy;
        }

        //animation set up
        // Check if the agent is moving
        if (moveSpeed != 0 || turnDirection != 0)
        {
            isWalking = true;
        }
        else
        {
            isWalking = false;
        }
        animator.SetBool("IsWalking", isWalking);

        transform.Rotate(Vector3.up, rotation * Time.fixedDeltaTime);

        // Apply a penalty for every step to encourage the agent to reach the goal
        AddReward(-0.01f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> actions = actionsOut.ContinuousActions;

        // Control forward and backward movement
        float moveInput = 0;
        if (Input.GetKey("w")) moveInput = 1;
        else if (Input.GetKey("s")) moveInput = -1;

        // Control turning
        float turnInput = 0;
        if (Input.GetKey("d")) turnInput = 1;
        else if (Input.GetKey("a")) turnInput = -1;

        actions[0] = moveInput;
        actions[1] = turnInput;
    }



    //Checkpoint Intergartion
    public void PassCheckpoint(int checkpointIndex)
    {
        // Reward the agent for passing a checkpoint
        SetReward(1f);

        // Deactivate the passed checkpoint
        checkpoints[checkpointIndex].SetActive(false);

        // Check if all active checkpoints have been passed
        if (!CheckpointsActive())
        {
            // All active checkpoints have been passed; activate new ones
            currentCheckpointIndex += 3; // Increment the current checkpoint index
            ActivateCheckpoints(currentCheckpointIndex, currentCheckpointIndex + 2);
        }
    }

    private bool CheckpointsActive()
    {
        for (int i = currentCheckpointIndex; i < currentCheckpointIndex + 3; i++)
        {
            if (i < checkpoints.Count && checkpoints[i].activeSelf)
            {
                return true; // At least one checkpoint is active
            }
        }
        return false; // No active checkpoints in the current group
    }

    private void ActivateCheckpoints(int startIndex, int endIndex)
    {
        for (int i = startIndex; i < endIndex; i++)
        {
            if (i >= 0 && i < checkpoints.Count)
            {
                checkpoints[i].SetActive(true);
            }
        }
    }

}