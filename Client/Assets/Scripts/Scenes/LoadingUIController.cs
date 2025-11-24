using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUIController : MonoBehaviour
{
    public static LoadingUIController Instance;

    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText;

    private void Awake()
    {
        Instance = this;

        progressBar.value = 0f;
        progressText.text = "0%";
    }

    public void SetProgress(float value)
    {
        progressBar.value = value;
        progressText.text = $"{(value * 100f):0}%";
    }
}
