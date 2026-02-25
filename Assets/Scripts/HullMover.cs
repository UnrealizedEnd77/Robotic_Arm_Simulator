using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ArticulationBody))]
public class HullMover : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;            // top forward/back speed (m/s)
    public float moveAcceleration = 10f;   // how fast it reaches top speed
    public float moveDamping = 8f;         // how fast it stops

    [Header("Rotation")]
    public float maxTurnSpeed = 90f;       // top yaw speed (deg/s)
    public float turnAcceleration = 15f;   // how fast yaw ramps up
    public float turnDamping = 10f;        // how fast yaw stops

    [Header("Stability")]
    [Tooltip("How low to place the center of mass (negative = lower)")]
    public float centerOfMassY = -0.5f;

    private ArticulationBody hullBody;
    private PlayerInput playerInput;
    private InputAction moveAction;

    private float currentSpeed = 0f;
    private float currentTurnSpeed = 0f;

    void Awake()
    {
        hullBody = GetComponent<ArticulationBody>();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];

        // Lock center of mass low so the tank can't flip
        hullBody.automaticCenterOfMass = false;
        hullBody.centerOfMass = new Vector3(0f, centerOfMassY, 0f);

        // Lock the inertia tensor so child bodies don't shift it
        hullBody.automaticInertiaTensor = false;
    }

    void FixedUpdate()
    {
        if (moveAction == null) return;

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        float dt = Time.fixedDeltaTime;

        // ── Forward / Back ──────────────────────────────────────────
        float targetSpeed = moveInput.y * maxSpeed;
        if (Mathf.Abs(moveInput.y) > 0.01f)
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, moveAcceleration * dt);
        else
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, moveDamping * dt);

        // ── Yaw (A / D) ────────────────────────────────────────────
        float targetTurn = moveInput.x * maxTurnSpeed;
        if (Mathf.Abs(moveInput.x) > 0.01f)
            currentTurnSpeed = Mathf.MoveTowards(currentTurnSpeed, targetTurn, turnAcceleration * dt);
        else
            currentTurnSpeed = Mathf.MoveTowards(currentTurnSpeed, 0f, turnDamping * dt);

        // ── Apply velocity directly (immune to child-body reactions) ──
        // Keep existing vertical velocity (gravity), replace horizontal
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 desiredHorizontal = flatForward * currentSpeed;

        Vector3 vel = hullBody.linearVelocity;
        vel.x = desiredHorizontal.x;
        vel.z = desiredHorizontal.z;
        hullBody.linearVelocity = vel;

        // ── Apply angular velocity directly ──────────────────────────
        // Only allow yaw rotation; kill pitch and roll entirely
        float yawRad = currentTurnSpeed * Mathf.Deg2Rad;
        hullBody.angularVelocity = Vector3.up * yawRad;
    }
}