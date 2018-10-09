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

			//initialize console colors
			InitializeConsoleColors();

			//Clear console to fill entire screen
			Console.Clear();

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

		private static void InitializeConsoleColors()
		{
			//Set console to black (terminal on Mac displays white)
			Console.BackgroundColor = ConsoleColor.Black;

			//Set text to mustard yellow
			Console.ForegroundColor = ConsoleColor.DarkYellow;
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
				DisplayImageToConsole(imageByteArray, ImageHeightInPixels);

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

		private static void DisplayImageToConsole(byte[] imageByteArray, int resizedImageHeight)
		{
			//clear screen
			Console.Clear();

			//note: MUST keep memory stream active while working with image
			using (var ms = new MemoryStream(imageByteArray))
			{
				//convert byte-array to image
				var sourceImage = new Bitmap(ms);

				//calculate amount to resize image
				var percent = decimal.Divide(resizedImageHeight, sourceImage.Height);

				//recalculate image size and store as struct
				Size recalculatedImageSize = new Size((int)(sourceImage.Width * percent), (int)(sourceImage.Height * percent));

				//create resized image; width is doubled because console characters are rectangular
				Bitmap resizedImage = new Bitmap(sourceImage, recalculatedImageSize.Width * 2, recalculatedImageSize.Height);

				//todo: can we dispose of the memory stream here?

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
			}

			//reset console colors
			InitializeConsoleColors();
		}

		private static void WritePixelToConsole((ConsoleColor foregroundColor, ConsoleColor backgroundColor, char consoleCharacter) pixel)
		{
			Console.ForegroundColor = pixel.foregroundColor;
			Console.BackgroundColor = pixel.backgroundColor;
			Console.Write(pixel.consoleCharacter);
		}
	}
}
