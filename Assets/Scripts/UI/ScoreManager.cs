using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Rigidbody playerRb;
    public TrafficSpawner spawner;

    [Header("Scoring")]
    public float passOffsetZ = 2.0f;
    public int score;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text speedText;
    public TMP_Text distanceText;

    [Header("Speed Display")]
    public bool showKmh = true;
    public float speedSmooth = 10f;

    [Header("Distance Display")]
    public bool distanceUseKmOver1000m = true;
    public float distanceSmooth = 8f;

    float speedSmoothed;
    float distanceSmoothed;

    float startZ;
    float bestDistance;

    void Awake()
    {
        // ScoreManager initialized
    }

    void Start()
    {
        Debug.Log($"ScoreManager Start - player: {player}, spawner: {spawner}, playerRb: {playerRb}");
        Debug.Log($"ScoreManager Start - scoreText: {scoreText}, speedText: {speedText}, distanceText: {distanceText}");
        
        if (player == null || spawner == null)
        {
            Debug.LogError("ScoreManager: player/spawner not assigned.");
            enabled = false;
            return;
        }
        if (playerRb == null) playerRb = player.GetComponent<Rigidbody>();

        startZ = player.position.z;
        bestDistance = 0f;
        
        Debug.Log($"ScoreManager initialized - startZ: {startZ}");

        RefreshScoreUI();
        RefreshSpeedUI(0f);
        RefreshDistanceUI(0f);
    }

    void Update()
    {
        UpdateScore();
        UpdateSpeedUI();
        UpdateDistanceUI();
        
        // Debug every 60 frames
        if (Time.frameCount % 60 == 0 && playerRb != null && player != null)
        {
            float speedMs = Vector3.Dot(playerRb.linearVelocity, Vector3.forward);
            float dist = player.position.z - startZ;
            Debug.Log($"[ScoreManager] Speed: {speedMs:F2} m/s ({speedMs*3.6f:F1} km/h), Distance: {dist:F1} m, PlayerZ: {player.position.z:F2}");
        }
    }

    void UpdateScore()
    {
        var cars = spawner.ActiveCars;
        float pz = player.position.z;

        for (int i = 0; i < cars.Count; i++)
        {
            var c = cars[i];
            if (c == null || !c.gameObject.activeSelf) continue;
            if (c.scored) continue;

            if (pz > c.transform.position.z + passOffsetZ)
            {
                c.scored = true;
                score += 1;
                RefreshScoreUI();
            }
        }
    }

    void UpdateSpeedUI()
    {
        if (speedText == null || playerRb == null) return;

        // Highway straight: forward is world Z
        float speedMs = Mathf.Max(0f, Vector3.Dot(playerRb.linearVelocity, Vector3.forward));

        speedSmoothed = Mathf.Lerp(speedSmoothed, speedMs, 1f - Mathf.Exp(-speedSmooth * Time.deltaTime));
        RefreshSpeedUI(speedSmoothed);
    }

    void UpdateDistanceUI()
    {
        if (distanceText == null || player == null) return;

        float rawDist = Mathf.Max(0f, player.position.z - startZ); // meters if 1 unit = 1 meter

        // Smooth display only (not smoothing physics)
        distanceSmoothed = Mathf.Lerp(distanceSmoothed, rawDist, 1f - Mathf.Exp(-distanceSmooth * Time.deltaTime));

        if (rawDist > bestDistance) bestDistance = rawDist;

        RefreshDistanceUI(distanceSmoothed);
    }

    void RefreshScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"SCORE: {score}";
    }

    void RefreshSpeedUI(float speedMs)
    {
        if (speedText == null) return;

        if (showKmh)
        {
            float kmh = speedMs * 3.6f;
            speedText.text = $"SPEED: {kmh:0} km/h";
        }
        else
        {
            speedText.text = $"SPEED: {speedMs:0.0} m/s";
        }
    }

    void RefreshDistanceUI(float meters)
    {
        if (distanceText == null) return;

        if (distanceUseKmOver1000m && meters >= 1000f)
        {
            float km = meters / 1000f;
            distanceText.text = $"DIST: {km:0.00} km";
        }
        else
        {
            distanceText.text = $"DIST: {meters:0} m";
        }
    }

    public void ResetRunUI()
    {
        score = 0;
        speedSmoothed = 0f;
        distanceSmoothed = 0f;

        startZ = player != null ? player.position.z : 0f;

        RefreshScoreUI();
        RefreshSpeedUI(0f);
        RefreshDistanceUI(0f);
    }

    public float GetBestDistance() => bestDistance;
}
