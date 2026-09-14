namespace Opoint8182.Common
{
    public interface IDamageDealer
    {
        float Damage { get; }
        float HealthDamageMultiplier => 1f;
        float FuelDamageMultiplier => 1f;
    }
}
