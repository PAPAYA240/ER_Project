using System.Collections.Generic;
using UnityEngine;

public class HyunwooSound : MonoBehaviour
{

    enum Sounds
    {
        Q_1, Q_2, Q_3,
        W_1, W_2, W_3,
        E_1, E_2, E_3,
        R_1, R_2, R_3,
    }

    Dictionary<string, string> _sounds = new Dictionary<string, string>();

    private void Awake()
    {
        _sounds.Add("Q_1", "Hyunwoo/Resources/sound/voice/hyunwoo/s000/ko/Hyunwoo_PlaySkill1007200Seq0_1_ko");
        _sounds.Add("Q_2", "Hyunwoo/Resources/sound/voice/hyunwoo/s000/ko/Hyunwoo_PlaySkill1007200Seq0_2_ko");
        _sounds.Add("Q_3", "Hyunwoo/Resources/sound/voice/hyunwoo/s000/ko/Hyunwoo_PlaySkill1007200Seq0_3_ko");
        _sounds.Add("W_1", "Hyunwoo/Resources/sound/voice/hyunwoo/s000/ko/Hyunwoo_PlaySkill1007300Seq0_1_ko");
        _sounds.Add("W_2", "Hyunwoo/Resources/sound/voice/hyunwoo/s000/ko/Hyunwoo_PlaySkill1007300Seq0_2_ko");
        _sounds.Add("W_3", "Hyunwoo/Resources/sound/voice/hyunwoo/s000/ko/Hyunwoo_PlaySkill1007300Seq0_3_ko");
        _sounds.Add("E_1", "Hyunwoo/Resources/sound/voice/hyunwoo/s000/ko/Hyunwoo_PlaySkill1007400Seq0_1_ko");
        _sounds.Add("E_2", "Hyunwoo/Resources/sound/voice/hyunwoo/s000/ko/Hyunwoo_PlaySkill1007400Seq0_2_ko");
        _sounds.Add("E_3", "Hyunwoo/Resources/sound/voice/hyunwoo/s000/ko/Hyunwoo_PlaySkill1007400Seq0_3_ko");
        _sounds.Add("R_1", "Hyunwoo/Resources/sound/voice/hyunwoo/s000/ko/Hyunwoo_PlaySkill1007500Seq0_1_ko");
        _sounds.Add("R_2", "Hyunwoo/Resources/sound/voice/hyunwoo/s000/ko/Hyunwoo_PlaySkill1007500Seq0_2_ko");
        _sounds.Add("R_3", "Hyunwoo/Resources/sound/voice/hyunwoo/s000/ko/Hyunwoo_PlaySkill1007500Seq0_3_ko");
    }

    void Start()
    {
        
    }

    
    void Update()
    {
        
    }

    public void UseSkill(string keyCode)
    {
        string key = keyCode.ToString() + $"_{Random.Range(1,4)}";
        Managers.Sound.Play(_sounds[key], Define.Sound.Effect, 0.1f);
    }
}
