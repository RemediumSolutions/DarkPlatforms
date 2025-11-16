
using UnityEngine;

public class CrystalController : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip collectClip;

    [Header("Objects")]
    public GameObject crystalObj;
    public GameObject teleporterObj;

    [Header("Spawn Settings")]
    public float minDistance = 4f;
    public float maxDistance = 7f;

    private AudioSource sfx;
    private Transform player;

    void Awake()
    {
        sfx = gameObject.AddComponent<AudioSource>();

        // Cache player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("<color=red>❌ No Player with 'Player' tag!</color>");

        // Cache objects if not assigned
        if (crystalObj == null) crystalObj = GameObject.FindGameObjectWithTag("Crystal");
        if (teleporterObj == null) teleporterObj = GameObject.FindGameObjectWithTag("Teleporter");

        // 🔥 START WITH TELEPORTER ACTIVE, CRYSTAL OFF
        if (teleporterObj) teleporterObj.SetActive(true);
        else Debug.LogError("<color=red>❌ Teleporter not found!</color>");

        if (crystalObj) crystalObj.SetActive(false);
        else Debug.LogError("<color=red>❌ Crystal not found!</color>");

        // Initial spawn
        SpawnTeleporter();

        Debug.Log("<color=cyan>🎮 CrystalController READY | Teleporter ACTIVE | Crystal HIDDEN</color>");
    }

    void Start()
    {
        // Initial position debug
        LogPositions();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // CRYSTAL COLLECTED → Activate Teleporter
        if (transform.CompareTag("Crystal"))
        {
            Debug.Log("<color=lime>💎 CRYSTAL COLLECTED!</color>");
            sfx.PlayOneShot(collectClip);

            var crystalCollider = crystalObj.GetComponent<Collider>();
            if (crystalCollider) crystalCollider.enabled = false;

            // Hide crystal, show teleporter
            //crystalObj.SetActive(false);
            teleporterObj.SetActive(true);

            SpawnTeleporter();
        }
        // TELEPORTER COLLECTED → Activate Crystal
        else if (transform.CompareTag("Teleporter"))
        {
            Debug.Log("<color=orange>🚀 TELEPORTER USED!</color>");
            sfx.PlayOneShot(collectClip);

            var teleporterCollider = teleporterObj.GetComponent<Collider>();
            if (teleporterCollider) teleporterCollider.enabled = false;

            // Hide teleporter, show crystal
            //teleporterObj.SetActive(false);
            crystalObj.SetActive(true);

            SpawnCrystal();
        }
    }

    void SpawnCrystal()
    {
        if (crystalObj == null || player == null) return;
        teleporterObj.SetActive(false);
        var crystalCollider = crystalObj.GetComponent<Collider>();
        if (crystalCollider) crystalCollider.enabled = true;
        Relocate(crystalObj, "Crystal");
    }

    void SpawnTeleporter()
    {
        if (teleporterObj == null || player == null) return;
        crystalObj.SetActive(false);
        var teleporterCollider = teleporterObj.GetComponent<Collider>();
        if (teleporterCollider) teleporterCollider.enabled = true;
        Relocate(teleporterObj, "Teleporter");
    }

    private void Relocate(GameObject target, string type)
    {
        target.SetActive(true);

        // Guaranteed minimum distance
        Vector2 playerPos2D = new Vector2(player.position.x, player.position.y);
        Vector2 targetPos2D;
        int attempts = 0;

        do
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(minDistance, maxDistance);
            targetPos2D = playerPos2D + new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );
            attempts++;
        } while (Vector2.Distance(playerPos2D, targetPos2D) < minDistance && attempts < 50);

        // 2D plane (Z=0)
        Vector3 newPos = new Vector3(targetPos2D.x, targetPos2D.y, 0f);
        target.transform.position = newPos;

        Debug.Log($"<color=lime>✅ {type} SPAWNED at {newPos:F1} | Dist={Vector2.Distance(playerPos2D, targetPos2D):F1}m</color>");
    }

    private void LogPositions()
    {
        if (player == null) return;

        float crystalDist = crystalObj ? Vector3.Distance(crystalObj.transform.position, player.position) : 0f;
        float teleporterDist = teleporterObj ? Vector3.Distance(teleporterObj.transform.position, player.position) : 0f;

        Debug.Log($"<color=lime>🎮 PLAYER: {player.position:F2} | Crystal: {crystalObj?.transform.position:F2} ({crystalDist:F1}m) | Teleporter: {teleporterObj?.transform.position:F2} ({teleporterDist:F1}m)</color>");
        Debug.Log($"<color=lime>🎮 ACTIVE: {(crystalObj?.activeInHierarchy == true ? "Crystal" : "Teleporter")}</color>");
    }

    // Scene View visualization
    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            // Spawn range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(new Vector3(player.position.x, player.position.y, 0), minDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(new Vector3(player.position.x, player.position.y, 0), maxDistance);
        }

        // Current positions
        if (crystalObj)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(crystalObj.transform.position, 0.5f);
        }
        if (teleporterObj)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(teleporterObj.transform.position, 0.5f);
        }
    }
}


