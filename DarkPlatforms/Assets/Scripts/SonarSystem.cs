using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class SonarSystem : MonoBehaviour
{
    [Header("Sonar")]
    public AudioClip pingClip;
    public float maxRange = 15f;
    public float speedOfSound = 343f;
    public int raysPerPing = 72;
    public float fanAngle = 360f;
    public bool scanXZ = true;

    private AudioSource pingSource;
    private Transform listener;

    void Awake()
    {
        pingSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        pingSource.spatialBlend = 0f;
        listener = Camera.main?.transform;

        var pi = GetComponent<PlayerInput>();
        if (pi != null)
        {
            // FORCE correct map
            pi.defaultActionMap = "Player";
            pi.SwitchCurrentActionMap("Player");

            Debug.Log($"[SonarSystem] FORCED Action Map: {pi.currentActionMap.name}");

            // Confirm Echo is in current map
            var echoAction = pi.currentActionMap.FindAction("Echo");
            if (echoAction != null)
            {
                Debug.Log($"[SonarSystem] 'Echo' action FOUND in {pi.currentActionMap.name} map!");
            }
            else
            {
                Debug.LogError("[SonarSystem] 'Echo' action NOT in current map!");
            }
        }
    }

    // Try multiple method name variations
    void OnEcho(InputValue value)
    {
        Debug.Log("[Sonar] OnEcho (capital E) called!");
        TriggerPing();
    }


    private void TriggerPing()
    {
        Debug.Log($"[Sonar] *** PING TRIGGERED! *** from {transform.position}");

        if (pingClip != null)
            pingSource.PlayOneShot(pingClip);
        else
            Debug.LogWarning("[Sonar] No pingClip assigned!");

        StartCoroutine(PerformPing());
    }

    IEnumerator PerformPing()
    {
        Debug.Log($"[Sonar] Scanning {raysPerPing} rays...");
        List<RaycastHit> hits = new();

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
                hits.Add(hit);
                Debug.Log($"[Sonar] HIT: {hit.collider.name} @ {hit.distance:F1}m");
            }
        }

        foreach (var hit in hits)
        {
            float delay = hit.distance * 2f / speedOfSound;
            if (hit.collider.TryGetComponent<EchoResponder>(out var responder))
            {
                responder.TriggerEcho(delay, listener.position);
            }
        }

        yield return null;
    }

    void OnDrawGizmosSelected()
    {
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