using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using System.Linq;

public class SoundManager : MonoBehaviour
{
    [System.Serializable]
    public class SoundTrack
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = true;
        [HideInInspector] public AudioSource source;
    }

    [System.Serializable]
    public class RoomData
    {
        public string roomName; // Room 1, Room 2, Room 3
        public GameObject metalDoor; // Metal door for this room
        public AudioClip roomEnemyMusic; // Unique music for this room
        [Range(5f, 30f)] public float musicDuration = 15f; // How long music plays
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [HideInInspector] public bool isDoorUnlocked = false;
        [HideInInspector] public bool isMusicPlaying = false;
        [HideInInspector] public AudioSource audioSource;
        [HideInInspector] public Coroutine musicCoroutine;
    }

    [Header("BACKGROUND MUSIC")]
    public SoundTrack backgroundMusic;
    [Range(0f, 2f)] public float backgroundFadeSpeed = 1f;

    [Header("ROOM CONFIGURATION")]
    public RoomData room1;
    public RoomData room2;
    public RoomData room3;

    [Header("SOUND SETTINGS")]
    [Range(0f, 2f)] public float roomMusicFadeSpeed = 2f;
    [Range(0f, 1f)] public float maxVolume = 0.7f;
    public bool debugLogs = true;

    [Header("AUDIO SOURCES")]
    public AudioSource backgroundSource;
    public AudioSource sfxSource;

    [Header("AUDIO MIXER")]
    public AudioMixer mainMixer;
    public string volumeParameter = "volume";

    // State
    private float backgroundTargetVolume = 0f;
    private bool isTransitioning = false;
    private List<RoomData> allRooms;
    private Dictionary<GameObject, string> enemyToRoomMap = new Dictionary<GameObject, string>();

    // Singleton instance
    private static SoundManager _instance;
    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SoundManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("SoundManager");
                    _instance = obj.AddComponent<SoundManager>();
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeSoundSystem();
    }

    void InitializeSoundSystem()
    {
        // Initialize all rooms list
        allRooms = new List<RoomData> { room1, room2, room3 };

        // Setup background music
        if (backgroundMusic != null && backgroundMusic.clip != null)
        {
            if (backgroundSource == null)
            {
                backgroundSource = gameObject.AddComponent<AudioSource>();
                backgroundSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("Master")[0];
                backgroundSource.playOnAwake = false;
                backgroundSource.loop = true;
            }

            backgroundSource.clip = backgroundMusic.clip;
            backgroundSource.volume = 0f;
            backgroundSource.loop = backgroundMusic.loop;
            backgroundSource.Play();
            backgroundTargetVolume = backgroundMusic.volume * maxVolume;
        }
        else
        {
            Debug.LogError("No background music assigned!");
        }

        // Setup SFX source
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("Master")[0];
            sfxSource.playOnAwake = false;
        }

        // Setup audio sources for each room
        foreach (RoomData room in allRooms)
        {
            if (room != null)
            {
                // Create audio source for this room
                room.audioSource = gameObject.AddComponent<AudioSource>();
                room.audioSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("Master")[0];
                room.audioSource.playOnAwake = false;
                room.audioSource.loop = false; // Don't loop room music

                if (room.roomEnemyMusic != null)
                {
                    room.audioSource.clip = room.roomEnemyMusic;
                    room.audioSource.volume = 0f;
                }
                else
                {
                    Debug.LogWarning($"No enemy music assigned for {room.roomName}!");
                }
            }
        }

        if (debugLogs)
            Debug.Log($"Sound Manager Initialized with {allRooms.Count} rooms");
    }

    void Update()
    {
        // Smoothly transition background music volume
        if (!Mathf.Approximately(backgroundSource.volume, backgroundTargetVolume))
        {
            backgroundSource.volume = Mathf.MoveTowards(
                backgroundSource.volume,
                backgroundTargetVolume,
                backgroundFadeSpeed * Time.deltaTime
            );
        }

        // Check all doors for unlock status
        CheckAllDoors();
    }

    void CheckAllDoors()
    {
        foreach (RoomData room in allRooms)
        {
            if (room != null && room.metalDoor != null)
            {
                bool doorActive = room.metalDoor.activeInHierarchy;

                // Door just became unlocked/destroyed
                if (!doorActive && !room.isDoorUnlocked)
                {
                    room.isDoorUnlocked = true;
                    OnDoorUnlocked(room);
                }
                // Door was re-locked (if possible in your game)
                else if (doorActive && room.isDoorUnlocked)
                {
                    room.isDoorUnlocked = false;
                    OnDoorRelocked(room);
                }
            }
        }
    }

    void OnDoorUnlocked(RoomData room)
    {
        if (debugLogs)
            Debug.Log($"🚪 {room.roomName} door unlocked! Playing room enemy music...");

        // Start room music
        StartRoomMusic(room);
    }

    void OnDoorRelocked(RoomData room)
    {
        if (debugLogs)
            Debug.Log($"🔒 {room.roomName} door relocked.");

        // Stop room music if playing
        StopRoomMusic(room);
    }

    void StartRoomMusic(RoomData room)
    {
        if (room.isMusicPlaying || room.audioSource == null || room.roomEnemyMusic == null)
            return;

        // Stop any existing music coroutine for this room
        if (room.musicCoroutine != null)
        {
            StopCoroutine(room.musicCoroutine);
        }

        // Start new music sequence
        room.musicCoroutine = StartCoroutine(RoomMusicSequence(room));
    }

    void StopRoomMusic(RoomData room)
    {
        if (room.musicCoroutine != null)
        {
            StopCoroutine(room.musicCoroutine);
            room.musicCoroutine = null;
        }

        // Fade out room music
        if (room.audioSource != null)
        {
            StartCoroutine(FadeOutAudioSource(room.audioSource, roomMusicFadeSpeed));
        }

        room.isMusicPlaying = false;
    }

    IEnumerator RoomMusicSequence(RoomData room)
    {
        room.isMusicPlaying = true;

        if (debugLogs)
            Debug.Log($"🎵 Starting {room.roomName} enemy music for {room.musicDuration} seconds");

        // Fade out background music
        float originalBackgroundVol = backgroundTargetVolume;
        backgroundTargetVolume = 0.1f * maxVolume; // Lower background volume, not completely mute

        // Wait for background to fade down
        yield return new WaitForSeconds(0.5f);

        // Fade in room enemy music
        yield return StartCoroutine(FadeInAudioSource(room.audioSource,
            room.musicVolume * maxVolume,
            roomMusicFadeSpeed));

        // Play for specified duration
        float timer = 0f;
        while (timer < room.musicDuration && room.isMusicPlaying)
        {
            timer += Time.deltaTime;

            // If music stopped playing, restart it
            if (!room.audioSource.isPlaying && room.audioSource.time > 0)
            {
                room.audioSource.Play();
            }

            yield return null;
        }

        // Fade out room music
        yield return StartCoroutine(FadeOutAudioSource(room.audioSource, roomMusicFadeSpeed));

        // Fade background music back up
        backgroundTargetVolume = originalBackgroundVol;

        room.isMusicPlaying = false;

        if (debugLogs)
            Debug.Log($"🎵 {room.roomName} enemy music completed");
    }

    IEnumerator FadeInAudioSource(AudioSource source, float targetVolume, float fadeSpeed)
    {
        if (source == null) yield break;

        source.volume = 0f;

        if (!source.isPlaying)
        {
            source.Play();
        }

        while (source.volume < targetVolume)
        {
            source.volume = Mathf.MoveTowards(source.volume, targetVolume, fadeSpeed * Time.deltaTime);
            yield return null;
        }

        source.volume = targetVolume;
    }

    IEnumerator FadeOutAudioSource(AudioSource source, float fadeSpeed)
    {
        if (source == null) yield break;

        float startVolume = source.volume;

        while (source.volume > 0.01f)
        {
            source.volume = Mathf.MoveTowards(source.volume, 0f, fadeSpeed * Time.deltaTime);
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
    }

    // Public API Methods

    public void RegisterEnemyToRoom(GameObject enemy, string roomName)
    {
        if (enemy == null || string.IsNullOrEmpty(roomName)) return;

        // Remove enemy from any previous room
        UnregisterEnemy(enemy);

        // Add to new room
        enemyToRoomMap[enemy] = roomName;

        if (debugLogs)
            Debug.Log($"👾 Enemy {enemy.name} registered to {roomName}");
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        if (enemy == null) return;

        if (enemyToRoomMap.ContainsKey(enemy))
        {
            string roomName = enemyToRoomMap[enemy];
            enemyToRoomMap.Remove(enemy);

            if (debugLogs)
                Debug.Log($"👾 Enemy {enemy.name} unregistered from {roomName}");
        }
    }

    public RoomData GetRoomByName(string roomName)
    {
        foreach (RoomData room in allRooms)
        {
            if (room != null && room.roomName == roomName)
            {
                return room;
            }
        }
        return null;
    }

    public void ManuallyTriggerRoomMusic(string roomName)
    {
        RoomData room = GetRoomByName(roomName);
        if (room != null && !room.isDoorUnlocked)
        {
            room.isDoorUnlocked = true;
            OnDoorUnlocked(room);
        }
    }

    public void StopAllRoomMusic()
    {
        foreach (RoomData room in allRooms)
        {
            if (room != null && room.isMusicPlaying)
            {
                StopRoomMusic(room);
            }
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume * maxVolume);
        }
    }

    public void SetMasterVolume(float volume)
    {
    mainMixer.SetFloat("volume", volume); 

    maxVolume = Mathf.InverseLerp(-80f, 0f, volume);

    if (backgroundMusic != null)
        backgroundTargetVolume = backgroundMusic.volume * maxVolume;

        // Update background volume
        if (backgroundMusic != null)
            backgroundTargetVolume = backgroundMusic.volume * maxVolume;

        // Update all room music volumes if playing
        foreach (RoomData room in allRooms)
        {
            if (room != null && room.isMusicPlaying && room.audioSource != null)
            {
                room.audioSource.volume = room.musicVolume * maxVolume;
            }
        }
    }

    public void PauseAllMusic()
    {
        backgroundSource.Pause();

        foreach (RoomData room in allRooms)
        {
            if (room != null && room.audioSource != null)
            {
                room.audioSource.Pause();
            }
        }
    }

    public void ResumeAllMusic()
    {
        backgroundSource.UnPause();

        foreach (RoomData room in allRooms)
        {
            if (room != null && room.audioSource != null)
            {
                room.audioSource.UnPause();
            }
        }
    }

    public void StopAllMusic()
    {
        backgroundSource.Stop();
        backgroundTargetVolume = 0f;

        foreach (RoomData room in allRooms)
        {
            StopRoomMusic(room);
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // Gizmos for debugging
    void OnDrawGizmos()
    {
        if (!debugLogs) return;

        // Draw room info in scene view
        foreach (RoomData room in allRooms)
        {
            if (room != null && room.metalDoor != null)
            {
                Gizmos.color = room.isDoorUnlocked ? Color.green : Color.red;
                Gizmos.DrawWireCube(room.metalDoor.transform.position, Vector3.one * 2f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(room.metalDoor.transform.position + Vector3.up * 3f,
                    $"{room.roomName}\nDoor: {(room.isDoorUnlocked ? "UNLOCKED" : "LOCKED")}\nMusic: {(room.isMusicPlaying ? "PLAYING" : "STOPPED")}");
#endif
            }
        }
    }
}