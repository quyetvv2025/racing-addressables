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
	public Vector2 spawnDistanceAhead = new Vector2(150f, 230f);
	[Tooltip("Khoảng an toàn không spawn sát Player")]
	public float minSpawnDistanceFromPlayer = 50f;
	public float minGapSameLane = 30f;
	public int maxCarsActive = 5;

	[Header("Despawn")]
	public float clearPassedDistance = 20f;

    [Header("Traffic speed")]
    [Range(0.1f, 1f)] public float minSpeedPctOfPlayerMax = 0.55f;
    [Range(0.1f, 1f)] public float maxSpeedPctOfPlayerMax = 0.90f;
    public float playerMaxSpeed = 45f;

	[Header("Safety")]
	public float speedEpsilon = 0.5f;   // m/s: để tạo chênh nhẹ, tránh "dính" bằng nhau gây rung
	public float spawnInterval = 1f;
	[Tooltip("Bật log để theo dõi spawn")] public bool logSpawnDebug;

    readonly List<TrafficCar> active = new List<TrafficCar>();
    public IReadOnlyList<TrafficCar> ActiveCars => active;
	float nextSpawnTime;

    void Start()
    {
        if (player == null || pool == null)
        {
            Debug.LogError("TrafficSpawner: player/pool not assigned.");
            enabled = false;
            return;
        }

		TrafficSpawner[] spawners = FindObjectsByType<TrafficSpawner>(FindObjectsSortMode.None);
		if (spawners.Length > 1)
		{
			Debug.LogWarning($"[TrafficSpawner] Phat hien {spawners.Length} TrafficSpawner trong scene. Gioi han maxCarsActive ap dung theo toan scene.");
		}
    }

	void Update()
	{
		DespawnPassed();

		if (Time.time < nextSpawnTime)
		{
			return;
		}

		if (!CanSpawn())
		{
			if (logSpawnDebug && Time.frameCount % 90 == 0)
			{
				int aheadCount = CountAheadCarsGlobal();
				Debug.Log($"[TrafficSpawner] Đã đủ xe phía trước ({aheadCount}/{maxCarsActive}), không spawn.");
			}
			return;
		}

		TrySpawnOnce();
		nextSpawnTime = Time.time + spawnInterval;
	}

	bool CanSpawn()
	{
		return CountAheadCarsGlobal() < maxCarsActive;
	}

	float CalculateSpawnZ()
	{
		float farAhead = Mathf.Max(spawnDistanceAhead.x, spawnDistanceAhead.y);
		return player.position.z + Mathf.Max(farAhead, minSpawnDistanceFromPlayer);
	}

	int FindBestLane(float spawnZ, out float chosenSpeed)
	{
		chosenSpeed = 0f;

		int laneIndex = -1;
		float bestGap = -1f;

		for (int lane = 0; lane < laneCount; lane++)
		{
			if (!TryComputeLaneSpeedSafe(lane, spawnZ, out float speed, out float gapScore))
			{
				continue;
			}

			if (gapScore > bestGap)
			{
				bestGap = gapScore;
				laneIndex = lane;
				chosenSpeed = speed;
			}
		}

		return laneIndex;
	}

	void TrySpawnOnce()
	{
		if (!CanSpawn())
		{
			return;
		}

		float spawnZ = CalculateSpawnZ();
		int bestLane = FindBestLane(spawnZ, out float chosenSpeed);

		if (bestLane == -1)
		{
			if (logSpawnDebug && Time.frameCount % 30 == 0)
			{
				Debug.Log("[TrafficSpawner] Không tìm được làn an toàn để spawn.");
			}
			return;
		}

		TrafficCar spawned = SpawnAt(bestLane, spawnZ, chosenSpeed);
		if (CountAheadCarsGlobal() > maxCarsActive)
		{
			active.Remove(spawned);
			pool.Return(spawned);
			if (logSpawnDebug)
			{
				Debug.Log("[TrafficSpawner] Rollback spawn do vuot gioi han xe ahead toan scene.");
			}
		}
	}

	float GetRandomTrafficSpeed()
	{
		float pct = Random.Range(minSpeedPctOfPlayerMax, maxSpeedPctOfPlayerMax);
		return Mathf.Clamp(playerMaxSpeed * pct, 0f, playerMaxSpeed);
	}

	bool TryComputeLaneSpeedSafe(int lane, float spawnZ, out float chosenSpeed, out float gapScore)
	{
		chosenSpeed = 0f;
		gapScore = -1f;

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

		if (aheadDZ < minGapSameLane) return false;
		if (behindDZ < minGapSameLane) return false;

		float minAllowed = 0f;
		float maxAllowed = playerMaxSpeed;

		if (behind != null)
			minAllowed = Mathf.Max(minAllowed, behind.speed + speedEpsilon);

		if (ahead != null)
			maxAllowed = Mathf.Min(maxAllowed, ahead.speed - speedEpsilon);

		if (minAllowed > maxAllowed)
		{
			return false;
		}

		float baseSpeed = GetRandomTrafficSpeed();
		chosenSpeed = Mathf.Clamp(baseSpeed, minAllowed, maxAllowed);

		gapScore = Mathf.Min(aheadDZ, behindDZ);
		return true;
	}

	TrafficCar SpawnAt(int lane, float z, float speed)
	{
		float x = LaneToX(lane);

		TrafficCar car = pool.Get();
        car.transform.position = new Vector3(x, 1f, z);
        car.transform.rotation = Quaternion.identity;

		car.ResetState(lane, speed);
		active.Add(car);
		return car;
	}

	void DespawnPassed()
	{
		float cutoffBehindZ = player.position.z - clearPassedDistance;

		for (int i = active.Count - 1; i >= 0; i--)
		{
			var c = active[i];
			if (c == null) { active.RemoveAt(i); continue; }

			float z = c.transform.position.z;
			if (z < cutoffBehindZ)
			{
				active.RemoveAt(i);
				pool.Return(c);
			}
		}
	}

	int CountAheadCarsGlobal()
	{
		float playerZ = player.position.z;
		int count = 0;
		TrafficCar[] allCars = FindObjectsByType<TrafficCar>(FindObjectsSortMode.None);
		for (int i = 0; i < allCars.Length; i++)
		{
			TrafficCar c = allCars[i];
			if (c == null || !c.gameObject.activeSelf)
			{
				continue;
			}

			float dz = c.transform.position.z - playerZ;
			if (dz >= 0f)
			{
				count++;
			}
		}

		return count;
	}

    float LaneToX(int lane)
    {
        if (laneCount <= 1) return roadCenterX;

        float center = (laneCount - 1) * 0.5f; // 3 lanes -> 1, 2 lanes -> 0.5
        return roadCenterX + (lane - center) * laneOffset;
    }
}
