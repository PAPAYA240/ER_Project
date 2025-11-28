using System;
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

    HashSet<AbigailSound> _otherAbigailSoundDict;

    private void Start()
    {
        SetUpOtherAbigailSound();
        LoadAudioClips();
    }

    void SetUpOtherAbigailSound()
    {
        _otherAbigailSoundDict = new HashSet<AbigailSound> { 
            AbigailSound.Q, AbigailSound.QFirstHit,AbigailSound.QSecondHit,
            AbigailSound.W, AbigailSound.WHit,
            AbigailSound.E, AbigailSound.EHit,
            AbigailSound.R, AbigailSound.RHit,
            AbigailSound.Attack1, AbigailSound.Attack2, AbigailSound.AttackHit,
            AbigailSound.PassiveAttack, AbigailSound.PassiveAttackHit,
            AbigailSound.WeaponSkill
        };
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
                if(clip == null)
                    continue;

                _audioClipDict[sound].Add(clip);
            }
        }
    }

    public void Play(int objectId, AbigailSound sound)
    {
        if (Managers.Object.MyPlayer.Id != objectId && !_otherAbigailSoundDict.Contains(sound))
            return;

        if (!_audioClipDict.TryGetValue(sound, out List<AudioClip> clips) || clips.Count == 0)
            return;

        if (!_audioTypeDict.TryGetValue(sound, out Define.Sound soundType))
            return;

        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Count)];
        if (randomClip == null)
            return;

        Managers.Sound.Play(randomClip, soundType, 0.2f);
    }
}
