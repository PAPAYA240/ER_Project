using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class CoroutineRunner
{
    private readonly List<IEnumerator> _coroutines = new List<IEnumerator>();

    public void Start(IEnumerator routine) => _coroutines.Add(routine);

    public void Update()
    {
        for (int i = 0; i < _coroutines.Count; i++)
        {
            if (!_coroutines[i].MoveNext())
                _coroutines.RemoveAt(i--);
        }
    }
}

