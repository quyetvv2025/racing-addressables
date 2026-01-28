using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public TrafficPool pool;

    [Header("Lanes")]
    public int laneCount = 3;
    public float laneOffset = 3.2f;
    public float roadCenterX = 0f;

    [Header("Spawn (make cars farther apart)")]
    public Vector2 spawnDistanceAhead = new Vector2(110f, 170f); // xa hơn
    public float minGapSameLane = 34f;                           // xa hơn (20–28 -> 34)
    public int maxCarsActive = 14;
    public float despawnBehind = 80f;

    [Header("Traffic speed")]
    [Range(0.1f, 1f)] public float minSpeedPctOfPlayerMax = 0.55f;
    [Range(0.1f, 1f)] public float maxSpeedPctOfPlayerMax = 0.90f;
    public float playerMaxSpeed = 45f;

    [Header("Safety")]
    public float speedEpsilon = 0.5f;   // m/s: để tạo chênh nhẹ, tránh “dính” bằng nhau gây rung
    public int spawnAttemptsPerTick = 8;
    public float spawnCooldown = 0.35f;

    readonly List<TrafficCar> active = new List<TrafficCar>();
    float nextSpawnTime;

    void Start()
    {
        if (player == null || pool == null)
        {
            Debug.LogError("TrafficSpawner: player/pool not assigned.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        // DespawnFarBehind();

        // if (Time.time < nextSpawnTime) return;
        // if (active.Count >= maxCarsActive) return;

        // bool spawned = TrySpawnFair(spawnAttemptsPerTick);
        // nextSpawnTime = Time.time + spawnCooldown;
        // if (!spawned) nextSpawnTime = Time.time + spawnCooldown * 0.6f; // thử lại sớm hơn chút nếu fail
    }

    bool TrySpawnFair(int attempts)
    {
        for (int i = 0; i < attempts; i++)
        {
            int lane = Random.Range(0, laneCount);
            float spawnZ = player.position.z + Random.Range(spawnDistanceAhead.x, spawnDistanceAhead.y);

            // 1) Gap check (same lane)
            if (!IsLaneGapOk(lane, spawnZ, minGapSameLane)) continue;

            // 2) Speed constraint in same lane: behind <= new <= ahead
            if (!TryComputeLaneSafeSpeed(lane, spawnZ, out float speed)) continue;

            SpawnAt(lane, spawnZ, speed);
            return true;
        }
        return false;
    }

    bool IsLaneGapOk(int lane, float spawnZ, float minGap)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var c = active[i];
            if (c == null || !c.gameObject.activeSelf) continue;
            if (c.laneIndex != lane) continue;

            float dz = Mathf.Abs(c.transform.position.z - spawnZ);
            if (dz < minGap) return false;
        }
        return true;
    }

    bool TryComputeLaneSafeSpeed(int lane, float spawnZ, out float chosenSpeed)
    {
        // Random base speed
        float pct = Random.Range(minSpeedPctOfPlayerMax, maxSpeedPctOfPlayerMax);
        float baseSpeed = playerMaxSpeed * pct;

        // Find nearest ahead and nearest behind in this lane
        TrafficCar ahead = null;
        TrafficCar behind = null;
        float aheadDZ = float.MaxValue;
        float behindDZ = float.MaxValue;

        for (int i = 0; i < active.Count; i++)
        {
            var c = active[i];
            if (c == null || !c.gameObject.activeSelf) continue;
            if (c.laneIndex != lane) continue;

            float dz = c.transform.position.z - spawnZ;

            if (dz > 0f && dz < aheadDZ)
            {
                aheadDZ = dz;
                ahead = c;
            }
            else if (dz < 0f && -dz < behindDZ)
            {
                behindDZ = -dz;
                behind = c;
            }
        }

        // Build allowed speed window: [minAllowed, maxAllowed]
        float minAllowed = 0f;
        float maxAllowed = playerMaxSpeed;

        // If there is a car behind, new car must be >= behind speed (so behind won't catch up and hit)
        if (behind != null)
            minAllowed = Mathf.Max(minAllowed, behind.speed + speedEpsilon);

        // If there is a car ahead, new car must be <= ahead speed (so new won't catch up and hit)
        if (ahead != null)
            maxAllowed = Mathf.Min(maxAllowed, ahead.speed - speedEpsilon);

        // Clamp baseSpeed into window
        chosenSpeed = Mathf.Clamp(baseSpeed, minAllowed, maxAllowed);

        // If window is invalid, reject spawn for this attempt
        // (e.g., behind is already faster than ahead)
        if (minAllowed > maxAllowed) return false;

        // If clamped speed hits boundaries too hard, it's still okay — but must respect window.
        return chosenSpeed >= minAllowed && chosenSpeed <= maxAllowed;
    }

    void SpawnAt(int lane, float z, float speed)
    {
        float x = LaneToX(lane);

        TrafficCar car = pool.Get();
        car.transform.position = new Vector3(x, 1f, z);
        car.transform.rotation = Quaternion.identity;

        car.ResetState(lane, speed);
        active.Add(car);
    }

    void DespawnFarBehind()
    {
        float cutoffZ = player.position.z - despawnBehind;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var c = active[i];
            if (c == null) { active.RemoveAt(i); continue; }

            if (c.transform.position.z < cutoffZ)
            {
                active.RemoveAt(i);
                pool.Return(c);
            }
        }
    }

    float LaneToX(int lane)
    {
        if (laneCount <= 1) return roadCenterX;

        float center = (laneCount - 1) * 0.5f; // 3 lanes -> 1, 2 lanes -> 0.5
        return roadCenterX + (lane - center) * laneOffset;
    }
}
