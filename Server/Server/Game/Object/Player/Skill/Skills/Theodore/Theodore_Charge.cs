
using Google.Protobuf.Protocol;
using Server.Game;
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
        //_keyCode = KeyCode.Q;
        _animName = ANIM_CHARGE;
    }
    public override void OnEnter(Player p, SkillContext ctx)
    {
        base.OnEnter(p, ctx);
    }
    public override void OnTick(Player p, SkillContext ctx)
    {
    }
    public override void OnHit(Player p, SkillContext ctx)
    { }
 
    public override void OnExit(Player p, SkillContext ctx)
    { 
        base.OnExit(p, ctx);
    }

    // 스킬 중에 애니메이션 변동을 필요로 하는 조건으로 움직임, 
    // 스킬 중 움직임 시
    public override void OnMove(Player p)
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
