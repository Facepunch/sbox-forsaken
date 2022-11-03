using Sandbox;
using SandboxEditor;
using System.Linq;

namespace Facepunch.Forsaken;

public class DayNightGradient
{
	private struct GradientNode
	{
		public Color Color;
		public float Time;

		public GradientNode( Color color, float time )
		{
			Color = color;
			Time = time;
		}
	}

	private GradientNode[] Nodes;

	public DayNightGradient( Color dawnColor, Color dayColor, Color duskColor, Color nightColor )
	{
		Nodes = new GradientNode[7];
		Nodes[0] = new GradientNode( nightColor, 0f );
		Nodes[1] = new GradientNode( nightColor, 0.2f );
		Nodes[2] = new GradientNode( dawnColor, 0.3f );
		Nodes[3] = new GradientNode( dayColor, 0.5f );
		Nodes[4] = new GradientNode( dayColor, 0.7f );
		Nodes[5] = new GradientNode( duskColor, 0.85f );
		Nodes[6] = new GradientNode( nightColor, 1f );
	}

	public Color Evaluate( float fraction )
	{
		for ( var i = 0; i < Nodes.Length; i++ )
		{
			var node = Nodes[i];
			var nextIndex = i + 1;

			if ( Nodes.Length < nextIndex )
				nextIndex = 0;

			var nextNode = Nodes[nextIndex];

			if ( fraction >= node.Time && fraction <= nextNode.Time )
			{
				var duration = (nextNode.Time - node.Time);
				var interpolate = (1f / duration) * (fraction - node.Time);

				return Color.Lerp( node.Color, nextNode.Color, interpolate );
			}
		}

		return Nodes[0].Color;
	}
}

[Title( "Time Controller" )]
[EditorSprite( "editor/time_controller.vmat" )]
[Description( "Controls the day and night cycle in the game and defines the colors to blend between." )]
[HammerEntity]
public partial class TimeController : ModelEntity
{
	[Property( Title = "Dawn Color" )]
	public Color DawnColor { get; set; } = Color.Orange.Darken( 0.5f );

	[Property( Title = "Dawn Sky Color" )]
	public Color DawnSkyColor { get; set; } = Color.Orange.Darken( 0.5f );

	[Property( Title = "Day Color" )]
	public Color DayColor { get; set; } = Color.Yellow.Lighten( 0.8f );

	[Property( Title = "Day Sky Color" )]
	public Color DaySkyColor { get; set; } = Color.Blue.Lighten( 0.8f );

	[Property( Title = "Dusk Color" )]
	public Color DuskColor { get; set; } = Color.Orange.Darken( 0.5f );

	[Property( Title = "Dusk Sky Color" )]
	public Color DuskSkyColor { get; set; } = Color.Orange.Darken( 0.5f );

	[Property( Title = "Night Color" )]
	public Color NightColor { get; set; } = Color.Blue.Darken( 0.9f );

	[Property( Title = "Night Sky Color" )]
	public Color NightSkyColor { get; set; } = Color.Black.Lighten( 0.2f );

	protected Output OnBecomeNight { get; set; }
	protected Output OnBecomeDusk { get; set; }
	protected Output OnBecomeDawn { get; set; }
	protected Output OnBecomeDay { get; set; }

	public EnvironmentLightEntity Environment
	{
		get
		{
			if ( InternalEnvironment == null )
				InternalEnvironment = All.OfType<EnvironmentLightEntity>().FirstOrDefault();
			return InternalEnvironment;
		}
	}

	private EnvironmentLightEntity InternalEnvironment;
	private DayNightGradient SkyColorGradient;
	private DayNightGradient ColorGradient;

	public override void Spawn()
	{
		ColorGradient = new DayNightGradient( DawnColor, DayColor, DuskColor, NightColor );
		SkyColorGradient = new DayNightGradient( DawnSkyColor, DaySkyColor, DuskSkyColor, NightSkyColor );

		TimeSystem.OnSectionChanged += HandleTimeSectionChanged;

		base.Spawn();
	}

	private void HandleTimeSectionChanged( TimeSection section )
	{
		if ( section == TimeSection.Dawn )
			OnBecomeDawn.Fire( this );
		else if ( section == TimeSection.Day )
			OnBecomeDay.Fire( this );
		else if ( section == TimeSection.Dusk )
			OnBecomeDusk.Fire( this );
		else if ( section == TimeSection.Night )
			OnBecomeNight.Fire( this );
	}

	[Event.Tick.Server]
	private void Tick()
	{
		var environment = Environment;
		if ( !environment.IsValid() ) return;

		var sunAngle = ((TimeSystem.TimeOfDay / 24f) * 360f);
		var radius = 10000f;

		environment.Color = ColorGradient.Evaluate( (1f / 24f) * TimeSystem.TimeOfDay );
		environment.SkyColor = SkyColorGradient.Evaluate( (1f / 24f) * TimeSystem.TimeOfDay );

		environment.Position = Vector3.Zero + Rotation.From( 0, 0, sunAngle + 60f ) * ( radius * Vector3.Right );
		environment.Position += Rotation.From( 0, sunAngle, 0 ) * ( radius * Vector3.Forward );

		var direction = (Vector3.Zero - environment.Position).Normal;
		environment.Rotation = Rotation.LookAt( direction, Vector3.Up );
	}
}
