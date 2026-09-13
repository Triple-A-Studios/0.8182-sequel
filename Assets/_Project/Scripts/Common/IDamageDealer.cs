using System;

namespace Opoint8182.Common
{
    public interface IDamageDealer
    {
        float Damage { get; }
        event Action<float> DamageDealt;
    }
}
