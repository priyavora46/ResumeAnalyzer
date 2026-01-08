using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResumeAnalyzer.Models;

namespace ResumeAnalyzer.Services
{
    public class GeminiApiService
    {
        private readonly string _apiKey;
        private static readonly HttpClient _httpClient = new HttpClient();

        private static readonly string[] ModelOptions = new[]
        {
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent",
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent",
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent",
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro-latest:generateContent",
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent",
            "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-001:generateContent"
        };

        public GeminiApiService()
        {
            _apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new Exception("Gemini API Key is not configured");
            }
        }

        public async Task<AnalysisResultViewModel> AnalyzeResumeAsync(string resumeText, string jobDescription, string targetPosition)
        {
            try
            {
                string prompt = BuildAnalysisPrompt(resumeText, jobDescription, targetPosition);
                string geminiResponse = await CallGeminiApiAsync(prompt);
                return ParseGeminiResponse(geminiResponse, resumeText);
            }
            catch (Exception ex)
            {
                throw new Exception("Error analyzing resume: " + ex.Message, ex);
            }
        }

        private string BuildAnalysisPrompt(string resumeText, string jobDescription, string targetPosition)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("You are an expert resume analyst. Analyze this resume and give REALISTIC scores.");
            sb.AppendLine();
            sb.AppendLine("RESUME TEXT:");
            sb.AppendLine(resumeText);
            sb.AppendLine();

            if (!string.IsNullOrEmpty(jobDescription))
            {
                sb.AppendLine("JOB DESCRIPTION:");
                sb.AppendLine(jobDescription);
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(targetPosition))
            {
                sb.AppendLine("TARGET POSITION: " + targetPosition);
                sb.AppendLine();
            }

