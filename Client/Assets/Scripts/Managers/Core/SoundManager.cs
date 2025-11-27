using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    public void Play(string path, Define.Sound type = Define.Sound.Effect, float volume = 1.0f, float pitch = 1.0f)
    {
        AudioClip audioClip = GetOrAddAudioClip(path, type);
        Play(audioClip, type, volume, pitch);
    }
    public float Play3D(AudioClip audioClip, Vector3 position, Define.Sound type = Define.Sound.Effect, float pitch = 1.0f)
    {
        if (audioClip == null)
            return -1.0f;

        GameObject go = new GameObject($"Sound_{audioClip}");
        go.transform.position = position;

        AudioSource audioSource = go.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.pitch = pitch;
        audioSource.clip = audioClip;
        audioSource.Play();

        Object.Destroy(go, audioClip.length + 0.1f);
        return audioClip.length;
    }
    public float Play3D(string path, Vector3 position, Define.Sound type = Define.Sound.Effect, float pitch = 1.0f)
    {
        AudioClip audioClip = GetOrAddAudioClip(path, type);
        if (audioClip == null)
            return -1.0f;

        GameObject go = new GameObject($"Sound_{audioClip}");
        go.transform.position = position;

        AudioSource audioSource = go.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.pitch = pitch;
        audioSource.clip = audioClip;
        audioSource.Play();

        Object.Destroy(go, audioClip.length + 0.1f);
        return audioClip.length;
    }

    public AudioClip PlayLoop(AudioClip audioClip, Define.Sound type = Define.Sound.Effect, float volume = 1.0f, float pitch = 1.0f)
    {
        GameObject loopObject = new GameObject($"LoopSound_{audioClip.name}");
        AudioSource loopSource = loopObject.AddComponent<AudioSource>();

        loopSource.volume = volume;
        loopSource.pitch = pitch;

        loopSource.clip = audioClip;
        loopSource.loop = true;
        loopSource.Play();

        _loopSources.Add(audioClip.name, loopSource);
        return audioClip;
    }
    public void Play(AudioClip audioClip, Define.Sound type = Define.Sound.Effect, float volume = 1.0f, float pitch = 1.0f)
    {
        if (audioClip == null)
            return;

        if (type == Define.Sound.Bgm)
        {
            AudioSource audioSource = _audioSources[(int)Define.Sound.Bgm];
            if (audioSource.isPlaying)
                audioSource.Stop();

            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.clip = audioClip;
            audioSource.Play();
        }
        else
        {
             AudioSource audioSource = _audioSources[(int)Define.Sound.Effect];
             audioSource.volume = volume;
             audioSource.pitch = pitch;
             audioSource.loop = false;
             audioSource.PlayOneShot(audioClip); 
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
}
