# AI Chat with Structured Output

这是一个集成的AI聊天系统，**ChatClient同时支持普通文本和结构化输出**，使用统一的SchemaLibrary来管理所有schema。

## 🚀 核心特点

- **统一客户端**: 一个ChatClient同时支持文本聊天和结构化输出
- **概念清晰**: 结构化输出是Chat的一种能力，不是独立功能
- **统一管理**: 所有schema都在一个SchemaLibrary ScriptableObject中管理
- **动态处理**: 使用JObject，无需预定义C#类
- **灵活选择**: 需要时可以反序列化为强类型
- **简单易用**: 通过schema名称直接调用
- **NPC集成**: NPCClient支持结构化对话

## 📋 快速开始

### 1. 创建Schema Library

1. 在Project窗口右键 → Create → Developerworks SDK → Schema Library
2. 命名为 `SchemaLibrary` 并保存到 `Resources/` 文件夹
3. 在Inspector中添加schema条目

### 2. 添加Schema示例

在SchemaLibrary中添加以下schema：

**NPCResponse Schema（推荐 - 智能对话历史管理）:**
```json
{
  "type": "object",
  "properties": {
    "talk": {
      "type": "string",
      "description": "NPC实际说的话（会自动添加到对话历史）"
    },
    "emotion": {
      "type": "string",
      "enum": ["happy", "sad", "angry", "surprised", "confused", "excited", "calm"],
      "description": "NPC的情感状态"
    },
    "actions": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "description": "NPC可能执行的动作",
      "maxItems": 3
    },
    "confidence": {
      "type": "integer",
      "minimum": 0,
      "maximum": 100,
      "description": "置信度 (0-100)"
    }
  },
  "required": ["talk", "emotion", "actions", "confidence"]
}
```

**传统Schema（兼容性）:**
```json
{
  "type": "object",
  "properties": {
    "dialogue": {
      "type": "string",
      "description": "NPC说的话"
    },
    "emotion": {
      "type": "string",
      "enum": ["happy", "sad", "angry", "surprised", "confused", "excited", "calm"],
      "description": "NPC的情感状态"
    },
    "actions": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "description": "NPC可能执行的动作",
      "maxItems": 3
    },
    "confidence": {
      "type": "integer",
      "minimum": 0,
      "maximum": 100,
      "description": "置信度 (0-100)"
    }
  },
  "required": ["dialogue", "emotion", "actions", "confidence"]
}
```

**QuestInfo Schema:**
```json
{
  "type": "object",
  "properties": {
    "title": {
      "type": "string",
      "description": "任务标题"
    },
    "description": {
      "type": "string",
      "description": "任务详细描述"
    },
    "objective": {
      "type": "string",
      "description": "主要目标"
    },
    "difficulty": {
      "type": "integer",
      "minimum": 1,
      "maximum": 10,
      "description": "难度等级 1-10"
    },
    "rewards": {
      "type": "array",
      "items": {
        "type": "string"
      },
      "description": "奖励列表"
    },
    "isMainQuest": {
      "type": "boolean",
      "description": "是否为主线任务"
    }
  },
  "required": ["title", "description", "objective", "difficulty", "rewards", "isMainQuest"]
}
```

## 🎯 使用方法

### 方法1: 使用ChatClient的结构化输出（推荐）

```csharp
// 创建聊天客户端（同时支持文本和结构化输出）
var chatClient = DW_SDK.Factory.CreateChatClient("gpt-4o-mini");

// 普通文本聊天
var textResult = await chatClient.TextGenerationAsync(config);
Debug.Log($"Text: {textResult.Response}");

// 结构化输出 - 返回JObject
var response = await chatClient.GenerateStructuredAsync(
    "NPCResponse", // schema名称
    "Hello, shopkeeper!",
    "You are a friendly merchant."
);

// 动态访问字段
if (response != null)
{
    string dialogue = response["dialogue"]?.ToString();
    string emotion = response["emotion"]?.ToString();
    
    // 处理数组
    var actions = response["actions"] as JArray;
    string[] actionList = actions?.ToObject<string[]>();
    
    Debug.Log($"NPC说: {dialogue} (情感: {emotion})");
}
```

### 方法2: 强类型反序列化（可选）

```csharp
// 定义数据类（可选）
[System.Serializable]
public class NPCResponse
{
    public string dialogue;
    public string emotion;
    public string[] actions;
    public int confidence;
}

// 生成并反序列化为强类型
var npcResponse = await chatClient.GenerateStructuredAsync<NPCResponse>(
    "NPCResponse",
    "Hello there!",
    "You are a friendly NPC."
);

if (npcResponse != null)
{
    Debug.Log($"NPC: {npcResponse.dialogue}");
    Debug.Log($"Actions: {string.Join(", ", npcResponse.actions)}");
}
```

### 方法3: NPC客户端集成

#### 3.1 传统方法（转换为prompt）

```csharp
// NPC结构化对话 - 返回JObject
var response = await npcClient.TalkStructured("Give me a quest!", "QuestInfo");

if (response != null)
{
    Debug.Log($"Quest Title: {response["title"]}");
    Debug.Log($"Difficulty: {response["difficulty"]}");
}

// NPC结构化对话 - 强类型
var quest = await npcClient.TalkStructured<QuestInfo>("Give me a quest!", "QuestInfo");

if (quest != null)
{
    Debug.Log($"Quest: {quest.title}");
    Debug.Log($"Rewards: {string.Join(", ", quest.rewards)}");
}
```

#### 3.2 新方法（使用Messages - 自动包含对话历史）

