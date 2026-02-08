using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class UI_TraitSelect : Monobehaviour
{
    [SerializeField]
    UI_TraitButton _havocFirstButton;

    public override void Init()
    {
        
    }

    private void Awake()
    {
        Init();
    }

    void Start()
    {
        if (_havocFirstButton != null)
            _havocFirstButton.SetSelected(true);
    }

    void Update()
    {

    }
}
