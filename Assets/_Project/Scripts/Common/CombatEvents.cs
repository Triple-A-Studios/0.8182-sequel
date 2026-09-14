using System;

namespace Opoint8182.Common
{
    public static class CombatEvents
    {
        public static event Action<IDamageDealer, float> DamageDealt;
        public static event Action<IRestorer, float> Restored;
        public static event Action<ICrashSource, float> Crashed;

        public static void RaiseDamageDealt(IDamageDealer source, float damage) => DamageDealt?.Invoke(source, damage);
        public static void RaiseRestored(IRestorer source, float amount) => Restored?.Invoke(source, amount);
        public static void RaiseCrashed(ICrashSource source, float quality) => Crashed?.Invoke(source, quality);
    }
}
