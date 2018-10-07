using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace TextToImageToAscii
{
	/// <summary>
	/// Source: https://docs.microsoft.com/en-us/azure/cognitive-services/bing-image-search/quickstarts/csharp
	/// </summary>
	public class BingSearch
    {
        private string SubscriptionKey { get; set; }
        private string UriBase { get; set; }

		public BingSearch(string subscriptionKey, string uriBase)
        {
            SubscriptionKey = subscriptionKey;
            UriBase = uriBase;
        }

        public string GetImageUrl(
			string searchTerm, 
			int? height = null, 
			AspectRatio aspectRatio = AspectRatio.All, 
			SafeSearch safeSearch = SafeSearch.Moderate, 
			ImageType imageType = ImageType.Photo,
			ImageSize imageSize = ImageSize.Medium,
			ImageFreshness imageFreshness = ImageFreshness.Year)
        {
			//validate search term
			if (string.IsNullOrWhiteSpace(searchTerm)) return null;

            //Construct the URI of the search request
            //https://docs.microsoft.com/en-us/rest/api/cognitiveservices/bing-images-api-v7-reference#query-parameters
            var uriQuery = UriBase + "?count=1&offset=0" +    
                (height.HasValue ? $"&height={height}" : "") +                          
                $"&aspect={aspectRatio}" +				                    
                $"&safeSearch={safeSearch}" +             
                $"&imageType={imageType}" +               
                $"&size={imageSize}" +
				$"&freshness={imageFreshness}" +					
                $"&q={Uri.EscapeDataString(searchTerm)}";

            //make a RESTful call to Bing...
            using (var httpClient = new HttpClient())
            {
				//add subscription to header
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);

				//make request
                var response = httpClient.GetAsync(uriQuery).Result;

				//check status or throw exception
				if (!response.IsSuccessStatusCode) return null;

				//get response as JSON
                var json = response.Content.ReadAsStringAsync().Result;

                //deserialize the resulting json
                var searchResult = JsonConvert.DeserializeObject<SearchResult>(json);

				//get content URL
				return searchResult?.value?.ElementAtOrDefault(0)?.contentUrl;
			}
        }

		/// <summary>
		/// Search Result model for binding
		/// </summary>
		private class SearchResult
		{
			public List<SearchResultValue> value { get; set; }
			public class SearchResultValue 
			{
				public string contentUrl { get; set; }
			}
		}
    }

    /// <summary>
    /// Filter images by the following aspect ratios
    /// </summary>
    public enum AspectRatio
    {
        Square,
        Wide,
        Tall,
        All
    }

    /// <summary>
    /// Used to filter images for adult content.
    /// </summary>
    public enum SafeSearch
    {
        Off,
        Moderate,
        Strict
    }

    /// <summary>
    /// Filter images by the following image types
    /// </summary>
    public enum ImageType
    {
        Clipart,
        Line,
        Photo
	}

	/// <summary>
	/// Used to set image size
	/// </summary>
	public enum ImageSize
	{
		Small,
		Medium,
		Large
	}

	/// <summary>
	/// Used to set image freshness
	/// </summary>
	public enum ImageFreshness
	{
		Day,
		Week,
		Month,
		Year
	}
}
