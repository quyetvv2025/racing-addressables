# AGENTS.md - PROMETEO Car Controller

This document provides coding guidelines and conventions for AI agents working with the PROMETEO Car Controller Unity asset package.

## Project Overview

**PROMETEO Car Controller** is a free Unity car physics controller asset designed for arcade-style racing games. It provides realistic car physics with WheelColliders, drifting mechanics, and support for both desktop and mobile platforms.

- **Language:** C# (pre-Unity 6, uses older APIs)
- **Architecture:** Component-based MonoBehaviour pattern
- **Key Features:** Wheel-based physics, drift mechanics, touch controls, visual effects
- **Scripts:** 3 core scripts (775 LOC total)
- **License:** Free to use in commercial/personal projects, cannot resell script itself

## Build, Test, and Run Commands

### Using PROMETEO in Unity

**Setup:**
1. Open Unity Editor
2. Navigate to `Assets/PROMETEO - Car Controller/Scenes/`
3. Open `Demo.unity` (desktop) or `Mobile Devices Demo.unity` (mobile)
4. Press Play to test

**Integration into existing project:**
1. Drag car prefab from `Assets/PROMETEO - Car Controller/Prefabs/` into scene
2. Assign wheel meshes and colliders in Inspector
3. Configure parameters in `PrometeoCarController` component
4. Add `PrometeoTouchInput` components to UI buttons for mobile

**No build commands** - This is a Unity asset package (integrated via Unity Editor)

### Testing

**Manual testing only:**
- Play mode in Unity Editor
- Test controls: WASD + Space (desktop) or touch buttons (mobile)
- Verify wheel rotation, steering, drifting effects
- Check UI speed display updates

**No automated test framework** configured for this asset

### Documentation

**PDF Documentation:**
- `Assets/PROMETEO - Car Controller/Documentation/Documentation (Prometeo).pdf`
- Contains setup instructions, parameter explanations, troubleshooting

## Code Style Guidelines

### File Organization

**Directory Structure:**
```
Assets/PROMETEO - Car Controller/
├── Scripts/
│   ├── PrometeoCarController.cs    # Main car physics controller
│   ├── PrometeoTouchInput.cs       # Touch input handler
│   └── (other scripts)
├── Editor/
│   └── PrometeoEditor.cs           # Custom Inspector UI
├── Prefabs/                        # Car prefabs
├── Materials/                      # Visual materials
├── Meshes/                         # 3D models
├── Effects/                        # Particle systems
├── Sounds/                         # Audio assets
└── Scenes/                         # Demo scenes
```

### C# Coding Conventions

**Naming:**
```csharp
public class PrometeoCarController        // PascalCase for classes
{
    public int maxSpeed = 90;             // camelCase for public fields
    private Rigidbody carRigidbody;       // camelCase for private fields
    
    public void GoForward() { }           // PascalCase for methods
}
```

**Field Organization:**
```csharp
// 1. Public configuration fields (grouped with [Space] attributes)
[Space(20)]
[Range(20, 190)]
public int maxSpeed = 90;

// 2. Public references (wheels, effects, UI)
public GameObject frontLeftMesh;
public WheelCollider frontLeftCollider;

// 3. HideInInspector public state
[HideInInspector]
public float carSpeed;

// 4. Private implementation fields
private Rigidbody carRigidbody;
private float steeringAxis;
```

**Range attributes for parameters:**
```csharp
[Range(20, 190)]
public int maxSpeed = 90;               // Constrain values in Inspector

[Range(0.1f, 1f)]
public float steeringSpeed = 0.5f;      // Use floats for precision
```

### Type Declarations

**Explicit types preferred:**
```csharp
// Good
WheelFrictionCurve FLwheelFriction = new WheelFrictionCurve();
float steeringAngle = steeringAxis * maxSteeringAngle;

// Used in code (acceptable for obvious types)
var steeringAngle = steeringAxis * maxSteeringAngle;
```

