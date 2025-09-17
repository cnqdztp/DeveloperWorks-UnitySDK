using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Developerworks_SDK.Example
{
    public class Demo_MenuUI : MonoBehaviour
    {
        public static Demo_MenuUI instance;
        [SerializeField] private GameObject tab, frontpage;
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ShowMenuScene()
        {
            SceneManager.LoadScene("0-Menu");
            frontpage.SetActive(true);
            tab.SetActive(false);
        }
        
        public void ShowChatScene()
        {
            SceneManager.LoadScene("1-Chat");
            frontpage.SetActive(false);
            tab.SetActive(true);
        }

        public void ShowImageScene()
        {
            SceneManager.LoadScene("2-Image");
            frontpage.SetActive(false);
            tab.SetActive(true);
        }

        public void ShowStructuredScene()
        {
            SceneManager.LoadScene("3-Structured");
            frontpage.SetActive(false);
            tab.SetActive(true);
        }
    }
}