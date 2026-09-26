using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Opoint8182.Bootstrap
{
    public readonly struct AsyncOperationGroup
    {
        public readonly List<AsyncOperation> operations;

        public float Progress => operations.Count == 0 ? 0f : operations.Average(o => o.progress);
        public bool IsDone => operations.All(o => o.isDone);

        public AsyncOperationGroup(int initialCapacity)
        {
            operations = new List<AsyncOperation>(initialCapacity);
        }
    }
}
