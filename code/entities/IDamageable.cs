using Sandbox;

namespace Facepunch.Forsaken;

public interface IDamageable : IValid
{
	public void TakeDamage( DamageInfo info );
	public Vector3 Position { get; set; }
	public float Health { get; }
	public float MaxHealth { get; }
	public LifeState LifeState { get; }
}
