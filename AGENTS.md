# AGENTS.md - Traffic Racing Unity Project

Guidelines for AI agents working on this Unity 6000.0.65f1 C# racing game.

## Project Overview

- **Game:** 3D mobile endless runner - avoid traffic on 3-lane road
- **Language:** C# 9.0 / .NET Standard 2.1
- **Platforms:** Android (primary), iOS, PC
- **Rendering:** URP 17.0.4, Input System 1.17.0
- **Scripts:** `Assets/Scripts/` (~11 files, ~1000 LOC)

## Build, Test, and Run Commands

### Building
Unity projects build through the Editor UI, not CLI:
1. Open in Unity Hub (6000.0.65f1)
2. `File > Build Settings` or use profiles in `Assets/Settings/Build Profiles/`
3. Select platform and click "Build"

### Testing
**Framework:** Unity Test Framework 1.6.0 (NUnit-based)

```bash
# Run all tests (EditMode)
Unity -runTests -testPlatform EditMode -projectPath .

# Run all tests (PlayMode)
Unity -runTests -testPlatform PlayMode -projectPath .

# Run single test class
Unity -runTests -testPlatform EditMode -testFilter "TestClassName"

# Run single test method
Unity -runTests -testPlatform EditMode -testFilter "TestClassName.TestMethodName"
```

**In Editor:** `Window > General > Test Runner` - right-click test to run individually.

**Test structure:** Create `Assets/Tests/EditMode/` and `Assets/Tests/PlayMode/` with `.asmdef` files.

### Linting
- **Static Analysis:** Microsoft.Unity.Analyzers (via `.csproj`)
- **Suppressed warnings:** `0169`, `USG0001`
- **IDE:** VS Code with Unity Tools extension, or JetBrains Rider

## Code Style Guidelines

### Naming Conventions
```csharp
public class PlayerCarMotor { }      // PascalCase: classes
public float maxSpeed = 45f;         // camelCase: public/private fields
public float MaxSpeed => maxSpeed;   // PascalCase: properties
public void MoveForward() { }        // PascalCase: methods
const float GRAVITY = 9.81f;         // UPPER_CASE: constants
```

### File Organization
```
Assets/Scripts/
  Player/    - PlayerCarMotor.cs, IPlayerInput.cs, *PlayerInput.cs
  Traffic/   - TrafficSpawner.cs, TrafficPool.cs, TrafficCar.cs
  Road/      - RoadManager.cs
  UI/        - ScoreManager.cs, HoldButton.cs
  Core/      - ChaseCamera.cs
```
- One public class per file, filename matches class name

### Field Ordering (in class)
1. Serialized fields with `[Header]` attributes
2. Private fields
3. Properties
4. Unity lifecycle (`Awake`, `Start`, `Update`, `FixedUpdate`, `LateUpdate`)
5. Public methods
6. Private methods

### Import Order
```csharp
using UnityEngine;         // 1. Unity namespaces
using TMPro;               // 2. Third-party
using System.Collections;  // 3. System namespaces
```

### Formatting
- **Indentation:** Tabs
- **Braces:** Always use, even for single-line blocks
- **Spacing:** Space after `if`/`for`/`while`, no space before method parens

```csharp
// Good
if (condition)
{
    DoSomething();
}

// Avoid
if (condition) DoSomething();
```

### Type Declarations
```csharp
// Prefer explicit types
Rigidbody rb = GetComponent<Rigidbody>();
float speed = 45f;

// var OK only when type is obvious
var car = GetComponent<TrafficCar>();
```

### Error Handling
```csharp
void Start()
{
    if (player == null || pool == null)
    {
        Debug.LogError("TrafficSpawner: player/pool not assigned.");
        enabled = false;  // Disable component
        return;
    }
}

// Null checks before use
if (scoreText != null)
    scoreText.text = $"SCORE: {score}";
```
- No try-catch - use `Debug.LogError` and disable components
- Validate references in `Start()`/`Awake()`

### Unity Patterns
```csharp
// Cache components in Awake
private Rigidbody rb;
void Awake() { rb = GetComponent<Rigidbody>(); }

// Physics in FixedUpdate only
void FixedUpdate()
{
    rb.linearVelocity = newVelocity;  // Unity 6+ API
    rb.AddForce(force, ForceMode.Acceleration);
}

// Inspector references with Header
[Header("References")]
public Transform player;
public TrafficPool pool;
```

### Logging
```csharp
Debug.Log($"[ClassName] message: {value}");
Debug.LogWarning("[ClassName] warning");
Debug.LogError("ClassName: critical error");

// Periodic debug (every N frames)
if (Time.frameCount % 60 == 0)
    Debug.Log($"[PlayerCarMotor] Speed: {speed:F2}");
```

### Collections & Performance
```csharp
// Readonly collections, expose as IReadOnly
private readonly List<TrafficCar> active = new List<TrafficCar>();
public IReadOnlyList<TrafficCar> ActiveCars => active;

// Object pooling (see TrafficPool.cs)
TrafficCar car = pool.Get();
pool.Return(car);

// Avoid per-frame allocations
for (int i = 0; i < list.Count; i++) { }  // Not foreach or LINQ
```

## Physics & Layers

**Configured Layers:**
- 6: `Player`, 7: `Traffic`, 8: `Wall`, 9: `Road`

```csharp
public LayerMask roadLayer = ~0;
Physics.Raycast(origin, Vector3.down, out hit, distance, roadLayer);

// Rigidbody setup
rb.interpolation = RigidbodyInterpolation.Interpolate;
rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
```

## Common Patterns

```csharp
// Interface-based input
public interface IPlayerInput { float Throttle { get; } float Steer { get; } }

// String interpolation for UI
scoreText.text = $"SCORE: {score}";
speedText.text = $"SPEED: {kmh:0} km/h";

// Smooth transitions
value = Mathf.Lerp(value, target, 1f - Mathf.Exp(-speed * Time.deltaTime));
```

---
**Unity Version:** 6000.0.65f1 | **Last Updated:** February 2026
