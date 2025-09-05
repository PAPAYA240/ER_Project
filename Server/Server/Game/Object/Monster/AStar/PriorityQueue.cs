using System;
using System.Collections.Generic;

public class MyPriorityQueue<TElement> where TElement : IComparable<TElement>
{
    private List<TElement> _heap = new List<TElement>();

    public int Count => _heap.Count;

    public void Enqueue(TElement element)
    {
        _heap.Add(element);
        SiftUp(_heap.Count - 1);
    }
    public TElement Dequeue()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        var result = _heap[0];
        _heap[0] = _heap[_heap.Count - 1];
        _heap.RemoveAt(_heap.Count - 1);

        if (_heap.Count > 1)
        {
            SiftDown(0);
        }

        return result;
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (_heap[index].CompareTo(_heap[parentIndex]) >= 0)
            {
                break;
            }

            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void SiftDown(int index)
    {
        int leftChildIndex = 2 * index + 1;
        while (leftChildIndex < _heap.Count)
        {
            int rightChildIndex = leftChildIndex + 1;
            int smallerChildIndex = leftChildIndex;

            if (rightChildIndex < _heap.Count && _heap[rightChildIndex].CompareTo(_heap[leftChildIndex]) < 0)
            {
                smallerChildIndex = rightChildIndex;
            }

            if (_heap[index].CompareTo(_heap[smallerChildIndex]) <= 0)
            {
                break;
            }

            Swap(index, smallerChildIndex);
            index = smallerChildIndex;
            leftChildIndex = 2 * index + 1;
        }
    }

    private void Swap(int i, int j)
    {
        var temp = _heap[i];
        _heap[i] = _heap[j];
        _heap[j] = temp;
    }
}