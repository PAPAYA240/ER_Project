using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Todo. Player -> Play3D 로 바꿔놓기
public class SoundController : MonoBehaviour
{
    public Dictionary<Define.Sound, Dictionary<string, List<ClipInfo>>> SoundClipDict = new  Dictionary<Define.Sound, Dictionary<string, List<ClipInfo>>>();

    #region Voice
    public void GetRandomVoice(string skillKey)
    {
        if (!SoundClipDict.ContainsKey(Define.Sound.Voice))
            return;
        if (!SoundClipDict[Define.Sound.Voice].ContainsKey(skillKey))
            return;

        List<ClipInfo> paths = SoundClipDict[Define.Sound.Voice][skillKey];
        int index =  UnityEngine.Random.Range(0, paths.Count);
        UseVoice(paths[index].Clip, Define.Sound.Voice);
    }
    #endregion

    #region Effect
    public void GetRandomEffect(string skillKey)
    {
        if (!SoundClipDict[Define.Sound.Effect].ContainsKey(skillKey))
            return;
        List<ClipInfo> paths = SoundClipDict[Define.Sound.Effect][skillKey];
        int index = UnityEngine.Random.Range(0, paths.Count);
        UseSkill(paths[index].Clip, Define.Sound.Effect);
    }

    public void GetRandom3DEffect(string skillKey, Vector3 position)
    {
        if (!SoundClipDict[Define.Sound.Effect].ContainsKey(skillKey))
            return;

        List<ClipInfo> paths = SoundClipDict[Define.Sound.Effect][skillKey];
        int index = UnityEngine.Random.Range(0, paths.Count);
        Use3DSkill(paths[index].Clip, Define.Sound.Effect, position);
    }
    public void GetEffect(string soundName)
    {
        if (!SoundClipDict.ContainsKey(Define.Sound.Effect))
            return;
        if (!SoundClipDict[Define.Sound.Effect].ContainsKey(soundName))
            return;

        List<ClipInfo> clips = SoundClipDict[Define.Sound.Effect][soundName];
        foreach (ClipInfo clip in clips)
        {
            // 사운드 이름에 Loop, Ing 들어가면 루프 사운드임
            bool isLoopSound = clip.Clip.name.Contains("Loop") || clip.Clip.name.Contains("Ing");
            if (isLoopSound)
            {
                AudioClip loopSoud = Managers.Sound.PlayLoop(clip.Clip, Define.Sound.Effect, 0.1f);
                StartCoroutine(StopLoopAfterTime(loopSoud.name, clip.Duration));
            }
            else
                UseSkill(clip.Clip, Define.Sound.Effect);
        }
    }

    public void GetEffect3D(string soundName, Vector3 position, bool isf= false)
    {
        if (!SoundClipDict.ContainsKey(Define.Sound.Effect))
            return;
        if (!SoundClipDict[Define.Sound.Effect].ContainsKey(soundName))
            return;

        List<ClipInfo> clips = SoundClipDict[Define.Sound.Effect][soundName];
        foreach(ClipInfo clip in clips)
        {
            // 사운드 이름에 Loop, Ing 들어가면 루프 사운드임
            bool isLoopSound = clip.Clip.name.Contains("Loop") || clip.Clip.name.Contains("Ing");
            if (isLoopSound)
            {
                AudioClip loopSoud = Managers.Sound.PlayLoop(clip.Clip, Define.Sound.Effect, 0.1f);
                StartCoroutine(StopLoopAfterTime(loopSoud.name, clip.Duration)); 
            }
            else
                Use3DSkill(clip.Clip, Define.Sound.Effect, position); 
        }
    }
    private IEnumerator StopLoopAfterTime(string clipName, float duration)
    {
        yield return new WaitForSeconds(duration);

        Managers.Sound.StopLoopSound(clipName);
    }
    #endregion


    public void UseVoice(AudioClip clip, Define.Sound type)
    {
        Managers.Sound.Play(
            audioClip : clip, 
            type : type, 
            volume: 0.1f);
    }
    public void UseSkill(AudioClip clip, Define.Sound type)
    {
        Managers.Sound.Play(clip, type, 0.1f);
    }
    public void Use3DSkill(AudioClip clip, Define.Sound type, Vector3 poisiton)
    {
        Managers.Sound.Play3D(clip, poisiton, type);
    }
  

    #region Sound Load
    public void PreloadMonsterAllSounds(MonsterType charType)
    {
        if (charType == MonsterType.MonsterNone)
            return;

        foreach (var charEntry in DataManager.SoundMcDict[charType])
        {
            Define.Sound soundType = charEntry.Key;
            if (!SoundClipDict.ContainsKey(soundType))
            {
                SoundClipDict.Add(soundType, new Dictionary<string, List<ClipInfo>>());
            }

            foreach (var soundTypeEntry in charEntry.Value)
            {
                string skillKey = soundTypeEntry.Key;
                List<SoundData> paths = soundTypeEntry.Value;

                List<ClipInfo> clipInfos = new List<ClipInfo>();

                foreach (SoundData data in paths)
                {
                    AudioClip clip = Resources.Load<AudioClip>(data.Path);

                    if (clip != null)
                    {
                        clipInfos.Add(new ClipInfo()
                        {
                            Clip = clip,
                            Duration = data.Duration
                        });
                    }
                }
                SoundClipDict[soundType][skillKey] = clipInfos;
            }
        }
    }
    public void PreloadCharAllSounds(CharacterType charType)
    {
        if (charType == CharacterType.CharacterNone)
            return;

        foreach (var charEntry in DataManager.SoundDict[charType])
        {
            Define.Sound soundType = charEntry.Key;
            if (!SoundClipDict.ContainsKey(soundType))
                SoundClipDict.Add(soundType, new Dictionary<string, List<ClipInfo>>());

            foreach (var soundTypeEntry in charEntry.Value)
            {
                string skillKey = soundTypeEntry.Key;
                List<SoundData> paths = soundTypeEntry.Value;

                List<ClipInfo> clipInfos = new List<ClipInfo>();

                foreach (SoundData data in paths)
                {
                    AudioClip clip = Resources.Load<AudioClip>(data.Path);

                    if (clip != null)
                    {
                        clipInfos.Add(new ClipInfo()
                        {
                            Clip = clip,
                            Duration = data.Duration
                        });
                    }
                }
                SoundClipDict[soundType][skillKey] = clipInfos;
            }
        }
    }
    #endregion
}
