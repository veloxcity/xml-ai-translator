using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using SimpleXmlEditor.Localization;

namespace SimpleXmlEditor
{
    public partial class SettingsWindow : Window
    {
        public string ApiKey { get; private set; }
        public string Model { get; private set; }
        public string TargetLanguage { get; private set; }
        public string ProgramLanguage { get; private set; }
        public string CustomPrompt { get; private set; }
        private readonly MainWindow _mainWindow;

        public SettingsWindow(string currentApiKey, string currentModel, string currentTargetLanguage, string currentProgramLanguage, string currentCustomPrompt, MainWindow mainWindow)
        {
            InitializeComponent();
            
            _mainWindow = mainWindow;
            ApiKeyTextBox.Text = currentApiKey;
            
            // Set current model - check both display text and tag
            foreach (System.Windows.Controls.ComboBoxItem item in ModelComboBox.Items)
            {
                if (item.Tag?.ToString() == currentModel || 
                    item.Content?.ToString().StartsWith(currentModel) == true)
                {
                    ModelComboBox.SelectedItem = item;
                    break;
                }
            }
            
            if (ModelComboBox.SelectedItem == null && ModelComboBox.Items.Count > 0)
            {
                ModelComboBox.SelectedIndex = 0;
            }

            // Set current target language
            foreach (System.Windows.Controls.ComboBoxItem item in TargetLanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == currentTargetLanguage)
                {
                    TargetLanguageComboBox.SelectedItem = item;
                    break;
                }
            }
            
            // Default to Turkish if nothing selected
            if (TargetLanguageComboBox.SelectedItem == null)
            {
                TargetLanguageComboBox.SelectedIndex = 0; // Turkish
            }

            // Set current program language
            foreach (System.Windows.Controls.ComboBoxItem item in ProgramLanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == currentProgramLanguage)
                {
                    ProgramLanguageComboBox.SelectedItem = item;
                    break;
                }
            }
            
            // Default to English if nothing selected
            if (ProgramLanguageComboBox.SelectedItem == null)
            {
                ProgramLanguageComboBox.SelectedIndex = 0; // English
            }

            // Set custom prompt
            CustomPromptTextBox.Text = string.IsNullOrEmpty(currentCustomPrompt) ? GetDefaultPrompt() : currentCustomPrompt;
        }

        private async void RefreshModelsBtn_Click(object sender, RoutedEventArgs e)
        {
            var apiKey = ApiKeyTextBox.Text.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("Please enter an API Key first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RefreshModelsBtn.IsEnabled = false;
            RefreshModelsBtn.Content = "Loading...";

            try
            {
                var models = await _mainWindow.FetchAvailableModelsAsync(apiKey);

                if (models.Count > 0)
                {
                    ModelComboBox.Items.Clear();
                    
                    // Get rate limits from MainWindow
                    var rateLimits = _mainWindow.GetModelLimits();
                    
                    foreach (var model in models)
                    {
                        var displayText = model;
                        
                        // Add rate limit info to display text if available
                        if (rateLimits.ContainsKey(model))
                        {
                            var limits = rateLimits[model];
                            var rpmText = limits.requestsPerMinute.ToString();
                            var rpdText = limits.requestsPerDay == -1 ? "∞" : limits.requestsPerDay.ToString();
                            var tpmText = limits.tokensPerMinute == -1 ? "∞" : $"{limits.tokensPerMinute / 1000}K";
                            
                            displayText = $"{model} ({rpmText}/min, {rpdText}/day, {tpmText} tokens)";
                        }
                        
                        var item = new System.Windows.Controls.ComboBoxItem
                        {
                            Content = displayText,
                            Tag = model // Store the actual model name in Tag
                        };
                        
                        ModelComboBox.Items.Add(item);
                    }
                    
                    if (ModelComboBox.Items.Count > 0)
                    {
                        ModelComboBox.SelectedIndex = 0;
                    }
                    
                    MessageBox.Show($"Found {models.Count} models with rate limit information", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("No models found. Please check your API key.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error fetching models: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                RefreshModelsBtn.IsEnabled = true;
                RefreshModelsBtn.Content = "🔄 Refresh";
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ApiKey = ApiKeyTextBox.Text.Trim();
            
            // Get the actual model name from the selected item's Tag
            if (ModelComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
            {
                Model = selectedItem.Tag?.ToString() ?? "";
            }
            else
            {
                Model = ModelComboBox.SelectedItem?.ToString() ?? "";
            }

            // Get target language
            if (TargetLanguageComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem targetLangItem)
            {
                TargetLanguage = targetLangItem.Tag?.ToString() ?? "Turkish";
            }
            else
            {
                TargetLanguage = "Turkish";
            }

            // Get program language
            if (ProgramLanguageComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem programLangItem)
            {
                ProgramLanguage = programLangItem.Tag?.ToString() ?? "en";
            }
            else
            {
                ProgramLanguage = "en";
            }

            // Get custom prompt
            CustomPrompt = CustomPromptTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(ApiKey))
            {
                MessageBox.Show("Please enter an API Key", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (string.IsNullOrEmpty(Model))
            {
                MessageBox.Show("Please select a model", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ResetPromptBtn_Click(object sender, RoutedEventArgs e)
        {
            CustomPromptTextBox.Text = GetDefaultPrompt();
        }

        private string GetDefaultPrompt()
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
    }
}