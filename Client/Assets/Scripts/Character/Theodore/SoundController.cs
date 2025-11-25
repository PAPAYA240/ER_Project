using Google.Protobuf.Protocol;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    public void GetRandomVoice(string skillKey)
    {
        List<string> paths = DataManager.SoundDict[CharacterType.Theodore][Define.Sound.Voice][skillKey];
        UseSkill(paths, Define.Sound.Voice);
    }

    public void GetRandomEffect(string skillKey)
    {
        List<string> paths = DataManager.SoundDict[CharacterType.Theodore][Define.Sound.Effect][skillKey];
        UseSkill(paths, Define.Sound.Effect);
    }
    public void UseSkill(List<string> paths, Define.Sound type)
    {
        int index =  Random.Range(0, paths.Count);
        Managers.Sound.Play(paths[index], type, 0.1f);
    }
}
