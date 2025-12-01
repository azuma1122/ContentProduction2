using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Game
{
    public class SoundUIManager : MonoBehaviour
    {
        // 整数倍
        private const float INTEGER_TIMES = 4;

        // ボリューム定数
        private enum Volume
        {
            MIN = 0,
            HAFE = 2,
            MAX = 4,
        }

        // ボリュームUI管理用
        private enum VolumeUI
        {
            MIN,
            SOFT,
            LOUD,
            MAX,

            MAX_SIZE,
        }

        // BGMのUI
        [SerializeField] private Image _bgmVolume;
        // SEのUI
        [SerializeField] private Image _seVoluem;
        // BGMのAudioSorce
        [SerializeField] private AudioSource _bgmAudioSource;
        // SEのAudioSource
        [SerializeField] private AudioSource _seAudioSource;
        // UIスプライト
        [SerializeField] private Sprite[] _volumeUI = new Sprite[(int)VolumeUI.MAX_SIZE];

        [SerializeField] private Slider BGMSlider;
        [SerializeField] private Slider SESlider;


        private void Awake()
        {

            if (!BGMManager.instance)
            {
                Debug.LogWarning("BGMManagerのシングルトンなし");
                return;
            }
          
        }
        private void Start()
        {
            if (_bgmAudioSource == null)
            {

                _bgmAudioSource = BGMManager.instance.GetAudioSource();
                Debug.Log(_bgmAudioSource.name);

                BGMManager.instance.LoadVolumeSettings();

            }

            if (_seAudioSource == null)
            {
                _seAudioSource = GameObject.Find(GameConstants.Object.SE_MANAGER).GetComponent<AudioSource>();
            }
            if (BGMSlider == null) return;

            // BGMManagerから現在の音量を取得してスライダーに反映
            BGMSlider.value = BGMManager.instance.VolumeChange(_bgmAudioSource.volume);

            // スライダーの値が変わったらBGMManagerに反映
            BGMSlider.onValueChanged.AddListener((value) =>
            {
                if (BGMManager.instance != null)
                {
                    BGMManager.instance.VolumeChange(value);
                }
            });
        }
        /// <summary>
        /// 更新処理
        /// </summary>
        private void Update()
        {


            BGMUIUpdate();
            SEUIUpdate();
        }

        /// <summary>
        /// BGMUI更新
        /// </summary>
        private void BGMUIUpdate()
        {
            if (!_bgmAudioSource) return;
            if (_bgmAudioSource.mute)
            {
                _bgmVolume.sprite = _volumeUI[(int)VolumeUI.MIN];
                return;
            }
            switch (CheckCurrentVolume(_bgmAudioSource.volume))
            {
                case VolumeUI.MIN:

                    _bgmVolume.sprite = _volumeUI[(int)VolumeUI.MIN];
                    break;

                case VolumeUI.SOFT:
                    _bgmVolume.sprite = _volumeUI[(int)VolumeUI.SOFT];
                    break;

                case VolumeUI.LOUD:
                    _bgmVolume.sprite = _volumeUI[(int)VolumeUI.LOUD];
                    break;

                case VolumeUI.MAX:
                    _bgmVolume.sprite = _volumeUI[(int)VolumeUI.MAX];
                    break;
            }
        }

        /// <summary>
        /// SEUI更新
        /// </summary>
        private void SEUIUpdate()
        {
            if (!_seAudioSource) return;
            if (_seAudioSource.mute)
            {
                _seVoluem.sprite = _volumeUI[(int)VolumeUI.MIN];
                return;
            }

            switch (CheckCurrentVolume(_seAudioSource.volume))
            {
                case VolumeUI.MIN:
                    _seVoluem.sprite = _volumeUI[(int)VolumeUI.MIN];
                    break;

                case VolumeUI.SOFT:
                    _seVoluem.sprite = _volumeUI[(int)VolumeUI.SOFT];
                    break;

                case VolumeUI.LOUD:
                    _seVoluem.sprite = _volumeUI[(int)VolumeUI.LOUD];
                    break;

                case VolumeUI.MAX:
                    _seVoluem.sprite = _volumeUI[(int)VolumeUI.MAX];
                    break;
            }
        }

        /// <summary>
        /// 音量チェック
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        private VolumeUI CheckCurrentVolume(float volume)
        {
            volume *= INTEGER_TIMES;

            if (volume == (int)Volume.MIN) return VolumeUI.MIN;

            else if (volume == (int)Volume.MAX) return VolumeUI.MAX;

            else if (volume <= (int)Volume.HAFE) return VolumeUI.SOFT;

            else return VolumeUI.LOUD;
        }


    }


}