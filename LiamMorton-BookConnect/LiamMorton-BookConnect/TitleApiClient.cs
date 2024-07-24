using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class TitleApiClient : ITitleApiClient
{
    private readonly IHttpClientFactory _clientFactory;
    //manage client using HttpClientFactory
    public TitleApiClient(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }
    //send request json data and return TitleDto or null if no json response given
    public async Task<TitleDto> GetTitleAsync(string isbn)
    {
        //add isbn to path
        var isbnPath = "https://app.bookconnect.ca/api/partner/title/" + isbn;
        //Get request to api
        var request = new HttpRequestMessage(HttpMethod.Get, isbnPath);
        //add bci-key
        request.Headers.Add("bci-key", "jHuB7IY1mZfLiS3VLXuvFF3PaB3TigZ1TXypOADo0jo4sasgJt6b2heDFAGev62J");
        //request for json data
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //create client
        var client = _clientFactory.CreateClient();
        //get response
        var response = await client.SendAsync(request);


        if (response.IsSuccessStatusCode)
        {
            //get media type from response
            var contentType = response.Content.Headers.ContentType.MediaType;
            //get data from response
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine(responseContent);

            //make sure data recived is in json format
            if (contentType.Contains("application/json"))
            {
                return ParseJson(responseContent);
            }
            else
            {
                Console.Error.WriteLine("Received non-JSON response.");
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    //parse json response into TitleDto format
    public TitleDto ParseJson(string responseContent)
    {
        //make json object
        JObject jsonObject;
        
        string ClianedResponseContent;

        //make sure there is data in response
        if (responseContent.Length > 0)
        {
            //remove back slashes and quotations on both ends of json
            responseContent = responseContent.Substring(1, responseContent.Length - 2);
            ClianedResponseContent = responseContent.Replace("\\", "");
            Console.Error.WriteLine($"{ClianedResponseContent}");
        }
        else
        {
            return null;
        }

        //if there is still data in json
        if (ClianedResponseContent.Length > 0)
        {
            try
            {
                //Check if json is in expected format and pare json data into json object
                if (ClianedResponseContent.Trim().StartsWith("{") || ClianedResponseContent.Trim().StartsWith("["))
                {
                    jsonObject = JObject.Parse(ClianedResponseContent);
                }
                else
                {
                    //return null if json is not in expected format and display error
                    Console.Error.WriteLine("Received unexpected JSON format.");
                    return null;
                }

                // Extract specific values from the Jason Object defult N/A if null
                //title name
                string title = jsonObject["TitleText"]?.ToString() ?? "N/A";
                //subtitle
                string subtitle = jsonObject["Subtitle"]?.ToString() ?? "N/A";
                //cover image link
                string coverImage = jsonObject["CoverImageLink"]?.ToString() ?? "N/A";
                //price
                decimal price = jsonObject["RetailUS"]?.ToObject<decimal>() ?? 0;
                //description where text type is '10'
                string description = jsonObject["OtherTexts"]?.FirstOrDefault(text => text["TextTypeCode"]?.ToString() == "01")?["TextBody"]?.ToString() ?? "N/A";
                //authers get contributer array as jarray
                JArray authorsArray = (JArray)jsonObject["Contributors"];
                //compine auther full name into string and add to authers list
                var authors = authorsArray?.Select(author => $"{author["ContributorPerson"]?["NamesBeforeKey"]} {author["ContributorPerson"]?["KeyNames"]}").ToList() ?? new List<string> { "N/A" };
                //subject from subject description field
                string subject = jsonObject["TitleGroup"]?["CodeSubject1"]?["SubjectDescription"]?.ToString() ?? "N/A";

                //Create and return TitleDto
                var titleDto = new TitleDto
                {
                    Title = title,
                    Subtitle = subtitle,
                    CoverImage = coverImage,
                    Price = price,
                    Description = description,
                    Authors = authors,
                    Subject = subject
                };

                return titleDto;
            }
            catch (JsonReaderException ex)
            {
                //display error and return null if json file has errors
                Console.Error.WriteLine($"JSON Parsing Error: {ex.Message}");
                return null;
            }
        }
        else
        {
            //display error and return null
            Console.Error.WriteLine($"No data recived");
            return null;
        }
    }
}

