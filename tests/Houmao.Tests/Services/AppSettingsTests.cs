using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Houmao.Models;
using Houmao.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Houmao.Tests.Services
{
    public class AppSettingsTests : IDisposable
    {
        private readonly string _testSettingsPath;
        private readonly Mock<ILogger<AppSettings>> _loggerMock;
        private readonly AppSettings _settings;
        
        public AppSettingsTests()
        {
            _testSettingsPath = Path.Combine(Path.GetTempPath(), $"houmao-test-{Guid.NewGuid()}.json");
            _loggerMock = new Mock<ILogger<AppSettings>>();
            _settings = new AppSettings(_loggerMock.Object, _testSettingsPath);
        }
        
        [Fact]
        public void Load_WhenFileDoesNotExist_ShouldUseDefaults()
        {
            // Act
            _settings.Load();
            
            // Assert
            _settings.Providers.Should().HaveCount(1);
            _settings.Providers[0].Name.Should().Be("OpenAI");
            _settings.Providers[0].IsDefault.Should().BeTrue();
            _settings.StartWithWindows.Should().BeFalse();
            _settings.SelectToCopyEnabled.Should().BeFalse();
            _settings.TrackUsageHistory.Should().BeTrue();
            _settings.FollowSystemTheme.Should().BeTrue();
            _settings.Theme.Should().Be("System");
        }
        
        [Fact]
        public void Load_WhenFileExists_ShouldLoadSettings()
        {
            // Arrange
            var testData = new
            {
                providers = new[]
                {
                    new
                    {
                        id = Guid.NewGuid(),
                        name = "Test Provider",
                        apiUrl = "https://api.test.com/v1",
                        models = new[] { "model1", "model2" },
                        apiKey = "test-key",
                        isDefault = true
                    }
                },
                startWithWindows = true,
                selectToCopyEnabled = true,
                trackUsageHistory = false,
                followSystemTheme = false,
                theme = "Dark",
                windowLeft = 100.0,
                windowTop = 200.0
            };
            
            var json = JsonSerializer.Serialize(testData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            File.WriteAllText(_testSettingsPath, json);
            
            // Act
            _settings.Load();
            
            // Assert
            _settings.Providers.Should().HaveCount(1);
            _settings.Providers[0].Name.Should().Be("Test Provider");
            _settings.Providers[0].ApiUrl.Should().Be("https://api.test.com/v1");
            _settings.Providers[0].Models.Should().HaveCount(2);
            _settings.Providers[0].ApiKey.Should().Be("test-key");
            _settings.Providers[0].IsDefault.Should().BeTrue();
            _settings.StartWithWindows.Should().BeTrue();
            _settings.SelectToCopyEnabled.Should().BeTrue();
            _settings.TrackUsageHistory.Should().BeFalse();
            _settings.FollowSystemTheme.Should().BeFalse();
            _settings.Theme.Should().Be("Dark");
            _settings.WindowLeft.Should().Be(100.0);
            _settings.WindowTop.Should().Be(200.0);
        }
        
        [Fact]
        public void Save_ShouldCreateFile()
        {
            // Arrange
            _settings.Load();
            
            // Act
            _settings.Save();
            
            // Wait for async save
            System.Threading.Thread.Sleep(100);
            
            // Assert
            File.Exists(_testSettingsPath).Should().BeTrue();
            
            var json = File.ReadAllText(_testSettingsPath);
            json.Should().Contain("OpenAI");
        }
        
        [Fact]
        public void AddProvider_ShouldAddToList()
        {
            // Arrange
            _settings.Load();
            var provider = new Provider
            {
                Name = "New Provider",
                ApiUrl = "https://api.new.com/v1",
                Models = new List<string> { "new-model" }
            };
            
            // Act
            _settings.AddProvider(provider);
            
            // Assert
            _settings.Providers.Should().HaveCount(2);
            _settings.Providers.Should().Contain(p => p.Name == "New Provider");
        }
        
        [Fact]
        public void DeleteProvider_ShouldRemoveFromList()
        {
            // Arrange
            _settings.Load();
            var providerId = _settings.Providers[0].Id;
            
            // Act
            _settings.DeleteProvider(providerId);
            
            // Assert
            _settings.Providers.Should().BeEmpty();
        }
        
        [Fact]
        public void SetDefaultProvider_ShouldUpdateDefault()
        {
            // Arrange
            _settings.Load();
            var provider1 = new Provider { Name = "Provider 1", IsDefault = true };
            var provider2 = new Provider { Name = "Provider 2" };
            _settings.AddProvider(provider1);
            _settings.AddProvider(provider2);
            
            // Act
            _settings.SetDefaultProvider(provider2.Id);
            
            // Assert
            _settings.DefaultProvider.Should().Be(provider2);
            provider1.IsDefault.Should().BeFalse();
            provider2.IsDefault.Should().BeTrue();
        }
        
        [Fact]
        public void ResolveModel_WithMention_ShouldReturnMatchingModel()
        {
            // Arrange
            _settings.Load();
            var provider = new Provider
            {
                Name = "Test",
                Models = new List<string> { "model1", "model2" }
            };
            _settings.AddProvider(provider);
            
            // Act
            var result = _settings.ResolveModel("model2");
            
            // Assert
            result.Should().NotBeNull();
            result!.Provider.Should().Be(provider);
            result.ModelId.Should().Be("model2");
        }
        
        [Fact]
        public void ResolveModel_WithProviderName_ShouldReturnFirstModel()
        {
            // Arrange
            _settings.Load();
            var provider = new Provider
            {
                Name = "Test",
                Models = new List<string> { "model1", "model2" }
            };
            _settings.AddProvider(provider);
            
            // Act
            var result = _settings.ResolveModel("Test");
            
            // Assert
            result.Should().NotBeNull();
            result!.Provider.Should().Be(provider);
            result.ModelId.Should().Be("model1");
        }
        
        [Fact]
        public void ResolveModel_WithoutMention_ShouldReturnDefault()
        {
            // Arrange
            _settings.Load();
            
            // Act
            var result = _settings.ResolveModel(null);
            
            // Assert
            result.Should().NotBeNull();
            result!.Provider.Should().Be(_settings.DefaultProvider);
        }
        
        public void Dispose()
        {
            if (File.Exists(_testSettingsPath))
            {
                File.Delete(_testSettingsPath);
            }
        }
    }
}