```csharp
// 先建立对话历史
await npcClient.Talk("Hello! I'm new to this town.");
await npcClient.Talk("What shops are available here?");

// 现在使用新方法，自动将完整对话历史作为messages传递
var response = await npcClient.TalkStructuredWithHistory(
    "Based on our conversation, give me a structured summary", 
    "NPCResponse"
);

if (response != null)
{
    Debug.Log($"Dialogue: {response["dialogue"]}"); 
    Debug.Log($"Context-aware response based on full conversation history!");
    Debug.Log($"History length: {npcClient.GetHistoryLength()} messages");
}

// 或者使用强类型版本
var structuredQuest = await npcClient.TalkStructuredWithHistory<QuestInfo>(
    "Give me a quest based on what we've discussed", 
    "QuestInfo"
);
```

**优势**：
- ✅ **自动包含完整对话历史** - AI能够基于整个对话上下文生成结构化回复
- ✅ **无需手动管理消息** - NPCClient自动处理messages格式  
- ✅ **更准确的上下文感知** - 比单一prompt包含更丰富的对话信息
- ✅ **智能对话历史管理** - 自动提取结构化回复中的对话内容

#### 3.3 智能对话历史管理

NPCClient现在支持智能处理结构化回复：

```csharp
// NPCClient会自动寻找结构化回复中的"对话"字段
// 支持的字段名称：talk, dialogue, response, message, content, text, speech, say
// 如果找到，将使用该字段内容作为对话历史；否则使用原始JSON

// 示例Schema包含talk字段：
// {
//   "talk": "Hello adventurer! The forest is dangerous at night.",
//   "emotion": "concerned", 
//   "actions": ["point_north", "give_warning"]
// }
// 
// 历史记录中将保存："Hello adventurer! The forest is dangerous at night."
// 而不是完整的JSON结构

var response = await npcClient.TalkStructuredWithHistory("Tell me about the forest", "NPCAdvice");
// NPCClient自动提取response.talk字段并添加到对话历史

// 手动管理历史记录
npcClient.AppendChatMessage("user", "I found a mysterious artifact");
npcClient.AppendChatMessage("assistant", "That artifact is very powerful!");

// 撤销最近的N条消息
int reverted = npcClient.RevertChatMessages(2); // 撤销最近2条消息
Debug.Log($"撤销了 {reverted} 条消息");
```

### 方法4: 使用Messages进行结构化输出（对话上下文）

```csharp
// 创建对话消息列表，使用统一的ChatMessage类型
var messages = new List<ChatMessage>
{
    new ChatMessage
    {
        Role = "system",
        Content = "You are a helpful shop assistant in a fantasy RPG."
    },
    new ChatMessage
    {
        Role = "user", 
        Content = "What potions do you have?"
    },
    new ChatMessage
    {
        Role = "assistant",
        Content = "We have healing potions, mana potions, and strength potions."
    },
    new ChatMessage
    {
        Role = "user",
        Content = "Give me details about healing potions in structured format."
    }
};

// 使用messages进行结构化生成
var response = await chatClient.GenerateStructuredAsync("NPCResponse", messages);

if (response != null)
{
    Debug.Log($"Dialogue: {response["dialogue"]}");
    Debug.Log($"Emotion: {response["emotion"]}");
}
```

### 方法5: 直接使用Schema JSON

```csharp
string choiceSchema = @"{
    ""type"": ""object"",
    ""properties"": {
        ""choice"": {
            ""type"": ""string"",
            ""enum"": [""left"", ""right"", ""forward""],
            ""description"": ""选择的方向""
        },
        ""reasoning"": {
            ""type"": ""string"",
            ""description"": ""选择原因""
        }
    },
    ""required"": [""choice"", ""reasoning""]
}";

var result = await chatClient.GenerateStructuredWithSchemaAsync(
    choiceSchema,
    "Which way should I go?",
    "DirectionChoice"
);

Debug.Log($"Choice: {result["choice"]}, Reason: {result["reasoning"]}");
```

## 🔧 高级功能

### 自定义Schema Library

```csharp
// 设置自定义schema库
DW_SchemaLibrary customLibrary = // 从其他地方加载
chatClient.SetSchemaLibrary(customLibrary);

// 查看可用schema
string[] schemas = chatClient.GetAvailableSchemas();
Debug.Log($"Available schemas: {string.Join(", ", schemas)}");

// 检查schema是否存在
bool hasSchema = chatClient.HasSchema("NPCResponse");
```

### 管理Schema Library

```csharp
// 运行时添加schema (Editor only)
#if UNITY_EDITOR
schemaLibrary.AddSchema("NewSchema", "Description", jsonSchemaString);
#endif

// 查找schema
var schemaEntry = schemaLibrary.FindSchema("NPCResponse");
if (schemaEntry != null && schemaEntry.IsValid())
{
    Debug.Log($"Schema found: {schemaEntry.description}");
}
```

## ⚡ 性能提示

1. **重用客户端**: 创建一次，多次使用
2. **缓存Schema Library**: 避免重复加载
3. **按需强类型**: 只在需要时反序列化为强类型
4. **动态访问**: 对于简单字段访问，直接使用JObject更高效

## 🎨 最佳实践

1. **Schema设计**: 保持schema简单明确，避免过度嵌套
2. **命名规范**: 使用描述性的schema名称
3. **错误处理**: 始终检查返回值是否为null
4. **类型安全**: 在访问JObject字段时使用安全导航
5. **文档化**: 为每个schema添加清晰的描述

## 🔍 调试技巧

```csharp
// 打印完整的JObject结构
Debug.Log($"Full response: {response?.ToString(Formatting.Indented)}");

// 检查字段是否存在
if (response.ContainsKey("dialogue"))
{
    Debug.Log($"Dialogue found: {response["dialogue"]}");
}

// 安全访问嵌套字段
string nested = response["nested"]?["field"]?.ToString();
```

这个新设计更加简洁和实用，符合你的需求：统一管理、动态处理、灵活使用！🎉