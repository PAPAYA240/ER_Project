using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_KDA : Monobehaviour
{
    enum Texts 
    { 
        Kill, Death, Asist
    }

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
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

    public void SetKDA(int kill, int death, int asist)
    {
        GetText((int)Texts.Kill).text = kill.ToString();
        GetText((int)Texts.Death).text = death.ToString();
        GetText((int)Texts.Asist).text = asist.ToString();
    }
}
