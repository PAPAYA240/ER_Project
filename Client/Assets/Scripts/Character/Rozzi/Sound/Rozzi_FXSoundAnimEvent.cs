using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Windows;
using static System.Runtime.CompilerServices.RuntimeHelpers;

public class Rozzi_FXSoundAnimEvent : MonoBehaviour
{
    private PlayerController _owner;

    private void Awake()
    {
        _owner = GetComponentInParent<PlayerController>();
    }

    // 애니메이션 이벤트에서 직접 호출할 함수
    public void OnSkillAnimEvent(string msg)
    {
        if (_owner == null)
            return;

        // "Cast|Q"
        var tokens = msg.Split('|');
        string eventName = tokens[0]; // Cast
        string keyStr = tokens.Length > 1 ? tokens[1] : null;

        if (!TryParseEvent(eventName, out var evt))
        {
            //Debug.LogWarning($"[Rozzi_FXSoundAnimEvent] Unknown eventName: {eventName}");
            return;
        }

        if (!TryParseKeyCode(keyStr, out var key))
        {
            //Debug.LogWarning($"[Rozzi_FXSoundAnimEvent] Unknown keyCode: {keyStr}");
            return;
        }

        SkillSoundRouter.Play(_owner, key, evt, _owner.transform.position);
    }

    private bool TryParseEvent(string eventName, out SkillSoundEvent evt)
    {
        return System.Enum.TryParse(eventName, out evt);
    }

    private bool TryParseKeyCode(string keyCode, out KeyCode key)
    {
        return System.Enum.TryParse<KeyCode>(keyCode, out key);
    }
}
