using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class EmoticonController
{
    private PlayerController _owner;
    private int _emoticonId;

    // 최근 사용 시간 기록 (슬라이딩 윈도우용)
    private readonly Queue<float> _useTimes = new Queue<float>();

    private const int MAX_USES = 5;         // 5번까지 가능
    private const float WINDOW = 10f;       // 10초 창
    private const float MIN_INTERVAL = 1f;  // 사용 간 최소 텀 1초

    private float _lastUseTime = -999f;     // 연속 사용 방지용

    public EmoticonController(PlayerController owner)
    {
        _owner = owner;
        _emoticonId = GetSpriteId(owner.ObjInfo.Player.CharType);
    }

    public bool TryUseEmoticon()
    {
        float now = Time.time;

        // 1) 최소 텀(1초) 체크
        if (now - _lastUseTime < MIN_INTERVAL)
            return false;

        // 2) 5초 슬라이딩 윈도우 정리
        while (_useTimes.Count > 0 && now - _useTimes.Peek() > WINDOW)
            _useTimes.Dequeue();

        // 3) 5회 제한 체크
        if (_useTimes.Count >= MAX_USES)
            return false;

        // 4) 사용 허가 → 기록 저장
        _useTimes.Enqueue(now);
        _lastUseTime = now;

        // 5) 서버로 패킷 전송
        SendEmoticonPacket(_emoticonId);

        // 6) UI 재생 (내 화면)
        _owner.EmoticonUI.Play(_emoticonId);

        return true;
    }

    private void SendEmoticonPacket(int emoticonId)
    {
        if (_owner is MyPlayerController mpc)
        {
            C_Emoticon packet = new C_Emoticon()
            {
                ObjectId = _owner.Id,
                EmoticonId = emoticonId
            };

            mpc.SendPacket(packet);
        }
    }

    // 서버에서 브로드캐스트 받은 경우 (남이 쓴 이모티콘 재생)
    public void PlayEmoticonFromServer(int emoticonId)
    {
        _owner.EmoticonUI.Play(emoticonId);
    }

    private int GetSpriteId(CharacterType characterType)
    {
        switch (characterType)
        {
            case CharacterType.Rozzi:
                return 0;
            case CharacterType.Yuki:
                return 1;
            case CharacterType.Abigail:
                return 2;
            case CharacterType.Theodore:
                return 3;
            case CharacterType.Hyunwoo:
                return 4;
            default:
                return 0;
        }
    }
}
