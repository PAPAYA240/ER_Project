using Google.Protobuf.Protocol;
using UnityEngine;

public class AnimatorEvent : MonoBehaviour
{
    public void OnSkillEnd()
    {
        Managers.EffectHandler.PlayEffect(SkillEffectType.QAttack);
    }
}
