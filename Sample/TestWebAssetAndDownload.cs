//using System;
//using System.Collections.Generic;
//using System.Text;
//using Cysharp.Threading.Tasks;
//using Saro.UI;
//using UnityEngine;

//namespace Saro.XAsset.Tests
//{
//    /// <summary>
//    /// 直接下载网络资源，并加载
//    /// </summary>
//    public class TestWebAssetAndDownload : MonoBehaviour
//    {
//        private async void Start()
//        {
//            var bytes = await Main.Resolve<XAssetManager>().LoadExtraAssetAsync("Extra/language_zh.txt");

//            if (bytes != null)
//            {
//                UIManager.Current.AddToast(Encoding.UTF8.GetString(bytes));
//            }
//            else
//            {
//                Debug.LogError("null");
//            }
//        }
//    }
//}
