using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TextToImageToAscii
{
	internal static class PixelToCharacterUtility
	{
		//array of the classic "16 named colors"
		//source: https://en.wikipedia.org/wiki/Web_colors#Hex_triplet
		private static int[] ArgbValues = {
			0x000000,	//black
			0x000080,	//navy
			0x008000,	//green
			0x008080,	//teal
			0x800000,	//maroon
			0x800080,	//purple
			0x808000,	//olive
			0xC0C0C0,	//silver
			0x808080,	//grey
			0x0000FF,	//blue
			0x00FF00,	//lime
			0x00FFFF,	//aqua
			0xFF0000,	//red
			0xFF00FF,	//fuchsia
			0xFFFF00,	//yellow
			0xFFFFFF	//white
		};

		//list of console "shade" characters
		private static char[] ShadeSymbols = new char[] {
			(char)9617,	//1/4
			(char)9618,	//2/4
			(char)9619,	//3/4
			(char)9608	//4/4
		};

		//character cache
		private static Dictionary<int, (ConsoleColor foregroundColor, ConsoleColor backgroundColor, char consoleCharacter)> CharacterCache = new Dictionary<int, (ConsoleColor foregroundColor, ConsoleColor backgroundColor, char consoleCharacter)>();

		//argb values projected into array of system color values
		private static Color[] _colors;
		private static Color[] ColorValues =>
			_colors ?? (_colors = ArgbValues.Select(argbValue => Color.FromArgb(argbValue)).ToArray());

		internal static (ConsoleColor foregroundColor, ConsoleColor backgroundColor, char consoleCharacter) GetCharacterProperties(Color pixelColor)
		{
			//check character cache
			if (CharacterCache.ContainsKey(pixelColor.GetHashCode()))
				return CharacterCache[pixelColor.GetHashCode()];

			//initialize "rounded" pixel values
			int[] bestHit = new int[] { 0, 0, 4, int.MaxValue }; //ForeColor, BackColor, Symbol, Score

			//we will loop through every possible color combination and rank how closely it matches the actual pixel

			//loop through possible shade values
			for (int shadeSymbol = ShadeSymbols.Length; shadeSymbol > 0; shadeSymbol--)
			{
				//loop through possible foreground color values
				for (int foregroundColor = 0; foregroundColor < ColorValues.Length; foregroundColor++)
				{
					//loop through possilbe background colors
					for (int backgroundColor = 0; backgroundColor < ColorValues.Length; backgroundColor++)
					{
						//create RGB color value
						int R = (ColorValues[foregroundColor].R * shadeSymbol + ColorValues[backgroundColor].R * (ShadeSymbols.Length - shadeSymbol)) / ShadeSymbols.Length;
						int G = (ColorValues[foregroundColor].G * shadeSymbol + ColorValues[backgroundColor].G * (ShadeSymbols.Length - shadeSymbol)) / ShadeSymbols.Length;
						int B = (ColorValues[foregroundColor].B * shadeSymbol + ColorValues[backgroundColor].B * (ShadeSymbols.Length - shadeSymbol)) / ShadeSymbols.Length;

						//tally a score of how close the resulting RGB color matches the actual pixel
						int score = (pixelColor.R - R) * (pixelColor.R - R) + (pixelColor.G - G) * (pixelColor.G - G) + (pixelColor.B - B) * (pixelColor.B - B);

						//exclude out-lyers
						if (!(shadeSymbol > 1 && shadeSymbol < 4 && score > 50000)) // rule out too weird combinations
						{
							//if the score is below the threshold of the previous best, then replace
							if (score < bestHit[3])
							{
								bestHit[3] = score;
								bestHit[0] = foregroundColor;
								bestHit[1] = backgroundColor;
								bestHit[2] = shadeSymbol;
							}
						}
					}
				}
			}

			//create tuple
			var result = (foregroundColor: (ConsoleColor)bestHit[0], 
				          backgroundColor: (ConsoleColor)bestHit[1], 
						  consoleCharacter: ShadeSymbols[bestHit[2] - 1]);

			//cache the result
			CharacterCache.Add(pixelColor.GetHashCode(), result);

			//return tuple of console character attributes
			return result;
		}
	}
}
