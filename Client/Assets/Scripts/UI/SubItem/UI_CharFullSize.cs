using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_CharFullSize : Monobehaviour
{
    enum Images { CharImg }

    public override void Init()
    {
        Bind<Image>(typeof(Images));

        //SetImage("Sprite/CharFull_Yuki_S005");
    }

    private void Awake()
    {
        Init();
    }

    void Start()
    {
       
    }

    void Update()
    {
        
    }

    public void SetImage(string path)
    {
        Image img = GetImage((int)Images.CharImg);
        img.sprite = Managers.Resource.Load<Sprite>(path);

        Vector2 size = img.sprite.rect.size;
        RectTransform rt = img.gameObject.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        float ratio = 400f / Mathf.Max(rt.sizeDelta.x, rt.sizeDelta.y);
        rt.localScale = new Vector3(ratio, ratio, ratio);
    }
}
