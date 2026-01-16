using System;
using System.Collections.Generic;
using System.Globalization;

namespace SimpleXmlEditor.Localization
{
    public static class LocalizationManager
    {
        private static Dictionary<string, Dictionary<string, string>> _translations = new();
        private static string _currentLanguage = "en";

        static LocalizationManager()
        {
            InitializeTranslations();
        }

        public static string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                _currentLanguage = value;
                LanguageChanged?.Invoke();
            }
        }

        public static event Action LanguageChanged;

        public static string GetString(string key)
        {
            if (_translations.ContainsKey(_currentLanguage) && 
                _translations[_currentLanguage].ContainsKey(key))
            {
                return _translations[_currentLanguage][key];
            }

            // Fallback to English
            if (_translations.ContainsKey("en") && 
                _translations["en"].ContainsKey(key))
            {
                return _translations["en"][key];
            }

            return key; // Return key if translation not found
        }

        public static List<(string Code, string Name)> GetAvailableLanguages()
        {
            return new List<(string, string)>
            {
                ("en", "English"),
                ("tr", "Türkçe"),
                ("es", "Español"),
                ("fr", "Français"),
                ("de", "Deutsch"),
                ("it", "Italiano"),
                ("pt", "Português"),
                ("ru", "Русский"),
                ("ja", "日本語"),
                ("ko", "한국어"),
                ("zh", "中文"),
                ("ar", "العربية"),
                ("hi", "हिन्दी"),
                ("nl", "Nederlands")
            };
        }

        private static void InitializeTranslations()
        {
            // English (default)
            _translations["en"] = new Dictionary<string, string>
            {
                // Window titles
                ["WindowTitle"] = "XML AI Translator by Veloxcity",
                ["SettingsTitle"] = "Settings - XML AI Translator",
                
                // Main UI
                ["Load"] = "Load",
                ["Save"] = "Save",
                ["Settings"] = "Settings",
                ["Stats"] = "Stats",
                ["Ready"] = "Ready",
                
                // AI Translation
                ["AITranslationCenter"] = "AI Translation Center",
                ["TranslateSelected"] = "Translate Selected",
                ["TranslateAll"] = "Translate All",
                ["Translate"] = "Translate",
                ["First"] = "First",
                ["rows"] = "rows",
                ["Cache"] = "Cache",
                ["ClearCache"] = "Clear Cache",
                ["Pause"] = "Pause",
                ["Resume"] = "Resume",
                ["Stop"] = "Stop",
                
                // Data Grid
                ["TranslationData"] = "Translation Data",
                ["Select"] = "Select",
                ["Status"] = "Status",
                ["Key"] = "Key",
                ["Original"] = "Original",
                ["Translation"] = "Translation",
                
                // Activity Log
                ["ActivityLog"] = "Activity Log",
                ["RealTime"] = "Real-time",
                ["AutoScroll"] = "Auto-scroll",
                ["ClearLog"] = "Clear Log",
                
                // Settings
                ["AIConfiguration"] = "AI Configuration",
                ["ConfigureSettings"] = "Configure your AI translation settings",
                ["APIKey"] = "API Key",
                ["EnterAPIKey"] = "Enter your Google Gemini API key from AI Studio",
                ["AIModel"] = "AI Model",
                ["SelectModel"] = "Select an AI model with rate limits and pricing info",
                ["Refresh"] = "Refresh",
                ["TargetLanguage"] = "Target Language",
                ["SelectTargetLanguage"] = "Select the target language for translation",
                ["ProgramLanguage"] = "Program Language",
                ["SelectProgramLanguage"] = "Select the interface language",
                ["CustomPrompt"] = "Custom Prompt",
                ["CustomPromptHelp"] = "Customize the AI translation prompt. Use {LANGUAGE} for target language and {CONTEXT} for content type.",
                ["Reset"] = "Reset",
                ["QuickTips"] = "Quick Tips",
                ["TipAPIKey"] = "Get your free API key from https://aistudio.google.com",
                ["TipModels"] = "Flash models are faster, Pro models are more accurate",
                ["TipRateLimits"] = "Rate limits are automatically optimized per model",
                ["TipCache"] = "Translation cache reduces API costs",
                ["TipLanguages"] = "60+ languages supported for translation",
                ["SaveApply"] = "Save & Apply",
                ["Cancel"] = "Cancel",
                
                // Messages
                ["PleaseEnterAPIKey"] = "Please enter an API Key",
                ["PleaseSelectModel"] = "Please select a model",
                ["NoModelsFound"] = "No models found. Please check your API key.",
                ["ModelsFoundSuccess"] = "Found {0} models with rate limit information",
                ["ErrorFetchingModels"] = "Error fetching models: {0}",
                ["TranslationComplete"] = "Translation complete: {0} success, {1} failed",
                ["TranslationStopped"] = "Translation stopped: {0} success, {1} failed",
                ["NoEntriesNeedTranslation"] = "No entries need translation"
            };

            // Turkish
            _translations["tr"] = new Dictionary<string, string>
            {
                ["WindowTitle"] = "XML AI Çevirici by Veloxcity",
                ["SettingsTitle"] = "Ayarlar - XML AI Çevirici",
                
                ["Load"] = "Yükle",
                ["Save"] = "Kaydet",
                ["Settings"] = "Ayarlar",
                ["Stats"] = "İstatistik",
                ["Ready"] = "Hazır",
                
                ["AITranslationCenter"] = "AI Çeviri Merkezi",
                ["TranslateSelected"] = "Seçilenleri Çevir",
                ["TranslateAll"] = "Tümünü Çevir",
                ["Translate"] = "Çevir",
                ["First"] = "İlk",
                ["rows"] = "satır",
                ["Cache"] = "Önbellek",
                ["ClearCache"] = "Önbelleği Temizle",
                ["Pause"] = "Duraklat",
                ["Resume"] = "Devam Et",
                ["Stop"] = "Durdur",
                
                ["TranslationData"] = "Çeviri Verileri",
                ["Select"] = "Seç",
                ["Status"] = "Durum",
                ["Key"] = "Anahtar",
                ["Original"] = "Orijinal",
                ["Translation"] = "Çeviri",
                
                ["ActivityLog"] = "Etkinlik Günlüğü",
                ["RealTime"] = "Gerçek zamanlı",
                ["AutoScroll"] = "Otomatik kaydır",
                ["ClearLog"] = "Günlüğü Temizle",
                
                ["AIConfiguration"] = "AI Yapılandırması",
                ["ConfigureSettings"] = "AI çeviri ayarlarınızı yapılandırın",
                ["APIKey"] = "API Anahtarı",
                ["EnterAPIKey"] = "AI Studio'dan Google Gemini API anahtarınızı girin",
                ["AIModel"] = "AI Modeli",
                ["SelectModel"] = "Hız limitleri ve fiyat bilgileri olan bir AI modeli seçin",
                ["Refresh"] = "Yenile",
                ["TargetLanguage"] = "Hedef Dil",
                ["SelectTargetLanguage"] = "Çeviri için hedef dili seçin",
                ["ProgramLanguage"] = "Program Dili",
                ["SelectProgramLanguage"] = "Arayüz dilini seçin",
                ["CustomPrompt"] = "Özel Prompt",
                ["CustomPromptHelp"] = "AI çeviri prompt'unu özelleştirin. Hedef dil için {LANGUAGE}, içerik türü için {CONTEXT} kullanın.",
                ["Reset"] = "Sıfırla",
                ["QuickTips"] = "Hızlı İpuçları",
                ["TipAPIKey"] = "Ücretsiz API anahtarınızı https://aistudio.google.com adresinden alın",
                ["TipModels"] = "Flash modeller daha hızlı, Pro modeller daha doğru",
                ["TipRateLimits"] = "Hız limitleri model başına otomatik optimize edilir",
                ["TipCache"] = "Çeviri önbelleği API maliyetlerini azaltır",
                ["TipLanguages"] = "60+ dil çeviri için desteklenir",
                ["SaveApply"] = "Kaydet ve Uygula",
                ["Cancel"] = "İptal",
                
                ["PleaseEnterAPIKey"] = "Lütfen bir API Anahtarı girin",
                ["PleaseSelectModel"] = "Lütfen bir model seçin",
                ["NoModelsFound"] = "Model bulunamadı. Lütfen API anahtarınızı kontrol edin.",
                ["ModelsFoundSuccess"] = "{0} model bulundu, hız limiti bilgileri ile",
                ["ErrorFetchingModels"] = "Model getirme hatası: {0}",
                ["TranslationComplete"] = "Çeviri tamamlandı: {0} başarılı, {1} başarısız",
                ["TranslationStopped"] = "Çeviri durduruldu: {0} başarılı, {1} başarısız",
                ["NoEntriesNeedTranslation"] = "Çevrilmesi gereken giriş yok"
            };

            // Spanish
            _translations["es"] = new Dictionary<string, string>
            {
                ["WindowTitle"] = "Traductor XML AI by Veloxcity",
                ["SettingsTitle"] = "Configuración - Traductor XML AI",
                
                ["Load"] = "Cargar",
                ["Save"] = "Guardar",
                ["Settings"] = "Configuración",
                ["Stats"] = "Estadísticas",
                ["Ready"] = "Listo",
                
                ["AITranslationCenter"] = "Centro de Traducción AI",
                ["TranslateSelected"] = "Traducir Seleccionados",
                ["TranslateAll"] = "Traducir Todo",
                ["Translate"] = "Traducir",
                ["First"] = "Primeras",
                ["rows"] = "filas",
                ["Cache"] = "Caché",
                ["ClearCache"] = "Limpiar Caché",
                ["Pause"] = "Pausar",
                ["Resume"] = "Reanudar",
                ["Stop"] = "Detener",
                
                ["TranslationData"] = "Datos de Traducción",
                ["Select"] = "Seleccionar",
                ["Status"] = "Estado",
                ["Key"] = "Clave",
                ["Original"] = "Original",
                ["Translation"] = "Traducción",
                
                ["ActivityLog"] = "Registro de Actividad",
                ["RealTime"] = "Tiempo real",
                ["AutoScroll"] = "Desplazamiento automático",
                ["ClearLog"] = "Limpiar Registro",
                
                ["AIConfiguration"] = "Configuración AI",
                ["ConfigureSettings"] = "Configure sus ajustes de traducción AI",
                ["APIKey"] = "Clave API",
                ["EnterAPIKey"] = "Ingrese su clave API de Google Gemini desde AI Studio",
                ["AIModel"] = "Modelo AI",
                ["SelectModel"] = "Seleccione un modelo AI con límites de velocidad e información de precios",
                ["Refresh"] = "Actualizar",
                ["TargetLanguage"] = "Idioma Objetivo",
                ["SelectTargetLanguage"] = "Seleccione el idioma objetivo para la traducción",
                ["ProgramLanguage"] = "Idioma del Programa",
                ["SelectProgramLanguage"] = "Seleccione el idioma de la interfaz",
                ["CustomPrompt"] = "Prompt Personalizado",
                ["CustomPromptHelp"] = "Personalice el prompt de traducción AI. Use {LANGUAGE} para idioma objetivo y {CONTEXT} para tipo de contenido.",
                ["Reset"] = "Restablecer",
                ["QuickTips"] = "Consejos Rápidos",
                ["SaveApply"] = "Guardar y Aplicar",
                ["Cancel"] = "Cancelar"
            };

            // Add more languages as needed...
        }
    }
}