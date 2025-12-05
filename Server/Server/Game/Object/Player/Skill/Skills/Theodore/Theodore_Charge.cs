
using Google.Protobuf.Protocol;
using Server.Game;
using System.Numerics;
using System.Xml.Linq;
using static Server.Data.DataUtils;

public sealed class Theodore_Charge : SkillHandlerBase
{
    public override bool CanMoveDuringCast => true;
    private bool _isChargingAnimPlaying = false;

    const string ANIM_CHARGE = "CHARGE";
    const string ANIM_CHARGE_RUN = "CHARGE_RUN";
    public Theodore_Charge()
    {
        _characterType = CharacterType.Theodore;
        _keyCode = KeyCode.None;
        _animName = ANIM_CHARGE;
    }
    public override void OnEnter(Player p, SkillContext ctx)
    {
        HitboxRequired = false;
        base.OnEnter(p, ctx);
        p.SendSkillEffect(default(Vector2), KeyCode.Q,
            type : "Select",
            name : "FX_Charging");

        SendSkillConfirmPacket(p);
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
        // Skill_Q 로 이동했을 때 위치가 초기화 된다. 
        //base.OnExit(p, ctx); 

        // *todo : 나중에 Effect Remove 해주기
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
        if (_isChargingAnimPlaying)
        {
            p.SendAnimPacket(ANIM_CHARGE, 0.1f);
            _isChargingAnimPlaying = false;
        }
    }
}
