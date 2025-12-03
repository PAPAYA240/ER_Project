using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class SoundManager
{
    private AudioSource[] _audioSources = new AudioSource[(int)Define.Sound.MaxCount];
    private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioSource> _loopSources = new Dictionary<string, AudioSource>();
    // MP3 Player   -> AudioSource
    // MP3 음원     -> AudioClip
    // 관객(귀)     -> AudioListener

    AudioClip _blink = null;

    public void Init()
    {
        GameObject root = GameObject.Find("@Sound");
        if (root == null)
        {
            root = new GameObject { name = "@Sound" };
            Object.DontDestroyOnLoad(root);

            string[] soundNames = System.Enum.GetNames(typeof(Define.Sound));
            for (int i = 0; i < soundNames.Length - 1; i++)
            {
                GameObject go = new GameObject { name = soundNames[i] };
                _audioSources[i] = go.AddComponent<AudioSource>();
                go.transform.parent = root.transform;
            }

            _audioSources[(int)Define.Sound.Bgm].loop = true;

            _blink = Managers.Resource.Load<AudioClip>("sound/fx/common/TacticalSkill_Blink");
        }
    }

    public void Clear()
    {
        foreach (AudioSource audioSource in _audioSources)
        {
            audioSource.clip = null;
            audioSource.Stop();
        }
        _audioClips.Clear();
    }

    public void Play(string path, Define.Sound type = Define.Sound.Effect, float volume = 0.15f)
    {
        AudioClip audioClip = GetOrAddAudioClip(path, type);
        Play(audioClip, type, volume);
    }

    public float Play3D(AudioClip audioClip, Vector3 position, Define.Sound type = Define.Sound.Effect, 
        float volume = 0.15f, bool forcePlay = false, int id = -1)
    {
        if (audioClip == null)
            return -1.0f;

        if(type == Define.Sound.Voice && id != -1)
        {
            CleanupFinishedVoices();

            if (IsSpeaking(id))
            {
                if(forcePlay)
                    StopVoice(id);
                else
                    return -1.0f;
            }                
        }

        GameObject go = new GameObject($"Sound_{audioClip}");
        go.transform.position = position;

        AudioSource audioSource = go.AddComponent<AudioSource>();
        audioSource.volume = volume * 0.45f; // 3D 사운드는 2D보다 소리 낮춤
        audioSource.spatialBlend = 1f;

        audioSource.rolloffMode = AudioRolloffMode.Linear; // 감쇠 모드
        audioSource.minDistance = 4f;            // 이 거리까지는 최대 음량
        audioSource.maxDistance = 20f;           // 이 거리까지는 서서히 작아짐

        audioSource.dopplerLevel = 0f;           // 도플러 효과 없음
        audioSource.spread = 0;               // 최대 넓이로 스테레오 유지

        audioSource.spatialize = true;           // 3D 공간화 활성화
        audioSource.spatializePostEffects = true; // 3D 효과 강화

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.pitch = 1f;
        audioSource.clip = audioClip;
        audioSource.Play();

        if (type == Define.Sound.Voice && id != -1)
            RegisterVoice(id, audioSource);

        Object.Destroy(go, audioClip.length + 0.1f);
        return audioClip.length;
    }

    public float Play3D(string path, Vector3 position, Define.Sound type = Define.Sound.Effect,
        float volume = 0.15f)
    {
        AudioClip audioClip = GetOrAddAudioClip(path, type);
        if (audioClip == null)
            return -1.0f;

        GameObject go = new GameObject($"Sound_{audioClip}");
        go.transform.position = position;

        AudioSource audioSource = go.AddComponent<AudioSource>();
        audioSource.volume = volume;
        audioSource.spatialBlend = 1f;
        audioSource.pitch = 1;
        audioSource.clip = audioClip;
        audioSource.Play();

        Object.Destroy(go, audioClip.length + 0.1f);
        return audioClip.length; 
    }

    public AudioClip PlayLoop(AudioClip audioClip, Define.Sound type = Define.Sound.Effect, float volume = 0.15f)
    {
        if (audioClip == null)
            return null;

        if(_loopSources == null)
            _loopSources = new Dictionary<string, AudioSource>();

        if (_loopSources != null && _loopSources.ContainsKey(audioClip.name))
            StopLoopSound(audioClip.name);

        GameObject loopObject = new GameObject($"LoopSound_{audioClip.name}");
        AudioSource loopSource = loopObject.AddComponent<AudioSource>();

        loopSource.volume = volume;
        loopSource.pitch = 1;

        loopSource.clip = audioClip;
        loopSource.loop = true;
        loopSource.Play();

        _loopSources.Add(audioClip.name, loopSource);
        return audioClip;
    }
    public AudioClip Play3DSoundLoop(AudioClip audioClip, Vector3 position, Define.Sound type = Define.Sound.Effect, float volume = 0.15f)
    {
        if (audioClip == null)
            return null;

        if (_loopSources == null)
            _loopSources = new Dictionary<string, AudioSource>();

        if (_loopSources != null && _loopSources.ContainsKey(audioClip.name))
            StopLoopSound(audioClip.name);

        GameObject loopObject = new GameObject($"LoopSound_{audioClip.name}");
        loopObject.transform.position = position;

        AudioSource loopSource = loopObject.AddComponent<AudioSource>();
        loopSource.volume = volume;
        loopSource.spatialBlend = 1f; ;
        loopSource.pitch = 1;
        loopSource.clip = audioClip;
        loopSource.loop = true;

        loopSource.rolloffMode = AudioRolloffMode.Linear; 
        loopSource.minDistance = 4f;          
        loopSource.maxDistance = 20f;        

        loopSource.dopplerLevel = 0f;         
        loopSource.spread = 0;           

        loopSource.spatialize = true;           
        loopSource.spatializePostEffects = true; 

        loopSource.Play();

        _loopSources.Add(audioClip.name, loopSource);
        return audioClip;
    }

    public void Play(AudioClip audioClip, Define.Sound type = Define.Sound.Effect, 
        float volume = 0.15f, bool forcePlay = false)
    {
        if (audioClip == null)
            return;

        if (type == Define.Sound.Bgm)
        {
            AudioSource audioSource = _audioSources[(int)Define.Sound.Bgm];
            if (audioSource.isPlaying)
                audioSource.Stop();

            audioSource.volume = volume;
            audioSource.pitch = 1;
            audioSource.clip = audioClip;
            audioSource.Play();
        }
        else
        {
             AudioSource audioSource = _audioSources[(int)type];
             audioSource.volume = volume;
             audioSource.pitch = 1;
             audioSource.loop = false;

             if(type == Define.Sound.Voice)
             {
                if (audioSource.isPlaying)
                {
                    if (forcePlay)
                        audioSource.Stop();
                    else
                        return;
                }

                audioSource.clip = audioClip;
                audioSource.Play();
             }

             audioSource.PlayOneShot(audioClip, volume);  
        }
    }

    public void StopLoopSound(string clipName)
    {
        if (_loopSources.TryGetValue(clipName, out AudioSource loopSource))
        {
            loopSource.Stop();
            Object.Destroy(loopSource.gameObject);
            _loopSources.Remove(clipName);
        }
    }
    
    AudioClip GetOrAddAudioClip(string path, Define.Sound type = Define.Sound.Effect)
    {
		AudioClip audioClip = null;

		if (type == Define.Sound.Bgm)
		{
			audioClip = Managers.Resource.Load<AudioClip>(path);
		}
		else
		{
			if (_audioClips.TryGetValue(path, out audioClip) == false)
			{
				audioClip = Managers.Resource.Load<AudioClip>(path);
				_audioClips.Add(path, audioClip);
			}
		}

		if (audioClip == null)
			Debug.Log($"AudioClip Missing ! {path}");

		return audioClip;
    }

    #region Voice
    Dictionary<int, AudioSource> _voiceSources = new Dictionary<int, AudioSource>();

    public bool IsSpeaking(int id)
    {
        return _voiceSources.ContainsKey(id) &&
               _voiceSources[id] != null &&
               _voiceSources[id].isPlaying;
    }

    public void RegisterVoice(int id, AudioSource voiceSource)
    {
        // 기존 Voice 정지 및 제거
        StopVoice(id);

        // 새로운 Voice 등록
        _voiceSources[id] = voiceSource;
    }

    public void StopVoice(int id)
    {
        if (_voiceSources.TryGetValue(id, out var existingSource))
        {
            if (existingSource != null && existingSource.isPlaying)
            {
                existingSource.Stop();
            }

            _voiceSources.Remove(id);
        }
    }

    public void CleanupFinishedVoices()
    {
        var finishedCharacters = _voiceSources
            .Where(kvp => kvp.Value == null || !kvp.Value.isPlaying)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var characterId in finishedCharacters)
        {
            _voiceSources.Remove(characterId);
        }
    }

    public void StopAllVoices()
    {
        foreach (var source in _voiceSources.Values)
        {
            if (source != null && source.isPlaying)
                source.Stop();
        }
        _voiceSources.Clear();
    }
    #endregion

    #region 공용
    public void Blink(int id, Vector3 pos)
    {
        if (id == Managers.Object.MyPlayer.Id)
            Play(_blink, Define.Sound.Effect, 0.35f);
        else
            Play3D(_blink, pos, Define.Sound.Effect, 0.35f);
    }
    #endregion
}
