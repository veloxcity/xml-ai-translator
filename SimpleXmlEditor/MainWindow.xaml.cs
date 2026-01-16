using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleXmlEditor.Localization;

namespace SimpleXmlEditor
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<LocalizationEntry> _entries;
        private readonly HttpClient _httpClient;
        private Dictionary<string, string> _cache;
        private Dictionary<string, (double input, double output)> _modelPricing;
        private Dictionary<string, (int requestsPerMinute, int requestsPerDay, int tokensPerMinute)> _modelLimits;
        private string _apiKey = "";
        private string _model = "";
        private string _targetLanguage = "Turkish";
        private string _programLanguage = "en";
        private string _customPrompt = "";
        private int _cacheHits = 0;
        private int _apiCalls = 0;
        private int _totalInputChars = 0;
        private int _totalOutputChars = 0;
        private double _totalCost = 0.0;
        private DateTime _lastRequestTime = DateTime.MinValue;
        private Queue<DateTime> _recentRequests = new Queue<DateTime>();

        // Token calculation constants (approximate)
        private const int CHARS_PER_TOKEN = 4; // Rough estimate: 1 token ≈ 4 characters
        private const int BATCH_OVERHEAD_TOKENS = 100; // Overhead for batch prompt structure

        // Translation control
        private CancellationTokenSource _translationCancellationTokenSource;
        private bool _isTranslationPaused = false;
        private bool _isTranslationRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            _entries = new ObservableCollection<LocalizationEntry>();
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _cache = new Dictionary<string, string>();
            _modelPricing = new Dictionary<string, (double input, double output)>();
            _modelLimits = new Dictionary<string, (int requestsPerMinute, int requestsPerDay, int tokensPerMinute)>();
            
            EntriesGrid.ItemsSource = _entries;
            
            // DataGrid selection sync with checkboxes
            EntriesGrid.SelectionChanged += EntriesGrid_SelectionChanged;
            
            LoadConfig();
            ApplyLocalization();
            LoadXml();
            UpdateCacheInfo();
            AddLog("✅ Application started");
        }

        private void ApplyLocalization()
        {
            // Update window title
            this.Title = LocalizationManager.GetString("WindowTitle");
            
            // Update main UI elements
            LoadBtn.Content = $"📁 {LocalizationManager.GetString("Load")}";
            SaveBtn.Content = $"💾 {LocalizationManager.GetString("Save")}";
            SettingsBtn.Content = $"⚙️ {LocalizationManager.GetString("Settings")}";
            StatsBtn.Content = $"📊 {LocalizationManager.GetString("Stats")}";
            
            // Update status
            if (StatusText.Text == "Ready")
                StatusText.Text = LocalizationManager.GetString("Ready");
            
            // Update translation buttons
            TranslateSelectedBtn.Content = $"🎯 {LocalizationManager.GetString("TranslateSelected")}";
            TranslateAllBtn.Content = $"🚀 {LocalizationManager.GetString("TranslateAll")}";
            TranslatePartialBtn.Content = $"▶️ {LocalizationManager.GetString("Translate")}";
            ClearCacheBtn.Content = $"🗑️ {LocalizationManager.GetString("ClearCache")}";
            
            // Update DataGrid headers
            if (EntriesGrid.Columns.Count >= 5)
            {
                EntriesGrid.Columns[0].Header = "✓";
                EntriesGrid.Columns[1].Header = LocalizationManager.GetString("Status");
                EntriesGrid.Columns[2].Header = LocalizationManager.GetString("Key");
                EntriesGrid.Columns[3].Header = LocalizationManager.GetString("Original");
                EntriesGrid.Columns[4].Header = LocalizationManager.GetString("Translation");
            }
            
            // Update control buttons
            PauseBtn.Content = $"⏸️ {LocalizationManager.GetString("Pause")}";
            StopBtn.Content = $"⏹️ {LocalizationManager.GetString("Stop")}";
            ClearLogBtn.Content = "🗑️";
        }

        private void EntriesGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Sync DataGrid selection with IsSelected property
            foreach (LocalizationEntry entry in e.AddedItems)
            {
                entry.IsSelected = true;
            }
            
            foreach (LocalizationEntry entry in e.RemovedItems)
            {
                entry.IsSelected = false;
            }
        }

        // Handle checkbox changes to sync with DataGrid selection
        private void OnEntrySelectionChanged(LocalizationEntry entry, bool isSelected)
        {
            if (isSelected)
            {
                if (!EntriesGrid.SelectedItems.Contains(entry))
                {
                    EntriesGrid.SelectedItems.Add(entry);
                }
            }
            else
            {
                if (EntriesGrid.SelectedItems.Contains(entry))
                {
                    EntriesGrid.SelectedItems.Remove(entry);
                }
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists("config.json"))
                {
                    var json = File.ReadAllText("config.json");
                    var config = JsonConvert.DeserializeObject<dynamic>(json);
                    _apiKey = config?.GeminiApiKey ?? "";
                    _model = config?.GeminiModel ?? "";
                    _targetLanguage = config?.TargetLanguage ?? "Turkish";
                    _programLanguage = config?.ProgramLanguage ?? "en";
                    _customPrompt = config?.CustomPrompt ?? "";
                    
                    // Set the program language
                    LocalizationManager.CurrentLanguage = _programLanguage;
                    
                    AddLog($"✅ Config loaded - API Key: {(_apiKey.Length > 0 ? "Set" : "Not set")}");
                }

                if (File.Exists("translation_cache.json"))
                {
                    var cacheJson = File.ReadAllText("translation_cache.json");
                    _cache = JsonConvert.DeserializeObject<Dictionary<string, string>>(cacheJson) ?? new Dictionary<string, string>();
                    AddLog($"✅ Cache loaded - {_cache.Count} entries");
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Config load error: {ex.Message}");
            }
        }

        private void SaveConfig()
        {
            try
            {
                var config = new { 
                    GeminiApiKey = _apiKey, 
                    GeminiModel = _model,
                    TargetLanguage = _targetLanguage,
                    ProgramLanguage = _programLanguage,
                    CustomPrompt = _customPrompt
                };
                File.WriteAllText("config.json", JsonConvert.SerializeObject(config, Formatting.Indented));
                
                File.WriteAllText("translation_cache.json", JsonConvert.SerializeObject(_cache, Formatting.Indented));
                AddLog("✅ Config saved");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Config save error: {ex.Message}");
            }
        }

        private void LoadXml(string fileName = "stable_us.xml")
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    AddLog($"❌ {fileName} not found");
                    return;
                }

                var doc = XDocument.Load(fileName);
                var ns = XNamespace.Get("urn:schemas-microsoft-com:office:spreadsheet");
                var rows = doc.Descendants(ns + "Row");

                _entries.Clear();
                foreach (var row in rows)
                {
                    var cells = row.Elements(ns + "Cell").ToList();
                    if (cells.Count >= 2)
                    {
                        var keyData = cells[0].Element(ns + "Data");
                        var valueData = cells[1].Element(ns + "Data");

                        var key = keyData?.Value ?? "";
                        var value = valueData?.Value ?? "";

                        var entry = new LocalizationEntry
                        {
                            Key = key,
                            Value = value,
                            Translation = "",
                            IsSelected = false
                        };

                        // Subscribe to selection changes for bidirectional sync
                        entry.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(LocalizationEntry.IsSelected) && s is LocalizationEntry changedEntry)
                            {
                                OnEntrySelectionChanged(changedEntry, changedEntry.IsSelected);
                            }
                        };

                        // Check if translation exists in cache
                        var cacheKey = GetCacheKey(value);
                        if (_cache.ContainsKey(cacheKey))
                        {
                            entry.Translation = _cache[cacheKey];
                        }

                        _entries.Add(entry);
                    }
                }

                StatusText.Text = $"Loaded {_entries.Count} entries";
                AddLog($"✅ XML loaded - {_entries.Count} entries");
            }
            catch (Exception ex)
            {
                AddLog($"❌ XML load error: {ex.Message}");
                MessageBox.Show($"Error loading XML: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveXml(string fileName = "stable_us.xml")
        {
            try
            {
                var ns = XNamespace.Get("urn:schemas-microsoft-com:office:spreadsheet");
                
                var workbook = new XElement(ns + "Workbook",
                    new XAttribute(XNamespace.Xmlns + "ss", ns.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "o", "urn:schemas-microsoft-com:office:office"),
                    new XAttribute(XNamespace.Xmlns + "x", "urn:schemas-microsoft-com:office:excel"),
                    new XAttribute(XNamespace.Xmlns + "html", "http://www.w3.org/TR/REC-html40")
                );

                var worksheet = new XElement(ns + "Worksheet",
                    new XAttribute(ns + "Name", "Metro localization")
                );

                var table = new XElement(ns + "Table");

                // Add columns
                table.Add(new XElement(ns + "Column",
                    new XAttribute(ns + "AutoFitWidth", "0"),
                    new XAttribute(ns + "Width", "480")
                ));

                table.Add(new XElement(ns + "Column",
                    new XAttribute(ns + "AutoFitWidth", "0"),
                    new XAttribute(ns + "Width", "650")
                ));

                // Add rows
                foreach (var entry in _entries)
                {
                    var row = new XElement(ns + "Row");

                    var cell1 = new XElement(ns + "Cell",
                        new XElement(ns + "Data",
                            new XAttribute(ns + "Type", "String"),
                            entry.Key
                        )
                    );

                    var cell2 = new XElement(ns + "Cell",
                        new XElement(ns + "Data",
                            new XAttribute(ns + "Type", "String"),
                            string.IsNullOrEmpty(entry.Translation) ? entry.Value : entry.Translation
                        )
                    );

                    row.Add(cell1, cell2);
                    table.Add(row);
                }

                worksheet.Add(table);
                workbook.Add(worksheet);

                var doc = new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XProcessingInstruction("mso-application", "progid=\"Excel.Sheet\""),
                    workbook
                );

                doc.Save(fileName);
                SaveConfig(); // Save cache too
                
                StatusText.Text = $"Saved {_entries.Count} entries to {Path.GetFileName(fileName)}";
                AddLog($"💾 XML saved to {fileName} - {_entries.Count} entries");
            }
            catch (Exception ex)
            {
                AddLog($"❌ XML save error: {ex.Message}");
                MessageBox.Show($"Error saving XML: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task<Dictionary<string, string>> TranslateBatchAsync(List<LocalizationEntry> batch)
        {
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_model) || !batch.Any())
                return new Dictionary<string, string>();

            var results = new Dictionary<string, string>();

            // Check cache first
            var uncachedEntries = new List<LocalizationEntry>();
            foreach (var entry in batch)
            {
                var cacheKey = GetCacheKey(entry.Value);
                if (_cache.ContainsKey(cacheKey))
                {
                    results[entry.Value] = _cache[cacheKey];
                    _cacheHits++;
                }
                else
                {
                    uncachedEntries.Add(entry);
                }
            }

            if (!uncachedEntries.Any())
            {
                UpdateCacheInfo();
                return results;
            }

            // Create batch translation prompt
            var prompt = CreateBatchTranslationPrompt(uncachedEntries);

            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.3, // Lower temperature for more consistent translations
                        topP = 0.8,
                        topK = 40
                    }
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseText = await response.Content.ReadAsStringAsync();
                var responseJson = JObject.Parse(responseText);

                var translationText = responseJson["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()?.Trim();

                if (!string.IsNullOrEmpty(translationText))
                {
                    var batchResults = ParseBatchTranslationResponse(translationText, uncachedEntries);
                    
                    // Cache and add to results
                    foreach (var kvp in batchResults)
                    {
                        var cacheKey = GetCacheKey(kvp.Key);
                        _cache[cacheKey] = kvp.Value;
                        results[kvp.Key] = kvp.Value;
                    }

                    _apiCalls++;
                    
                    // Calculate costs
                    var inputChars = prompt.Length;
                    var outputChars = translationText.Length;
                    _totalInputChars += inputChars;
                    _totalOutputChars += outputChars;
                    
                    var cost = CalculateCost(inputChars, outputChars, _model);
                    _totalCost += cost;
                    
                    UpdateCacheInfo();
                    AddLog($"💰 Batch translation cost: ${cost:F6} ({uncachedEntries.Count} entries, Input: {inputChars} chars, Output: {outputChars} chars)");
                }

                return results;
            }
            catch (Exception ex)
            {
                AddLog($"❌ Batch translation error: {ex.Message}");
                return results;
            }
        }

        private string CreateBatchTranslationPrompt(List<LocalizationEntry> entries)
        {
            var prompt = string.IsNullOrEmpty(_customPrompt) ? GetDefaultBatchPrompt() : _customPrompt;
            
            // Replace placeholders
            prompt = prompt.Replace("{LANGUAGE}", _targetLanguage);
            prompt = prompt.Replace("{CONTEXT}", "game localization");
            
            // Build the texts list
            var textsBuilder = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                textsBuilder.AppendLine($"{i + 1}. \"{entries[i].Value}\"");
            }
            
            prompt = prompt.Replace("{TEXTS}", textsBuilder.ToString().TrimEnd());
            
            return prompt;
        }

        private string GetDefaultBatchPrompt()
        {
            return @"You are a professional game localization translator. Translate the following English texts to {LANGUAGE}.

IMPORTANT RULES:
1. Provide ONLY ONE best translation for each text
2. Keep the gaming context and natural flow
3. Use {LANGUAGE} gaming terminology when appropriate
4. Be concise and accurate
5. Return translations in the exact JSON format shown below

Context: {CONTEXT}

Input texts to translate:
{TEXTS}

Return your translations in this exact JSON format:
{
  ""translations"": [
    {""index"": 1, ""translation"": ""Translation here""},
    {""index"": 2, ""translation"": ""Translation here""}
  ]
}

Only return the JSON, no explanations or additional text.";
        }

        private Dictionary<string, string> ParseBatchTranslationResponse(string response, List<LocalizationEntry> entries)
        {
            var results = new Dictionary<string, string>();

            try
            {
                // Clean up response - remove markdown code blocks if present
                var cleanResponse = response.Trim();
                if (cleanResponse.StartsWith("```json"))
                {
                    cleanResponse = cleanResponse.Substring(7);
                }
                if (cleanResponse.EndsWith("```"))
                {
                    cleanResponse = cleanResponse.Substring(0, cleanResponse.Length - 3);
                }
                cleanResponse = cleanResponse.Trim();

                var jsonResponse = JObject.Parse(cleanResponse);
                var translations = jsonResponse["translations"] as JArray;

                if (translations != null)
                {
                    foreach (var translation in translations)
                    {
                        var index = translation["index"]?.ToObject<int>() ?? 0;
                        var translatedText = translation["translation"]?.ToString()?.Trim();

                        if (index > 0 && index <= entries.Count && !string.IsNullOrEmpty(translatedText))
                        {
                            var originalText = entries[index - 1].Value;
                            results[originalText] = translatedText;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"⚠️ Error parsing batch response: {ex.Message}");
                
                // Fallback: try to extract translations line by line
                var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < Math.Min(lines.Length, entries.Count); i++)
                {
                    var line = lines[i].Trim();
                    // Remove common prefixes
                    line = line.Replace($"{i + 1}.", "").Replace("-", "").Trim();
                    if (line.StartsWith("\"") && line.EndsWith("\""))
                    {
                        line = line.Substring(1, line.Length - 2);
                    }
                    
                    if (!string.IsNullOrEmpty(line) && i < entries.Count)
                    {
                        results[entries[i].Value] = line;
                    }
                }
            }

            return results;
        }

        private string GetCacheKey(string text)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(hash);
        }

        private async Task<string> TranslateAsync(string text)
        {
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_model))
                return null;

            // Check cache
            var cacheKey = GetCacheKey(text);
            if (_cache.ContainsKey(cacheKey))
            {
                _cacheHits++;
                UpdateCacheInfo();
                return _cache[cacheKey];
            }

            // Dynamic retry logic based on model limits
            var maxRetries = _modelLimits.ContainsKey(_model) ? 
                Math.Min(5, _modelLimits[_model].requestsPerMinute / 10) : 3;
            maxRetries = Math.Max(2, maxRetries); // At least 2 retries

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // Track this request for rate limiting
                    TrackRequest();

                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
                    
                    var prompt = $"Translate the following English text to Turkish. " +
                               $"This is for game localization, so keep it natural and fluent. " +
                               $"Only provide the translation, no explanations.\n\n" +
                               $"English: {text}\nTurkish:";

                    var requestBody = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new[]
                                {
                                    new { text = prompt }
                                }
                            }
                        }
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(url, content);

                    // Handle rate limiting with model-specific delays
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        var delay = CalculateOptimalDelay() * (attempt + 1);
                        AddLog($"⏳ Rate limited (429), waiting {delay/1000}s before retry {attempt + 1}/{maxRetries}");
                        await Task.Delay(delay);
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    var responseText = await response.Content.ReadAsStringAsync();
                    var responseJson = JObject.Parse(responseText);

                    var translation = responseJson["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()?.Trim();

                    if (!string.IsNullOrEmpty(translation))
                    {
                        _cache[cacheKey] = translation;
                        _apiCalls++;
                        
                        // Calculate and track costs
                        var inputChars = text.Length;
                        var outputChars = translation.Length;
                        _totalInputChars += inputChars;
                        _totalOutputChars += outputChars;
                        
                        var cost = CalculateCost(inputChars, outputChars, _model);
                        _totalCost += cost;
                        
                        UpdateCacheInfo();
                        AddLog($"💰 Translation cost: ${cost:F6} (Input: {inputChars} chars, Output: {outputChars} chars)");
                        
                        return translation;
                    }

                    return null;
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("429"))
                {
                    if (attempt < maxRetries - 1)
                    {
                        var delay = CalculateOptimalDelay() * (attempt + 2);
                        AddLog($"⏳ Rate limited (HTTP 429), waiting {delay/1000}s before retry {attempt + 1}/{maxRetries}");
                        await Task.Delay(delay);
                        continue;
                    }
                    AddLog($"❌ Translation failed after {maxRetries} attempts: Rate limited");
                    return null;
                }
                catch (Exception ex)
                {
                    if (attempt < maxRetries - 1)
                    {
                        var delay = CalculateOptimalDelay();
                        AddLog($"⏳ Error, retrying in {delay/1000}s: {ex.Message}");
                        await Task.Delay(delay);
                        continue;
                    }
                    AddLog($"❌ Translation error after {maxRetries} attempts: {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        private async Task<List<string>> GetAvailableModelsAsync()
        {
            if (string.IsNullOrEmpty(_apiKey))
                return new List<string>();

            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models?key={_apiKey}";
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                var models = new List<string>();
                _modelPricing.Clear(); // Clear existing pricing
                _modelLimits.Clear(); // Clear existing limits

                if (json["models"] is JArray modelsArray)
                {
                    foreach (var model in modelsArray)
                    {
                        var modelName = model["name"]?.ToString().Replace("models/", "");
                        var methods = model["supportedGenerationMethods"] as JArray;
                        
                        if (methods != null && methods.Any(m => m.ToString() == "generateContent"))
                        {
                            models.Add(modelName ?? string.Empty);

                            // Extract token limits and model info (pricing not available in API)
                            var inputTokenLimit = model["inputTokenLimit"]?.ToObject<int>() ?? 0;
                            var outputTokenLimit = model["outputTokenLimit"]?.ToObject<int>() ?? 0;
                            var displayName = model["displayName"]?.ToString() ?? modelName;
                            var description = model["description"]?.ToString() ?? "";

                            AddLog($"📋 {modelName}: {displayName} (Input: {inputTokenLimit}, Output: {outputTokenLimit} tokens)");

                            // Since API doesn't provide rate limits, estimate based on model type and token limits
                            var estimatedLimits = EstimateRateLimits(modelName, inputTokenLimit, outputTokenLimit);
                            _modelLimits[modelName] = estimatedLimits;
                            
                            AddLog($"⚡ Estimated rate limits for {modelName}: {estimatedLimits.requestsPerMinute}/min, {estimatedLimits.requestsPerDay}/day");
                        }
                    }
                }

                AddLog($"✅ Found {models.Count} models, {_modelPricing.Count} with pricing, {_modelLimits.Count} with rate limits");
                return models;
            }
            catch (Exception ex)
            {
                AddLog($"❌ Error fetching models: {ex.Message}");
                return new List<string>();
            }
        }

        private (int requestsPerMinute, int requestsPerDay, int tokensPerMinute) EstimateRateLimits(string modelName, int inputTokenLimit, int outputTokenLimit)
        {
            // Real Gemini API rate limits based on official documentation
            var rateLimits = new Dictionary<string, (int rpm, int rpd, int tpm)>
            {
                // Gemini 3 (Newest)
                { "gemini-3-pro-preview", (5, 50, 250000) },
                { "gemini-3-flash-preview", (10, 200, 1000000) },
                { "gemini-3-flash-thinking", (5, 50, 250000) },
                
                // Gemini 2.5 (Production)
                { "gemini-2.5-pro", (2, 50, 250000) },
                { "gemini-2.5-pro-001", (2, 50, 250000) },
                { "gemini-2.5-flash", (15, 1500, 1000000) },
                { "gemini-2.5-flash-001", (15, 1500, 1000000) },
                { "gemini-2.5-flash-lite", (30, 2000, 1000000) },
                { "gemini-2.5-flash-lite-001", (30, 2000, 1000000) },
                { "gemini-2.5-flash-8b", (30, 2000, 1000000) },
                
                // Gemini 2.0 (Stable)
                { "gemini-2.0-pro", (5, 100, 500000) },
                { "gemini-2.0-flash", (15, 1500, 1000000) },
                { "gemini-2.0-flash-001", (15, 1500, 1000000) },
                { "gemini-2.0-flash-lite", (30, 1500, 1000000) },
                { "gemini-2.0-flash-exp", (15, 1500, 1000000) },
                
                // Multimodal / Visual
                { "gemini-2.5-flash-image", (10, 1500, -1) },
                { "gemini-2.0-flash-image", (10, 1500, -1) },
                { "imagen-3.0-generate-002", (2, 100, -1) },
                { "imagen-3.0-capability-001", (2, 100, -1) },
                
                // Audio
                { "gemini-2.5-flash-audio", (5, 500, -1) },
                { "gemini-live-2.5-flash", (3, -1, -1) },
                
                // Experimental
                { "gemini-exp-2026", (5, 50, 250000) },
                { "gemini-2.5-pro-exp-0205", (5, 50, 250000) },
                { "gemini-2.0-flash-thinking-exp", (5, 50, 250000) },
                { "learnlm-1.5-pro-experimental", (5, 50, 250000) },
                
                // Light / Open Source
                { "gemma-2-27b-it", (15, 1500, 250000) },
                { "gemma-2-9b-it", (30, 2000, 500000) },
                { "gemma-2-2b-it", (30, -1, 1000000) }, // Unlimited daily
                
                // Embeddings
                { "text-embedding-005", (100, 10000, -1) },
                { "text-multilingual-embedding-002", (100, 10000, -1) },
                
                // Legacy
                { "gemini-1.5-pro-latest", (2, 50, 32000) },
                { "gemini-1.5-flash-latest", (15, 1500, 1000000) },
                { "gemini-1.5-flash-8b-latest", (15, 1500, 1000000) },
                { "gemini-1.5-pro", (2, 50, 32000) },
                { "gemini-1.5-flash", (15, 1500, 1000000) },
                { "gemini-pro", (60, 1500, 120000) },
                
                // Special Tasks
                { "aqa", (5, 100, -1) },
                { "med-gemini-preview", (2, 20, 100000) },
                
                // Latest aliases
                { "gemini-flash-latest", (15, 1500, 1000000) },
                { "gemini-flash-lite-latest", (30, 2000, 1000000) },
                { "gemini-pro-latest", (5, 100, 500000) }
            };

            // Direct lookup first
            if (rateLimits.ContainsKey(modelName))
            {
                return rateLimits[modelName];
            }

            // Pattern matching for variations
            foreach (var kvp in rateLimits)
            {
                if (modelName.Contains(kvp.Key) || kvp.Key.Contains(modelName))
                {
                    return kvp.Value;
                }
            }

            // Fallback pattern matching
            if (modelName.Contains("3-pro") || modelName.Contains("3.0"))
            {
                return (5, 50, 250000); // Gemini 3 Pro tier
            }
            else if (modelName.Contains("3-flash") || modelName.Contains("3-"))
            {
                return (10, 200, 1000000); // Gemini 3 Flash tier
            }
            else if (modelName.Contains("2.5-pro"))
            {
                return (2, 50, 250000); // Gemini 2.5 Pro tier
            }
            else if (modelName.Contains("2.5-flash-lite") || modelName.Contains("2.5") && modelName.Contains("lite"))
            {
                return (30, 2000, 1000000); // Gemini 2.5 Flash Lite tier
            }
            else if (modelName.Contains("2.5-flash") || modelName.Contains("2.5"))
            {
                return (15, 1500, 1000000); // Gemini 2.5 Flash tier
            }
            else if (modelName.Contains("2.0-pro"))
            {
                return (5, 100, 500000); // Gemini 2.0 Pro tier
            }
            else if (modelName.Contains("2.0-flash-lite") || modelName.Contains("2.0") && modelName.Contains("lite"))
            {
                return (30, 1500, 1000000); // Gemini 2.0 Flash Lite tier
            }
            else if (modelName.Contains("2.0-flash") || modelName.Contains("2.0"))
            {
                return (15, 1500, 1000000); // Gemini 2.0 Flash tier
            }
            else if (modelName.Contains("1.5-pro"))
            {
                return (2, 50, 32000); // Gemini 1.5 Pro tier
            }
            else if (modelName.Contains("1.5-flash"))
            {
                return (15, 1500, 1000000); // Gemini 1.5 Flash tier
            }
            else if (modelName.Contains("gemma"))
            {
                return (30, 2000, 500000); // Gemma tier
            }
            else if (modelName.Contains("exp") || modelName.Contains("preview") || modelName.Contains("experimental"))
            {
                return (5, 50, 250000); // Experimental tier
            }
            else if (modelName.Contains("embedding"))
            {
                return (100, 10000, -1); // Embedding tier
            }
            else if (modelName.Contains("image") || modelName.Contains("imagen"))
            {
                return (10, 1500, -1); // Image generation tier
            }
            else if (modelName.Contains("audio") || modelName.Contains("live"))
            {
                return (5, 500, -1); // Audio tier
            }
            
            // Ultra-conservative default for unknown models
            return (2, 20, 10000);
        }

        private (int requestsPerMinute, int requestsPerDay, int tokensPerMinute) GetDefaultRateLimits(string modelName)
        {
            // Conservative defaults based on known Gemini API limits
            var defaults = new Dictionary<string, (int rpm, int rpd, int tpm)>
            {
                { "gemini-2.0-flash-exp", (15, 1500, 1000000) }, // Free tier limits
                { "gemini-1.5-flash", (15, 1500, 1000000) },
                { "gemini-1.5-pro", (2, 50, 32000) },
                { "gemini-pro", (60, 1500, 120000) }
            };

            if (defaults.ContainsKey(modelName))
            {
                return defaults[modelName];
            }

            // Ultra-conservative default for unknown models
            return (10, 100, 10000);
        }

        // Public method for SettingsWindow to access
        public async Task<List<string>> FetchAvailableModelsAsync(string apiKey)
        {
            var originalApiKey = _apiKey;
            _apiKey = apiKey;
            
            try
            {
                return await GetAvailableModelsAsync();
            }
            finally
            {
                _apiKey = originalApiKey;
            }
        }

        // Public method to get model limits for SettingsWindow
        public Dictionary<string, (int requestsPerMinute, int requestsPerDay, int tokensPerMinute)> GetModelLimits()
        {
            return new Dictionary<string, (int requestsPerMinute, int requestsPerDay, int tokensPerMinute)>(_modelLimits);
        }

        private int CalculateOptimalDelay()
        {
            if (!_modelLimits.ContainsKey(_model))
            {
                return 3000; // Default 3 seconds if no limit info
            }

            var (requestsPerMinute, requestsPerDay, tokensPerMinute) = _modelLimits[_model];
            
            // Clean up old requests (older than 1 minute)
            var oneMinuteAgo = DateTime.Now.AddMinutes(-1);
            while (_recentRequests.Count > 0 && _recentRequests.Peek() < oneMinuteAgo)
            {
                _recentRequests.Dequeue();
            }

            // Calculate delay based on requests per minute
            var requestsInLastMinute = _recentRequests.Count;
            var remainingRequests = Math.Max(0, requestsPerMinute - requestsInLastMinute);
            
            if (remainingRequests == 0)
            {
                // We've hit the limit, wait until the oldest request is 1 minute old
                var oldestRequest = _recentRequests.Peek();
                var waitTime = (int)(60000 - (DateTime.Now - oldestRequest).TotalMilliseconds);
                AddLog($"⏳ Rate limit reached ({requestsPerMinute}/min), waiting {waitTime/1000}s");
                return Math.Max(waitTime, 1000);
            }

            // Calculate optimal delay to spread requests evenly
            var optimalDelay = (int)(60000.0 / requestsPerMinute);
            
            // Add some buffer to be safe
            optimalDelay = (int)(optimalDelay * 1.2);
            
            // Minimum 1 second, maximum 30 seconds
            optimalDelay = Math.Max(1000, Math.Min(optimalDelay, 30000));
            
            AddLog($"⚡ Optimal delay for {_model}: {optimalDelay/1000}s ({remainingRequests} requests remaining this minute)");
            return optimalDelay;
        }

        private void TrackRequest()
        {
            _recentRequests.Enqueue(DateTime.Now);
            _lastRequestTime = DateTime.Now;
        }

        private int EstimateTokens(string text)
        {
            return (int)Math.Ceiling(text.Length / (double)CHARS_PER_TOKEN);
        }

        private List<List<LocalizationEntry>> CreateBatches(List<LocalizationEntry> entries)
        {
            var batches = new List<List<LocalizationEntry>>();
            var currentBatch = new List<LocalizationEntry>();
            var currentTokens = BATCH_OVERHEAD_TOKENS; // Start with overhead

            // Get model's token limit
            var tokenLimit = GetModelTokenLimit(_model);
            var maxBatchTokens = (int)(tokenLimit * 0.7); // Use 70% of limit for safety

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Value)) continue;

                var entryTokens = EstimateTokens(entry.Value) + 20; // +20 for JSON structure overhead
                
                // If adding this entry would exceed limit, start new batch
                if (currentTokens + entryTokens > maxBatchTokens && currentBatch.Count > 0)
                {
                    batches.Add(currentBatch);
                    currentBatch = new List<LocalizationEntry>();
                    currentTokens = BATCH_OVERHEAD_TOKENS;
                }

                currentBatch.Add(entry);
                currentTokens += entryTokens;

                // Safety limit: max 20 entries per batch to avoid too complex requests
                if (currentBatch.Count >= 20)
                {
                    batches.Add(currentBatch);
                    currentBatch = new List<LocalizationEntry>();
                    currentTokens = BATCH_OVERHEAD_TOKENS;
                }
            }

            // Add remaining entries
            if (currentBatch.Count > 0)
            {
                batches.Add(currentBatch);
            }

            return batches;
        }

        private int GetModelTokenLimit(string modelName)
        {
            // Get input token limits from API data or estimates
            var tokenLimits = new Dictionary<string, int>
            {
                { "gemini-3-pro-preview", 2000000 },
                { "gemini-3-flash-preview", 1000000 },
                { "gemini-2.5-pro", 2000000 },
                { "gemini-2.5-flash", 1000000 },
                { "gemini-2.0-flash", 1000000 },
                { "gemini-1.5-pro", 2000000 },
                { "gemini-1.5-flash", 1000000 },
                { "gemini-pro", 30720 }
            };

            // Try exact match first
            if (tokenLimits.ContainsKey(modelName))
                return tokenLimits[modelName];

            // Pattern matching
            foreach (var kvp in tokenLimits)
            {
                if (modelName.Contains(kvp.Key) || kvp.Key.Contains(modelName))
                    return kvp.Value;
            }

            // Conservative default
            return 30720;
        }

        private double CalculateCost(int inputChars, int outputChars, string modelName)
        {
            // First try to use API pricing
            if (_modelPricing.ContainsKey(modelName))
            {
                var (inputPrice, outputPrice) = _modelPricing[modelName];
                return (inputChars * inputPrice / 1000.0) + (outputChars * outputPrice / 1000.0);
            }

            // If no pricing available from API, use a generic estimate
            // This should rarely be used since we fetch pricing from API
            var genericInputPrice = 0.000075; // Per 1K chars
            var genericOutputPrice = 0.0003;  // Per 1K chars
            
            AddLog($"⚠️ Using generic pricing for {modelName} - consider refreshing models for accurate pricing");
            return (inputChars * genericInputPrice / 1000.0) + (outputChars * genericOutputPrice / 1000.0);
        }

        private void UpdateCacheInfo()
        {
            var costText = _totalCost > 0 ? $" | Cost: ${_totalCost:F4}" : "";
            CacheInfo.Text = $"💾 Cache: {_cache.Count} | Hits: {_cacheHits} | API: {_apiCalls}{costText}";
        }

        private void AddLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogTextBox.Text += $"[{timestamp}] {message}\n";
            LogTextBox.ScrollToEnd();
        }

        // Event Handlers
        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select XML Localization File",
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                DefaultExt = "xml",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadXml(openFileDialog.FileName);
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save XML Localization File",
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                DefaultExt = "xml",
                FileName = "localized.xml"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                SaveXml(saveFileDialog.FileName);
            }
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsWindow(_apiKey, _model, _targetLanguage, _programLanguage, _customPrompt, this);
            if (settings.ShowDialog() == true)
            {
                _apiKey = settings.ApiKey;
                _model = settings.Model;
                _targetLanguage = settings.TargetLanguage;
                
                // Update program language if changed
                if (_programLanguage != settings.ProgramLanguage)
                {
                    _programLanguage = settings.ProgramLanguage;
                    LocalizationManager.CurrentLanguage = _programLanguage;
                    ApplyLocalization();
                }
                
                _customPrompt = settings.CustomPrompt;
                
                SaveConfig();
                AddLog($"✅ Settings updated - Model: {_model}, Language: {_targetLanguage}");
            }
        }

        private void StatsBtn_Click(object sender, RoutedEventArgs e)
        {
            var total = _entries.Count;
            var translated = _entries.Count(e => !string.IsNullOrEmpty(e.Translation));
            var untranslated = total - translated;
            var progress = total > 0 ? (translated * 100.0 / total) : 0;

            var stats = $"📊 Statistics\n\n" +
                       $"Total Entries: {total}\n" +
                       $"✅ Translated: {translated}\n" +
                       $"❌ Untranslated: {untranslated}\n" +
                       $"📈 Progress: {progress:F1}%\n\n" +
                       $"💾 Cache: {_cache.Count}\n" +
                       $"🎯 Cache Hits: {_cacheHits}\n" +
                       $"🌐 API Calls: {_apiCalls}";

            MessageBox.Show(stats, "Statistics", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void TranslateSelectedBtn_Click(object sender, RoutedEventArgs e)
        {
            var selected = _entries.Where(entry => entry.IsSelected).ToList();
            if (!selected.Any())
            {
                MessageBox.Show("Please select entries to translate", "Info");
                return;
            }

            await TranslateEntries(selected);
        }

        private async void TranslatePartialBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PartialCountTxt.Text, out var count) || count <= 0)
            {
                MessageBox.Show("Please enter a valid number", "Error");
                return;
            }

            var untranslated = _entries.Where(e => string.IsNullOrEmpty(e.Translation) && !string.IsNullOrEmpty(e.Value)).Take(count).ToList();
            if (!untranslated.Any())
            {
                MessageBox.Show("No untranslated entries found", "Info");
                return;
            }

            await TranslateEntries(untranslated);
        }

        private async void TranslateAllBtn_Click(object sender, RoutedEventArgs e)
        {
            var untranslated = _entries.Where(e => string.IsNullOrEmpty(e.Translation) && !string.IsNullOrEmpty(e.Value)).ToList();
            if (!untranslated.Any())
            {
                MessageBox.Show("No untranslated entries found", "Info");
                return;
            }

            var result = MessageBox.Show($"Translate {untranslated.Count} entries? This may take a while.", 
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await TranslateEntries(untranslated);
            }
        }

        private async Task TranslateEntries(List<LocalizationEntry> entries)
        {
            _translationCancellationTokenSource = new CancellationTokenSource();
            _isTranslationRunning = true;
            _isTranslationPaused = false;
            
            try
            {
                ShowControlButtons(true);
                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.IsIndeterminate = false;
                
                var successCount = 0;
                var failCount = 0;

                // Filter out entries that need translation
                var entriesToTranslate = entries.Where(e => !string.IsNullOrEmpty(e.Value) && string.IsNullOrEmpty(e.Translation)).ToList();
                
                if (!entriesToTranslate.Any())
                {
                    AddLog("ℹ️ No entries need translation");
                    StatusText.Text = "No entries need translation";
                    return;
                }

                // Create batches based on token limits
                var batches = CreateBatches(entriesToTranslate);
                
                ProgressBar.Maximum = batches.Count;
                ProgressBar.Value = 0;

                AddLog($"🌍 Starting batch translation: {entriesToTranslate.Count} entries in {batches.Count} batches");
                AddLog($"📊 Model: {_model} (Rate limits: {(_modelLimits.ContainsKey(_model) ? $"{_modelLimits[_model].requestsPerMinute}/min" : "unknown")})");

                for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
                {
                    // Check for cancellation
                    if (_translationCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        AddLog($"⏹️ Translation stopped at batch {batchIndex + 1}/{batches.Count}");
                        break;
                    }

                    // Handle pause
                    while (_isTranslationPaused && !_translationCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(500, _translationCancellationTokenSource.Token);
                    }

                    if (_translationCancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    var batch = batches[batchIndex];
                    var batchSize = batch.Count;
                    
                    StatusText.Text = $"Translating batch {batchIndex + 1}/{batches.Count} ({batchSize} entries)...";
                    ProgressBar.Value = batchIndex;
                    
                    AddLog($"🔄 Processing batch {batchIndex + 1}/{batches.Count}: {batchSize} entries");

                    // Track request for rate limiting
                    TrackRequest();

                    var batchResults = await TranslateBatchAsync(batch);
                    
                    // Apply translations
                    var batchSuccessCount = 0;
                    var batchFailCount = 0;
                    
                    foreach (var entry in batch)
                    {
                        if (batchResults.ContainsKey(entry.Value))
                        {
                            entry.Translation = batchResults[entry.Value];
                            batchSuccessCount++;
                            AddLog($"✅ {entry.Key.Substring(0, Math.Min(40, entry.Key.Length))}: {entry.Translation.Substring(0, Math.Min(30, entry.Translation.Length))}...");
                        }
                        else
                        {
                            batchFailCount++;
                            AddLog($"❌ Failed: {entry.Key.Substring(0, Math.Min(40, entry.Key.Length))}");
                        }
                    }

                    successCount += batchSuccessCount;
                    failCount += batchFailCount;

                    AddLog($"📊 Batch {batchIndex + 1} complete: {batchSuccessCount} success, {batchFailCount} failed");

                    // Use model-specific optimal delay between batches
                    if (batchIndex < batches.Count - 1 && !_translationCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        var delay = CalculateOptimalDelay();
                        StatusText.Text = $"Waiting {delay/1000}s before next batch (rate limit optimization)...";
                        
                        try
                        {
                            await Task.Delay(delay, _translationCancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }

                ProgressBar.Value = batches.Count;

                // Auto-save if we have successful translations
                if (successCount > 0)
                {
                    SaveXml();
                }

                var statusMessage = _translationCancellationTokenSource.Token.IsCancellationRequested 
                    ? $"Translation stopped: {successCount} success, {failCount} failed"
                    : $"Batch translation complete: {successCount} success, {failCount} failed";
                    
                StatusText.Text = statusMessage;
                AddLog($"🎉 {statusMessage}");
                
                if (failCount > 0)
                {
                    AddLog($"💡 Tips to reduce failures:");
                    AddLog($"   • Wait 15-30 minutes between large batches");
                    AddLog($"   • Try a different model with higher limits");
                    AddLog($"   • Check if API key has sufficient quota");
                }

                // Show efficiency stats
                var efficiency = entriesToTranslate.Count > 0 ? (successCount * 100.0 / entriesToTranslate.Count) : 0;
                AddLog($"📈 Translation efficiency: {efficiency:F1}% ({successCount}/{entriesToTranslate.Count})");
                AddLog($"⚡ Batch efficiency: {batches.Count} API calls instead of {entriesToTranslate.Count} (saved {entriesToTranslate.Count - batches.Count} calls)");

                // Show rate limit summary
                if (_modelLimits.ContainsKey(_model))
                {
                    var limits = _modelLimits[_model];
                    var requestsInLastMinute = _recentRequests.Count;
                    AddLog($"📊 Rate limit status: {requestsInLastMinute}/{limits.requestsPerMinute} requests used this minute");
                }
            }
            catch (OperationCanceledException)
            {
                AddLog($"⏹️ Translation was cancelled");
                StatusText.Text = "Translation cancelled";
            }
            catch (Exception ex)
            {
                AddLog($"❌ Error: {ex.Message}");
                MessageBox.Show($"Translation error: {ex.Message}", "Error");
            }
            finally
            {
                _isTranslationRunning = false;
                _isTranslationPaused = false;
                ShowControlButtons(false);
                PauseBtn.Content = "⏸️ Pause"; // Reset pause button text
                ProgressBar.Visibility = Visibility.Collapsed;
                ProgressBar.Value = 0;
                _translationCancellationTokenSource?.Dispose();
                _translationCancellationTokenSource = null;
            }
        }

        private void ClearCacheBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Clear {_cache.Count} cached translations?", 
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _cache.Clear();
                SaveConfig();
                UpdateCacheInfo();
                AddLog("🗑️ Cache cleared");
            }
        }

        private void ClearLogBtn_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Text = "";
            AddLog("🗑️ Log cleared");
        }

        private void PauseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isTranslationRunning)
            {
                _isTranslationPaused = !_isTranslationPaused;
                
                if (_isTranslationPaused)
                {
                    PauseBtn.Content = "▶️ Resume";
                    StatusText.Text = "Translation paused";
                    AddLog("⏸️ Translation paused by user");
                }
                else
                {
                    PauseBtn.Content = "⏸️ Pause";
                    StatusText.Text = "Translation resumed";
                    AddLog("▶️ Translation resumed by user");
                }
            }
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isTranslationRunning && _translationCancellationTokenSource != null)
            {
                _translationCancellationTokenSource.Cancel();
                AddLog("⏹️ Translation stopped by user");
                StatusText.Text = "Translation stopped";
            }
        }

        private void ShowControlButtons(bool show)
        {
            PauseBtn.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            StopBtn.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            
            // Disable/enable translation buttons
            TranslateSelectedBtn.IsEnabled = !show;
            TranslatePartialBtn.IsEnabled = !show;
            TranslateAllBtn.IsEnabled = !show;
        }
    }

    public class LocalizationEntry : INotifyPropertyChanged
    {
        private string _key = "";
        private string _value = "";
        private string _translation = "";
        private bool _isSelected;

        public string Key
        {
            get => _key;
            set { _key = value; OnPropertyChanged(nameof(Key)); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(nameof(Value)); }
        }

        public string Translation
        {
            get => _translation;
            set 
            { 
                _translation = value; 
                OnPropertyChanged(nameof(Translation));
                OnPropertyChanged(nameof(StatusIcon));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public string StatusIcon => string.IsNullOrEmpty(Translation) ? "❌" : "✅";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}