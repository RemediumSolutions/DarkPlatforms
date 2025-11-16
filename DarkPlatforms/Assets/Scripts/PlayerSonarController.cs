using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider), typeof(PlayerInput))]
public class PlayerSonarController : MonoBehaviour
{
    [Header("=== Movement ===")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("=== Ground Check ===")]
    public float groundCheckDistance = 0.6f;
    public LayerMask groundLayer;

    [Header("=== Jump Sound ===")]
    public AudioClip jumpClip;

    [Header("=== Sonar System ===")]
    public AudioClip pingClip;
    public float maxRange = 15f;
    public float speedOfSound = 343f;
    public int raysPerPing = 72;
    public float fanAngle = 180f;
    public bool scanXZ = true;

    // Private
    private Rigidbody rb;
    private AudioSource sfxSource;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction echoAction;  // ← NEW: Direct reference
    private Vector2 moveInput;
    private bool isGrounded;
    private Transform listener;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.spatialBlend = 0f;

        playerInput = GetComponent<PlayerInput>();
        playerInput.defaultActionMap = "Player";
        playerInput.SwitchCurrentActionMap("Player");

        // CRITICAL: Cache ACTIONS DIRECTLY
        moveAction = playerInput.actions["Move"];
        echoAction = playerInput.actions["Echo"];  // ← GET ECHO ACTION

        // FORCE ENABLE ALL (fixes 90% of issues)
        moveAction?.Enable();
        echoAction?.Enable();

        listener = Camera.main?.transform;

        // *** THE MAGIC: Subscribe DIRECTLY to events ***
        playerInput.onActionTriggered += OnActionTriggered;

        Debug.Log("[PlayerSonarController] ✅ FULLY ARMED – Press E for PING!");
    }

    private void OnActionTriggered(InputAction.CallbackContext context)
    {
        // Movement (value change)
        if (context.action.name == "Move")
        {
            moveInput = context.ReadValue<Vector2>();
        }
        // JUMP (performed = pressed)
        else if (context.action.name == "Jump" && context.performed)
        {
            if (isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                if (jumpClip != null) sfxSource.PlayOneShot(jumpClip);
                Debug.Log("[Player] JUMP!");
            }
        }
        // ECHO/PING (performed = pressed)
        else if (context.action.name == "Echo" && context.performed)
        {
            Debug.Log("[Sonar] 🎵 PING TRIGGERED! (E pressed)");
            TriggerPing();
        }
    }

    void Update()
    {
        // Apply movement
        Vector3 vel = rb.linearVelocity;
        vel.x = moveInput.x * moveSpeed;
        rb.linearVelocity = vel;

        // Ground check
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    private void TriggerPing()
    {
        if (pingClip != null) sfxSource.PlayOneShot(pingClip);
        StartCoroutine(PerformPing());
    }

    IEnumerator PerformPing()
    {
        Debug.Log($"[Sonar] Scanning {raysPerPing} rays...");
        float startAngle = -fanAngle * 0.5f;
        float step = fanAngle / (raysPerPing - 1);

        for (int i = 0; i < raysPerPing; i++)
        {
            float angle = startAngle + i * step;
            Vector3 dir = scanXZ
                ? Quaternion.Euler(0, 0, angle) * Vector3.right
                : Quaternion.Euler(angle, 0, 0) * Vector3.forward;

            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, maxRange))
            {
                if (hit.collider.TryGetComponent<EchoResponder>(out var responder))
                {
                    float dist = hit.distance;
                    float delay = dist * 2f / speedOfSound;  // to and back

                    // CORRECT: Use hit.point and hit.normal
                    responder.TriggerEcho(delay, listener.position, hit.point, hit.normal);

                    Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.yellow, 1f); // Visual debug
                }
            }
        }
        yield return null;
    }

    void OnDestroy()
    {
        playerInput.onActionTriggered -= OnActionTriggered;  // Cleanup
    }

    // Gizmos unchanged...
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);

        Gizmos.color = Color.cyan;
        float startAngle = -fanAngle * 0.5f;
        float step = fanAngle / (raysPerPing - 1);
        for (int i = 0; i < raysPerPing; i++)
        {
            float angle = startAngle + i * step;
            Vector3 dir = scanXZ
                ? Quaternion.Euler(0, 0, angle) * Vector3.right
                : Quaternion.Euler(angle, 0, 0) * Vector3.forward;
            Gizmos.DrawRay(transform.position, dir * maxRange);
        }
    }
}