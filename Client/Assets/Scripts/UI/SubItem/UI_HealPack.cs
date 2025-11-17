using TMPro;
using UnityEngine;

public class UI_HealPack : UI_Base
{
    enum Texts
    {
        Sec_Text,
    }

    enum Images
    {
        Progress,
    }

    const float MAX_TIME = 75;
    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<UnityEngine.UI.Image>(typeof(Images));
    }

    void Update()
    {
    }
    public void SetSecText(int sec)
    {
        if (sec <= 0)
            GetText((int)Texts.Sec_Text).gameObject.SetActive(false);
        else
            GetText((int)Texts.Sec_Text).gameObject.SetActive(true);

        GetText((int)Texts.Sec_Text).text = $"{sec.ToString()}s";
    }

    public void SetProgressAmount(float currentRemainingTime)
    {
        float ratio = currentRemainingTime / MAX_TIME; 
        float progress = 1.0f - ratio;
        GetImage((int)Images.Progress).fillAmount = progress;
    }
}
