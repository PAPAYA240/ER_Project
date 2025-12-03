using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;

public enum SkillSoundEvent
{
    Cast,               // 시전 시작
    Cast2,              // 시전 시작2
    Hit,                // 적중
    Event,              // 특정 이벤트
    ProjectileCount,    // 투사체 카운트다운
    ProjectileAttach,   // 투사체 부착
    ProjectileExplode,  // 투사체 폭발
    Cancel,             // 시전 취소 / 끊김
}

public static class SkillSoundRouter
{
    // 네 프로젝트에 있을 법한 enum
    // 실제 enum 이름/타입 맞춰서 수정해줘
    public static void Play(PlayerController player, KeyCode skillKey, SkillSoundEvent evt, Vector3 position)
    {
        PlayEffect3D(player, skillKey, evt, position);
    }

    public static void Play(GameObject player, KeyCode skillKey, SkillSoundEvent evt, Vector3 position)
    {
        PlayerController p = player.GetComponentInChildren<PlayerController>();
        if (p == null)
            return;
        
        PlayEffect3D(p, skillKey, evt, position);
    }

    private static void PlayEffect3D(PlayerController player, KeyCode skillKey, SkillSoundEvent evt, Vector3 position)
    {
        CharacterType type = player.ObjInfo.Player.CharType;

        string soundKey = BuildSoundKey(type, skillKey, evt);

        player.Sound.GetEffect3D(soundKey, position);
    }

    private static string BuildSoundKey(CharacterType charType, KeyCode skillKey, SkillSoundEvent evt)
    {
        string charName = charType.ToString();
        string skillName = skillKey.ToString();

        return $"{charName}_{skillName}_{evt}";
    }
}
