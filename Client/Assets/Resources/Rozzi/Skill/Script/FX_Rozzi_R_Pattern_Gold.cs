using UnityEngine;

[RequireComponent(typeof(Renderer))] // MeshRenderer, ParticleSystemRenderer 둘 다 포함
public class FX_Rozzi_R_Pattern_Gold : MonoBehaviour
{
    [Tooltip("0 → 1 까지 차오르는 데 걸리는 시간 (초)")]
    public float duration = 3f;

    [Tooltip("Shader Graph에서 Progress 프로퍼티 Reference 이름")]
    public string propertyName = "_Progress";

    private ParticleSystemRenderer _renderer;
    private MaterialPropertyBlock _mpb;
    private float _time;

    private void Awake()
    {
        _renderer = GetComponent<ParticleSystemRenderer>();
        _mpb = new MaterialPropertyBlock();
        _time = 0f;
        SetProgress(0f);
    }

    private void OnEnable()
    {
        // 오브젝트가 활성화될 때마다 항상 0부터 다시 시작
        _time = 0f;
        SetProgress(0f);
    }

    void OnDisable()
    {
        SetProgress(0f); // 풀로 돌아갈 때 항상 0으로
    }

    private void Update()
    {
        // 인게임(Play 모드)에서만 동작
        //if (!Application.isPlaying)
        //    return;

        _time += Time.deltaTime;
        float t = Mathf.Clamp01(_time / duration);
        SetProgress(t);
    }

    private void SetProgress(float value)
    {
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(propertyName, value);
        _renderer.SetPropertyBlock(_mpb);

        //Debug.Log($"_mpb : {_mpb.GetFloat("_Progress")}, value : {value}");
    }
}
