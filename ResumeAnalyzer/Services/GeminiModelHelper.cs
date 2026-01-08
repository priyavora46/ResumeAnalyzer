using System;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ResumeAnalyzer.Services
{
    public class GeminiModelHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static async Task<string> ListAvailableModels()
        {
            try
            {
                string apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];

                // Try different API versions
                string[] apiVersions = { "v1", "v1beta" };

                foreach (var version in apiVersions)
                {
                    try
                    {
                        string url = $"https://generativelanguage.googleapis.com/{version}/models?key={apiKey}";
                        var response = await _httpClient.GetAsync(url);
                        string content = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            var json = JObject.Parse(content);
                            var models = json["models"];

                            string result = $"Available models for API {version}:\n\n";

                            if (models != null)
                            {
                                foreach (var model in models)
                                {
                                    string modelName = model["name"]?.ToString();
                                    var supportedMethods = model["supportedGenerationMethods"];

                                    result += $"Model: {modelName}\n";
                                    if (supportedMethods != null)
                                    {
                                        result += $"Supports: {string.Join(", ", supportedMethods)}\n";
                                    }
                                    result += "\n";
                                }
                            }

                            return result;
                        }
                    }
                    catch (Exception )
                    {
                        // Try next version
                        continue;
                    }
                }

                return "Could not list models. API key might be invalid or restricted.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}