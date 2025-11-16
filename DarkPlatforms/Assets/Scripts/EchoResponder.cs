
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class EchoResponder : MonoBehaviour
{
    public AudioClip echoClip;
    [Range(0.01f, 0.1f)] public float volumeFalloff = 0.03f;
    public float pitchHeightFactor = 0.2f;

    // 🔍 DEBUG COUNTERSe
    [Header("🕵️ DEBUG INFO")]
    [SerializeField] private int echoCount = 0;
    [SerializeField] private float lastEchoDelay = 0f;
    [SerializeField] private float lastEchoVolume = 0f;
    [SerializeField] private float lastEchoPitch = 0f;
    [SerializeField] private bool hasAudioListener = false;

    void Start()
    {
        // 🔍 AUTO-CHECK SETUP
        hasAudioListener = FindObjectsOfType<AudioListener>().Length > 0;
        if (!hasAudioListener) Debug.LogError($"<color=red>❌ NO AudioListener in scene! Echoes won't play on {gameObject.name}</color>");

        if (echoClip == null) Debug.LogError($"<color=red>❌ NO ECHO CLIP on {gameObject.name}!</color>");
        else Debug.Log($"<color=green>✅ EchoResponder READY on {gameObject.name} | Clip={echoClip.name} | Len={echoClip.length:F2}s</color>");
    }

    // OLD 2-parameter version
    public void TriggerEcho(float delay, Vector3 listenerPos)
    {
        TriggerEcho(delay, listenerPos, transform.position, Vector3.up);
    }

    // NEW 4-parameter version
    public void TriggerEcho(float delay, Vector3 listenerPos, Vector3 hitPoint, Vector3 hitNormal)
    {
        echoCount++;
        lastEchoDelay = delay;
        Debug.Log($"<color=orange>🎤 ECHO #{echoCount} TRIGGERED on {gameObject.name}!</color>");
        Debug.Log($"<color=orange>   📍 HitPt={hitPoint:F2} | Normal={hitNormal:F2} | Delay={delay:F3}s</color>");
        Debug.Log($"<color=orange>   👂 Listener={listenerPos:F2} | Dist={Vector3.Distance(hitPoint, listenerPos):F1}m</color>");

        if (echoClip == null)
        {
            Debug.LogError("<color=red>❌ ECHO CLIP NULL → ABORT</color>");
            return;
        }

        float dist = Vector3.Distance(hitPoint, listenerPos);
        float volume = Mathf.Max(Mathf.Clamp01(1f - dist * volumeFalloff), 0.15f);
        float heightDiff = hitPoint.y - listenerPos.y;
        float pitch = Mathf.Clamp(1f + heightDiff * pitchHeightFactor, 0.5f, 2f);

        lastEchoVolume = volume;
        lastEchoPitch = pitch;

        Debug.Log($"<color=orange>   🔊 CALC: Vol={volume:F2} | Pitch={pitch:F2} | Dist={dist:F1}m</color>");

        StartCoroutine(PlayEchoForced(delay, hitPoint, volume, pitch));
    }


    IEnumerator PlayEchoForced2(AudioClip echoClip, float delay, Vector3 pos, float vol, float pitch)
    {
        Debug.Log($"<color=yellow>🔊 ECHO DEBUG: Clip={echoClip?.name} Length={echoClip?.length:F2}s IsNull={echoClip == null}</color>");

        if (echoClip == null || echoClip.length == 0)
        {
            Debug.LogError("<color=red>❌ ECHO CLIP INVALID! NULL or 0 length</color>");
            yield break;
        }

        // Create AudioSource
        GameObject echoGO = new GameObject("EchoGO");
        AudioSource echoAS = echoGO.AddComponent<AudioSource>();

        echoAS.clip = echoClip;
        echoAS.volume = 1f;  // 🔥 FORCE AUDIBLE
        echoAS.spatialBlend = 0f;  // 2D for testing
        echoAS.Play();

        Debug.Log($"<color=cyan>🔊 Playing {echoClip.name} | Vol={echoAS.volume} | Length={echoClip.length:F2}s</color>");

        // Wait FULL duration + buffer
        yield return new WaitForSeconds(echoClip.length + 0.1f);

        Debug.Log("<color=gray>✅ Echo finished naturally</color>");
        Destroy(echoGO);
    }

    private IEnumerator PlayEchoForced(float delay, Vector3 pos, float vol, float pitch)
    {
        Debug.Log($"<color=cyan>⏳ Waiting {delay:F3}s for echo...</color>");
        yield return new WaitForSeconds(delay);

        if (echoClip == null || echoClip.length == 0)
        {
            Debug.LogError("<color=red>❌ ECHO CLIP INVALID! NULL or 0 length</color>");
            yield break;
        }

        // 🔍 FORCE 2D TEST FIRST (uncomment to test)
        // return; // ← Use this line to test 2D only

        GameObject echoGO = new GameObject($"ECHO_{gameObject.name}_{Time.frameCount}");
        echoGO.transform.position = pos;
        DontDestroyOnLoad(echoGO); // Prevent scene unload issues

        // === 3D HRTF SOURCE ===
        AudioSource hrtf = echoGO.AddComponent<AudioSource>();
        hrtf.spatialize = true;
        hrtf.spatialBlend = 1f;
        hrtf.clip = echoClip;
        hrtf.volume = vol;
        hrtf.pitch = pitch;
        hrtf.rolloffMode = AudioRolloffMode.Linear;
        hrtf.minDistance = 0.1f;
        hrtf.maxDistance = 30f;
        hrtf.dopplerLevel = 0f;
        hrtf.Play();

        Debug.Log($"<color=cyan>🎵 HRTF.Play() → isPlaying={hrtf.isPlaying} | time={hrtf.time:F2}/{echoClip.length:F2} | vol={hrtf.volume:F2}</color>");

        // === 2D BACKUP (LOUDER for testing) ===
        AudioSource backup = echoGO.AddComponent<AudioSource>();
        backup.clip = echoClip;
        backup.spatialBlend = 0f;
        backup.volume = vol * 0.8f; // Louder backup
        backup.pitch = pitch;
        backup.Play();

        Debug.Log($"<color=lime>🔊 2D Backup.Play() → isPlaying={backup.isPlaying} | Should HEAR THIS!</color>");

        // === WAIT PROPERLY ===
        float duration = echoClip.length / Mathf.Abs(pitch);
        yield return new WaitForSeconds(duration + 0.3f); // Extra buffer

        // 🔍 CONFIRM FINISHED
        if (hrtf != null) Debug.Log($"<color=gray>✅ HRTF finished: time={hrtf.time:F2}</color>");
        if (backup != null) Debug.Log($"<color=gray>✅ 2D finished: time={backup.time:F2}</color>");

        if (echoGO != null)
        {
            Destroy(echoGO);
            Debug.Log("<color=gray>🗑️  ECHO GO destroyed</color>");
        }
    }

    // 🔍 GIZMOS
    void OnDrawGizmosSelected()
    {
        Gizmos.color = echoClip ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 1f);

        // Echo range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f / volumeFalloff);
    }

    // 🔍 DEBUG TOGGLE (Press R on this object)
    void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log($"<color=cyan>📊 ECHO STATS on {gameObject.name}: Count={echoCount} | LastVol={lastEchoVolume:F2} | LastPitch={lastEchoPitch:F2} | ListenerOK={hasAudioListener}</color>");
        }
    }
}

