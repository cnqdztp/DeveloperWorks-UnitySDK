using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Developerworks_SDK;
using Developerworks_SDK.Auth;
using Developerworks_SDK.Public;
using UnityEngine;
using UnityEngine.UI;

public class ExampleGameManager : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Text _text;
    [SerializeField] private Image _image;
    async void Start()
    {
        // 初始化 Developerworks SDK。
        // 这是使用SDK任何功能之前都必须调用的第一步，这会开始读取本地的玩家信息，如果玩家信息不存在或者国旗，则打开登录窗口。
        // 如果传入您的开发者密钥（Developer Key），则会跳过任何鉴权。
        // await DW_SDK.InitializeAsync("dev-f66ce34d-e747-4292-97f5-079f82f10de4");
        var result = await DW_SDK.InitializeAsync();

        if(!result)
        {
            Debug.LogError("initialization failed");
            return;
        }

        StandardImageGen();

    }
    
    List<ChatMessage> _selfManagedHistory = new List<ChatMessage>();


    async UniTask StandardImageGen()
    {
        var imageGen = DW_SDK.Factory.CreateImageClient();
        var genResult = await imageGen.GenerateImageAsync("a futuristic city");
        _image.sprite = DW_AIImageClient.Texture2DToSprite( DW_AIImageClient.Base64ToTexture2D(genResult));
    }
    async UniTask StandardChat()
    {
        //你需要自行管理AI的历史信息，自行创建一个历史记录，自行操作其中的内容
        //是否支持设置多个system信息，不同的模型行为各不相同，但TextGeneration提供较高的自由度，所以并不在这里做任何限制
        _selfManagedHistory.Add(new ChatMessage()
        {
            Role = "system",
            Content = "你扮演《底特律变人》的康纳"
        });
        _selfManagedHistory.Add(new ChatMessage()
        {
            Role = "user",
            Content = "你的工作是什么"
        });
        var chat = DW_SDK.Factory.CreateChatClient();//新建一个对话客户端
        var result = await chat.TextGenerationAsync(new ChatConfig(_selfManagedHistory));//对话
        _selfManagedHistory.Add(new ChatMessage()
        {
            Role = "assistant",
            Content = result.Response
        });
        _selfManagedHistory.Add(new ChatMessage()
        {
            Role = "user",
            Content = "你喜欢你的工作吗"
        });
        _selfManagedHistory.Add(new ChatMessage()
        {
            Role = "system",
            Content = "你扮演一个普通人"
        });
        result = await chat.TextGenerationAsync(new ChatConfig(_selfManagedHistory));//对话
        Debug.Log(result.Response);
        
    }
    
    [SerializeField] private DW_NPCClient _npcClient,_npcClient2;
    async UniTask SimpleChat()
    {
        var npc =_npcClient;
        var reply = await npc.Talk("1+1等于几");
        Debug.Log(reply);
        var history = npc.SaveHistory();
        //Npc则会帮助你管理历史记录，设置系统提示词时会
        npc.SetSystemPrompt("扮演一个恨铁不成钢的老师");
        await Task.Delay(5000);
        reply = await npc.Talk("再+2呢？");
        Debug.Log(reply);
        var npc2 = _npcClient2;
        npc2.LoadHistory(history);
        reply = await npc2.Talk("再+2呢？");
        Debug.Log(reply);

    }
    
    async UniTask StandardChatStream()
    {

        var chat = DW_SDK.Factory.CreateChatClient();
        _selfManagedHistory.Add(new ChatMessage()
        {
            Role = "system",
            Content = "一千零一夜的故事是什么？"
        });
        _selfManagedHistory.Add(new ChatMessage()
        {
            Role = "user",
            Content = "你的工作是什么"
        });
        await chat.TextChatStreamAsync(new ChatStreamConfig(_selfManagedHistory), (s) =>
            {
                var original = _text.text;
                _text.text = original + s;
            },
            (s) =>
            {
                _text.text = s;
            });
    }
    
    async void SimpleChatStream()
    {

        var chat = _npcClient;
        await chat.TalkStream("东京怎么玩？",(s)=>_text.text=s,(s)=>{_text.text=s;});

    }

}
