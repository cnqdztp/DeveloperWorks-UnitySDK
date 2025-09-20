using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Developerworks_SDK.Example
{
    public class Demo_ImageSceneManager : MonoBehaviour
    {
        async void Start()
        {
            /* 初始化 Developerworks SDK。
             * 这是使用SDK任何功能之前都必须调用的第一步，这会开始读取本地的玩家信息，如果未登录则自动打开登录窗口。
             * 如果传入您的开发者密钥（Developer Key），则会跳过任何鉴权。
             * Initialize Developerworks SDK.
             * This must be called before everything, and it will start to read local player information
             * and if there is not, it will automatically start up the login modal.
             * If you pass in your developer key, the sdk skips player validation.
             */
            var result = await DW_SDK.InitializeAsync();

            if (!result)
            {
                Debug.LogError(
                    "initialization failed, you should place a sdk object first, then fill in your gameId in the sdk object. 初始化失败，你需要放置一个sdk prefab，然后将你的游戏Id填写到sdk里");
                return;
            }

        }
        
        [SerializeField] private InputField userInputField;
        [SerializeField] private Image _image;
        [SerializeField] private Button sendBtn;

        private void Awake()
        {
            sendBtn.onClick.AddListener(()=>OnButtonClicked());
        }

        private async UniTaskVoid OnButtonClicked()
        {
            sendBtn.interactable = false;
            var imageGen = DW_SDK.Factory.CreateImageClient("flux-1-schnell");
            try
            {
                var genResult = await imageGen.GenerateImageAsync(userInputField.text,"1024x1024");
                _image.sprite =  genResult.ToSprite();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                // throw;
            }
           
            sendBtn.interactable = true;

        }
    }
    
}