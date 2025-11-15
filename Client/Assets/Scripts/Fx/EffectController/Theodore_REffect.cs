using UnityEngine;

public class EffectController : MonoBehaviour
{
    [SerializeField] public Renderer effectRenderer;

    private static readonly int GradetionNoiseOpacityID = Shader.PropertyToID("_Gradetion_Noise_Opacity");

    public float activeValue = 0.5f;

    private const float InitialValue = 0.087f;

    private Material materialInstance;

    const float START_VALUE = 1;
    const float END_VALUE = 0.077f;

    bool _isActive = false;
    void Awake()
    {
        if (effectRenderer != null)
            materialInstance = effectRenderer.material;
    }
    private void Update()
    {
        if (_isActive)
        {
            float currentValue = materialInstance.GetFloat(GradetionNoiseOpacityID);
            float noiseOpacity = Mathf.Lerp(currentValue, END_VALUE, Time.deltaTime * 4.0f);
            materialInstance.SetFloat(GradetionNoiseOpacityID, noiseOpacity);
        }
    }
    private void OnEnable()
    {
        if (materialInstance != null)
        {
            materialInstance.SetFloat(GradetionNoiseOpacityID, START_VALUE);
            _isActive = true;
        }
    }


}