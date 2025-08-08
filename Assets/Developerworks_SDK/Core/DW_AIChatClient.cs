using System;
using Cysharp.Threading.Tasks;

namespace Developerworks_SDK
{
    public class DW_AIChatClient
    {
        private readonly string _model;
        private readonly Services.ChatService _chatService;

        internal DW_AIChatClient(string model, Services.ChatService chatService)
        {
            _model = model;
            _chatService = chatService;
        }

        public async UniTask<Public.AIResult<string>> TextGenerationAsync(Public.ChatConfig config)
        {
            return await _chatService.RequestAsync(_model, config);
        }

        public async UniTask TextChatStreamAsync(Public.ChatStreamConfig config, Action<string> onNewChunk, Action<string> onConcluded)
        {
            await _chatService.RequestStreamAsync(_model, config, onNewChunk, onConcluded);
        }
    }
}