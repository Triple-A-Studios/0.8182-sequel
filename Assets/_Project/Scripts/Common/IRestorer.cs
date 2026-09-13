using System;

namespace Opoint8182.Common
{
    public interface IRestorer
    {
        float Restore { get; }
        event Action<float> Restored;
    }
}
