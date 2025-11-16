using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class TimeAnnouncer : MonoBehaviour
{
    // --------------------------------------------------------------------- //
    // 1. PUBLIC API
    // --------------------------------------------------------------------- //
    public void AnnounceTime(int totalSeconds) => StartCoroutine(PlayAnnouncement(totalSeconds));

    // --------------------------------------------------------------------- //
    // 2. CONFIGURATION
    // --------------------------------------------------------------------- //
    [Header("Timing")]
    [Tooltip("How many seconds to shave off the end of each word clip. " +
             "Positive = shorter pause, negative = longer pause.")]
    [Range(-0.5f, 0.5f)]
    public float gapBetweenWords = 0.05f;   // <-- tweak this value

    // --------------------------------------------------------------------- //
    // 3. INTERNALS
    // --------------------------------------------------------------------- //
    private AudioSource source;
    private readonly Dictionary<string, AudioClip> clips = new();

    private const string IntroClip = "you_completed_the_challenge_in";
    private const string AndClip = "and";
    private const string MinuteSingular = "minute";
    private const string MinutePlural = "minutes";
    private const string SecondSingular = "second";
    private const string SecondPlural = "seconds";
    private const string ClipFolder = "Audio/Times";

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        LoadAllClips();
    }

    // --------------------------------------------------------------------- //
    // 4. LOADING
    // --------------------------------------------------------------------- //
    private void LoadAllClips()
    {
        AudioClip[] all = Resources.LoadAll<AudioClip>(ClipFolder);
        foreach (var c in all)
        {
            string clean = System.Text.RegularExpressions.Regex.Replace(c.name, @"\s+", "_").ToLower();
            clips[clean] = c;
            clips[c.name.ToLower()] = c;
        }
        DebugLoadedClips();
        if (clips.Count == 0) Debug.LogError($"No audio clips found in Assets/{ClipFolder}!");
    }

    private void DebugLoadedClips()
    {
        Debug.Log("=== LOADED CLIPS ===");
        foreach (var kvp in clips) Debug.Log($"'{kvp.Key}' → {kvp.Value.name}");
        Debug.Log("=== END ===");
    }

    // --------------------------------------------------------------------- //
    // 5. ANNOUNCEMENT LOGIC
    // --------------------------------------------------------------------- //
    private IEnumerator PlayAnnouncement(int totalSeconds)
    {
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        yield return PlayClip(IntroClip);

        if (minutes > 0)
        {
            yield return PlayNumber(minutes);
            yield return PlayClip(minutes == 1 ? MinuteSingular : MinutePlural);
        }

        if (minutes > 0 && seconds > 0)
            yield return PlayClip(AndClip);

        if (seconds > 0 || totalSeconds == 0)
        {
            yield return PlayNumber(seconds);
            yield return PlayClip(seconds == 1 ? SecondSingular : SecondPlural);
        }
    }

    // --------------------------------------------------------------------- //
    // 6. HELPERS – now with adjustable gap
    // --------------------------------------------------------------------- //
    private IEnumerator PlayClip(string key)
    {
        if (!clips.TryGetValue(key, out AudioClip clip))
        {
            Debug.LogWarning($"Clip missing: {key}");
            yield break;
        }

        source.PlayOneShot(clip);
        float wait = Mathf.Max(0f, clip.length - gapBetweenWords);
        yield return new WaitForSeconds(wait);
    }

    private IEnumerator PlayNumber(int value)
    {
        if (value < 0) yield break;

        if (value <= 10)
        {
            yield return PlayClip(NumberName(value));
            yield break;
        }

        int tens = (value / 10) * 10;
        if (tens >= 20 && tens <= 50 && value % 10 == 0)
        {
            yield return PlayClip(NumberName(tens));
            yield break;
        }

        // tens + optional ones
        yield return PlayClip(NumberName(tens));
        int ones = value % 10;
        if (ones > 0) yield return PlayClip(NumberName(ones));
    }

    private static string NumberName(int n) => n switch
    {
        0 => "zero",
        1 => "one",
        2 => "two",
        3 => "three",
        4 => "four",
        5 => "five",
        6 => "six",
        7 => "seven",
        8 => "eight",
        9 => "nine",
        10 => "ten",
        20 => "twenty",
        30 => "thirty",
        40 => "forty",
        50 => "fifty",
        _ => n.ToString()
    };
}