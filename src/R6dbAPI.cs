using Jint;
using Jint.Native;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace UplayNameChecker
{
    public class R6dbAPI
    {
        private HttpClient httpClient = new HttpClient();
        private const string URI = "https://api.statsdb.net/r6/namecheck/";
        private string APIKey = "";

        public async Task<bool> IsNameAvailable(string name)
        {
            if (string.IsNullOrEmpty(APIKey)) APIKey = await GenerateAuthToken();

            try
            {
                var reqUri = URI + System.Net.WebUtility.UrlEncode(name);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, reqUri);
                request.Headers.Add("x-api-key", APIKey);
                request.Headers.Add("origin", "https://r6db.net");

                var response = await httpClient.SendAsync(request);
                var str = await response.Content.ReadAsStringAsync();

                if (str.Contains("Unauthorized"))
                {
                    Log.Info("Error 403, API-Key might have expired, generating new one...");
                    APIKey = await GenerateAuthToken();
                    return await IsNameAvailable(name); //lets hope this doesnt cause a Stackoverflow Exception :D
                }

                return (str.Contains("exists\":false"));
            }
            catch (Exception ex)
            {
                Log.Error($"Could not check name {name}: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GenerateAuthToken()
        {
            try
            {
                var paramaters = await RequestAuthGeneratorCode();
                var generateAuthToken = ParseAuthTokenGenerator(paramaters[0], int.Parse(paramaters[1]), paramaters[2],
                                                                int.Parse(paramaters[3]), int.Parse(paramaters[4]),
                                                                int.Parse(paramaters[5]));

                var authcode = generateAuthToken.Invoke(new JsValue(GetCurrentTimestamp())).AsString();
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(authcode));
            }
            catch(Exception ex)
            {
                Log.Fatal("Couldn't generate auth token: " + ex.Message);
                return "";
            }
        }

        private async Task<string[]> RequestAuthGeneratorCode()
        {
            var response = await httpClient.GetAsync("https://api.statsdb.net/authscript");
            var str = await response.Content.ReadAsStringAsync();

            //js fixups :/
            str = str.Substring(str.IndexOf("decodeURIComponent(escape(r))}") + "decodeURIComponent(escape(r))}".Length + 2).Replace("))", "").Replace("\"","");
            return str.Split(',');
        }

        private JsValue ParseAuthTokenGenerator(string encoded, int unknown0, string key, int unknown1, int unknown2, int unknown3)
        {
            Engine engine = new Engine();

            //those 2 shouldn't change
            engine.Execute(JSCode.CharDecrypter);
            engine.Execute(JSCode.StringDecoder);

            var jsCode = engine.Invoke("decodeFunction", encoded, unknown0, key, unknown1, unknown2, unknown3).AsString()
                                                                                        .Replace(JSCode.UselessBSCode, "")
                                                                                        .Replace("btoa", "") + "}"; // jint has no btoa support

            engine.Execute(jsCode);

            //return callable generateAuthToken function 
            return engine.GetValue("generateAuthToken");
        }
        
        private ulong GetCurrentTimestamp()
        {
            return (ulong)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }
}
