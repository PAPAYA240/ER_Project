
using Google.Protobuf.Protocol;
using Server.Game;
using System.Numerics;
using static Server.Data.DataUtils;

public sealed class Theodore_Charge : SkillHandlerBase
{
    public override bool CanMoveDuringCast => true;
    private bool _isChargingAnimPlaying = false;
    
    const string ANIM_CHARGE = "CHARGE";
    const string ANIM_CHARGE_RUN = "CHARGE_RUN";
    private string ANIM_IDLE = "WAIT";

    public Theodore_Charge()
    {
        _characterType = CharacterType.Theodore;
        _keyCode = KeyCode.None;
        _animName = ANIM_CHARGE;
    }
    public override void OnEnter(Player p, SkillContext ctx)
    {
        HitboxRequired = false;
        CanStopSkill = true;

        base.OnEnter(p, ctx);
        p.SendSkillEffect(default(Vector2), KeyCode.Q,
            type : "Select",
            name : "FX_Charging");

        SendSkillConfirmPacket(p, false);
    }
    public override void OnTick(Player p, SkillContext ctx)
    {
    }
    public override void OnHit(Player p, SkillContext ctx)
    { 
        return;
    }

    public override void OnExit(Player p, SkillContext ctx)
    {
        p.SendRemoveEffect(KeyCode.Q, type : "Select");
    }

    // 스킬 중에 애니메이션 변동을 필요로 하는 조건으로 움직임, 
    // 스킬 중 움직임 시
    public override void OnMove(Player p, C_Move packet)
    {
        if (!_isChargingAnimPlaying)
        {
            p.SendAnimPacket(ANIM_CHARGE_RUN, 0.1f);
            _isChargingAnimPlaying = true;
        }
    }

    public override void OnStop(Player p)
    {
         p.SendAnimPacket(ANIM_IDLE, 0.1f);
         _isChargingAnimPlaying = false;
    }
}
