using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerSonarController_Fallback : MonoBehaviour
{
    public float moveSpeed = 6f;

    public AudioClip jumpClip;
    public AudioClip pingClip;

    // 🔍 DEBUG COUNTERS
    [Header("🕵️ DEBUG INFO")]
    [SerializeField] private int pingCount = 0;
    [SerializeField] private int hitCount = 0;
    [SerializeField] private int responderCount = 0;
    [SerializeField] private string lastPingTime = "";

    private Rigidbody rb;
    private AudioSource sfx;
    private bool isGrounded;
    private Vector2 moveInput;

    private List<Vector3> lastPingHits = new List<Vector3>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.spatialBlend = 0f;

        Debug.Log("<color=cyan>🎮 FREE-FLIGHT SONAR READY – WASD = Full 2.5D Movement + E = Ping!</color>");

    }

    void Update()
    {
        // === MOVEMENT ===
        float moveX = 0f;
        float moveY = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = +1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveY = +1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveY = -1f;
        moveInput = new Vector2(moveX, moveY).normalized;




        // === PING ON E ===
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            pingCount++;
            lastPingTime = Time.time.ToString("F3");
            Debug.Log($"<color=magenta>🔔 PING #{pingCount} STARTED @ {lastPingTime}s | Pos={transform.position:F2} | PingClip={pingClip != null}</color>");

            if (pingClip)
            {
                sfx.PlayOneShot(pingClip);
                Debug.Log("<color=magenta>🔊 PING SOUND PLAYED</color>");
            }
            else
            {
                Debug.LogError("<color=red>❌ PING CLIP MISSING!</color>");
            }

            StartCoroutine(PerformPing());
        }

        // 🔍 DEBUG: Stats toggle (press D)
        if (Keyboard.current.dKey.wasPressedThisFrame && Keyboard.current.leftShiftKey.isPressed)
        {
            Debug.Log($"<color=cyan>📊 STATS: Pings={pingCount} | Hits={hitCount} | Responders={responderCount} | LastPing={lastPingTime}s</color>");
        }
    }

    void FixedUpdate()
    {
        Vector3 velocity = new Vector3(moveInput.x * moveSpeed, moveInput.y * moveSpeed, 0f);
        rb.linearVelocity = velocity;
    }

    IEnumerator PerformPing()
    {
        int rays = 72;
        float angleStep = 360f / (rays - 1);
        int thisPingHits = 0;
        int thisPingResponders = 0;

        lastPingHits.Clear();  // Reset visualization

        Debug.Log($"<color=magenta>📡 SHOOTING {rays} RAYS (ALL LAYERS + TRIGGERS)...</color>");

        for (int i = 0; i < rays; i++)
        {
            float angle = -90f + i * angleStep;
            Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.right;

            Debug.DrawRay(transform.position, dir * 15f, Color.gray, 1f);

            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, 15f, ~0, QueryTriggerInteraction.Collide))
            {
                thisPingHits++;
                hitCount++;

                Debug.DrawRay(transform.position, dir * hit.distance, Color.cyan, 2f);

                // STORE HIT POINT FOR GIZMOS
                lastPingHits.Add(hit.point);

                Debug.Log($"<color=cyan>🎯 HIT '{hit.collider.name}' @ {hit.distance:F1}m | Pt={hit.point:F1}</color>");

                if (hit.collider.TryGetComponent<EchoResponder>(out var responder))
                {
                    thisPingResponders++;
                    responderCount++;

                    float delay = hit.distance * 2f / 343f;
                    Vector3 listenerPos = Camera.main ? Camera.main.transform.position : transform.position;

                    Debug.Log($"<color=orange>🔊 ECHO RESPONDER '{responder.name}' | Delay={delay:F3}s</color>");
                    responder.TriggerEcho(delay, listenerPos, hit.point, hit.normal);
                }
            }
        }

        Debug.Log($"<color=magenta>✅ PING #{pingCount}: {thisPingHits}/{rays} hits | {thisPingResponders} responders</color>");
        yield return null;
    }

    void OnDrawGizmosSelected()
    {
        // Ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;

        // Movement direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, new Vector3(moveInput.x, moveInput.y, 0) * 2f);

        // 🔍 PING RANGE CIRCLE
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 15f);
    }

    void OnDrawGizmos()
    {
        if (lastPingHits == null || lastPingHits.Count == 0) return;

        Gizmos.color = Color.green;
        foreach (var point in lastPingHits)
        {
            Gizmos.DrawSphere(point, 0.1f);
        }

        // Ping range
        Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 15f);
    }

}






/*
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerSonarController_Fallback : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpForce = 12f;
    public float groundCheckDistance = 0.6f;
    public LayerMask groundLayer;
    public AudioClip jumpClip;
    public AudioClip pingClip;

    private Rigidbody rb;
    private AudioSource sfx;
    private bool isGrounded;
    private Vector2 moveInput;  // x = horizontal, y = vertical

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;                    // NO GRAVITY - free flight!
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;  // Smooth movement

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.spatialBlend = 0f;

        Debug.Log("<color=cyan>🎮 FREE-FLIGHT SONAR READY – WASD = Full 2.5D Movement + E = Ping!</color>");
    }

    void Update()
    {
        // === MOVEMENT (WASD + Arrows) ===
        float moveX = 0f;
        float moveY = 0f;

        // Horizontal (A/D = left/right)
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = +1f;

        // VERTICAL (W/S = up/down) ← NEW!
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveY = +1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveY = -1f;

        moveInput = new Vector2(moveX, moveY).normalized;  // Smooth 45° diagonals

        // === GROUND CHECK (still works for jump) ===
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);

        // === JUMP (Space - optional, works when grounded) ===
        if (isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            if (jumpClip) sfx.PlayOneShot(jumpClip);
            Debug.Log("JUMP! (Space pressed)");
        }

        // === PING ON E ===
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("🔔 PING! (E pressed)");
            if (pingClip) sfx.PlayOneShot(pingClip);
            StartCoroutine(PerformPing());
        }
    }

    void FixedUpdate()
    {
        // Apply smooth physics movement
        Vector3 velocity = new Vector3(moveInput.x * moveSpeed, moveInput.y * moveSpeed, 0f);
        rb.linearVelocity = velocity;
    }

    IEnumerator PerformPing()
    {
        int rays = 72;
        float angleStep = 360f / (rays - 1);
        for (int i = 0; i < rays; i++)
        {
            float angle = -90f + i * angleStep;
            Vector3 dir = Quaternion.Euler(0, 0, angle) * Vector3.right;
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, 15f))
            {
                if (hit.collider.TryGetComponent<EchoResponder>(out var responder))
                {
                    float delay = hit.distance * 2f / 343f;
                    responder.TriggerEcho(delay, Camera.main.transform.position, hit.point, hit.normal);
                }
            }
        }
        yield return null;
    }

    void OnDrawGizmosSelected()
    {
        // Ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * groundCheckDistance);

        // Movement direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, new Vector3(moveInput.x, moveInput.y, 0) * 2f);
    }
}

*/