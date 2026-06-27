using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Houmao.Models;
using Houmao.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Houmao.Tests.Services
{
    public class AiClientTests
    {
        private readonly Mock<ILogger<AiClient>> _loggerMock;
        private readonly Mock<IAppSettings> _settingsMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly AiClient _aiClient;
        
        public AiClientTests()
        {
            _loggerMock = new Mock<ILogger<AiClient>>();
            _settingsMock = new Mock<IAppSettings>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            
            // 设置默认 Provider
            var provider = new Provider
            {
                Name = "Test",
                ApiUrl = "https://api.test.com/v1",
                ApiKey = "test-key",
                Models = new List<string> { "test-model" }
            };
            
            _settingsMock.Setup(s => s.ResolveModel(null))
                .Returns(new ResolvedModel(provider, "test-model"));
            
            _aiClient = new AiClient(_httpClient, _loggerMock.Object, _settingsMock.Object);
        }
        
        [Fact]
        public async Task StreamAsync_ShouldReturnTokens()
        {
            // Arrange
            var responseJson = @"data: {""choices"":[{""delta"":{""content"":""Hello""}}]}

data: {""choices"":[{""delta"":{""content"":"" World""}}]}

data: [DONE]
";
            
            var response = new HttpResponseMessage
            {
                Content = new StringContent(responseJson)
            };
            
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            
            var request = new ChatRequest
            {
                Model = "test-model",
                Messages = new List<ChatMessage>
                {
                    ChatMessage.CreateUserMessage("Test")
                },
                Stream = true
            };
            
            // Act
            var tokens = new List<string>();
            await foreach (var token in _aiClient.StreamAsync(request))
            {
                tokens.Add(token);
            }
            
            // Assert
            tokens.Should().HaveCount(2);
            tokens[0].Should().Be("Hello");
            tokens[1].Should().Be(" World");
        }
        
        [Fact]
        public async Task StreamAsync_WithThinkTags_ShouldFilterThem()
        {
            // Arrange
            var responseJson = @"data: {""choices"":[{""delta"":{""content"":""<think>reasoning</think>""}}]}

data: {""choices"":[{""delta"":{""content"":""actual response""}}]}

data: [DONE]
";
            
            var response = new HttpResponseMessage
            {
                Content = new StringContent(responseJson)
            };
            
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            
            var request = new ChatRequest
            {
                Model = "test-model",
                Messages = new List<ChatMessage>
                {
                    ChatMessage.CreateUserMessage("Test")
                },
                Stream = true
            };
            
            // Act
            var tokens = new List<string>();
            await foreach (var token in _aiClient.StreamAsync(request))
            {
                tokens.Add(token);
            }
            
            // Assert
            tokens.Should().HaveCount(1);
            tokens[0].Should().Be("actual response");
        }
        
        [Fact]
        public async Task StreamAsync_WithReasoningContent_ShouldFallbackWhenContentEmpty()
        {
            // Arrange
            var responseJson = "data: {\"choices\":[{\"delta\":{\"reasoning_content\":\"This is reasoning\"}}]}\n\ndata: {\"choices\":[{\"delta\":{\"content\":\"actual response\"}}]}\n\ndata: [DONE]\n";
            
            var response = new HttpResponseMessage
            {
                Content = new StringContent(responseJson)
            };
            
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            
            var request = new ChatRequest
            {
                Model = "test-model",
                Messages = new List<ChatMessage>
                {
                    ChatMessage.CreateUserMessage("Test")
                },
                Stream = true
            };
            
            // Act
            var tokens = new List<string>();
            await foreach (var token in _aiClient.StreamAsync(request))
            {
                tokens.Add(token);
            }
            
            // Assert
            tokens.Should().HaveCount(2);
            tokens[0].Should().Be("This is reasoning");
            tokens[1].Should().Be("actual response");
        }
        
        [Fact]
        public async Task AskAsync_ShouldReturnCompleteResponse()
        {
            // Arrange
            var responseJson = @"{
                ""choices"": [{
                    ""message"": {
                        ""role"": ""assistant"",
                        ""content"": ""Hello World""
                    }
                }]
            }";
            
            var response = new HttpResponseMessage
            {
                Content = new StringContent(responseJson)
            };
            
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            
            var request = new ChatRequest
            {
                Model = "test-model",
                Messages = new List<ChatMessage>
                {
                    ChatMessage.CreateUserMessage("Test")
                },
                Stream = false
            };
            
            // Act
            var result = await _aiClient.AskAsync(request);
            
            // Assert
            result.Should().NotBeNull();
            result.Content.Should().Be("Hello World");
        }
        
        [Fact]
        public async Task AskAsync_WithThinkTags_ShouldFilterThem()
        {
            // Arrange
            var responseJson = @"{
                ""choices"": [{
                    ""message"": {
                        ""role"": ""assistant"",
                        ""content"": ""<think>reasoning</think>actual response""
                    }
                }]
            }";
            
            var response = new HttpResponseMessage
            {
                Content = new StringContent(responseJson)
            };
            
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            
            var request = new ChatRequest
            {
                Model = "test-model",
                Messages = new List<ChatMessage>
                {
                    ChatMessage.CreateUserMessage("Test")
                },
                Stream = false
            };
            
            // Act
            var result = await _aiClient.AskAsync(request);
            
            // Assert
            result.Should().NotBeNull();
            result.Content.Should().Be("actual response");
        }
        
        [Fact]
        public void CancelCurrentRequest_ShouldCancelActiveRequest()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            
            // Act
            _aiClient.CancelCurrentRequest();
            
            // Assert
            _aiClient.IsRequestActive.Should().BeFalse();
        }
        
        [Fact]
        public async Task AskStreamAsync_ShouldBuildCorrectRequest()
        {
            // Arrange
            HttpRequestMessage? capturedRequest = null;
            
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
                {
                    capturedRequest = req;
                })
                .ReturnsAsync(new HttpResponseMessage
                {
                    Content = new StringContent("data: [DONE]\n")
                });
            
            var history = new List<ChatMessage>
            {
                ChatMessage.CreateUserMessage("Previous question"),
                ChatMessage.CreateAssistantMessage("Previous answer")
            };
            
            var attachments = new List<Attachment>
            {
                new Attachment
                {
                    FileName = "test.jpg",
                    Type = AttachmentType.Image,
                    MimeType = "image/jpeg",
                    Base64Data = "dGVzdA=="
                }
            };
            
            // Act
            var tokens = new List<string>();
            await foreach (var token in _aiClient.AskStreamAsync("New question", history, attachments))
            {
                tokens.Add(token);
            }
            
            // Assert
            capturedRequest.Should().NotBeNull();
            capturedRequest!.RequestUri.Should().Be(new Uri("https://api.test.com/v1/chat/completions"));
            capturedRequest.Headers.Authorization.Should().NotBeNull();
            capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
            capturedRequest.Headers.Authorization.Parameter.Should().Be("test-key");
        }
    }
}