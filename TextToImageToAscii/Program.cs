using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace TextToImageToAscii
{
	class MainClass
    {
        static int[] cColors = { 0x000000, 0x000080, 0x008000, 0x008080, 0x800000, 0x800080, 0x808000, 0xC0C0C0, 0x808080, 0x0000FF, 0x00FF00, 0x00FFFF, 0xFF0000, 0xFF00FF, 0xFFFF00, 0xFFFFFF };

        public static void Main(string[] args)
        {
			//format the console
			InitializeConsole();

			//convert words into ASCII art
			ConvertNamedObjectToConsoleArt();
        }

        private static void ConvertNamedObjectToConsoleArt()
        {
			try
			{
				//get user text
				var userText = GetUserText();

                //get image from text
                var imageByteArray = GetImageByteArrayFromText(userText);

                //note: MUST keep memory stream active while working with image
                Bitmap bitmap;
                using (var ms = new MemoryStream(imageByteArray))
                {
                    //convert byte-array to image
                    bitmap = new Bitmap(ms);

                    //display image to console
                    ConsoleWriteImage(bitmap);
                }

                //prompt user to try again
                TryAgain();
			}
			catch
			{
				Console.Clear();
				Console.WriteLine("Oops! There was a problem with that search.");
				TryAgain();
			}
		}

        private static void TryAgain()
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

		//source: https://stackoverflow.com/questions/35263590/programmatically-set-console-window-size-and-position?rq=1
		//Declare WinApi functions to get access to window handles
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr GetConsoleWindow();

		[DllImport("user32.dll", SetLastError = true)]
		internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

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
			Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);
		}

		private static string GetUserText()
        {
            //prompt user to enter name of image to ascii-fy
            Console.Write("Enter the name of an object to ASCIIify: ");

            //return user-entered text
            return Console.ReadLine();
        }

        private static Byte[] GetImageByteArrayFromText(string userText)
        {
            //get configuration settings
            var subscriptionKey = ConfigurationManager.AppSettings["BingImageSearch_SubscriptionKey"];
            var uriBase = ConfigurationManager.AppSettings["BingImageSearch_UriBase"];

            //get url of image from text using Bing Search API
            var imageUrl = new BingSearch(subscriptionKey, uriBase)
                .GetImageUrl(userText, 
                             height: 120, 
                             aspectRatio: AspectRatio.Square);

            //get image from URL
            using (var webClient = new WebClient())
            {
                return webClient.DownloadData(imageUrl);
            }
        }

        private static void ConsoleWriteImage(Bitmap source)
        {
            //clear screen
            Console.Clear();

            //int sMax = 39;
            int sMax = 60;
            decimal percent = Math.Min(decimal.Divide(sMax, source.Width), decimal.Divide(sMax, source.Height));
            Size dSize = new Size((int)(source.Width * percent), (int)(source.Height * percent));
            Bitmap bmpMax = new Bitmap(source, dSize.Width * 2, dSize.Height);
            for (int i = 0; i < dSize.Height; i++)
            {
                for (int j = 0; j < dSize.Width; j++)
                {
                    ConsoleWritePixel(bmpMax.GetPixel(j * 2, i));
                    ConsoleWritePixel(bmpMax.GetPixel(j * 2 + 1, i));
                }
                System.Console.WriteLine();
            }
            Console.ResetColor();
        }

        private static void ConsoleWritePixel(Color cValue)
        {
            Color[] cTable = cColors.Select(x => Color.FromArgb(x)).ToArray();
            char[] rList = new char[] { (char)9617, (char)9618, (char)9619, (char)9608 }; // 1/4, 2/4, 3/4, 4/4
            int[] bestHit = new int[] { 0, 0, 4, int.MaxValue }; //ForeColor, BackColor, Symbol, Score

            for (int rChar = rList.Length; rChar > 0; rChar--)
            {
                for (int cFore = 0; cFore < cTable.Length; cFore++)
                {
                    for (int cBack = 0; cBack < cTable.Length; cBack++)
                    {
                        int R = (cTable[cFore].R * rChar + cTable[cBack].R * (rList.Length - rChar)) / rList.Length;
                        int G = (cTable[cFore].G * rChar + cTable[cBack].G * (rList.Length - rChar)) / rList.Length;
                        int B = (cTable[cFore].B * rChar + cTable[cBack].B * (rList.Length - rChar)) / rList.Length;
                        int iScore = (cValue.R - R) * (cValue.R - R) + (cValue.G - G) * (cValue.G - G) + (cValue.B - B) * (cValue.B - B);
                        if (!(rChar > 1 && rChar < 4 && iScore > 50000)) // rule out too weird combinations
                        {
                            if (iScore < bestHit[3])
                            {
                                bestHit[3] = iScore; //Score
                                bestHit[0] = cFore;  //ForeColor
                                bestHit[1] = cBack;  //BackColor
                                bestHit[2] = rChar;  //Symbol
                            }
                        }
                    }
                }
            }
            Console.ForegroundColor = (ConsoleColor)bestHit[0];
            Console.BackgroundColor = (ConsoleColor)bestHit[1];
            Console.Write(rList[bestHit[2] - 1]);


        }
	}
}
