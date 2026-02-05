using UnityEngine;

public class PlayerCarMotor : MonoBehaviour
{
    [Header("References")]
    public MonoBehaviour inputSource; // must implement IPlayerInput
    private IPlayerInput input;
    private Rigidbody rb;

    [Header("Speed")]
    public float maxSpeed = 45f;          // m/s  (~162 km/h)
    public float accel = 18f;             // m/s^2
    public float brakeDecel = 28f;        // m/s^2
    public float naturalDecel = 6f;       // when no throttle

    [Header("Steering")]
    public float lateralSpeed = 10f;      // m/s sideways at low speed
    public float steerStrength = 1f;      // multiplier
    public AnimationCurve steerBySpeed = AnimationCurve.EaseInOut(0, 1f, 1f, 0.35f);
    // x = normalized speed (0..1), y = steer factor

    [Header("Clamp to Road")]
    public float clampHalfWidth = 5.5f;   // road half width minus padding
    public float clampPadding = 0.2f;

    [Header("Stability")]
    public float extraDownforce = 20f;    // helps keep on road (giảm từ 30 → 20)
    [Tooltip("Tắt Unity gravity và dùng custom gravity. Bật nếu xe vẫn bounce.")]
    public bool useCustomGravity = false;  // Mặc định bật
    public float customGravity = 15f;     // Custom gravity force (mạnh hơn 9.81)

    [Header("Ground Detection")]
    [Tooltip("Layer của road. Nếu chưa setup layer, để default.")]
    public LayerMask roadLayer = ~0;           // layer của road (default: all layers)
    public float groundCheckDistance = 3f;     // khoảng cách raycast xuống (tăng từ 2 → 3)
    public float groundStickForce = 80f;       // lực kéo xuống khi xe bay lên (tăng từ 50 → 80)
    public float minGroundDistance = 0.8f;     // khoảng cách tối thiểu từ ground (tăng từ 0.5 → 0.8)
    public float maxBounceVelocity = 0.1f;     // velocity.y tối đa khi ở ground (mới thêm)

    [Header("Collision Detection")]
    [Tooltip("Layer của traffic cars. Nếu chưa setup layer, để default.")]
    public LayerMask carLayer = ~0;            // layer của traffic cars (default: all layers)
    public float collisionCheckRadius = 2f;    // bán kính check va chạm

    private bool isCollidingWithCar = false;

    float targetForwardSpeed;
    Vector3 startPos;

    public float minSpeed = 15f; // luôn chạy tối thiểu 15

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;  // Ngăn penetration với tốc độ cao
		rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        
        // Tắt Unity gravity nếu dùng custom gravity
        if (useCustomGravity)
        {
            rb.useGravity = false;
            Debug.Log("[PlayerCarMotor] Using custom gravity: " + customGravity);
        }
        
        startPos = transform.position;

        input = inputSource as IPlayerInput;
        if (input == null)
        {
            Debug.LogError("PlayerCarMotor: inputSource must implement IPlayerInput.");
        }

        targetForwardSpeed = 15f; // base forward speed so car always moves
        
