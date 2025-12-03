using Google.Protobuf.Protocol;
using UnityEngine;

public class AnimatorEvent : MonoBehaviour
{
    [SerializeField]
    private PlayerController _player;

    public void OnSkillEnd()
    {
        _player?.YukiEffects?.PlayEffect(SkillEffectType.QAttack);
    }
}
