using System;
using System.Collections.Generic;
using System.Text;

public enum InputKind { Move, Attack, SkillKey }

public sealed class NextInputToken
{
    public bool Active = true;
    public int RemainingUses = 1;
    public double ExpireUtc;
    public int Priority = 0;

    public InputKind Trigger;

    // 치환할 스킬 식별자(문자열 키나 enum)
    public string ReplacementSkillKey; // ex) "Rozzi_Q_Dash"

    // 스킬 실행에 넘길 추가 파라미터(옵션)
    //public Dictionary<string, float> FParams = new Dictionary<string, float>();
    //public Dictionary<string, int> IParams = new Dictionary<string, int>();

    // 취소 조건(옵션)
    public bool CancelOnUseSkill;
    public bool CancelOnTakeDamage;
}