**Struct initialization:**
```csharp
// WheelFrictionCurve modification pattern
WheelFrictionCurve friction = new WheelFrictionCurve();
friction.extremumSlip = collider.sidewaysFriction.extremumSlip;
friction.extremumValue = collider.sidewaysFriction.extremumValue;
friction.stiffness = collider.sidewaysFriction.stiffness;
collider.sidewaysFriction = friction;  // Assign back to collider
```

**Null safety with try-catch:**
```csharp
// Pattern used in this codebase
try {
    if (carEngineSound != null) {
        carEngineSound.pitch = calculatedPitch;
    }
} catch(Exception ex) {
    Debug.LogWarning(ex);
}
```

### Formatting

**Indentation:** Spaces (not tabs) - appears to be 2-4 spaces

**Braces:** K&R style (opening brace on same line)
```csharp
// Good
public void TurnLeft(){
    steeringAxis = steeringAxis - (Time.deltaTime * 10f * steeringSpeed);
    if(steeringAxis < -1f){
        steeringAxis = -1f;
    }
}
```

**Spacing:**
```csharp
// No space after 'if' keyword (differs from typical C# style)
if(condition){
    DoSomething();
}

// Space around operators
steeringAxis = steeringAxis + (Time.deltaTime * 10f);
```

**Comments:**
```csharp
// Double-slash comments for sections
//
//STEERING METHODS
//

// Inline comments for explanations
// The following method turns the front car wheels to the left.
```

### Import/Using Statements

**Minimal imports:**
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // Only if using UI components
```

**Editor scripts:**
```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
```

### Unity-Specific Patterns

**MonoBehaviour lifecycle:**
```csharp
void Start()    // Initialize rigidbody, setup friction curves, InvokeRepeating
void Update()   // Handle input, physics calculations, wheel animations
```

**WheelCollider physics:**
```csharp
// Apply motor torque (acceleration)
frontLeftCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;

// Apply steering
frontLeftCollider.steerAngle = Mathf.Lerp(currentAngle, targetAngle, steeringSpeed);

// Apply brakes
frontLeftCollider.brakeTorque = brakeForce;

// Read wheel position/rotation
frontLeftCollider.GetWorldPose(out Vector3 position, out Quaternion rotation);
```

**Rigidbody manipulation:**
```csharp
// OLD Unity API (pre-Unity 6)
carRigidbody.velocity          // Use this for older Unity versions
carRigidbody.centerOfMass = bodyMassCenter;

// Deceleration formula
carRigidbody.velocity = carRigidbody.velocity * (1f / (1f + (0.025f * decelerationMultiplier)));
```

**Important:** This asset uses **old Unity API** (`velocity` instead of `linearVelocity`)

### Input Handling

**Dual input system:**
```csharp
// Desktop (keyboard)
if(Input.GetKey(KeyCode.W)){
    GoForward();
}

// Mobile (touch controls)
if(useTouchControls && throttlePTI.buttonPressed){
    GoForward();
}
```

**Touch input component:**
```csharp
// PrometeoTouchInput.cs - Attach to UI buttons
public void ButtonDown(){
    buttonPressed = true;  // Called by EventTrigger
}

