// 
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DnSpyAIRefactor
{
    public class OllamaService
    {
        private readonly HttpClient httpClient;
        
        public string ServerUrl { get; private set; }
        public string ModelName { get; private set; }
        
        public OllamaService(string serverUrl, string modelName)
        {
            ServerUrl = serverUrl;
            ModelName = modelName;
            
            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }
        
        public void UpdateConfiguration(string serverUrl, string modelName)
        {
            ServerUrl = serverUrl;
            ModelName = modelName;
        }
        
        public async Task<string> GenerateRenameSuggestionAsync(
            string code, 
            string currentName, 
            string entityType, 
            string context = "")
        {
            var prompt = CreateRenamePrompt(code, currentName, entityType, context);
            
            var request = new
            {
                model = ModelName,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.3,
                    top_p = 0.9
                }
            };
            
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync($"{ServerUrl}/api/generate", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<OllamaResponse>(responseJson);
                    
                    return CleanResponse(result?.Response ?? currentName);
                }
                else
                {
                    throw new Exception($"HTTP error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ollama request failed: {ex.Message}", ex);
            }
        }
        
        public async Task<CodeAnalysisResult> AnalyzeCodeAsync(string code)
        {
            var prompt = CreateAnalysisPrompt(code);
            
            var request = new
            {
                model = ModelName,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.2,
                    top_p = 0.8
                }
            };
            
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync($"{ServerUrl}/api/generate", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<OllamaResponse>(responseJson);
                    
                    return ParseAnalysisResult(result?.Response ?? "{}");
                }
                else
                {
                    throw new Exception($"HTTP error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Analysis request failed: {ex.Message}", ex);
            }
        }
        
        private string CreateRenamePrompt(string code, string currentName, string entityType, string context)
        {
            return $@"
            You are a C# code refactoring assistant. Suggest a better name for this {entityType}.
            
            Current name: {currentName}
            
            Code context:
            ```csharp
            {code}
            ```
            
            {(!string.IsNullOrEmpty(context) ? $"Additional context: {context}" : "")}
            
            Guidelines:
            1. Follow C# naming conventions
            2. Be descriptive but concise
            3. Consider the entity's purpose
            4. Use PascalCase for classes, methods, properties
            5. Use camelCase for parameters and local variables
            
            Respond ONLY with the new name, nothing else.
            
            New name:";
        }
        
        private string CreateAnalysisPrompt(string code)
        {
            return $@"
            Analyze this C# code and suggest improvements for naming and structure.
            
            Code:
            ```csharp
            {code.Substring(0, Math.Min(code.Length, 3000))}
            ```
            
            Provide response as JSON with this structure:
            {{
                ""analysis"": ""brief analysis text"",
                ""suggestions"": [
                    {{
                        ""oldName"": ""string"",
                        ""newName"": ""string"",
                        ""entityType"": ""class|method|property|field|parameter|variable"",
                        ""reason"": ""string"",
                        ""confidence"": 0.0
                    }}
                ],
                ""overallScore"": 0.0,
                ""keyIssues"": [""string""]
            }}
            
            Only respond with valid JSON, no other text.";
        }
        
        private string CleanResponse(string response)
        {
            // Удаляем обратные кавычки и лишние пробелы
            return response
                .Replace("```", "")
                .Replace("`", "")
                .Trim()
                .Split('\n')[0]
                .Trim();
        }
        
        private CodeAnalysisResult ParseAnalysisResult(string jsonResponse)
        {
            try
            {
                return JsonConvert.DeserializeObject<CodeAnalysisResult>(jsonResponse);
            }
            catch
            {
                return new CodeAnalysisResult();
            }
        }
    }
    
    public class OllamaResponse
    {
        [JsonProperty("response")]
        public string Response { get; set; }
        
        [JsonProperty("done")]
        public bool Done { get; set; }
    }
    
    public class CodeAnalysisResult
    {
        [JsonProperty("analysis")]
        public string Analysis { get; set; } = "";
        
        [JsonProperty("suggestions")]
        public List<RefactoringSuggestion> Suggestions { get; set; } = new List<RefactoringSuggestion>();
        
        [JsonProperty("overallScore")]
        public double OverallScore { get; set; }
        
        [JsonProperty("keyIssues")]
        public List<string> KeyIssues { get; set; } = new List<string>();
    }
    
    public class RefactoringSuggestion
    {
        [JsonProperty("oldName")]
        public string OldName { get; set; } = "";
        
        [JsonProperty("newName")]
        public string NewName { get; set; } = "";
        
        [JsonProperty("entityType")]
        public string EntityType { get; set; } = "";
        
        [JsonProperty("reason")]
        public string Reason { get; set; } = "";
        
        [JsonProperty("confidence")]
        public double Confidence { get; set; }
    }
}
