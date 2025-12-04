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

    public void OnEffect()
    {
        if (_player?.Sound != null)
        {
            GameObject effect = Managers.FX.Effect.FindEffect(_player.Id, "FX_Skill04_Charging");
            if (effect != null)
                Managers.FX.Effect.RemoveEffect(_player.Id, effect);

            GameObject effect1 = Managers.FX.Effect.FindEffect(_player.Id, "FX_Skill04_ShotWind");
            if (effect1 != null)
                Managers.FX.Effect.RemoveEffect(_player.Id, effect1);

            GameObject effect2 = Managers.FX.Effect.FindEffect(_player.Id, "FX_Skill04_Shot");
            if (effect2 != null)
                Managers.FX.Effect.RemoveEffect(_player.Id, effect2);

            GameObject effect3 = Managers.FX.Effect.FindEffect(_player.Id, "FX_R_Hit");
            if (effect3 != null)
                Managers.FX.Effect.RemoveEffect(_player.Id, effect3);
        }
    }
}
