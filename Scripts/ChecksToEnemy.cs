using Unity.MLAgents;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int checkpointIndex; // Assign a unique index to each checkpoint in the Inspector

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Solder"))
        {
            // Notify the agent that it passed this checkpoint
            other.GetComponent<SolderAgent>().PassCheckpoint(checkpointIndex);
            Destroy(gameObject);
        }
    }
}
