//using Cysharp.Threading.Tasks;
//using Saro.UI;
//using UnityEngine;

//namespace Saro.XAsset.Tests
//{
//    /// <summary>
//    /// 直接加载网络资源
//    /// </summary>
//    public class TestWebAsset : MonoBehaviour
//    {
//        private async void Start()
//        {
//            var texthandle = Main.Resolve<XAssetManager>().LoadAsset("http://localhost:8080/test_asset.json", typeof(TextAsset));
//            await texthandle;
//            texthandle.DecreaseRefCount();

//            var test_assest = texthandle.Text;
//            UIManager.Current.AddToast(test_assest);

//            var handle = Main.Resolve<XAssetManager>().LoadAsset("http://localhost:8080/bgm_t02_swap_t.wav", typeof(AudioClip));
//            await handle;
//            handle.DecreaseRefCount();

//            var audioClip = handle.GetAsset<AudioClip>();
//            var audioSource = new GameObject("audio").AddComponent<AudioSource>();
//            audioSource.clip = audioClip;
//            audioSource.volume = 0.1f;
//            audioSource.Play();
//            audioSource.gameObject.AddComponent<AudioListener>();
//        }
//    }
//}
