using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

// 스킬 사용 전체 흐름(검증→커밋→핸들러 진입→쿨타임 시작/감소)
// 쿨타임 자체의 시간 계산은 ICooldownController에 위임.
public sealed class SkillController
{
    private readonly Player _owner;
    private readonly ICooldownController _cd;

    public SkillController(Player owner, ICooldownController cooldownController)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _cd = cooldownController ?? throw new ArgumentNullException(nameof(cooldownController));

        // 이번 틱에 쿨이 0이 된 스킬을 알림(한 틱에 1개, CooldownController_Tick 정책)
        //_cd.OnReady += OnCooldownReady;
    }

    // 쿨타임이 종료되었을 때 이벤트 발생용
    //public void Update(int nowTick) => _cd.Update(nowTick);
    //Skill?.Update(TimeUtil.LastTick); 쓸려면 Player에서 호출해줘야함

    // 남은 쿨(초)
    public float GetCooldown(KeyCode key) => _cd.GetRemaining(key);
    public bool IsReady(KeyCode key) => _cd.IsReady(key);

    // GameRoom에서 들어온 C_SkillInput을 그대로 처리
    public bool HandleSkillPacket(C_SkillInput pkt)
    {
        if (pkt == null)
            return false;

        var key = (KeyCode)pkt.SkillKey;
        var ctx = new SkillContext
        {
            MousePos = new Vector2(pkt.MouseX, pkt.MouseZ),
            TargetId = pkt.TargetId,
            Key = key,
        };

        return TryCast(key, ctx);
    }

    // 스킬 시도(검증→커밋→핸들러 진입→상태전환). 실패 시 false 패킷 전송.
    public bool TryCast(KeyCode key, in SkillContext ctx)
    {
        // 1) 플레이어 상태/쿨타임
        if (!_owner.CanUseSkill(key))
        {
            _owner.SendSkillConfirmPacket(false);
            return false;
        }
        if (!_cd.IsReady(key))
        {
            _owner.SendSkillConfirmPacket(false);
            return false;
        }
        if (_owner.CurrentState is Player_SkillState skill && !skill.CanStopSkill)
        {
            _owner.SendSkillConfirmPacket(false);
            return false;
        }

        // 2) 핸들러 결정 & 개별 핸들러의 추가 검증
        var handler = SkillRegistry.Resolve(_owner.Info.Player.CharType, key);
        if (handler == null || !handler.CanCast(_owner, ctx))
        {
            _owner.SendSkillConfirmPacket(false);
            return false;
        }

        // 3) 커밋(코스트 소모 등) -> 패킷 보내는 타이밍으로 변경, 상태 전환
        if (handler is InstantHandlerBase instant)
            instant.ExecuteInstant(_owner);
        else
            _owner.ChangeState(new Player_SkillState(handler, ctx));

        return true;
    }

    // 쿨 강제 설정(초). 0이하 → 즉시 완료 판정
    public void SetCooldown(KeyCode key, float seconds) => _cd.SetRemaining(key, seconds);

    // (아이템/버프의 쿨가속(%) 반영하여) 쿨 시작
    public void StartCooldown(KeyCode key, float? overrideDurationSec = null, int? startTick = null)
    {
        var data = _owner.FindSkill(key);             // 현재 레벨/쿨 정보 가져오는 메서드(기존 그대로 사용)
        if (data == null)
            return;

        float baseCd = overrideDurationSec ?? data.CurLevelCooldown;
        float accelPct = 100f / (100f + _owner.TotalItemStat.SkillAcceleration);  
        float adjusted = baseCd * accelPct;

        _cd.Start(key, adjusted, startTick);
    }

    // 비율인 경우(isRatio: true) => value : 0 ~ 1 
    public void Reduce(KeyCode key, float value, bool isRatio = false) => _cd.Reduce(key, value, isRatio);

    public bool IsPassiveAttackReady(KeyCode key = KeyCode.T)
    {
        if (!_cd.IsReady(key))
            return false;

        return true;
    }
}