        // Setup physics material for car
        // - Bounce = 0.3 để cho phép bounce khi va chạm xe-xe
        // - BounceCombine = Average để xe-xe bounce với nhau
        // - Khi combine với Road (bounce=0, combine=Minimum) → kết quả = 0 (không bounce)
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            PhysicsMaterial carMaterial = new PhysicsMaterial("CarPhysics");
            carMaterial.dynamicFriction = 0f;
            carMaterial.staticFriction = 0f;
            carMaterial.bounciness = 0.3f;  // Có bounce cho va chạm xe-xe
            carMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
            carMaterial.bounceCombine = PhysicsMaterialCombine.Average;  // Average để xe-xe bounce
            col.material = carMaterial;
        }
    }

    void FixedUpdate()
    {
        if (input == null)
        {
            if (Time.frameCount % 120 == 0)
                Debug.LogWarning("[PlayerCarMotor] input is NULL! Cannot move car.");
            return;
        }

        // 1) Forward speed control
        float throttle = input.Throttle;
        float brake = input.Brake;

        float curForward = Vector3.Dot(rb.linearVelocity, Vector3.forward);
        
        // Debug every 60 fixed frames
        if (Time.frameCount % 60 == 0)
        {
            bool grounded = IsGrounded(out float dist);
            bool nearCar = IsNearOtherCar();
            Debug.Log($"[PlayerCarMotor] Throttle={throttle:F2}, Brake={brake:F2}, curSpeed={curForward:F2}, targetSpeed={targetForwardSpeed:F2}, velocity={rb.linearVelocity}, " +
                      $"velocity.y={rb.linearVelocity.y:F3}, grounded={grounded}, dist={dist:F2}, nearCar={nearCar}, colliding={isCollidingWithCar}");
        }

        if (throttle > 0f)
            targetForwardSpeed += accel * throttle * Time.fixedDeltaTime;
        else
            targetForwardSpeed -= naturalDecel * Time.fixedDeltaTime;

        if (brake > 0f)
            targetForwardSpeed -= brakeDecel * brake * Time.fixedDeltaTime;

        targetForwardSpeed = Mathf.Clamp(targetForwardSpeed, minSpeed, maxSpeed);

        // Approach target speed smoothly
        float newForward = Mathf.MoveTowards(curForward, targetForwardSpeed, accel * Time.fixedDeltaTime);

        // 2) Lateral steering (analog)
        float steer = input.Steer; // -1..1
        float speed01 = (maxSpeed <= 0.01f) ? 0f : Mathf.Clamp01(Mathf.Abs(newForward) / maxSpeed);
        float steerFactor = steerBySpeed.Evaluate(speed01) * steerStrength;

        float desiredLateral = steer * lateralSpeed * steerFactor;

        // Compose new velocity (keep current Y from physics)
        Vector3 v = rb.linearVelocity;
        v.z = newForward;
        v.x = desiredLateral;
        rb.linearVelocity = v;

        // 3) Ground stability - CHỈ khi KHÔNG va chạm xe
        if (!isCollidingWithCar && !IsNearOtherCar())
        {
            if (IsGrounded(out float distToGround))
            {
                // A) Xe đang bay lên (velocity.y > threshold) → kéo xuống MẠNH
                if (rb.linearVelocity.y > maxBounceVelocity)
                {
                    rb.AddForce(Vector3.down * groundStickForce, ForceMode.Acceleration);
                    
                    // DẬP NGAY velocity.y về maxBounceVelocity
                    Vector3 vel = rb.linearVelocity;
                    vel.y = Mathf.Min(vel.y, maxBounceVelocity);
                    rb.linearVelocity = vel;
                }
                
                // B) Quá gần ground → DẬP MẠNH velocity.y về 0
                if (distToGround < minGroundDistance)
                {
                    Vector3 vel = rb.linearVelocity;
                    
                    // Nếu velocity.y dương (đang bay lên) → set = 0 ngay
                    if (vel.y > 0f)
                    {
                        vel.y = 0f;
                    }
                    // Nếu velocity.y âm nhỏ (đang rơi nhẹ) → lerp về 0
                    else if (vel.y > -2f)
                    {
                        vel.y = Mathf.Lerp(vel.y, 0f, 0.5f);
                    }
                    // Nếu đang rơi mạnh (vel.y < -2) → giữ nguyên, để gravity xử lý
                    
                    rb.linearVelocity = vel;
                }
            }
        }

        // Apply gravity
        if (useCustomGravity)
        {
            // Custom gravity - mạnh hơn để xe dính road
            rb.AddForce(Vector3.down * customGravity, ForceMode.Acceleration);
        }
        else
        {
            // Downforce nhẹ (khi dùng Unity gravity)
            rb.AddForce(Vector3.down * extraDownforce * 0.5f, ForceMode.Acceleration);
        }

        // Reset collision flag mỗi frame
        isCollidingWithCar = false;

        // 4) Clamp X to road bounds (avoid flying off road)
        Vector3 p = rb.position;
        float limit = Mathf.Max(0f, clampHalfWidth - clampPadding);
        
        // Only apply clamp if we are actually out of bounds or about to be?
        // Checking current position is safer.
        if (p.x < startPos.x - limit || p.x > startPos.x + limit)
        {
             p.x = Mathf.Clamp(p.x, startPos.x - limit, startPos.x + limit);
             rb.position = p;
             
             // Optionally kill lateral velocity to stop pushing into wall
             Vector3 currentV = rb.linearVelocity;
             currentV.x = 0f; 
              rb.linearVelocity = currentV;
         }
    }

    /// <summary>
    /// Kiểm tra xe có đang chạm đất không, trả về khoảng cách
    /// Raycast từ nhiều điểm để chắc chắn hơn
    /// </summary>
    bool IsGrounded(out float distanceToGround)
    {
        Vector3 origin = transform.position;
        RaycastHit hit;
        
        // Raycast từ center
        if (Physics.Raycast(origin, Vector3.down, out hit, groundCheckDistance, roadLayer))
        {
            distanceToGround = hit.distance;
            
            // Vẽ debug ray (màu xanh = hit ground)
            Debug.DrawRay(origin, Vector3.down * hit.distance, Color.green);
            
            return true;
        }
        
        // Nếu center miss, thử raycast từ 4 góc xe
        float offset = 0.5f;  // khoảng cách từ center ra góc
        Vector3[] corners = new Vector3[]
        {
            origin + new Vector3(offset, 0, offset),    // front-right
            origin + new Vector3(-offset, 0, offset),   // front-left
            origin + new Vector3(offset, 0, -offset),   // back-right
            origin + new Vector3(-offset, 0, -offset)   // back-left
        };
        
        float minDist = groundCheckDistance;
        bool foundGround = false;
        
        foreach (var corner in corners)
        {
            if (Physics.Raycast(corner, Vector3.down, out hit, groundCheckDistance, roadLayer))
            {
                if (hit.distance < minDist)
                {
                    minDist = hit.distance;
                    foundGround = true;
                }
                Debug.DrawRay(corner, Vector3.down * hit.distance, Color.yellow);
            }
        }
        
        if (foundGround)
        {
            distanceToGround = minDist;
            return true;
        }
        
        // Vẽ debug ray (màu đỏ = miss)
        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, Color.red);
        
        distanceToGround = groundCheckDistance;
        return false;
    }

    /// <summary>
    /// Kiểm tra có xe nào gần không (đang va chạm hoặc sắp va chạm)
    /// </summary>
    bool IsNearOtherCar()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position, 
            collisionCheckRadius, 
            carLayer
        );
        
        // Nếu tìm thấy collider nào (ngoài chính xe này)
        foreach (var hit in hits)
        {
            if (hit.gameObject != this.gameObject)
                return true;
        }
        
        return false;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Nếu va chạm với xe khác
        if (((1 << collision.gameObject.layer) & carLayer) != 0)
        {
            isCollidingWithCar = true;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & carLayer) != 0)
        {
            isCollidingWithCar = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & carLayer) != 0)
        {
            // Chỉ set false nếu không còn xe nào gần
            isCollidingWithCar = IsNearOtherCar();
        }
    }
}