public void ButtonUp(){
    buttonPressed = false;
}
```

### Effects and Audio

**Particle system control:**
```csharp
if(isDrifting){
    RLWParticleSystem.Play();
}else{
    RLWParticleSystem.Stop();
}
```

**Trail renderer control:**
```csharp
if(isTractionLocked && carSpeed > 12f){
    RLWTireSkid.emitting = true;
}else{
    RLWTireSkid.emitting = false;
}
```

**Audio pitch modulation:**
```csharp
// Engine sound pitch based on speed
float engineSoundPitch = initialPitch + (Mathf.Abs(carRigidbody.velocity.magnitude) / 25f);
carEngineSound.pitch = engineSoundPitch;
```

### Error Handling

**Try-catch for optional features:**
```csharp
try{
    if(carSpeedText != null){
        carSpeedText.text = Mathf.RoundToInt(carSpeed).ToString();
    }
}catch(Exception ex){
    Debug.LogWarning(ex);
}
```

**Validation in Start:**
```csharp
if(useTouchControls){
    if(throttleButton != null && reverseButton != null){
        touchControlsSetup = true;
    }else{
        Debug.LogWarning("Touch controls not completely set up.");
    }
}
```

### InvokeRepeating Pattern

**Repeated method calls:**
```csharp
// Start() initialization
InvokeRepeating("CarSpeedUI", 0f, 0.1f);    // Call every 0.1 seconds
InvokeRepeating("CarSounds", 0f, 0.1f);

// Cancel when needed
CancelInvoke("DecelerateCar");
```

## Custom Editor

### PrometeoEditor.cs Patterns

**SerializedProperty pattern:**
```csharp
private SerializedProperty maxSpeed;
private SerializedObject SO;

void OnEnable(){
    SO = new SerializedObject(target);
    maxSpeed = SO.FindProperty("maxSpeed");
}

public override void OnInspectorGUI(){
    SO.Update();
    
    // Custom UI
    maxSpeed.intValue = EditorGUILayout.IntSlider("Max Speed:", maxSpeed.intValue, 20, 190);
    
    SO.ApplyModifiedProperties();
}
```

**Toggle groups for optional features:**
```csharp
useEffects.boolValue = EditorGUILayout.BeginToggleGroup("Use effects?", useEffects.boolValue);
    EditorGUILayout.PropertyField(RLWParticleSystem, new GUIContent("Particle System: "));
EditorGUILayout.EndToggleGroup();
```

## Common Patterns in This Codebase

### Drifting mechanics:
```csharp
// Check if drifting (based on lateral velocity)
if(Mathf.Abs(localVelocityX) > 2.5f){
    isDrifting = true;
}

// Modify wheel friction to drift
FLwheelFriction.extremumSlip = baseExtremum * handbrakeDriftMultiplier * driftingAxis;
frontLeftCollider.sidewaysFriction = FLwheelFriction;
```

### Speed calculation:
```csharp
// Calculate car speed from wheel RPM
carSpeed = (2 * Mathf.PI * wheelRadius * wheelRPM * 60) / 1000;  // km/h
```

### Smooth steering:
```csharp
// Accumulate steering axis over time
steeringAxis = steeringAxis + (Time.deltaTime * 10f * steeringSpeed);
steeringAxis = Mathf.Clamp(steeringAxis, -1f, 1f);

// Apply with Lerp for smoothness
frontLeftCollider.steerAngle = Mathf.Lerp(currentAngle, targetAngle, steeringSpeed);
```

## Migration Notes

### Unity 6 Compatibility

If updating this asset for Unity 6, replace:
```csharp
// OLD (Unity 5.x - 2022)
carRigidbody.velocity
carRigidbody.velocity = newVelocity;

// NEW (Unity 6+)
carRigidbody.linearVelocity
carRigidbody.linearVelocity = newVelocity;
```

## License and Attribution

**From PrometeoCarController.cs header:**
```
MESSAGE FROM CREATOR: This script was coded by Mena. You can use it in your games 
either these are commercial or personal projects. You can even add or remove functions 
as you wish. However, you cannot sell copies of this script by itself, since it is 
originally distributed as a free product.
```

**When modifying:**
- ✅ Allowed: Use in commercial/personal games
- ✅ Allowed: Modify and extend functionality
- ❌ Not allowed: Sell the script itself

---

**Last Updated:** February 2026  
**Original Asset:** PROMETEO Car Controller by Mena  
**Compatibility:** Unity 5.x - 2022.x (requires API updates for Unity 6+)  
**For setup instructions:** See `Documentation/Documentation (Prometeo).pdf`
