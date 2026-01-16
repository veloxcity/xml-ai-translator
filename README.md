# XML AI Translator by Veloxcity

🌐 **Modern XML Localization Tool with AI-Powered Batch Translation**

A powerful, modern WPF application for translating XML localization files using Google Gemini AI. Features intelligent batch processing, real-time rate limiting, and a beautiful Material Design-inspired interface.

![XML AI Translator](https://img.shields.io/badge/Platform-.NET%208.0-blue)
![License](https://img.shields.io/badge/License-MIT-green)
![AI](https://img.shields.io/badge/AI-Google%20Gemini-orange)

## ✨ Features

### 🤖 **AI-Powered Translation**
- **Google Gemini Integration**: Support for all Gemini models (2.5 Flash, 3.0 Pro, etc.)
- **Batch Translation**: Translate multiple entries in single API calls (90%+ cost reduction)
- **Smart Token Management**: Automatic batching based on model token limits
- **Single Best Translation**: AI provides one optimal translation instead of multiple options
- **Gaming Context Aware**: Optimized for game localization terminology

### 🎨 **Modern UI/UX**
- **Material Design**: Beautiful, modern interface with smooth animations
- **Real-time Progress**: Live progress tracking with pause/resume/stop controls
- **Activity Log**: Terminal-style activity log with timestamps
- **Responsive Design**: Clean, professional layout with card-based components

### ⚡ **Performance & Efficiency**
- **Intelligent Rate Limiting**: Model-specific rate limits with automatic optimization
- **Translation Cache**: Avoid re-translating identical content
- **Batch Processing**: Process 5-20 entries per API call based on token limits
- **Auto-save**: Automatic saving of successful translations

### 🛡️ **Reliability**
- **Pause/Resume**: Control translation process in real-time
- **Error Handling**: Robust error handling with retry logic
- **Progress Preservation**: Resume from where you left off
- **Graceful Cancellation**: Clean resource management

## 🚀 Quick Start

### Prerequisites
- **.NET 8.0 Runtime** (Windows)
- **Google Gemini API Key** (free from [AI Studio](https://aistudio.google.com))

### Installation
1. Download the latest release from [Releases](../../releases)
2. Extract the ZIP file
3. Run `SimpleXmlEditor.exe`

### Setup
1. **Get API Key**: Visit [Google AI Studio](https://aistudio.google.com) and create a free API key
2. **Configure**: Click ⚙️ Settings → Enter your API key → Refresh models → Select a model
3. **Load XML**: Click 📁 Load to open your XML localization file
4. **Translate**: Select entries and click 🎯 Translate Selected or 🚀 Translate All

## 📋 Supported XML Format

The application works with Microsoft Excel XML format commonly used for game localization:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet">
  <Worksheet ss:Name="Localization">
    <Table>
      <Row>
        <Cell><Data ss:Type="String">ui.menu.start</Data></Cell>
        <Cell><Data ss:Type="String">Start Game</Data></Cell>
      </Row>
    </Table>
  </Worksheet>
</Workbook>
```

## 🎯 Usage Tips

### **Batch Translation Efficiency**
- **Small batches**: Use "First 10 rows" for testing
- **Selected entries**: Use Ctrl+click to select specific entries
- **Full translation**: Use "Translate All" for complete files
- **Model selection**: Flash models are faster, Pro models are more accurate

### **Rate Limit Optimization**
- **Model limits**: Each model has different rate limits (displayed in settings)
- **Automatic delays**: The app calculates optimal delays between requests
- **Pause feature**: Use pause if you hit rate limits
- **Cache benefits**: Identical texts are cached to avoid re-translation

### **Cost Optimization**
- **Batch processing**: Reduces API calls by 90%+
- **Translation cache**: Avoids duplicate translations
- **Model selection**: Choose appropriate model for your needs
- **Selective translation**: Only translate what you need

## 🔧 Configuration

### **Supported Models**
- **Gemini 3.0 Pro**: Highest quality, lowest rate limits
- **Gemini 2.5 Flash**: Balanced speed and quality
- **Gemini 2.0 Flash**: Fast and efficient
- **Gemini Flash Lite**: Highest rate limits
- **And more**: All Gemini models supported

### **Rate Limits** (automatically detected)
- **Gemini 3 Pro**: 5 requests/min, 50/day
- **Gemini 2.5 Flash**: 15 requests/min, 1500/day
- **Gemini Flash Lite**: 30 requests/min, 2000/day

## 🛠️ Development

### **Build Requirements**
- Visual Studio 2022 or VS Code
- .NET 8.0 SDK
- Windows 10/11

### **Build Instructions**
```bash
git clone https://github.com/yourusername/xml-ai-translator.git
cd xml-ai-translator
dotnet build SimpleXmlEditor/SimpleXmlEditor.csproj
dotnet run --project SimpleXmlEditor/SimpleXmlEditor.csproj
```

### **Project Structure**
```
SimpleXmlEditor/
├── MainWindow.xaml          # Main UI
├── MainWindow.xaml.cs       # Main logic & batch translation
├── SettingsWindow.xaml      # Settings UI
├── SettingsWindow.xaml.cs   # Settings logic
└── SimpleXmlEditor.csproj   # Project file
```

## 📊 Performance Stats

### **Efficiency Improvements**
- **API Calls**: 90%+ reduction through batch processing
- **Translation Speed**: 5-10x faster than single-entry translation
- **Cost Savings**: Significant reduction in API costs
- **Rate Limit Optimization**: Smart delays prevent 429 errors

### **Batch Processing Examples**
- **1000 entries** → ~50-100 API calls (instead of 1000)
- **Gemini 2.5 Flash**: ~50-100 entries per batch
- **Gemini Pro**: ~10-20 entries per batch
- **Token efficiency**: 70% of model limits utilized safely

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### **Areas for Contribution**
- Additional AI providers (OpenAI, Claude, etc.)
- More XML format support
- UI/UX improvements
- Performance optimizations
- Bug fixes and testing

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Google Gemini AI** for powerful translation capabilities
- **Microsoft WPF** for the UI framework
- **Newtonsoft.Json** for JSON processing
- **Material Design** for UI inspiration

## 📞 Support

- **Issues**: [GitHub Issues](../../issues)
- **Discussions**: [GitHub Discussions](../../discussions)
- **Documentation**: This README and inline code comments

---

**Made with ❤️ by Veloxcity**

*Transform your XML localization workflow with AI-powered batch translation!*