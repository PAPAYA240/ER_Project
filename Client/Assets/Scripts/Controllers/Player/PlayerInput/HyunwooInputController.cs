using Google.Protobuf.Protocol;
using UnityEngine;

public class HyunwooInputController : PlayerInputController
{



    public override C_SkillInput GetSkillCommand()
    {
        // 배열 순서대로 키다운 검사 -> 처음 눌린 키에 대해 바로 생성/리턴
        for (int i = 0; i < _skillKeys.Length; i++)
        {
            var key = _skillKeys[i];
            if (!Input.GetKeyDown(key))
                continue;

            return _skill.TryCast((int)key, GetAttackableUnderCursorID(), GetMouseWorldPosition());
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            C_ChargingSkill packet = new C_ChargingSkill();
            packet.CharginRatio = 0.1f;
            packet.KeyCode = (int)KeyCode.R;

            Managers.Network.Send(packet);
            return null;
        }

        return null;
    }
}
