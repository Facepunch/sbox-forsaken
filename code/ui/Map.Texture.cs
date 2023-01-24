using Sandbox;
using System;

namespace Facepunch.Forsaken.UI;

public partial class Map
{
    public static Texture Texture { get; private set; }

	private static byte[] Data { get; set; }

	public static void Draw( int cx, int cy, float size, Color32 color )
	{
		var renderRadius = size * ((float)Math.PI * 0.5f);
		var xMin = (int)Math.Max( cx - size, 0 );
		var xMax = (int)Math.Min( cx + size, 1024 - 1 );
		var yMin = (int)Math.Max( cy - size, 0 );
		var yMax = (int)Math.Min( cy + size, 1024 - 1 );

		for ( int x = xMin; x < xMax; x++ )
		{
			for ( int y = yMin; y < yMax; y++ )
			{
				var index = ((y * 1024) + x) * 4;
				var p = new Vector3( cx - x, cy - y, 0f );
				var a = (p.Length - renderRadius) * 0.1f * 255;
				var b = a < 0 ? 0 : (a > 255 ? 255 : a);
				var sdf = (byte)(255 - b);

				Data[index + 0] = color.r;
				Data[index + 1] = color.g;
				Data[index + 2] = color.b;
				Data[index + 3] = sdf;
			}
		}

		Texture.Update( Data );
	}

	public static Texture CreateTexture()
	{
		if ( Texture is not null ) return Texture;

		Data = new byte[1024 * 1024 * 4];

		for ( int y = 0; y < 1024; y++ )
		{
			for ( int x = 0; x < 1024; x++ )
			{
				var index = ((y * 1024) + x) * 4;
				Data[index + 0] = 0;
				Data[index + 1] = 0;
				Data[index + 2] = 0;
				Data[index + 3] = 0;
			}
		}

		Texture = new Texture2DBuilder()
			.WithFormat( ImageFormat.RGBA8888 )
			.WithDynamicUsage()
			.WithSize( 1024, 1024 )
			.WithData( Data )
			.Finish();

		return Texture;
	}
}
