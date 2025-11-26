using UnityEngine;
using UnityEngine.UI;

public class UI_Fade : UI_Base
{
    enum Images
    { 
        Fade 
    }

    float _fadeSpeed = 7.5f;

    public override void Init()
    {
        Bind<Image>(typeof(Images));
    }

    void Update()
    {
        Color imageColor = GetImage((int)Images.Fade).color;
        imageColor.a = 0.5f + 0.5f * Mathf.Sin(Time.time * _fadeSpeed);
        GetImage((int)Images.Fade).color = imageColor;
    }


}
