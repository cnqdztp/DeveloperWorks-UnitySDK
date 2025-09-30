using System.Collections.Generic;

namespace Developerworks_SDK.Public
{
    public class DW_AIResult<T> { public bool Success { get; } public T Response { get; } public string ErrorMessage { get; } public DW_AIResult(T data) { Success = true; Response = data; } public DW_AIResult(string errorMessage) { Success = false; Response = default; ErrorMessage = errorMessage; } }
    
    public class DW_ChatMessage { public string Role; public string Content; }

    public abstract class DW_ChatConfigBase { public List<DW_ChatMessage> Messages { get; set; } = new List<DW_ChatMessage>(); public float Temperature { get; set; } = 0.7f; protected DW_ChatConfigBase(List<DW_ChatMessage> messages) { Messages = messages; } protected DW_ChatConfigBase(string userMessage) { Messages.Add(new DW_ChatMessage { Role = "user", Content = userMessage }); } }
    public class DW_ChatConfig : DW_ChatConfigBase { public DW_ChatConfig(string userMessage) : base(userMessage) { } public DW_ChatConfig(List<DW_ChatMessage> messages) : base(messages) { } }
    public class DW_ChatStreamConfig : DW_ChatConfigBase { public DW_ChatStreamConfig(string userMessage) : base(userMessage) { } public DW_ChatStreamConfig(List<DW_ChatMessage> messages) : base(messages) { } }

    
}