/*

using UnityEngine;

public class CrystalController : MonoBehaviour
{
    public AudioClip collectClip;
    private AudioSource sfx;

    private Transform player;

    private GameObject playerObj;
    private GameObject crystalObj;
    private GameObject teleporterObj;



    void Awake()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        playerObj = GameObject.FindGameObjectWithTag("Player");
        teleporterObj = GameObject.FindGameObjectWithTag("Teleporter");
        crystalObj = GameObject.FindGameObjectWithTag("Crystal");

        //crystalObj.SetActive(false);

        if (playerObj != null)
            player = playerObj.transform;

        if (crystalObj) RelocateCrystal1(crystalObj);
        if (teleporterObj) RelocateCrystal1(teleporterObj);

        Debug.Log($"<color=lime>🎮 PLAYER at: {player?.position:F2} | CRYSTAL at: {crystalObj?.transform.position:F2} | TELEPORTER at: {teleporterObj?.transform.position:F2}</color>");
        Debug.Log($"<color=lime>🎮 DISTANCES: Crystal={Vector3.Distance(transform.position, player?.position ?? Vector3.zero):F1}m | Teleporter={Vector3.Distance(teleporterObj?.transform.position ?? Vector3.zero, player?.position ?? Vector3.zero):F1}m</color>");

        //RelocateCrystal1(teleporterObj);
        //RelocateCrystal();
    }

     void Start()
    {
        //ecrystalObj.SetActive(false);
        //RelocateCrystal1(teleporterObj);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("On Trigger Enter");

        if (other.CompareTag("Player") && transform.CompareTag("Crystal"))
        {
            Debug.Log("Found Crystal");

            sfx.PlayOneShot(collectClip);

            //crystalObj.SetActive(false);
            teleporterObj.SetActive(true);

            RelocateCrystal1(teleporterObj);
            //RelocateCrystal();
        }

        if (other.CompareTag("Player") && transform.CompareTag("Teleporter"))
        {
            Debug.Log("Found Teleporter");

            sfx.PlayOneShot(collectClip);
            //teleporterObj.SetActive(false);
            crystalObj.SetActive(true);

            RelocateCrystal1(crystalObj);
            //RelocateCrystal();
        }
    }


    float GetRandomXOrY()
    {
        if (Random.value < 0.5f)
            return Random.Range(3f, 6f);      // positive: 3 to 7
        else
            return Random.Range(-6f, -3f);    // negative: -7 to -3
    }


    private void RelocateCrystal1(GameObject target)
    {
        target.SetActive(true);
        if (player == null) return;

        float x = GetRandomXOrY();
        float y = GetRandomXOrY();

        // 🔥 FIX: FORCE Z=0 (2D plane match!)
        Vector3 newPos = new Vector3(
            player.position.x + x,
            player.position.y + y,
            0f  // ← CRITICAL: Z=0!
        );

        target.transform.position = newPos;

        Debug.Log($"<color=lime>✅ {target.name} at {newPos:F2} (Z=0 FIXED!) | Dist={Vector2.Distance(new Vector2(newPos.x, newPos.y), new Vector2(player.position.x, player.position.y)):F1}m</color>");
    }

    private void RelocateCrystal1_old(GameObject target)
    {

        target.SetActive(true);

        if (player == null) return;

        Debug.Log("Player x:" + player.position.x);
        Debug.Log("Player y:" + player.position.y);


        float x, y;
        x = GetRandomXOrY();
        y = GetRandomXOrY();



        Debug.Log($"x: {x}, y: {y}, sum: {x + y}");


        //        Vector3 horizontalOffset = new Vector3(x, 0f, y);

        Vector2 horizontalOffset = new Vector2(player.position.x + x, player.position.y + y);

//        Vector2 targetPos = player.position + horizontalOffset;

        //transform.position = horizontalOffset;

        target.transform.position = horizontalOffset;

        if (transform.CompareTag("Teleporter"))
        {
            Debug.Log("Teleporter Found");

        }


        Debug.Log($"crystal position x: {transform.position.x}, y: {transform.position.y}, z: {transform.position.z}, sum: {transform.position.x + transform.position.y}");
    }


    private void RelocateCrystal()
    {
        if (player == null) return;

        Debug.Log("Player x:" + player.position.x);
        Debug.Log("Player y:" + player.position.y);


        float x, y;
        x = GetRandomXOrY();
        y = GetRandomXOrY();



        Debug.Log($"x: {x}, y: {y}, sum: {x + y}");


        //        Vector3 horizontalOffset = new Vector3(x, 0f, y);

        //Vector2 horizontalOffset = new Vector2(player.position.x + x, player.position.y + y);

        //        Vector2 targetPos = player.position + horizontalOffset;

        Vector3 newPos = new Vector3(
        player.position.x + x,
        player.position.y + y,
        0f  // 🔑 Z=0 for 2D plane
    );

        transform.position = newPos;


        if (transform.CompareTag("Teleporter"))
        {
            Debug.Log("Teleporter Found");

        }


        Debug.Log($"crystal position x: {transform.position.x}, y: {transform.position.y}, z: {transform.position.z}, sum: {transform.position.x + transform.position.y}");
    }

}
 


/*


using UnityEngine;

public class CrystalController : MonoBehaviour
{
    public AudioClip collectClip;
    public float minDistance = 10f;
    public float maxDistance = 15f;
    [Header("Plane Matching")]
    public bool usePlayerY = true;        // Use player's exact Y (true = same horizontal plane)
    public bool snapToGround = false;     // Raycast to ground instead
    public float groundOffset = 0.5f;     // Hover above ground
    public LayerMask groundMask = 1;      // Layers for ground raycast

    private AudioSource sfx;
    private Transform player;

    void Awake()
    {
        sfx = gameObject.AddComponent<AudioSource>();

        // Cache player reference
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError("CrystalController: No Player with 'Player' tag found!");

        RelocateCrystal();
    }

    private void RelocateCrystal()
    {
        if (player == null) return;

        // Random horizontal direction (XZ plane)
        Vector2 randomDirXZ = Random.insideUnitCircle.normalized;
        float distance = Random.Range(minDistance, maxDistance);

        // Horizontal offset only (Y=0)
        Vector3 horizontalOffset = new Vector3(randomDirXZ.x * distance, 0f, randomDirXZ.y * distance);

        // Start with player's position + horizontal offset
        Vector3 targetPos = player.position + horizontalOffset;

        // Option 1: Exact same Y as player (perfect horizontal plane match)
        if (usePlayerY)
        {
            targetPos.y = player.position.y;
        }
        // Option 2: Snap to ground surface
        else if (snapToGround)
        {
            // Raycast down from above to find ground
            if (Physics.Raycast(targetPos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, groundMask))
            {
                targetPos = hit.point + Vector3.up * groundOffset;
            }
            else
            {
                Debug.LogWarning("CrystalController: No ground hit! Using player Y as fallback.");
                targetPos.y = player.position.y;
            }
        }

        // Apply position - this GUARANTEES same horizontal plane if usePlayerY=true
        transform.position = targetPos;

        // Debug: Log positions for verification
        Debug.Log($"Crystal relocated to {transform.position} | Player at {player.position} | Y Diff: {Mathf.Abs(transform.position.y - player.position.y):F3}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        sfx.PlayOneShot(collectClip);

        // TODO: Add score here

        Destroy(gameObject, 0.1f);
    }

    // Scene view debug: Visualize spawn ring
    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            // Min/Max distance rings at player's Y
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(new Vector3(player.position.x, player.position.y, player.position.z), minDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(new Vector3(player.position.x, player.position.y, player.position.z), maxDistance);
        }
    }
}

*/