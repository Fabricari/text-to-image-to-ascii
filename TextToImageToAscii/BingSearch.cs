using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

namespace TextToImageToAscii
{
    /// <summary>
    /// Source: https://docs.microsoft.com/en-us/azure/cognitive-services/bing-image-search/quickstarts/csharp
    /// </summary>
    public class BingSearch
    {
        private string SubscriptionKey { get; set; }
        private string UriBase { get; set; }
        private int Height { get; set; }
        private AspectRatio AspectRatio { get; set; }
        private SafeSearch SafeSearch { get; set; }
        private ImageType ImageType { get; set; }

        public BingSearch(string subscriptionKey, string uriBase)
        {
            SubscriptionKey = subscriptionKey;
            UriBase = uriBase;
        }

        public string GetImageUrl(string searchTerm, int height = 75, AspectRatio aspectRatio = AspectRatio.Square, SafeSearch safeSearch = SafeSearch.Moderate, ImageType imageType = ImageType.Photo)
        {
            //set properties
            AspectRatio = aspectRatio;
            SafeSearch = safeSearch;
            ImageType = imageType;
            Height = height;

            //send a search request using the search term
            SearchResult result = BingImageSearch(searchTerm);

            //return content URL
            return result.value[0].contentUrl;
        }

        /// <summary>
        /// Performs a Bing Image search and return the results as a SearchResult.
        /// </summary>
        private SearchResult BingImageSearch(string searchQuery)
        {
			//todo: empty query will throw an error
            //Construct the URI of the search request
            //https://docs.microsoft.com/en-us/rest/api/cognitiveservices/bing-images-api-v7-reference#query-parameters
            var uriQuery = UriBase + "?count=1" +               //only 1 image
                "&offset=0" +                                   //offset search from first item returned
                $"&aspect={AspectRatio.Square.ToString()}" +    //aspect ratio
                $"&height={Height}" +                           //image height
                $"&safeSearch={SafeSearch}" +                   //safe-search setting
                $"&imageType={ImageType}" +                     //image-type
                $"&q={Uri.EscapeDataString(searchQuery)}";      //search string

            //make a RESTful call to Bing...
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
                var response = httpClient.GetAsync(uriQuery).Result;
				//todo: trap this
                response.EnsureSuccessStatusCode(); //is successful
                var json = response.Content.ReadAsStringAsync().Result;

                //deserialize the resulting json
                return JsonConvert.DeserializeObject<SearchResult>(json);
            }
        }
    }

    // A struct to return image search results seperately from headers
    public class SearchResult
    {
        public List<SearchResultValue> value { get; set; }
        public class SearchResultValue 
        {
            public string contentUrl { get; set; }
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
    /// Filter images for adult content. The following are the possible filter values.
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
}
