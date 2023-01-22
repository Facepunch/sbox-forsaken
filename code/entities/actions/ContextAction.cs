using Sandbox;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Facepunch.Forsaken;

public class ContextAction : EqualityComparer<ContextAction>, IValid
{
	public string Id { get; private set; }
	public string Name { get; private set; }
	public string Icon { get; private set; }
	public bool IsValid => !string.IsNullOrEmpty( Id );
	public int Hash { get; private set; }

	private Func<ForsakenPlayer, Availability> Condition { get; set; }

	public struct Availability
	{
		public bool IsAvailable { get; set; }
		public string Message { get; set; }
	}

	public ContextAction( string id, string name, string icon )
	{
		Id = id;
		Name = name;
		Icon = icon;
		Hash = id.FastHash();
	}

	public void SetCondition( Func<ForsakenPlayer, Availability> condition )
	{
		Condition = condition;
	}

	public bool IsAvailable( ForsakenPlayer player )
	{
		return Condition?.Invoke( player ).IsAvailable ?? true;
	}

	public Availability GetAvailability( ForsakenPlayer player )
	{
		return Condition?.Invoke( player ) ?? new Availability { IsAvailable = true };
	}

	public override bool Equals( ContextAction x, ContextAction y )
	{
		return x.Id == y.Id;
	}

	public override int GetHashCode( [DisallowNull] ContextAction obj )
	{
		return Id.GetHashCode();
	}
}
