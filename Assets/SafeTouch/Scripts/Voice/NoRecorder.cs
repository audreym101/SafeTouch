using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SafeTouch.Voice
{
    /// <summary>
    /// Keeps the child's spoken NOs on this phone, in the app's private folder
    /// (no other app can read it): the numbers for each NO in journal.json,
    /// and the sound of the first, the best and the 8 newest as small WAV
    /// files (NoJournal decides which). The child hears them on the My NO
    /// screen and the parent in Grown-ups > My child.
    ///
    /// Nothing here is ever sent: cloud sync sends Progress only, and the
    /// recordings are not part of it. A parent can switch recording off
    /// (the numbers are still kept) or delete the recordings at any time;
    /// a reset or withdrawn consent deletes everything (consent terms 1.6).
    /// </summary>
    public class NoRecorder
    {
        const string FolderName = "no-voice";
        const string JournalFile = "journal.json";

        string folder;
        AudioClip playing;

        public NoJournal Journal { get; private set; }

        /// <summary>Raised when a NO is added or recordings are deleted, so open screens can refresh.</summary>
        public event Action Changed;

        string Folder
        {
            get
            {
                if (folder == null) folder = Path.Combine(Application.persistentDataPath, FolderName);
                return folder;
            }
        }

        public void Load()
        {
            Journal = new NoJournal();
            try
            {
                string path = Path.Combine(Folder, JournalFile);
                if (File.Exists(path)) JsonUtility.FromJsonOverwrite(File.ReadAllText(path), Journal);
                // A recording whose file has gone (storage cleared) is shown without a play button.
                foreach (var e in Journal.entries)
                    if (e.HasAudio && !File.Exists(Path.Combine(Folder, e.audio))) e.audio = "";
            }
            catch (Exception ex)
            {
                Debug.LogWarning("SafeTouch: could not read the NO journal: " + ex.Message);
                Journal = new NoJournal();
            }
        }

        /// <summary>
        /// Keeps one spoken NO. samples is null when the parent has switched recording off: then only its
        /// numbers are kept. source is "practice" or "lesson:&lt;level&gt;".
        /// </summary>
        public NoEntry Add(NoRating rating, float[] samples, int sampleRate, string source)
        {
            if (Journal == null) Load();
            var now = DateTime.UtcNow;
            var entry = new NoEntry
            {
                id = now.ToString("yyyyMMddHHmmssfff"),
                at = now.ToString("o"),
                power = rating.Power,
                stars = rating.stars,
                loud = rating.loud,
                steady = rating.steady,
                brief = rating.brief,
                tip = rating.tip.ToString(),
                seconds = rating.seconds,
                source = source,
            };
            if (samples != null && samples.Length > 0)
            {
                try
                {
                    Directory.CreateDirectory(Folder);
                    string file = "no-" + entry.id + ".wav";
                    File.WriteAllBytes(Path.Combine(Folder, file), Wav.Encode(samples, sampleRate));
                    entry.audio = file;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("SafeTouch: could not save the NO recording: " + ex.Message);   // e.g. the phone is full
                }
            }
            Delete(Journal.Add(entry));
            Save();
            if (Changed != null) Changed();
            return entry;
        }

        /// <summary>Plays a kept NO through the app's voice channel. False when its sound is not kept.</summary>
        public bool Play(NoEntry entry, Managers.AudioManager audio)
        {
            var clip = LoadClip(entry);
            if (clip == null || audio == null) return false;
            if (playing != null) UnityEngine.Object.Destroy(playing);
            playing = clip;
            audio.PlayVoice(clip);
            return true;
        }

        AudioClip LoadClip(NoEntry entry)
        {
            if (entry == null || !entry.HasAudio) return null;
            try
            {
                string path = Path.Combine(Folder, entry.audio);
                if (!File.Exists(path)) return null;
                int rate;
                var samples = Wav.Decode(File.ReadAllBytes(path), out rate);
                if (samples == null || samples.Length == 0) return null;
                var clip = AudioClip.Create("NO " + entry.id, samples.Length, 1, rate, false);
                clip.SetData(samples, 0);
                return clip;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("SafeTouch: could not play the NO recording: " + ex.Message);
                return null;
            }
        }

        /// <summary>Deletes every recording; the NO numbers and achievements stay.</summary>
        public void DeleteRecordings()
        {
            if (Journal == null) Load();
            Delete(Journal.ForgetAudio());
            Save();
            if (Changed != null) Changed();
        }

        /// <summary>Deletes the recordings and the numbers: the parent reset the app or withdrew consent.</summary>
        public void DeleteAll()
        {
            try
            {
                if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("SafeTouch: could not delete the NO recordings: " + ex.Message);
            }
            Journal = new NoJournal();
            if (Changed != null) Changed();
        }

        void Delete(List<string> files)
        {
            foreach (var file in files)
            {
                try
                {
                    string path = Path.Combine(Folder, file);
                    if (File.Exists(path)) File.Delete(path);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("SafeTouch: could not delete " + file + ": " + ex.Message);
                }
            }
        }

        void Save()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(Path.Combine(Folder, JournalFile), JsonUtility.ToJson(Journal));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("SafeTouch: could not save the NO journal: " + ex.Message);
            }
        }
    }
}