/*

using UnityEngine;
using System.Collections;

public class EchoResponder : MonoBehaviour
{
    public AudioClip echoClip;
    [Range(0.01f, 0.1f)] public float volumeFalloff = 0.03f;
    public float pitchHeightFactor = 0.2f;

    // OLD 2-parameter version (kept for old scripts)
    public void TriggerEcho(float delay, Vector3 listenerPos)
    {
        // Fallback: use object center
        TriggerEcho(delay, listenerPos, transform.position, Vector3.up);
    }

    // NEW 4-parameter version (directional + hit point)
    public void TriggerEcho(float delay, Vector3 listenerPos, Vector3 hitPoint, Vector3 hitNormal)
    {

        Debug.Log($"<color=orange>ECHO HIT! Delay: {delay:F3}s | Hit: {hitPoint:F2}</color>");

        if (echoClip == null) return;

        float dist = Vector3.Distance(hitPoint, listenerPos);
        float volume = Mathf.Max(Mathf.Clamp01(1f - dist * volumeFalloff), 0.15f);
        float heightDiff = hitPoint.y - listenerPos.y;
        float pitch = Mathf.Clamp(1f + heightDiff * pitchHeightFactor, 0.5f, 2f);

        StartCoroutine(PlayEchoForced(delay, hitPoint, volume, pitch));
    }

    private IEnumerator PlayEchoForced(float delay, Vector3 pos, float vol, float pitch)
    {
        yield return new WaitForSeconds(delay);

        GameObject echoGO = new GameObject("ECHO");
        echoGO.transform.position = pos;

        // === 3D HRTF SOURCE (Microsoft Spatializer) ===
        AudioSource hrtf = echoGO.AddComponent<AudioSource>();

        // CRITICAL: Set spatialize BEFORE clip!
        hrtf.spatialize = true;                    // ← FIRST
        hrtf.spatialBlend = 1f;                    // 3D
        hrtf.clip = echoClip;                      // ← THEN assign clip
        hrtf.volume = vol;
        hrtf.pitch = pitch;
        hrtf.rolloffMode = AudioRolloffMode.Linear;
        hrtf.minDistance = 0.1f;
        hrtf.maxDistance = 30f;
        hrtf.dopplerLevel = 0f;                    // Optional: disable doppler for echoes
        hrtf.Play();                               // ← Use Play(), NOT PlayOneShot

        // === 2D BACKUP (Non-spatialized fallback) ===
        AudioSource backup = echoGO.AddComponent<AudioSource>();
        backup.clip = echoClip;
        backup.spatialBlend = 0f;                   // 2D
        backup.volume = vol * 0.5f;                // Quieter
        backup.pitch = pitch;
        backup.Play();                             // ← Play(), not PlayOneShot

        Debug.Log($"<color=cyan>HRTF ECHO @ {pos:F2} | Vol:{vol:F2} | Pitch:{pitch:F2}</color>");

        // Destroy after clip ends
        Destroy(echoGO, echoClip.length / pitch + 0.5f); // Account for pitch speedup
    }
}

*/
