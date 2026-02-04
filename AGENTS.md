# AGENTS.md - Traffic Racing Unity Project

This document provides coding guidelines and conventions for AI agents working on the Traffic Racing Unity game project.

## Project Overview

**Traffic Racing** is a 3D mobile endless runner racing game built with Unity 6000.0.65f1. Players control a car avoiding traffic on a 3-lane endless road, collecting score by passing traffic cars.

- **Language:** C# 9.0
- **Target:** .NET Standard 2.1
- **Build Targets:** Android (primary), iOS, PC
- **Rendering:** Universal Render Pipeline (URP) 17.0.4
- **Input:** Unity Input System 1.17.0
- **Codebase Size:** ~1,000 LOC (11 custom scripts)

## Build, Test, and Run Commands

### Building in Unity Editor

Unity projects are typically built through the Unity Editor UI, not command line:

1. Open project in Unity Hub (Unity 6000.0.65f1)
2. `File > Build Settings` or use Build Profiles in `Assets/Settings/Build Profiles/`
3. Select platform (Android, iOS, or PC)
4. Click "Build" or "Build and Run"

**Build profiles configured:**
- Mobile (Android/iOS) - Uses IL2CPP scripting backend
- PC - Development builds

**Build artifacts:**
- Android: `.apk` or `.aab` files
- iOS: Xcode project in `build_ios2/`

### Testing

**Test Framework:** Unity Test Framework 1.6.0 (NUnit-based)

**Note:** No test files currently exist in the project. To add tests:

1. Create test directories:
   ```
   Assets/Tests/EditMode/     # Editor/unit tests
   Assets/Tests/PlayMode/     # Runtime/integration tests
   ```

2. Add assembly definition files (.asmdef) referencing:
   - `UnityEngine.TestRunner`
   - `UnityEditor.TestRunner` (for EditMode)
   - `nunit.framework`

3. Run tests via Unity Editor:
   - `Window > General > Test Runner`
   - Or use Unity command line: `Unity -runTests -testPlatform EditMode`

**To run a single test:**
- In Test Runner window: Right-click test > "Run"
- Command line not commonly used for single tests in Unity

### Linting / Code Analysis

**Static Analysis:**
- Microsoft.Unity.Analyzers (configured in `.csproj`)
- Unity Source Generators (automatic)
- Warnings suppressed: `0169` (unused field), `USG0001`

**No custom linting tools** (no ESLint/Prettier equivalent configured)

**IDE Integration:**
- Visual Studio Code with "Visual Studio Tools for Unity" extension
- JetBrains Rider support available
- IntelliSense/code completion enabled

### Logging & Debugging

**Console logging patterns:**
```csharp
// Debug logging (removed in production builds)
Debug.Log($"[ClassName] message with {variable}");
Debug.LogWarning("[ClassName] warning message");
Debug.LogError("ClassName: error message");

// Periodic debugging (every N frames)
if (Time.frameCount % 60 == 0)
    Debug.Log($"[ClassName] Status: {value:F2}");
```

**Visual debugging:**
```csharp
Debug.DrawRay(origin, direction * distance, Color.green);  // Scene view only
```

## Code Style Guidelines

### File Organization

**Directory Structure:**
```
Assets/Scripts/
├── Player/         # Player car controls
├── Traffic/        # Traffic spawning and AI
├── Road/           # Road generation
├── UI/             # UI managers
└── Core/           # Core systems (camera, etc.)
```

**Naming Conventions:**
- Scripts: `PascalCase.cs` (e.g., `PlayerCarMotor.cs`)
- One public class per file
- File name matches primary class name

### C# Coding Conventions

**Naming:**
```csharp
public class PlayerCarMotor        // PascalCase for classes
{
    public float maxSpeed = 45f;   // camelCase for public fields
    private Rigidbody rb;          // camelCase for private fields
    
    public float MaxSpeed => maxSpeed;  // PascalCase for properties
    
    public void MoveForward() { }  // PascalCase for methods
    
    const float GRAVITY = 9.81f;   // UPPER_CASE for constants (if used)
}
```

**Field Ordering:**
1. Serialized fields (with `[Header]` attributes)
2. Private fields
3. Properties
4. Unity lifecycle methods (`Awake`, `Start`, `Update`, `FixedUpdate`)
5. Public methods
6. Private methods

**Header Attributes:**
```csharp
[Header("References")]
public Transform player;
public Rigidbody playerRb;

[Header("Speed Settings")]
public float maxSpeed = 45f;
public float acceleration = 18f;

[Tooltip("Custom gravity force applied to car")]
public float customGravity = 15f;
```

### Type Declarations

**Prefer explicit types:**
```csharp
// Good
Rigidbody rb = GetComponent<Rigidbody>();
float speed = 45f;

// Avoid
var rb = GetComponent<Rigidbody>();  // OK for obvious types
var x = CalculateComplexValue();    // Avoid when type unclear
```

**Null safety:**
```csharp
// Early validation in Awake/Start
if (player == null)
{
    Debug.LogError("PlayerCarMotor: player not assigned.");
    enabled = false;  // Disable component
    return;
}

// Null checks before use
if (input != null)
{
    float throttle = input.Throttle;
}
```