            sb.AppendLine("SCORING GUIDE:");
            sb.AppendLine("- Strong resume: 80-95");
            sb.AppendLine("- Average resume: 60-75");
            sb.AppendLine("- Weak resume: 40-60");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(targetPosition) || !string.IsNullOrEmpty(jobDescription))
            {
                sb.AppendLine("JOB MATCH:");
                sb.AppendLine("- Calculate jobMatchScore (0-100)");
                sb.AppendLine("- List matchingSkills");
                sb.AppendLine("- List missingSkills");
                sb.AppendLine();
            }

            sb.AppendLine("Return ONLY valid JSON (no markdown):");
            sb.Append("{\"overallScore\":85,\"summary\":\"text\",");
            sb.Append("\"skills\":[{\"name\":\"skill\",\"category\":\"Technical\",\"proficiency\":\"Advanced\"}],");
            sb.Append("\"strengths\":[\"text\"],\"weaknesses\":[\"text\"],\"recommendations\":[\"text\"],");
            sb.Append("\"missingKeywords\":[\"text\"],");
            sb.Append("\"contact\":{\"email\":\"text\",\"phone\":\"text\",\"linkedin\":\"text\",\"location\":\"text\"},");
            sb.Append("\"education\":[\"text\"],\"experience\":[\"text\"],");
            sb.Append("\"categoryScores\":{\"Technical Skills\":85,\"Soft Skills\":70,\"Experience\":80,\"Education\":90,\"Format & Structure\":75},");
            sb.Append("\"atsCompatibility\":\"High\",\"keywordMatchPercentage\":75,");
            sb.Append("\"jobMatchScore\":80,\"matchingSkills\":[\"text\"],\"missingSkills\":[\"text\"]}");

            return sb.ToString();
        }

        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            Exception lastException = null;

            foreach (string modelUrl in ModelOptions)
            {
                try
                {
                    string url = modelUrl + "?key=" + _apiKey;
                    string jsonBody = BuildRequestBody(prompt);

                    StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                    string responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        JObject json = JObject.Parse(responseBody);
                        JToken textToken = json.SelectToken("candidates[0].content.parts[0].text");

                        if (textToken != null)
                        {
                            return textToken.ToString();
                        }
                    }
                    else if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                    {
                        string errorMsg = "Gemini API Error: " + response.StatusCode.ToString();
                        try
                        {
                            JObject errorJson = JObject.Parse(responseBody);
                            JToken errorDetail = errorJson["error"]["message"];
                            if (errorDetail != null)
                            {
                                errorMsg = errorMsg + " - " + errorDetail.ToString();
                            }
                        }
                        catch { }
                        throw new Exception(errorMsg);
                    }
                }
                catch (HttpRequestException ex)
                {
                    lastException = ex;
                    continue;
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("NotFound") && !ex.Message.Contains("not found"))
                    {
                        throw;
                    }
                    lastException = ex;
                    continue;
                }
            }

            string finalError = "All Gemini models failed. Check API key at: https://aistudio.google.com/app/apikey";
            if (lastException != null)
            {
                finalError = finalError + " | Last error: " + lastException.Message;
            }
            throw new Exception(finalError);
        }

        private string BuildRequestBody(string prompt)
        {
            StringBuilder json = new StringBuilder();
            json.Append("{\"contents\":[{\"parts\":[{\"text\":\"");
            json.Append(EscapeJson(prompt));
            json.Append("\"}]}],\"generationConfig\":{\"temperature\":0.4,\"topK\":32,\"topP\":1,\"maxOutputTokens\":8192}}");
            return json.ToString();
        }

        private string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            return text.Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r")
                      .Replace("\t", "\\t");
        }

        private AnalysisResultViewModel ParseGeminiResponse(string geminiResponse, string originalResumeText)
        {
            try
            {
                string cleaned = geminiResponse.Trim();

                if (cleaned.StartsWith("```json"))
                {
                    cleaned = cleaned.Substring(7);
                }
                else if (cleaned.StartsWith("```"))
                {
                    cleaned = cleaned.Substring(3);
                }

                if (cleaned.EndsWith("```"))
                {
                    cleaned = cleaned.Substring(0, cleaned.Length - 3);
                }

                cleaned = cleaned.Trim();

                int firstBrace = cleaned.IndexOf('{');
                int lastBrace = cleaned.LastIndexOf('}');

                if (firstBrace >= 0 && lastBrace > firstBrace)
                {
                    cleaned = cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);
                }

                JObject json = JObject.Parse(cleaned);

                AnalysisResultViewModel result = new AnalysisResultViewModel();
                result.OverallScore = GetInt(json, "overallScore", EstimateScore(originalResumeText));
                result.Summary = GetString(json, "summary", "No summary");
                result.ATSCompatibility = GetString(json, "atsCompatibility", "Unknown");
                result.KeywordMatchPercentage = GetInt(json, "keywordMatchPercentage", 0);
                result.JobMatchScore = GetInt(json, "jobMatchScore", 0);

                ParseSkills(json, result);
                ParseArrays(json, result);
                ParseContact(json, result);
                ParseScores(json, result);

                return result;
            }
            catch (Exception ex)
            {
                return CreateFallback(originalResumeText, ex.Message);
            }
        }

        private void ParseSkills(JObject json, AnalysisResultViewModel result)
        {
            JToken skills = json["skills"];
            if (skills != null && skills.Type == JTokenType.Array)
            {
                foreach (JToken skill in skills)
                {
                    result.Skills.Add(new SkillAnalysis
                    {
                        Name = GetString(skill, "name", ""),
                        Category = GetString(skill, "category", ""),
                        Proficiency = GetString(skill, "proficiency", "")
                    });
                }
            }
        }

        private void ParseArrays(JObject json, AnalysisResultViewModel result)
        {
            result.Strengths = ParseArray(json["strengths"]);
            result.Weaknesses = ParseArray(json["weaknesses"]);
            result.Recommendations = ParseArray(json["recommendations"]);
            result.MissingKeywords = ParseArray(json["missingKeywords"]);
            result.Education = ParseArray(json["education"]);
            result.Experience = ParseArray(json["experience"]);
            result.MatchingSkills = ParseArray(json["matchingSkills"]);
            result.MissingSkills = ParseArray(json["missingSkills"]);
        }

        private void ParseContact(JObject json, AnalysisResultViewModel result)
        {
            JToken contact = json["contact"];
            if (contact != null)
            {
                result.Contact.Email = GetString(contact, "email", "");
                result.Contact.Phone = GetString(contact, "phone", "");
                result.Contact.LinkedIn = GetString(contact, "linkedin", "");
                result.Contact.Location = GetString(contact, "location", "");
            }
        }

        private void ParseScores(JObject json, AnalysisResultViewModel result)
        {
            JToken scores = json["categoryScores"];
            if (scores != null && scores.Type == JTokenType.Object)
            {
                JObject scoresObj = (JObject)scores;
                foreach (var kvp in scoresObj)
                {
                    int value = 0;
                    if (kvp.Value != null && int.TryParse(kvp.Value.ToString(), out value))
                    {
                        result.CategoryScores[kvp.Key] = value;
                    }
                }
            }
        }

        private List<string> ParseArray(JToken token)
        {
            List<string> result = new List<string>();
            if (token != null && token.Type == JTokenType.Array)
            {
                foreach (JToken item in token)
                {
                    result.Add(item.ToString());
                }
            }
            return result;
        }

        private string GetString(JToken token, string key, string defaultValue)
        {
            JToken value = token[key];
            return value != null ? value.ToString() : defaultValue;
        }

        private int GetInt(JToken token, string key, int defaultValue)
        {
            JToken value = token[key];
            if (value != null)
            {
                int result;
                if (int.TryParse(value.ToString(), out result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        private int EstimateScore(string text)
        {
            int score = 50;

            if (text.Length > 2000) score += 10;
            if (text.Length > 3000) score += 5;
            if (text.Contains("@")) score += 5;

            string lower = text.ToLower();
            if (lower.Contains("experience") || lower.Contains("work")) score += 10;
            if (lower.Contains("education") || lower.Contains("degree")) score += 10;
            if (lower.Contains("skill")) score += 10;
            if (lower.Contains("project")) score += 5;

            return Math.Min(score, 80);
        }

        private AnalysisResultViewModel CreateFallback(string text, string error)
        {
            int score = EstimateScore(text);

            AnalysisResultViewModel result = new AnalysisResultViewModel();
            result.OverallScore = score;
            result.Summary = "Analysis completed with estimated scores. Error: " + error;
            result.Strengths.Add("Resume content extracted");
            result.Recommendations.Add("Try analyzing again");
            result.CategoryScores.Add("Overall", score);

            return result;
        }
    }
}