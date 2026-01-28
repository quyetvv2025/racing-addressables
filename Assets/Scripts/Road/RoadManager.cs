using UnityEngine;
using System.Collections.Generic;

public class RoadManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Segment")]
    public Transform segmentPrefab;
    public int initialCount = 8;

    [Tooltip("Must match your segment length in Z (e.g. cube scale.z = 60)")]
    public float segmentLength = 60f;

    [Tooltip("How far ahead of the player we keep road segments")]
    public float aheadDistance = 120f;

    private readonly Queue<Transform> segments = new Queue<Transform>();
    private float nextSpawnZ;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("RoadManager: player is not assigned.");
            enabled = false;
            return;
        }
        if (segmentPrefab == null)
        {
            Debug.LogError("RoadManager: segmentPrefab is not assigned.");
            enabled = false;
            return;
        }

        // Spawn initial segments starting at Z=0 forward
        nextSpawnZ = 0f;
        for (int i = 0; i < initialCount; i++)
            SpawnOne();
    }

    void Update()
    {
        // We want the first segment to always be behind enough,
        // and ensure there's always road ahead.
        // Condition: if the player is getting close to the end of our “ahead buffer”, recycle.
        while (player.position.z + aheadDistance > nextSpawnZ)
        {
            // Recycle the oldest (front) segment to the end
            Transform oldest = segments.Dequeue();
            oldest.position = new Vector3(0f, oldest.position.y, nextSpawnZ);
            segments.Enqueue(oldest);

            nextSpawnZ += segmentLength;
        }
    }

    void SpawnOne()
    {
        Transform seg = Instantiate(segmentPrefab, new Vector3(0f, 0f, nextSpawnZ), Quaternion.identity, transform);
        segments.Enqueue(seg);
        nextSpawnZ += segmentLength;
    }
}