**Collections:**
```csharp
// Use readonly when possible
private readonly List<TrafficCar> active = new List<TrafficCar>();
public IReadOnlyList<TrafficCar> ActiveCars => active;
```

### Formatting

**Indentation:** Tabs (as per `.csproj` generation)

**Braces:** Always use braces, even for single-line blocks
```csharp
// Good
if (condition)
{
    DoSomething();
}

// Avoid
if (condition) DoSomething();
```

**Spacing:**
```csharp
// Space after control flow keywords
if (condition) { }
for (int i = 0; i < count; i++) { }
while (running) { }

// No space for method calls
DoSomething(param1, param2);
```

### Import/Using Statements

**Order:**
1. Unity namespaces
2. System namespaces
3. Third-party namespaces
4. Project namespaces

**Example:**
```csharp
using UnityEngine;
using TMPro;
using System.Collections.Generic;
```

**Avoid unused imports** (IDE will gray them out)

### Unity-Specific Patterns

**MonoBehaviour lifecycle:**
```csharp
void Awake()    // Component initialization, GetComponent calls
void Start()    // Scene-level initialization, requires other objects ready
void Update()   // Per-frame logic, input handling, UI updates
void FixedUpdate()  // Physics updates, Rigidbody manipulation
void LateUpdate()   // Camera following, after all Updates
```

**Rigidbody manipulation:**
```csharp
// Always use in FixedUpdate
void FixedUpdate()
{
    rb.linearVelocity = newVelocity;  // Unity 6+ API
    rb.AddForce(force, ForceMode.Acceleration);
}
```

**Component references:**
```csharp
// Cache in Awake/Start
private Rigidbody rb;
void Awake() { rb = GetComponent<Rigidbody>(); }

// Public references assigned in Inspector
[Header("References")]
public Transform player;
```

**Physics materials:**
```csharp
// Create at runtime when needed
PhysicsMaterial mat = new PhysicsMaterial("CarPhysics");
mat.bounciness = 0.3f;
mat.frictionCombine = PhysicsMaterialCombine.Minimum;
GetComponent<Collider>().material = mat;
```

### Error Handling

**Validation approach:**
```csharp
void Start()
{
    if (player == null || spawner == null)
    {
        Debug.LogError("ScoreManager: player/spawner not assigned.");
        enabled = false;  // Disable component to prevent errors
        return;
    }
}
```

**Null propagation:**
```csharp
// Safe UI updates
if (scoreText != null)
    scoreText.text = $"SCORE: {score}";
```

**No try-catch blocks** in current codebase - Unity uses error logging instead

### Comments and Documentation

**XML documentation for public APIs:**
```csharp
/// <summary>
/// Checks if car is grounded, returns distance to ground
/// </summary>
bool IsGrounded(out float distanceToGround)
```

**Inline comments for complex logic:**
```csharp
// Apply gravity
if (useCustomGravity)
{
    // Custom gravity - stronger to keep car on road
    rb.AddForce(Vector3.down * customGravity, ForceMode.Acceleration);
}
```

**Use Vietnamese for internal documentation when appropriate** (as seen in SETUP_GUIDE.md)

## Unity-Specific Considerations

### Layers and Tags

**Configured Layers:**
- Layer 6: `Player`
- Layer 7: `Traffic`
- Layer 8: `Wall`
- Layer 9: `Road`

**LayerMask usage:**
```csharp
public LayerMask roadLayer = ~0;  // Default: all layers
public LayerMask carLayer = ~0;

// In Physics.Raycast
Physics.Raycast(origin, direction, out hit, distance, roadLayer);
```

### Physics Configuration

**Important settings (ProjectSettings/DynamicsManager.asset):**
- Gravity: -9.81 (standard)
- Custom gravity system implemented in `PlayerCarMotor`
- Collision matrix configured for Player/Traffic/Road interactions

**Rigidbody setup:**
```csharp
rb.interpolation = RigidbodyInterpolation.Interpolate;
rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                 RigidbodyConstraints.FreezeRotationZ;
```

### Performance Patterns

**Object pooling:**
```csharp
// See TrafficPool.cs for implementation
TrafficCar car = pool.Get();    // Reuse instead of Instantiate
pool.Return(car);               // Return instead of Destroy
```

**Avoid per-frame allocations:**
```csharp
// Good: reuse list
private readonly List<TrafficCar> active = new List<TrafficCar>();

// Avoid: new allocation per frame
for (int i = 0; i < spawner.ActiveCars.Count; i++)  // Use Count, not .ToArray()
```

## Common Patterns in This Codebase

### Interface-based input abstraction:
```csharp
public interface IPlayerInput
{
    float Throttle { get; }
    float Brake { get; }
    float Steer { get; }
}
```

### String interpolation for UI:
```csharp
scoreText.text = $"SCORE: {score}";
speedText.text = $"SPEED: {kmh:0} km/h";  // Format specifiers
distanceText.text = $"DIST: {meters:0} m";
```

### Smooth value transitions:
```csharp
// Exponential smoothing
speedSmoothed = Mathf.Lerp(speedSmoothed, target, 
                           1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
```

---

**Last Updated:** February 2026  
**Unity Version:** 6000.0.65f1  
**For questions or updates:** See README.md or SETUP_GUIDE.md
