using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Todo. Player -> Play3D 로 바꿔놓기
public class SoundController : MonoBehaviour
{
    public Dictionary<Define.Sound, Dictionary<string, List<ClipInfo>>> SoundClipDict = new  Dictionary<Define.Sound, Dictionary<string, List<ClipInfo>>>();

    #region Voice
    // 2D Voice 재생
    public void GetRandomVoice(string skillKey)
    {
        if (!SoundClipDict.ContainsKey(Define.Sound.Voice))
            return;
        if (!SoundClipDict[Define.Sound.Voice].ContainsKey(skillKey))
            return;

        List<ClipInfo> paths = SoundClipDict[Define.Sound.Voice][skillKey];
        int index =  UnityEngine.Random.Range(0, paths.Count);
        UseSound(paths[index].Clip, Define.Sound.Voice);
    }
    // 3D Voice 재생
    public void GetRandom3DVoice(string skillKey, Vector3 position)
    {
        if (!SoundClipDict.ContainsKey(Define.Sound.Voice))
            return;
        if (!SoundClipDict[Define.Sound.Voice].ContainsKey(skillKey))
            return;

        List<ClipInfo> paths = SoundClipDict[Define.Sound.Voice][skillKey];
        int index = UnityEngine.Random.Range(0, paths.Count);
        Use3DSound(paths[index].Clip, Define.Sound.Voice, position);
    }
    #endregion

    #region Effect
    // 랜덤 2D Effect 재생
    public void GetRandomEffect(string skillKey)
    {
        if (!SoundClipDict[Define.Sound.Effect].ContainsKey(skillKey))
            return;
        List<ClipInfo> paths = SoundClipDict[Define.Sound.Effect][skillKey];
        int index = UnityEngine.Random.Range(0, paths.Count);
        UseSound(paths[index].Clip, Define.Sound.Effect);
    }

    // 랜덤 3D Effect 재생
    public void GetRandom3DEffect(string skillKey, Vector3 position)
    {
        if (!SoundClipDict[Define.Sound.Effect].ContainsKey(skillKey))
            return;

        List<ClipInfo> paths = SoundClipDict[Define.Sound.Effect][skillKey];
        int index = UnityEngine.Random.Range(0, paths.Count);
        Use3DSound(paths[index].Clip, Define.Sound.Effect, position);
    }

    // 특정 2D Effect 재생
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
                UseSound(clip.Clip, Define.Sound.Effect);
        }
    }

    // 특정 3D Effect 재생
    public void GetEffect3D(string soundName, Vector3 position)
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
                AudioClip loopSoud = Managers.Sound.Play3DSoundLoop(clip.Clip, position, Define.Sound.Effect, 0.1f);
                StartCoroutine(StopLoopAfterTime(loopSoud.name, clip.Duration)); 
            }
            else
                Use3DSound(clip.Clip, Define.Sound.Effect, position); 
        }
    }

    // 특정 반복 3D Effect 재생
    public AudioClip PlayLoopSound(string soundName, bool isLoopSound = false, Vector3 position = default(Vector3))
    {
        if (!SoundClipDict.ContainsKey(Define.Sound.Effect))
            return null;

        if (!SoundClipDict[Define.Sound.Effect].ContainsKey(soundName))
            return null;

        List<ClipInfo> clips = SoundClipDict[Define.Sound.Effect][soundName];

        if (clips == null || clips.Count == 0)
            return null;

        AudioClip loopSource;
        ClipInfo clipInfo = clips[0];

        if (isLoopSound)
            loopSource = Managers.Sound.Play3DSoundLoop(clipInfo.Clip, position, Define.Sound.Effect, 0.1f);
        else
            loopSource = Managers.Sound.PlayLoop(clipInfo.Clip, Define.Sound.Effect, 0.1f);
        return loopSource;
    }

    // 이름에 lng, Loop 가 들어가는 사운드를 duration 시간 후에 정지
    private IEnumerator StopLoopAfterTime(string clipName, float duration)
    {
        yield return new WaitForSeconds(duration);

        Managers.Sound.StopLoopSound(clipName);
    }
    #endregion

    #region Connection
    public void UseSound(AudioClip clip, Define.Sound type)
    {
        Managers.Sound.Play(
            audioClip : clip, 
            type : type, 
            volume: 0.1f);
    }
    public void Use3DSound(AudioClip clip, Define.Sound type, Vector3 posiiton)
    {
        Managers.Sound.Play3D(
            audioClip: clip,
            position: posiiton,
            type: type);
    }

    #endregion

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

        // 공용
        foreach (var charEntry in DataManager.SoundDict[CharacterType.CharacterNone])
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

        if (!DataManager.SoundDict.ContainsKey(charType))
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
