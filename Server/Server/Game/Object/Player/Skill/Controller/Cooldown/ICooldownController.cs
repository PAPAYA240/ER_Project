using System;
using System.Collections.Generic;
using System.Text;
using static Server.Data.DataUtils;

public interface ICooldownController
{
    bool IsReady(KeyCode key);
    float GetRemaining(KeyCode key);                                        // 남은 초 (준비되면 0)

    void Start(KeyCode key, float durationSec, int? startTick = null);      // tick기반 시작
    void Reduce(KeyCode key, float seconds);                                // 외부 임의 감소(예: 적중 시 -x초)
    void SetRemaining(KeyCode key, float seconds);                          // 강제 설정(테스트/특수효과)

    // 쿨타임이 종료되었을 때 이벤트 발생용
    //void Update(int nowTick);                                               
    //event Action<KeyCode> OnReady;                                          // 쿨이 0 되는 순간 1회 콜백
}

