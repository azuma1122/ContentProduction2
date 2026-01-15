using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class SoundUIManager : MonoBehaviour
    {
        private enum VolumeUI
        {
            MIN,  // 0: 消音
            SOFT, // 1: 小音
            LOUD, // 2: 中音
            MAX,  // 3: 最大
            MAX_SIZE,
        }

        [Header("UI References")]
        [SerializeField] private Image _bgmVolume;
        [SerializeField] private Image _seVoluem;
        [SerializeField] private Sprite[] _volumeUI = new Sprite[(int)VolumeUI.MAX_SIZE];
        [SerializeField] private Slider BGMSlider;
        [SerializeField] private Slider SESlider;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _bgmAudioSource;
        [SerializeField] private AudioSource _seAudioSource;

        [Header("Threshold Settings")]
        [SerializeField, Range(0f, 1f)] private float _minThreshold = 0.05f; // これ以下なら消音画像
        [SerializeField, Range(0f, 1f)] private float _midThreshold = 0.5f;  // これ以下ならSOFT画像
        [SerializeField, Range(0f, 1f)] private float _highThreshold = 0.9f; // これ以上ならMAX画像

        private void Start()
        {
            // --- BGM設定 ---
            if (BGMManager.instance != null)
            {
                if (_bgmAudioSource == null) _bgmAudioSource = BGMManager.instance.GetAudioSource();
                BGMManager.instance.LoadVolumeSettings();

                if (BGMSlider != null)
                {
                    BGMSlider.value = _bgmAudioSource.volume;
                    BGMSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
                    // 初期表示の更新
                    UpdateBGMVisual(BGMSlider.value);
                }
            }

            // --- SE設定 ---
            if (_seAudioSource == null)
            {
                var seObj = GameObject.Find(GameConstants.Object.SE_MANAGER);
                if (seObj != null) _seAudioSource = seObj.GetComponent<AudioSource>();
            }

            if (SESlider != null && _seAudioSource != null)
            {
                SESlider.value = _seAudioSource.volume;
                SESlider.onValueChanged.AddListener(OnSEVolumeChanged);
                // 初期表示の更新
                UpdateSEVisual(SESlider.value);
            }
        }

        private void OnBGMVolumeChanged(float value)
        {
            if (BGMManager.instance != null) BGMManager.instance.VolumeChange(value);
            UpdateBGMVisual(value);
        }

        private void OnSEVolumeChanged(float value)
        {
            if (SEManager.instance != null) SEManager.instance.VolumeChange(value);
            UpdateSEVisual(value);
        }

        private void UpdateBGMVisual(float volume)
        {
            if (_bgmVolume != null) _bgmVolume.sprite = _volumeUI[(int)CheckCurrentVolume(volume)];
        }

        private void UpdateSEVisual(float volume)
        {
            if (_seVoluem != null) _seVoluem.sprite = _volumeUI[(int)CheckCurrentVolume(volume)];
        }

        private VolumeUI CheckCurrentVolume(float volume)
        {
            // 1. まず「ほぼ0」かどうか
            if (volume <= _minThreshold) return VolumeUI.MIN;

            // 2. 次に「最大に近い」かどうか
            if (volume >= _highThreshold) return VolumeUI.MAX;

            // 3. 中間判定
            if (volume <= _midThreshold) return VolumeUI.SOFT;

            // 4. 消去法で LOUD
            return VolumeUI.LOUD;
        }
    }
}