using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public enum SkillFinishReason
{
    EarlyEnd = 0,
    Landed,
    Blocked,
    Interrupted,
    Canceled
}

public sealed class SkillContext
{
    public Vector2 MousePos;            // XZ
    public int TargetId;                // 필요 시
    public KeyCode Key;

    public bool IsFinishRequested { get; private set; }
    public SkillFinishReason FinishReason { get; private set; }

    private Action<SkillFinishReason> _onFinish;  

    // 나중에 Player_SkillState 안에서 붙여줄 콜백
    public void AttachFinishHandler(Action<SkillFinishReason> handler)
    {
        _onFinish = handler;
    }

    public void RequestFinish(SkillFinishReason reason = SkillFinishReason.EarlyEnd)
    {
        if (IsFinishRequested)
            return;

        IsFinishRequested = true;
        FinishReason = reason;

        _onFinish?.Invoke(reason);
    }
}