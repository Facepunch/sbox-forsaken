using Sandbox;

namespace Facepunch.Forsaken;

public interface IDamageable : IValid
{
	public void TakeDamage( DamageInfo info );
	public float Health { get; }
	public float MaxHealth { get; }
	public LifeState LifeState { get; }
}
