using System.Collections.Generic;
using UnityEngine;


public class TheodoreSound : ScriptableObject
{
    [System.Serializable]
    public class ClipEntry
    {
        public string key;  
        public List<AudioClip> clips = new List<AudioClip>();
    }

    [System.Serializable]
    public class SoundGroup
    {
        public Define.Sound soundType; 
        public List<ClipEntry> clipEntries = new List<ClipEntry>();

        private Dictionary<string, List<AudioClip>> clipDict;

        private void InitializeDictionary()
        {
            if (clipDict == null)
            {
                clipDict = new Dictionary<string, List<AudioClip>>();
                foreach (var entry in clipEntries)
                {
                    if (!string.IsNullOrEmpty(entry.key) && entry.clips.Count > 0)
                    {
                        clipDict[entry.key] = entry.clips;
                    }
                }
            }
        }

        public AudioClip GetRandomClip(string key)
        {
            InitializeDictionary();

            if (clipDict.Count == 0 || !clipDict.ContainsKey(key))
                return null;

            var clips = clipDict[key];
            if (clips.Count == 0)
                return null;

            return clips[Random.Range(0, clips.Count)];
        }
    }

    public List<SoundGroup> soundGroups = new List<SoundGroup>();

    private SoundGroup GetSoundGroup(Define.Sound soundType)
    {
        return soundGroups.Find(g => g.soundType == soundType);
    }

    public AudioClip GetRandomVoice(string skillKey)
    {
        var group = GetSoundGroup(Define.Sound.Voice);
        return group?.GetRandomClip(skillKey);
    }

    public AudioClip GetRandomEffect(string skillKey)
    {
        var group = GetSoundGroup(Define.Sound.Effect);
        return group?.GetRandomClip(skillKey);
    }
   
}

//public class TheodoreSound : MonoBehaviour
//{
//    enum Sounds
//    {
//        Q_1, Q_2, Q_3,
//        W_1, W_2, W_3,
//        E_1, E_2, E_3,
//        R_1, R_2, R_3,
//        Dead_1, Dead_2, Dead_3,

//    }
//    Dictionary<string, string> _sounds = new Dictionary<string, string>();

//    private void Awake()
//    {
//        // Skill
//        _sounds.Add("Q_1", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_playskill1062100seq0_1_ko");
//        _sounds.Add("Q_2", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_playskill1062200seq0_2_ko");
//        _sounds.Add("Q_3", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_playskill1062200seq0_3_ko");
//        _sounds.Add("W_1", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_playskill1062300seq0_1_ko");
//        _sounds.Add("W_2", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_playskill1062300seq0_2_ko");
//        _sounds.Add("W_3", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_playskill1062300seq0_3_ko");
//        _sounds.Add("E_1", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_playskill1062400seq0_1_ko");
//        _sounds.Add("E_2", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_playskill1062400seq0_2_ko");
//        _sounds.Add("E_3", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_playskill1062400seq0_3_ko");
//        _sounds.Add("R_1", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_playskill1062500seq0_1_ko");
//        _sounds.Add("R_2", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_playskill1062500seq0_1_ko");
//        _sounds.Add("R_3", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_playskill1062500seq0_3_ko");

//        // Dead
//        _sounds.Add("Dead_1", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_Dead_1_ko");
//        _sounds.Add("Dead_2", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_Dead_2_ko");
//        _sounds.Add("Dead_3", "Theodore/Resources/sound/voice/theodore/s000/ko/Theodore_Dead_3_ko");

//    }

//    void Start()
//    {

//    }

//    void Update()
//    {

//    }
//    public void UseSkill(string keyCode)
//    {
//        string key = keyCode.ToString() + $"_{Random.Range(1, 4)}";
//        Managers.Sound.Play(_sounds[key], Define.Sound.Effect, 0.1f);
//    }
//}
