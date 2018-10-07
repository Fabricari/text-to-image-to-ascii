using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace TextToImageToAscii
{
	class MainClass
	{
		static int ImageHeightInPixels = 100;

		//Declare WinApi function to get access to window handles
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr GetConsoleWindow();

		//Declare WinApi function to move the console window
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

		public static void Main(string[] args)
		{
			//format the console
			InitializeConsole();

			//convert words into ASCII art
			ConvertNamedObjectToConsoleArt();
		}

		private static void InitializeConsole()
		{
			//Set console title
			Console.Title = "Imposter Syndrome: Text to ASCII";

			//Set console to black (terminal on Mac displays white)
			Console.BackgroundColor = ConsoleColor.Black;

			//Clear console to fill entire screen
			Console.Clear();

			//Set text to mustard yellow
			Console.ForegroundColor = ConsoleColor.DarkYellow;

			//Get the handle for the console window using WinApi
			IntPtr ptr = GetConsoleWindow();

			//Move the window using WinApi
			MoveWindow(ptr, 0, 0, 0, 0, true);

			//Reset the cosole window to full size
			var width = Console.LargestWindowWidth - 3;
			var height = Console.LargestWindowHeight - 1;
			Console.SetWindowSize(width, height);

			//Set global Image Height in pixels
			var consoleBuffer = 2;
			ImageHeightInPixels = height - consoleBuffer;
		}

		private static void ConvertNamedObjectToConsoleArt()
		{
			try
			{
				//get user text
				var userText = GetUserText();

				//get image URL
				var imageUrl = GetImageUrl(userText);
				if (string.IsNullOrWhiteSpace(imageUrl)) throw new ApplicationException("No image URL returned for search.");

				//get byte-array from text
				var imageByteArray = GetImageByteArrayFromUrl(imageUrl);
				if (imageByteArray == null) throw new ApplicationException("Image not found.");

				//display image to console
				DisplayImageToConsole(imageByteArray);

				//prompt user to try again
				PromptUserToTryAgain();
			}
			catch (Exception ex)
			{
				//if there is an error, clear the console, alert user, and allow them to try again.
				Console.Clear();
				Console.WriteLine($"Oops! There was a problem with that search: {ex.Message}");
				PromptUserToTryAgain();
			}
		}

		private static string GetUserText()
		{
			//prompt user to enter name of image to ascii-fy
			Console.Write("Enter the name of an object to ASCIIify: ");

			//return user-entered text
			return Console.ReadLine();
		}

		private static void PromptUserToTryAgain()
		{
			//reset foreground/background colors
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.DarkYellow;

			//instruct user to hit key
			Console.Write("Hit any key to try again.");
			var key = Console.ReadKey();

			//if key is esc, exit program
			if (key.Key.Equals(ConsoleKey.Escape))
				Environment.Exit(0);

			//after user hits key, clear screen and repeat
			Console.Clear();
			ConvertNamedObjectToConsoleArt();
		}

		private static string GetImageUrl(string userText)
		{
			//get configuration settings
			var subscriptionKey = ConfigurationManager.AppSettings["BingImageSearch_SubscriptionKey"];
			var uriBase = ConfigurationManager.AppSettings["BingImageSearch_UriBase"];

			//get url of image from text using Bing Search API
			return new BingSearch(subscriptionKey, uriBase)
				.GetImageUrl(userText,
							 imageSize: ImageSize.Small,
							 aspectRatio: AspectRatio.Square);
		}

		private static Byte[] GetImageByteArrayFromUrl(string imageUrl)
		{
			using (HttpClient client = new HttpClient())
			{
				//make request
				var response = client.GetAsync(imageUrl).Result;

				//check status or throw exception
				if (!response.IsSuccessStatusCode) return null;

				//get response as byte-array
				return response.Content.ReadAsByteArrayAsync().Result;
			}
		}

		private static void DisplayImageToConsole(byte[] imageByteArray)
		{
			//note: MUST keep memory stream active while working with image
			using (var ms = new MemoryStream(imageByteArray))
			{
				//convert byte-array to image
				var source = new Bitmap(ms);

				//clear screen
				Console.Clear();

				//set image height
				int sMax = ImageHeightInPixels;

				decimal percent = Math.Min(decimal.Divide(sMax, source.Width), decimal.Divide(sMax, source.Height));
				Size dSize = new Size((int)(source.Width * percent), (int)(source.Height * percent));
				Bitmap bmpMax = new Bitmap(source, dSize.Width * 2, dSize.Height);
				for (int i = 0; i < dSize.Height; i++)
				{
					for (int j = 0; j < dSize.Width; j++)
					{
						WritePixelToConsole(bmpMax.GetPixel(j * 2, i));
						WritePixelToConsole(bmpMax.GetPixel(j * 2 + 1, i));
					}
					System.Console.WriteLine();
				}
				Console.ResetColor();
			}
		}

		private static void WritePixelToConsole(Color cValue)
		{
			//initialize array of 32-bit ARGB value
			int[] argbValues = { 0x000000, 0x000080, 0x008000, 0x008080, 0x800000, 0x800080, 0x808000, 0xC0C0C0, 0x808080, 0x0000FF, 0x00FF00, 0x00FFFF, 0xFF0000, 0xFF00FF, 0xFFFF00, 0xFFFFFF };

			//project argb values into array of system color values
			Color[] colorValues = argbValues.Select(argbValue => Color.FromArgb(argbValue)).ToArray();

			//initialize list of console "shade" characters
			char[] shadeSymbols = new char[] {
				(char)9617, //1/4
				(char)9618, //2/4
				(char)9619, //3/4
				(char)9608  //4/4
			};

			int[] bestHit = new int[] { 0, 0, 4, int.MaxValue }; //ForeColor, BackColor, Symbol, Score

			for (int shadeSymbol = shadeSymbols.Length; shadeSymbol > 0; shadeSymbol--)
			{
				for (int foregroundColor = 0; foregroundColor < colorValues.Length; foregroundColor++)
				{
					for (int backgroundColor = 0; backgroundColor < colorValues.Length; backgroundColor++)
					{
						int R = (colorValues[foregroundColor].R * shadeSymbol + colorValues[backgroundColor].R * (shadeSymbols.Length - shadeSymbol)) / shadeSymbols.Length;
						int G = (colorValues[foregroundColor].G * shadeSymbol + colorValues[backgroundColor].G * (shadeSymbols.Length - shadeSymbol)) / shadeSymbols.Length;
						int B = (colorValues[foregroundColor].B * shadeSymbol + colorValues[backgroundColor].B * (shadeSymbols.Length - shadeSymbol)) / shadeSymbols.Length;
						int score = (cValue.R - R) * (cValue.R - R) + (cValue.G - G) * (cValue.G - G) + (cValue.B - B) * (cValue.B - B);

						if (!(shadeSymbol > 1 && shadeSymbol < 4 && score > 50000)) // rule out too weird combinations
						{
							if (score < bestHit[3])
							{
								bestHit[3] = score; //Score
								bestHit[0] = foregroundColor;  //ForeColor
								bestHit[1] = backgroundColor;  //BackColor
								bestHit[2] = shadeSymbol;  //Symbol
							}
						}
					}
				}
			}

			Console.ForegroundColor = (ConsoleColor)bestHit[0];
			Console.BackgroundColor = (ConsoleColor)bestHit[1];
			Console.Write(shadeSymbols[bestHit[2] - 1]);
		}
	}
}
