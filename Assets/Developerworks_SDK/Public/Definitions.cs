using System.Collections.Generic;

namespace Developerworks_SDK.Public
{
    public class AIResult<T> { public bool Success { get; } public T Response { get; } public string ErrorMessage { get; } public AIResult(T data) { Success = true; Response = data; } public AIResult(string errorMessage) { Success = false; Response = default; ErrorMessage = errorMessage; } }
    
    public class ChatMessage { public string Role; public string Content; }

    public abstract class ChatConfigBase { public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>(); public float Temperature { get; set; } = 0.7f; protected ChatConfigBase(List<ChatMessage> messages) { Messages = messages; } protected ChatConfigBase(string userMessage) { Messages.Add(new ChatMessage { Role = "user", Content = userMessage }); } }
    public class ChatConfig : ChatConfigBase { public ChatConfig(string userMessage) : base(userMessage) { } public ChatConfig(List<ChatMessage> messages) : base(messages) { } }
    public class ChatStreamConfig : ChatConfigBase { public ChatStreamConfig(string userMessage) : base(userMessage) { } public ChatStreamConfig(List<ChatMessage> messages) : base(messages) { } }

    
}