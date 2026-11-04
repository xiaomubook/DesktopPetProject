using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

    public abstract class PetBase: MonoBehaviour
    {
        [SerializeField] protected PetAudioPlayer audioPlayer;
        [SerializeField] protected PetSpeechBubble speechBubble;
        [SerializeField] protected PetSpeechLibrary library; // 可空，空则用单例
        [SerializeField] protected Button clickButton;      // 宠物本体上的按钮，空则自动查找

        public bool IsSpeaking { get; protected set; }

        private int _lastSpokenId = -1;   // 上一次播过的 id，用来避免随机时连续重复
        private IPetAnim _anim;           // 宠物本体的说话动画（UIPulseScale / UISwing）

        protected PetSpeechLibrary Lib
        {
            get
            {
                if (library != null) return library;
                // 回退到全局实例是跨宠物串音的元凶，必须让它显眼
                Debug.LogError($"[{name}] library 未绑定，退回到全局 PetSpeechLibrary.Instance（很可能播成别人的音频）", this);
                return PetSpeechLibrary.Instance;
            }
        }

        protected virtual void Awake()
        {
            ResolveOwnComponents();
            _anim = FindAnim();

            if (clickButton == null) clickButton = FindClickButton();
            if (clickButton == null)
            {
                Debug.LogWarning($"[{name}] 没找到宠物本体的 Button，点击不会说话");
                return;
            }
            clickButton.onClick.AddListener(HandleClicked);
        }

        /// <summary>找自己层级下的说话动画组件（实现 IPetAnim 的那个）</summary>
        private IPetAnim FindAnim()
        {
            foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is IPetAnim anim) return anim;
            }
            return null;
        }

        /// <summary>
        /// 强制使用"自己层级下"的语音库和气泡。
        /// 场景里只要有一处引用拖成了别的宠物的，就会出现"点 A 播 B 的音频、字幕也串台"，
        /// 这里直接纠正并大声报错。
        /// </summary>
        private void ResolveOwnComponents()
        {
            var ownLib = GetComponentInChildren<PetSpeechLibrary>(true);
            if (ownLib != null && library != ownLib)
            {
                if (library != null)
                    Debug.LogError($"[{name}] library 指向了不属于自己的库「{library.name}」，已强制改用自身的「{ownLib.name}」", this);
                library = ownLib;
            }

            var ownBubble = GetComponentInChildren<PetSpeechBubble>(true);
            if (ownBubble != null && speechBubble != ownBubble)
            {
                if (speechBubble != null)
                    Debug.LogError($"[{name}] speechBubble 指向了不属于自己的气泡「{speechBubble.name}」，已强制改用自身的「{ownBubble.name}」", this);
                speechBubble = ownBubble;
            }
        }

        /// <summary>在子物体里找宠物本体的按钮；气泡上的 Button 不算</summary>
        private Button FindClickButton()
        {
            foreach (var b in GetComponentsInChildren<Button>(true))
            {
                if (b.GetComponent<PetSpeechBubble>() != null) continue;
                return b;
            }
            return null;
        }

        public virtual void Speak(int speechId)
        {
            if (IsSpeaking) return;
            if (audioPlayer == null || speechBubble == null)
            {
                Debug.LogWarning($"[{name}] audioPlayer / speechBubble 未绑定");
                return;
            }
            if (Lib == null || !Lib.TryGet(speechId, out var entry, out var clip) || clip == null)
            {
                Debug.LogWarning($"[{name}] 无法播放 speechId={speechId}");
                return;
            }

            IsSpeaking = true;
            _lastSpokenId = speechId;

            var usedLib = Lib;
            string libOwner = usedLib.transform.parent != null ? usedLib.transform.parent.name : usedLib.name;
            Debug.Log($"[{name}] 播放 id={speechId} 字幕=\"{entry.label}\" 音频={clip.name} 来自库={libOwner}", this);

            float duration = audioPlayer.Play(clip);          // 真实时长

            // 动画和音频同长：说话期间一直播，音频结束前收尾
            _anim?.PlayFor(duration);

            float strength = 1.2f;                            // 可后续从 entry 扩展
            float fadeOut  = Mathf.Min(0.2f, duration * 0.25f);// 短音频保护
            speechBubble.Show(entry.label, duration, strength, fadeOut);

            StartCoroutine(EndSpeakingWhenAudioDone());
        }

        private IEnumerator EndSpeakingWhenAudioDone()
        {
            // 比 WaitForSeconds(clip.length) 更稳：音频被打断/暂停都能正确结束
            yield return new WaitUntil(() => !audioPlayer.IsPlaying);
            IsSpeaking = false;
        }

        /// <summary>点击：从当前角色自己的语音库里随机抽一条（不会连着两次播同一条）</summary>
        protected virtual void HandleClicked()
        {
            if (IsSpeaking) return;

            var lib = Lib;
            if (lib == null)
            {
                Debug.LogWarning($"[{name}] 没有可用的语音库", this);
                return;
            }

            int id = lib.GetRandomId(_lastSpokenId);
            if (id < 0)
            {
                Debug.LogWarning($"[{name}] 语音库里没有可播放的音频", this);
                return;
            }

            Speak(id);
        }
        
    }

    /// <summary>宠物本体的说话动画。UIPulseScale / UISwing 都实现它，PetBase 按音频时长统一驱动。</summary>
    public interface IPetAnim
    {
        void PlayFor(float duration);
    }

    public interface IClickable
    {
        void OnPetClicked();
    }

    public interface ISpeakable
    {
        void Speak(int speechId);   // 用 id 引用语音库
        bool IsSpeaking { get; }
    }
    [Serializable]
    public class PetSpeechEntry
    {
        public int id;
        public string label;
        public string file;
        public float duration;
    }