using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Managers : MonoBehaviour
{
    static Managers s_instance; // 유일성이 보장된다
    static Managers Instance { get { Init(); return s_instance; } } // 유일한 매니저를 갖고온다

    #region Contents
    InfoManager _info = new InfoManager();
    ObjectManager _obj = new ObjectManager();
    NetworkManager _network = new NetworkManager();
    CombatTextManager _combatText = new CombatTextManager();
    private FXManager _fx;
    public static FXManager FX { get { return s_instance._fx; } }

    public static InfoManager Info { get { return Instance._info; } }
    public static ObjectManager Object { get { return Instance._obj; } }
    public static NetworkManager Network { get { return Instance._network; } }
    public static CombatTextManager CombatText { get { return Instance._combatText; } }
    #endregion

    #region Core
    DataManager _data = new DataManager();
    PoolManager _pool = new PoolManager();
    ResourceManager _resource = new ResourceManager();
    SceneManagerEx _scene = new SceneManagerEx();
    SoundManager _sound = new SoundManager();
    UIManager _ui = new UIManager();

    public static DataManager Data { get { return Instance._data; } }
    public static PoolManager Pool { get { return Instance._pool; } }
    public static ResourceManager Resource { get { return Instance._resource; } }
    public static SceneManagerEx Scene { get { return Instance._scene; } }
    public static SoundManager Sound { get { return Instance._sound; } }
    public static UIManager UI { get { return Instance._ui; } }
    #endregion

    void Start()
    {
        Init();
	}

    void Update()
    {
        _network.Update();
    }

    static void Init()
    {
        if (s_instance == null)
        {
			GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }
            DontDestroyOnLoad(go);
            s_instance = go.GetComponent<Managers>();

            GameObject fxManagerGo = new GameObject { name = "FXManager" };
            fxManagerGo.transform.parent = go.transform;
            s_instance._fx = fxManagerGo.AddComponent<FXManager>();

            s_instance._network.Init();
            s_instance._data.Init();
            s_instance._pool.Init();
            s_instance._combatText.Init();
            s_instance._sound.Init();
            s_instance._fx.Init();
        }		
	}

    public static void Clear()
    {
        Sound.Clear();
        Scene.Clear();
        UI.Clear();
        Pool.Clear();
        FX.Clear();
    }
}
