using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace TextToImageToAscii
{
	/// <summary>
	/// What is the Bing Image Search API?
	/// https://docs.microsoft.com/en-us/azure/cognitive-services/Bing-Image-Search/overview
	/// </summary>
	class MainClass
	{
		//Declare WinApi function to get access to window handles
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr GetConsoleWindow();

		//Declare WinApi function to move the console window
		[DllImport("user32.dll", SetLastError = true)]
		static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

		public static void Main(string[] args)
		{
			//position console window
			PositionConsoleWindow();

			//format the console
			InitializeConsole();

			//convert words into ASCII art
			ConvertNamedObjectToConsoleArt();
		}

		private static void PositionConsoleWindow() => MoveWindow(GetConsoleWindow(), 0, 0, 0, 0, true);

		private static int ImageHeightInPixels => Console.WindowHeight - 1;

		private static void InitializeConsole()
		{
			Console.Title = "Imposter Syndrome: Text to ASCII";
			ResetConsoleColors();
			Console.Clear();
			Console.SetWindowSize(
				Console.LargestWindowWidth - 3, 
				Console.LargestWindowHeight - 1);
		}

		private static void ResetConsoleColors()
		{
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.DarkYellow;
		}

		private static void ConvertNamedObjectToConsoleArt()
		{
			try
			{
				DisplayImageToConsole(GetImageByteArrayFromUrl(GetImageUrl(GetImageName())), ImageHeightInPixels);
				PromptUserToTryAgain();
			}
			catch (Exception ex)
			{
				Console.Clear();
				Console.WriteLine($"Oops! There was a problem with that search: {ex.Message}");
				PromptUserToTryAgain();
			}
		}

		private static string GetImageName()
		{
			Console.Write("Enter the name of an object to ASCIIify: ");
			var imageName = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(imageName)) throw new Exception("Image name is required.");
			return imageName;
		}

		private static string GetImageUrl(string imageName)
		{
			var subscriptionKey = ConfigurationManager.AppSettings["BingImageSearch_SubscriptionKey"];
			var uriBase = ConfigurationManager.AppSettings["BingImageSearch_UriBase"];
			var imageUrl = new BingImageSearch(subscriptionKey, uriBase)
				.GetImageUrl(imageName,
							 imageSize: ImageSize.Medium,
							 aspectRatio: AspectRatio.Wide);
			if (string.IsNullOrWhiteSpace(imageUrl)) throw new Exception("No image URL returned for search.");
			return imageUrl;
		}

		private static Byte[] GetImageByteArrayFromUrl(string imageUrl)
		{
			using (var client = new HttpClient())
			{
				var response = client.GetAsync(imageUrl).Result;
				if (!response.IsSuccessStatusCode) throw new Exception("Unable to retrieve image from URL.");
				var imageByteArray = response.Content.ReadAsByteArrayAsync().Result;
				if (imageByteArray == null) throw new Exception("Unable to convert image to byte array.");
				return imageByteArray;
			}
		}

		private static void PromptUserToTryAgain()
		{
			Console.Write("Hit any key to try again.");
			var key = Console.ReadKey();
			if (key.Key.Equals(ConsoleKey.Escape))
				Environment.Exit(0);
			Console.Clear();
			ConvertNamedObjectToConsoleArt();
		}

		private static void DisplayImageToConsole(byte[] imageByteArray, int resizedImageHeight)
		{
			//clear screen
			Console.Clear();

			//initialize local variables
			Size recalculatedImageSize;
			Bitmap resizedImage;

			//note: MUST keep memory stream active while working with image
			using (var ms = new MemoryStream(imageByteArray))
			{
				//convert byte-array to image
				var sourceImage = new Bitmap(ms);

				//calculate amount to resize image
				var percent = decimal.Divide(resizedImageHeight, sourceImage.Height);

				//recalculate image size and store as struct
				recalculatedImageSize = new Size((int)(sourceImage.Width * percent), (int)(sourceImage.Height * percent));

				//create resized image; width is doubled because console characters are rectangular
				resizedImage = new Bitmap(sourceImage, recalculatedImageSize.Width * 2, recalculatedImageSize.Height);
			}

			//loop through rows of image pixels
			for (int row = 0; row < recalculatedImageSize.Height; row++)
			{
				//loop through columns of image pixels
				for (int column = 0; column < recalculatedImageSize.Width; column++)
				{
					//write pixel to console (in pairs, to account for rectangular character space)
					WritePixelToConsole(PixelToCharacterUtility.GetCharacterProperties(resizedImage.GetPixel(column * 2, row)));
					WritePixelToConsole(PixelToCharacterUtility.GetCharacterProperties(resizedImage.GetPixel((column * 2) + 1, row)));
				}
				//wrap to next line
				Console.WriteLine();
			}

			//reset console colors
			ResetConsoleColors();
		}

		private static void WritePixelToConsole((ConsoleColor foregroundColor, ConsoleColor backgroundColor, char consoleCharacter) pixel)
		{
			Console.ForegroundColor = pixel.foregroundColor;
			Console.BackgroundColor = pixel.backgroundColor;
			Console.Write(pixel.consoleCharacter);
		}
	}
}
