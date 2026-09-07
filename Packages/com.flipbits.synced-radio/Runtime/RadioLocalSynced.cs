namespace flipbits.SyncedRadio
{

    using System;
    using UdonSharp;
    using UnityEngine;
    using VRC.SDKBase;

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class RadioLocalSynced : UdonSharpBehaviour
    {
        [Header("Drop in songs and an AudioSource — it just works")]
        public AudioClip[] songs;
        public AudioSource audioSource;

        [Header("Defaults")]
        public bool autoPlay = true;
        public bool defaultLoop = true;
        public bool defaultShuffle = false;

        [Header("Sync tuning")]
        public float pollInterval = 1f;          // non-owner: how often to check for a new playback event
        public float ownerResyncInterval = 5f;   // owner: re-send state as a late-join safety net

        [UdonSynced] private int syncedTrackIndex;
        [UdonSynced] private bool syncedIsPlaying;
        [UdonSynced] private double syncedStartServerTime;
        [UdonSynced] private bool syncedLoop;
        [UdonSynced] private bool syncedShuffle;

        private bool localMuted;    // Interact toggle, not synced

        // What this client has already applied — used to detect *changes* only
        private int appliedTrackIndex = -1;
        private double appliedStartTime = -1.0;
        private bool appliedIsPlaying = false;
        private bool hasApplied = false;

        private float ownerTimer;
        private float pollTimer;

        // Owner-only shuffle state
        private int[] shuffleQueue;
        private int shufflePosition;

        void Start()
        {
            if (songs == null || songs.Length == 0) return;

            if (Networking.IsOwner(gameObject))
            {
                syncedLoop = defaultLoop;
                syncedShuffle = defaultShuffle;
                _RebuildShuffleQueue();

                if (autoPlay)
                {
                    syncedTrackIndex = syncedShuffle ? _PopShuffle() : 0;
                    syncedIsPlaying = true;
                    syncedStartServerTime = Networking.GetServerTimeInSeconds();
                    RequestSerialization();
                    _PlayTrack(syncedTrackIndex, 0f);
                }
            }
        }

        void Update()
        {
            if (songs == null || songs.Length == 0) return;

            if (Networking.IsOwner(gameObject))
            {
                if (syncedIsPlaying)
                {
                    AudioClip clip = songs[syncedTrackIndex];
                    if (clip != null)
                    {
                        double elapsed = Networking.GetServerTimeInSeconds() - syncedStartServerTime;
                        if (elapsed >= clip.length) _AdvanceTrack();
                    }
                }

                ownerTimer += Time.deltaTime;
                if (ownerTimer >= ownerResyncInterval)
                {
                    ownerTimer = 0f;
                    RequestSerialization(); // re-send unchanged state; late joiners self-heal, no reseek for existing clients
                }
            }
            else
            {
                pollTimer += Time.deltaTime;
                if (pollTimer >= pollInterval)
                {
                    pollTimer = 0f;
                    _ApplyIfChanged();
                }
            }
        }

        public override void Interact()
        {
            localMuted = !localMuted;
            audioSource.mute = localMuted;
        }

        // --- Sync ---

        public override void OnDeserialization()
        {
            if (!Networking.IsOwner(gameObject)) _ApplyIfChanged();
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (Networking.IsOwner(gameObject)) RequestSerialization();
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (Networking.IsOwner(gameObject)) _RebuildShuffleQueue();
        }

        // --- Apply ONLY when a playback event changes ---

        private void _ApplyIfChanged()
        {
            if (!Networking.IsNetworkSettled) return;
            if (songs == null || songs.Length == 0) return;

            bool changed = !hasApplied
                        || syncedTrackIndex != appliedTrackIndex
                        || syncedStartServerTime != appliedStartTime
                        || syncedIsPlaying != appliedIsPlaying;

            if (!changed) return; // steady playback — do not touch the AudioSource

            hasApplied = true;
            appliedTrackIndex = syncedTrackIndex;
            appliedStartTime = syncedStartServerTime;
            appliedIsPlaying = syncedIsPlaying;

            if (!syncedIsPlaying)
            {
                audioSource.Pause();
                return;
            }

            if (syncedTrackIndex < 0 || syncedTrackIndex >= songs.Length) return;
            AudioClip clip = songs[syncedTrackIndex];
            if (clip == null) return;

            float elapsed = (float)(Networking.GetServerTimeInSeconds() - syncedStartServerTime);
            if (elapsed < 0f) elapsed = 0f;
            if (elapsed >= clip.length) return; // song's over on paper; owner will advance shortly

            _PlayTrack(syncedTrackIndex, elapsed);
        }

        private void _PlayTrack(int index, float startTime)
        {
            if (index < 0 || index >= songs.Length || songs[index] == null) return;

            audioSource.clip = songs[index];
            audioSource.time = Mathf.Clamp(startTime, 0f, songs[index].length - 0.01f);
            audioSource.Play();
            audioSource.mute = localMuted;
        }

        private void _TakeOwnership()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        // --- Track advancement (owner only) ---

        private void _AdvanceTrack()
        {
            int nextIndex;

            if (syncedShuffle)
            {
                nextIndex = _PopShuffle();
                if (!syncedIsPlaying) return;
            }
            else
            {
                nextIndex = syncedTrackIndex + 1;
                if (nextIndex >= songs.Length)
                {
                    if (!syncedLoop)
                    {
                        syncedIsPlaying = false;
                        audioSource.Stop();
                        RequestSerialization();
                        return;
                    }
                    nextIndex = 0;
                }
            }

            syncedTrackIndex = nextIndex;
            syncedIsPlaying = true;
            syncedStartServerTime = Networking.GetServerTimeInSeconds();
            RequestSerialization();
            _PlayTrack(syncedTrackIndex, 0f);
        }

        // --- Smart shuffle (Fisher-Yates, no repeats until a full pass) ---

        private void _RebuildShuffleQueue()
        {
            if (songs == null || songs.Length == 0) return;

            shuffleQueue = new int[songs.Length];
            for (int i = 0; i < songs.Length; i++) shuffleQueue[i] = i;

            for (int i = shuffleQueue.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int tmp = shuffleQueue[i];
                shuffleQueue[i] = shuffleQueue[j];
                shuffleQueue[j] = tmp;
            }
            shufflePosition = 0;
        }

        private int _PopShuffle()
        {
            if (shuffleQueue == null || shufflePosition >= shuffleQueue.Length)
            {
                int lastPlayed = syncedTrackIndex;
                _RebuildShuffleQueue();

                if (shuffleQueue.Length > 1 && shuffleQueue[0] == lastPlayed)
                {
                    int swapIdx = UnityEngine.Random.Range(1, shuffleQueue.Length);
                    shuffleQueue[0] = shuffleQueue[swapIdx];
                    shuffleQueue[swapIdx] = lastPlayed;
                }

                if (!syncedLoop)
                {
                    syncedIsPlaying = false;
                    audioSource.Stop();
                    RequestSerialization();
                    return syncedTrackIndex;
                }
            }
            return shuffleQueue[shufflePosition++];
        }

        // === Optional UI hooks ===

        public void NextTrack()
        {
            _TakeOwnership();
            _AdvanceTrack();
        }

        public void PreviousTrack()
        {
            _TakeOwnership();

            int prev;
            if (syncedShuffle)
            {
                prev = _PopShuffle();
                if (!syncedIsPlaying) return;
            }
            else
            {
                prev = syncedTrackIndex - 1;
                if (prev < 0) prev = songs.Length - 1;
            }

            syncedTrackIndex = prev;
            syncedIsPlaying = true;
            syncedStartServerTime = Networking.GetServerTimeInSeconds();
            RequestSerialization();
            _PlayTrack(syncedTrackIndex, 0f);
        }

        public void TogglePlayPause()
        {
            _TakeOwnership();

            if (syncedIsPlaying)
            {
                syncedIsPlaying = false;
                audioSource.Pause();
            }
            else
            {
                syncedIsPlaying = true;
                syncedStartServerTime = Networking.GetServerTimeInSeconds() - audioSource.time;
                audioSource.UnPause();
                audioSource.mute = localMuted;
            }
            RequestSerialization();
        }

        public void ToggleLoop()
        {
            _TakeOwnership();
            syncedLoop = !syncedLoop;
            RequestSerialization();
        }

        public void ToggleShuffle()
        {
            _TakeOwnership();
            syncedShuffle = !syncedShuffle;
            if (syncedShuffle) _RebuildShuffleQueue();
            RequestSerialization();
        }

        // === Getters for UI scripts ===

        public bool IsPlaying() { return syncedIsPlaying; }
        public bool IsLooping() { return syncedLoop; }
        public bool IsShuffled() { return syncedShuffle; }
        public bool IsLocalMuted() { return localMuted; }
        public int GetCurrentTrackIndex() { return syncedTrackIndex; }
        public string GetCurrentTrackName()
        {
            if (songs == null || songs.Length == 0 || songs[syncedTrackIndex] == null) return "";
            return songs[syncedTrackIndex].name;
        }
    }

}
