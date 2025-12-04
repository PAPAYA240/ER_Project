using Data;
using Google.Protobuf.Protocol;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimatorEvent : MonoBehaviour
{
    [SerializeField]
    private PlayerController _player;

    public void OnSkillEnd()
    {
        _player?.YukiEffects?.PlayEffect(SkillEffectType.QAttack);
    }

    public void OnEffect()
    {
        if (_player?.Sound != null)
        {
            _player.PlaySelectEffect(KeyCode.R, default(Vector3), default(Vector3), Quaternion.identity, "FX_Skill04_ShotWind", _player.transform);
            _player.PlaySelectEffect(KeyCode.R, default(Vector3), default(Vector3), Quaternion.identity, "FX_Skill04_Shot", _player.transform);
        }
    }
}
