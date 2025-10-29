using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager
{
	#region Pool
	class Pool
    {
        public GameObject Original { get; private set; }
        public Transform Root { get; set; }

        Stack<Poolable> _poolStack = new Stack<Poolable>();

        public void Init(GameObject original, int count = 5)
        {
            Original = original;
            Root = new GameObject().transform;
            Root.name = $"{original.name}_Root";

            for (int i = 0; i < count; i++)
                Push(Create());
        }

        Poolable Create()
        {
            GameObject go = Object.Instantiate<GameObject>(Original);
            go.name = Original.name;
            return go.GetOrAddComponent<Poolable>();
        }

        public void Push(Poolable poolable)
        {
            if (poolable == null)
                return;

            RectTransform rectTransform = poolable.transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.SetParent(Root, false);
            }
            else
            {
                poolable.transform.parent = Root;
            }

            poolable.gameObject.SetActive(false);
            poolable.IsUsing = false;

            _poolStack.Push(poolable);
        }

        public Poolable Pop(Transform parent)
        {
            Poolable poolable;

            if (_poolStack.Count > 0)
                poolable = _poolStack.Pop();
            else
                poolable = Create();

            if (poolable == null)
                return null;

            poolable.gameObject.SetActive(true);


            // === 경고 발생 가능성 있는 부분 시작 ===
            // 이 로직도 RectTransform 여부를 확인하여 SetParent를 쓰는 것이 좋습니다.
            if (parent == null)
            {
                RectTransform rectTransform = poolable.transform as RectTransform;
                if (rectTransform != null)
                {
                    rectTransform.SetParent(Managers.Scene.CurrentScene.transform, false);
                }
                else
                {
                    poolable.transform.parent = Managers.Scene.CurrentScene.transform;
                }
            }
            // === 경고 발생 가능성 있는 부분 끝 ===

            // === 경고 발생 가능성 있는 부분 시작 ===
            // 최종 parent 설정 부분입니다. 여기도 RectTransform 여부를 확인해야 합니다.
            RectTransform finalRectTransform = poolable.transform as RectTransform;
            if (finalRectTransform != null)
            {
                finalRectTransform.SetParent(parent, false);
            }
            else
            {
                poolable.transform.parent = parent;
            }
            // === 경고 발생 가능성 있는 부분 끝 ===

            // DontDestroyOnLoad 해제 용도
            //if (parent == null)
            //    poolable.transform.parent = Managers.Scene.CurrentScene.transform;

            //poolable.transform.parent = parent;
            poolable.IsUsing = true;

            return poolable;
        }
    }
	#endregion

	Dictionary<string, Pool> _pool = new Dictionary<string, Pool>();
    Transform _root;

    public void Init()
    {
        if (_root == null)
        {
            _root = new GameObject { name = "@Pool_Root" }.transform;
            Object.DontDestroyOnLoad(_root);
        }
    }

    public void CreatePool(GameObject original, int count = 5)
    {
        Pool pool = new Pool();
        pool.Init(original, count);
        pool.Root.parent = _root;

        _pool.Add(original.name, pool);
    }

    public void Push(Poolable poolable)
    {
        string name = poolable.gameObject.name;
        if (_pool.ContainsKey(name) == false)
        {
            GameObject.Destroy(poolable.gameObject);
            return;
        }

        _pool[name].Push(poolable);
    }

    public Poolable Pop(GameObject original, Transform parent = null)
    {
        if (_pool.ContainsKey(original.name) == false)
            CreatePool(original);

        return _pool[original.name].Pop(parent);
    }

    public GameObject GetOriginal(string name)
    {
        if (_pool.ContainsKey(name) == false)
            return null;
        return _pool[name].Original;
    }

    public void Clear()
    {
        foreach (Transform child in _root)
            GameObject.Destroy(child.gameObject);

        _pool.Clear();
    }
}
