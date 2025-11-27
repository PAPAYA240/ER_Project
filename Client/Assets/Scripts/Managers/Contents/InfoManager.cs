using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf.Protocol;

public class InfoManager
{
    public int Team { get; set; }

    public string UserName {  get; set; }

    public int PickIdx { get; set; }

    public List<PickScenePlayerInfo> _pspiList = new List<PickScenePlayerInfo>();

    public bool IsReady { get; set; } = false;
}
