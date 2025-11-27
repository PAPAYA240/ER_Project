using UnityEngine;

[ExecuteAlways] // 에디터 + 플레이 둘 다에서 실행
[RequireComponent(typeof(ParticleSystemRenderer))]
public class FX_Rozzi_R_Pattern_Gold : MonoBehaviour
{
    public ParticleSystem ps;                   // 파티클
    public float duration = 3f;                 // 0→1 가는 시간
    public string propertyName = "_Progress";   // Shader Graph Reference

    ParticleSystemRenderer _renderer;
    MaterialPropertyBlock _mpb;
    float _time;
    bool _wasPlaying;

    void Reset()
    {
        if (!ps)
            ps = GetComponent<ParticleSystem>();
    }

    void Awake()
    {
        _renderer = GetComponent<ParticleSystemRenderer>();
        _mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        _time = 0f;
        ApplyProgress(0f);
    }

    void Update()
    {
        if (!ps || !_renderer)
            return;

        bool playing = ps.isPlaying;

        // ▶ Play 버튼을 막 누른 순간(= false → true 변화) 감지
        if (playing && !_wasPlaying)
        {
            _time = 0f;          // 시간 리셋
            ApplyProgress(0f);   // 0부터 다시 시작
        }

        if (playing)
        {
            // 에디터에서도 돌아가도록 Time.deltaTime 사용
            _time += Time.deltaTime;
            float t = Mathf.Clamp01(_time / duration);
            ApplyProgress(t);           
        }

        _wasPlaying = playing;
    }

    void ApplyProgress(float value)
    {
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(propertyName, value);
        _renderer.SetPropertyBlock(_mpb);
    }
}
