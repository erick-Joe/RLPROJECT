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
    public List<Transform> MainPathChecks = new List<Transform>();
    public List<Transform> EnemyPathChecks = new List<Transform>();
    public Vector3 startingPosition = new Vector3(-2923.3f, 0f, 840.9f);
    public CharacterController characterController;
    public float speed = 10f;
    public float rayCastLength = 60f;
    public Animator animator;
  


    private bool isWalking;
    private bool enemyKilled = false;
    private Transform currentWaypoint;
    private int currentWaypointIndex = 0;
    private int currentEnemyIndex = 0;
    private object vectorAction;
    private int mainPathCheckpointIndex;
    private int branchPathCheckpointIndex;
    //Variables to help me track the checkpoints
    private int previousMainPathCheckpointIndex = -1;
    private int previousBranchPathCheckpointIndex = -1;



    [SerializeField] private MainPathCheckManager mainPathCheckpoints;
    [SerializeField] private EnemyPathCheckManager branchPathCheckpoints;
    [SerializeField] private string CheckpointTag;

    public override void Initialize()
    {
        // Initialize the list of enemies
        foreach (GameObject Enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            enemies.Add(Enemy);
        }

        // Initialize the list of main path checkpoints
        foreach (GameObject checkpointObject in GameObject.FindGameObjectsWithTag("MainPathCheck"))
        {
            MainPathChecks.Add(checkpointObject.transform); // Access the Transform component directly
        }

        // Initialize the list of enemy branch checkpoints
        foreach (GameObject checkpointObject in GameObject.FindGameObjectsWithTag("EnemyPathCheck"))
        {
            EnemyPathChecks.Add(checkpointObject.transform); // Access the Transform component directly
        }

        // Initialize the first checkpoint
        if (MainPathChecks.Count > 0)
        {
            currentWaypoint = MainPathChecks[currentWaypointIndex];
        }



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
        currentWaypointIndex = 0;
        enemyKilled = false;
        mainPathCheckpointIndex = 0;
        branchPathCheckpointIndex = 0;
        previousMainPathCheckpointIndex = -1;
        previousBranchPathCheckpointIndex = -1;


        //animations
        isWalking = false;
        //animator.SetBool("IsWalking", isWalking);

        // Reset checkpoints
        if (MainPathChecks.Count > 0)
        {
            currentWaypoint = MainPathChecks[currentWaypointIndex];
        }
        else
        {
            currentWaypoint = null;
        }


    }

    public override void CollectObservations(VectorSensor sensor)
    {

        // Observations for the nearest checkpoint
        if (currentWaypoint != null)
        {
            Vector3 toWaypoint = currentWaypoint.position - transform.position;
            sensor.AddObservation(toWaypoint.normalized); // Direction to the nearest checkpoint
            sensor.AddObservation(toWaypoint.magnitude);  // Distance to the nearest checkpoint
        }

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
            if (hitObject.CompareTag("EnemyPathCheck"))
            {
                sensor.AddObservation(hitObject.transform.position);
                float distanceToEnemyPathCheck = Vector3.Distance(transform.position, hitObject.transform.position);
                sensor.AddObservation(distanceToEnemyPathCheck);
            }
            if (hitObject.CompareTag("MainPathCheck"))
            {
                sensor.AddObservation(hitObject.transform.position);
                float distanceToMainPathCheck = Vector3.Distance(transform.position, hitObject.transform.position);
                sensor.AddObservation(distanceToMainPathCheck);
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
            AddReward(+6f);

            // Eliminate the enemy from the list and the scene
            enemies.RemoveAt(currentEnemyIndex);
            // Delayed destruction of the game object after 5 seconds
            StartCoroutine(DestroyAfterDelay(collision.gameObject, 2.0f));

           


            // Check if all enemies have been eliminated
            if (enemies.Count == 0)
            {
                //Debug.Log("COLLIDED WITH ENEMY11");
                //EndEpisode();// Turn This back Onn
            }
            else
            {
                // Move to the next enemy
                currentEnemyIndex++;
                enemyKilled = true;
                //Debug.Log("COLLIDED WITH ENEMY");

            }
        }
        if (collision.collider.tag == "Wall")
        {
            AddReward(-3);
            EndEpisode();
            //EndEpisode();// I have not decided on this
        }
        if (collision.collider.tag == "Poison")
        {
            AddReward(-6);
            EndEpisode();
        }



        // The checkpoint integration using the collision method
        if (collision.gameObject.CompareTag("MainPathCheck"))
        {
            int checkpointIndex = mainPathCheckpoints.GetCheckpointIndex(collision.gameObject);
            if (checkpointIndex > previousMainPathCheckpointIndex)
            {
                MainPathCheckpointTriggered(true);
                previousMainPathCheckpointIndex = checkpointIndex;

                // Add a reward for passing the correct checkpoint on the main path
                AddReward(1.0f);
                Debug.Log("Passed the correct MAIN checkpoint with index: " + previousMainPathCheckpointIndex);

            }
            else
            {
                MainPathCheckpointTriggered(false);

                // Apply a penalty for passing the wrong checkpoint on the main path
                AddReward(-1.0f);
                Debug.Log("Passed the Wrong MAIN checkpoint with index: " + previousMainPathCheckpointIndex);

            }
        }
        else if (collision.gameObject.CompareTag("EnemyPathCheck"))
        {
            int checkpointIndex = branchPathCheckpoints.GetCheckpointIndex(collision.gameObject);
            if (checkpointIndex > previousBranchPathCheckpointIndex)
            {
                BranchPathCheckpointTriggered(true);
                previousBranchPathCheckpointIndex = checkpointIndex;

                // Add a reward for passing the correct checkpoint on the branch path
                AddReward(2.0f);
                Debug.Log("Passed the correct ENEMY PATH checkpoint with index: " + previousBranchPathCheckpointIndex);

            }
            else
            {
                BranchPathCheckpointTriggered(false);

                // Apply a smaller penalty for passing the wrong checkpoint on the branch path
                AddReward(-0.5f);
                Debug.Log("Passed the wrong ENEMY PATH checkpoint with index: " + previousBranchPathCheckpointIndex);

            }
        }
    }

    // Coroutine to destroy the game object after a delay
    private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(obj);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var actionTaken = actions.ContinuousActions;

        float moveDirection = actionTaken[0]; // Control forward and backward movement
        float turnDirection = actionTaken[1]; // Control turning

        // Calculate forward and backward movement
        float moveSpeed = moveDirection * speed;
        transform.Translate(Vector3.forward * moveSpeed * Time.fixedDeltaTime);

        // Calculate rotation based on turnDirection
        float rotation = turnDirection * 180;

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

        // Check if the agent is close enough to the current waypoint
        if (currentWaypoint != null)
        {
            Vector3 toWaypoint = currentWaypoint.position - transform.position;
            float angleToWaypoint = Vector3.SignedAngle(transform.forward, toWaypoint, Vector3.up);

            // Apply rotation based on the angle to the waypoint
            rotation += angleToWaypoint;

            // Check if the agent is close enough to the current waypoint
            if (toWaypoint.magnitude < 2f)
            {
                // Switch to the next waypoint based on whether it's a main path or enemy branch waypoint
                if (currentWaypointIndex < MainPathChecks.Count - 1)
                {
                    currentWaypointIndex++;
                    currentWaypoint = MainPathChecks[currentWaypointIndex];
                }
                else
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex < MainPathChecks.Count + EnemyPathChecks.Count)
                    {
                        currentWaypoint = EnemyPathChecks[currentWaypointIndex - MainPathChecks.Count];
                    }
                    else
                    {
                        // The agent has reached the last checkpoint
                       
                    }
                }
            }
        }

        // Apply rotation based on the angle to the enemy
        if (currentEnemyIndex < enemies.Count)
        {
            Vector3 enemyDirection = enemies[currentEnemyIndex].transform.position - transform.position;
            float angleToEnemy = Vector3.SignedAngle(transform.forward, enemyDirection, Vector3.up);

            // Apply rotation based on the angle to the enemy
            rotation += angleToEnemy;
        }

        // Rotate the agent
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

    private void Start()
    {
        mainPathCheckpoints.OnCheckpointTriggered += MainPathCheckpointTriggered;
        branchPathCheckpoints.OnCheckpointTriggered += BranchPathCheckpointTriggered;
    }
    private void MainPathCheckpointTriggered(bool isCorrectCheckpoint)
    {
       
    }

    private void BranchPathCheckpointTriggered(bool isCorrectCheckpoint)
    {
        
    }

}
