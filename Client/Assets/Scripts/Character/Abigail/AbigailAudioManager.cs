using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.Protocol;
using UnityEngine;



public class AbigailAudioManager : MonoBehaviour
{
    Dictionary<AbigailSound, List<AudioClip>> _audioClipDict = new Dictionary<AbigailSound, List<AudioClip>>();

    Dictionary<AbigailSound, Define.Sound> _audioTypeDict = new Dictionary<AbigailSound, Define.Sound>();

    private void Start()
    {
        LoadAudioClips();
    }

    void LoadAudioClips()
    {
        foreach (var kvp in DataManager.AbigailAudioDict)
        {
            AbigailSound sound = kvp.Key;
            List<string> paths = kvp.Value;

            if (paths.Count == 0)
                continue;

            // 소리 종류
            string firstPart = paths[0].Split('/')[0].ToLower();
            Define.Sound soundType = firstPart == "voice" ? Define.Sound.Voice : Define.Sound.Effect;
            _audioTypeDict[sound] = soundType;

            // 소리 경로
            _audioClipDict[sound] = new List<AudioClip>();

            foreach (var path in paths)
            {
                AudioClip clip = Resources.Load<AudioClip>("Abigail/" + path);
                if (clip == null)
                    continue;
                    
                _audioClipDict[sound].Add(clip);
            }
        }
    }

    public void Play(int objectId, AbigailSound sound, Vector3 pos)
    {
        if (!_audioClipDict.TryGetValue(sound, out List<AudioClip> clips) || clips.Count == 0) return;  
        if (!_audioTypeDict.TryGetValue(sound, out Define.Sound soundType)) return;
        
        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Count)];
        if (randomClip == null)
            return;

        bool forcePlay = false; // 강제 재생
        if (sound == AbigailSound.Dead)
            forcePlay = true;

        float volume = 0.35f;
        if (soundType == Define.Sound.Voice)
            volume = 0.15f;

        if (objectId == Managers.Object.MyPlayer.Id)
            Managers.Sound.Play(randomClip, soundType, volume, forcePlay);
        else
            Managers.Sound.Play3D(randomClip, pos, soundType, volume, forcePlay, objectId);
    }
}
