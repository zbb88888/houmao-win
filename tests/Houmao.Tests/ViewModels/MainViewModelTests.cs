using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Houmao.Models;
using Houmao.Services;
using Houmao.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Houmao.Tests.ViewModels
{
    public class MainViewModelTests
    {
        private readonly Mock<ILogger<MainViewModel>> _loggerMock;
        private readonly Mock<IAiClient> _aiClientMock;
        private readonly Mock<IAppSettings> _settingsMock;
        private readonly Mock<IHistoryStore> _historyStoreMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly MainViewModel _viewModel;
        
        public MainViewModelTests()
        {
            _loggerMock = new Mock<ILogger<MainViewModel>>();
            _aiClientMock = new Mock<IAiClient>();
            _settingsMock = new Mock<IAppSettings>();
            _historyStoreMock = new Mock<IHistoryStore>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            
            // 设置默认 Provider
            var provider = new Provider
            {
                Name = "Test",
                ApiUrl = "https://api.test.com/v1",
                ApiKey = "test-key",
                Models = new List<string> { "test-model" }
            };
            
            _settingsMock.Setup(s => s.DefaultProvider).Returns(provider);
            _settingsMock.Setup(s => s.ResolveModel(It.IsAny<string>()))
                .Returns(new ResolvedModel(provider, "test-model"));
            _settingsMock.Setup(s => s.Providers).Returns(new List<Provider> { provider });
            
            // 设置 ServiceProvider 返回面板 ViewModel
            _serviceProviderMock.Setup(sp => sp.GetService(typeof(HistoryPanelViewModel)))
                .Returns(new HistoryPanelViewModel());
            _serviceProviderMock.Setup(sp => sp.GetService(typeof(HelpPanelViewModel)))
                .Returns(new HelpPanelViewModel(_settingsMock.Object));
            
            _viewModel = new MainViewModel(
                _loggerMock.Object,
                _aiClientMock.Object,
                _settingsMock.Object,
                _historyStoreMock.Object,
                _serviceProviderMock.Object);
        }
        
        [Fact]
        public async Task Submit_WithValidInput_ShouldCallAiClient()
        {
            // Arrange
            _viewModel.InputText = "Test question";
            
            var tokens = new List<string> { "Hello", " World" };
            _aiClientMock.Setup(c => c.AskStreamAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ChatMessage>>(),
                    It.IsAny<List<Attachment>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(ToAsyncEnumerable(tokens));
            
            // Act
            await _viewModel.SubmitCommand.ExecuteAsync(null);
            
            // Assert
            _aiClientMock.Verify(c => c.AskStreamAsync(
                "Test question",
                It.IsAny<List<ChatMessage>>(),
                It.IsAny<List<Attachment>>(),
                It.IsAny<CancellationToken>()), Times.Once);
            
            _viewModel.InputText.Should().BeEmpty();
            _viewModel.MessageCount.Should().Be(2); // user + assistant
        }
        
        [Fact]
        public async Task Submit_WithEmptyInput_ShouldNotCallAiClient()
        {
            // Arrange
            _viewModel.InputText = "";
            
            // Act
            await _viewModel.SubmitCommand.ExecuteAsync(null);
            
            // Assert
            _aiClientMock.Verify(c => c.AskStreamAsync(
                It.IsAny<string>(),
                It.IsAny<List<ChatMessage>>(),
                It.IsAny<List<Attachment>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
        
        [Fact]
        public async Task Submit_WithSingleCharB_ShouldToggleHistoryPanel()
        {
            // Arrange
            _viewModel.InputText = "b";
            
            // Act
            await _viewModel.SubmitCommand.ExecuteAsync(null);
            
            // Assert
            _viewModel.CurrentPanel.Should().BeOfType<HistoryPanelViewModel>();
            _viewModel.InputText.Should().BeEmpty();
        }
        
        [Fact]
        public async Task Submit_WithSingleCharH_ShouldToggleHelpPanel()
        {
            // Arrange
            _viewModel.InputText = "h";
            
            // Act
            await _viewModel.SubmitCommand.ExecuteAsync(null);
            
            // Assert
            _viewModel.CurrentPanel.Should().BeOfType<HelpPanelViewModel>();
            _viewModel.InputText.Should().BeEmpty();
        }
        
        [Fact]
        public async Task Submit_WithMention_ShouldUseSpecifiedModel()
        {
            // Arrange
            _viewModel.InputText = "@model1 Test with mention";
            
            var tokens = new List<string> { "Response" };
            _aiClientMock.Setup(c => c.AskStreamAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ChatMessage>>(),
                    It.IsAny<List<Attachment>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(ToAsyncEnumerable(tokens));
            
            // Act
            await _viewModel.SubmitCommand.ExecuteAsync(null);
            
            // Assert
            _settingsMock.Verify(s => s.ResolveModel("model1"), Times.Once);
        }
        
        [Fact]
        public void ClearConversation_ShouldResetHistory()
        {
            // Arrange
            // Simulate some history
            _viewModel.InputText = "test";
            
            // Act
            _viewModel.ClearConversationCommand.Execute(null);
            
            // Assert
            _viewModel.MessageCount.Should().Be(0);
            _viewModel.StatusText.Should().Be("Conversation cleared");
        }
        
        [Fact]
        public void GetPreviousCommand_ShouldReturnFromHistory()
        {
            // Arrange
            _viewModel.InputText = "first";
            _viewModel.SubmitCommand.Execute(null);
            
            _viewModel.InputText = "second";
            _viewModel.SubmitCommand.Execute(null);
            
            // Act
            var result = _viewModel.GetPreviousCommand();
            
            // Assert
            result.Should().Be("second");
        }
        
        [Fact]
        public void GetNextCommand_ShouldReturnNextFromHistory()
        {
            // Arrange
            _viewModel.InputText = "first";
            _viewModel.SubmitCommand.Execute(null);
            
            _viewModel.InputText = "second";
            _viewModel.SubmitCommand.Execute(null);
            
            _viewModel.GetPreviousCommand(); // "second"
            _viewModel.GetPreviousCommand(); // "first"
            
            // Act
            var result = _viewModel.GetNextCommand();
            
            // Assert
            result.Should().Be("second");
        }
        
        [Fact]
        public async Task Submit_WhenAiClientThrows_ShouldSetErrorStatus()
        {
            // Arrange
            _viewModel.InputText = "Test";
            
            _aiClientMock.Setup(c => c.AskStreamAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ChatMessage>>(),
                    It.IsAny<List<Attachment>>(),
                    It.IsAny<CancellationToken>()))
                .Throws(new Exception("Test error"));
            
            // Act
            await _viewModel.SubmitCommand.ExecuteAsync(null);
            
            // Assert
            _viewModel.StatusText.Should().Contain("Error");
            _viewModel.IsLoading.Should().BeFalse();
        }
        
        [Fact]
        public async Task CancelRequest_ShouldCancelActiveRequest()
        {
            // Arrange
            _viewModel.InputText = "Test";
            
            var cts = new CancellationTokenSource();
            _aiClientMock.Setup(c => c.AskStreamAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<ChatMessage>>(),
                    It.IsAny<List<Attachment>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(ToAsyncEnumerable(new List<string> { "token" }, cts.Token));
            
            // Act
            _viewModel.CancelRequestCommand.Execute(null);
            
            // Assert
            _viewModel.IsLoading.Should().BeFalse();
            _viewModel.StatusText.Should().Be("Request cancelled");
        }
        
        private static async IAsyncEnumerable<string> ToAsyncEnumerable(
            IEnumerable<string> items,
            CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;
                
                yield return item;
                await Task.Delay(1, cancellationToken);
            }
        }
    }
}