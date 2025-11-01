using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static Server.Data.DataUtils;

namespace Server.Game
{
    public partial class Player : Creature
    {
        public MoveIntent Intent { get; } = new MoveIntent();
        public sealed class MoveIntent
        {
            public bool Has;
            public C_SetMoveTarget Packet;

            public void Set(C_SetMoveTarget packet) { Has = true; Packet = packet; }
            public bool TryConsume(out C_SetMoveTarget packet)
            {
                if (Has)
                { packet = Packet; Has = false; return true; }
                packet = default;
                return false;
            }
            public void Clear() { Has = false; }
        }
        public void EnqueueMove(C_SetMoveTarget packet) => Intent.Set(packet);

        public readonly List<NextInputToken> Tokens = new List<NextInputToken>();

        #region Skill
        public bool CanUseSkill(KeyCode keyCode)
        {
            if (_skills[keyCode].CurLevel == 0)
                return false;

            // ��Ÿ�� üũ
            if (!CheckCoolTime(keyCode))
                return false;

            // ���׹̳� üũ
            if (!CheckStamina(keyCode))
                return false;

            return true;
        }

        // üũ ������ ������ ����
        public void CommitSkillUsage(KeyCode keyCode)
        {
            // ��Ÿ�� ��� ����
            _ = CoInputCooltime(keyCode, FindSkill(keyCode).CurLevelCooldown);

            // ���׹̳� ����
            Stamina -= FindSkill(keyCode).CurLevelStamina;
        }

        public float GetCoolTime(KeyCode key)
        {
            if (_coolDownDict.ContainsKey(key))
                return 0f;
            return _coolDownDict[key].coolTime;
        }

        private bool CheckCoolTime(KeyCode key)
        {
            if (!_coolDownDict[key].isCoolDown)
                return true;

            return false;
        }

        private bool CheckStamina(KeyCode key)
        {
            if (Stamina < FindSkill(key).CurLevelStamina)
                return false;

            return true;
        }

        private async Task CoInputCooltime(KeyCode key, float time)
        {
            _coolDownDict[key].isCoolDown = true;

            var sw = Stopwatch.StartNew();

            while (sw.Elapsed.TotalSeconds < time)
            {
                _coolDownDict[key].coolTime = (float)(time - sw.Elapsed.TotalSeconds);
                await Task.Delay(10); // 0.01�ʸ��� ���� ��Ÿ�� ����
            }

            _coolDownDict[key].isCoolDown = false;
            _coolDownDict[key].coolTime = 0.0f;
        }

        private Skill FindSkill(KeyCode key)
        {
            return _skills[key];
        }
        #endregion

        // ��ū �߰�
        public void AddToken(NextInputToken t, double windowSec)
        {
            t.ExpireUtc = TimeUtil.UtcSec() + windowSec; // ����ð�
            t.Active = true;
            Tokens.Add(t);
            // (����) �켱���� ���� ������ �����ص� ��
            // Tokens.Sort((a,b) => b.Priority.CompareTo(a.Priority));
        }

        // �� ƽ ��ȿ�� ��ū���� �˻�
        public void TickTokens()
        {
            double now = TimeUtil.UtcSec();
            for (int i = Tokens.Count - 1; i >= 0; --i)
            {
                var t = Tokens[i];
                if (!t.Active || t.RemainingUses <= 0 || now > t.ExpireUtc)
                    Tokens.RemoveAt(i);
            }
        }

        public bool TryHandleMoveWithTokens(C_SetMoveTarget req)
        {
            if (req == null)
                return false;

            // 1) ��ȿ�� ��ū ����� (����/�ܿ��� ����)
            var tok = Tokens
                .Where(t => t.Active
                            && t.Trigger == InputKind.Move
                            && t.RemainingUses > 0
                            && TimeUtil.UtcSec() <= t.ExpireUtc)
                .OrderByDescending(t => t.Priority)
                .FirstOrDefault();

            if (tok == null)
                return false;

            // 2) ġȯ ��ų ĳ��Ʈ
            var skill = SkillRegistry.Create(tok.ReplacementSkillKey);
            if (skill == null)
                return false;

            var ctx = new SkillContext
            {
                Key = skill.GetKeyCode(),
                MousePos = new Vector2(req.TargetPos.PosX, req.TargetPos.PosZ),
            };

            if (!skill.CanCast(this, ctx))
                return false;

            ChangeState(new Player_SkillState(skill, ctx));

            // 3) ��ū �Ҹ�/��Ȱ��
            tok.RemainingUses--;
            if (tok.RemainingUses <= 0)
                tok.Active = false;

            return true;
        }

        // �̺�Ʈ ��� ���(��ų ����/�ǰ� ��)
        public void CancelTokensOnSkillCast()
        {
            Tokens.RemoveAll(t => t.CancelOnUseSkill);
        }

        public void CancelTokensOnDamage()
        {
            Tokens.RemoveAll(t => t.CancelOnTakeDamage);
        }

        // (�ɼ�) ��ȸ ����: Ư�� Ʈ������ �ְ� �켱���� ��ū
        public NextInputToken PeekToken(InputKind trigger)
        {
            NextInputToken best = null;
            int bestPrio = int.MinValue;
            double now = TimeUtil.UtcSec();

            for (int i = 0; i < Tokens.Count; i++)
            {
                var t = Tokens[i];
                if (!t.Active || now > t.ExpireUtc || t.RemainingUses <= 0)
                    continue;
                if (t.Trigger != trigger)
                    continue;
                if (t.Priority > bestPrio)
                { best = t; bestPrio = t.Priority; }
            }
            return best;
        }
    }
}
