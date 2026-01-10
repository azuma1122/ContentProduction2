using Game.GameSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game
{
    public class BGMManager : MonoBehaviour
    {
        private enum BGM
        {
            TITLE,
            SELECT_STAGE,
            STAGE,
            CLEAR,
            ALL_CLEAR,
            CREDITS,

            MAX_BGM,
        }

        private const string BGM_PREF_KEY = "BGM_VOLUME";
        private const string SE_PREF_KEY = "SE_VOLUME";

        private AudioSource _audioSource;

        public AudioSource GetAudioSource() { return _audioSource; }

        [SerializeField] private AudioClip[] _audioClips = new AudioClip[(int)BGM.MAX_BGM];

        private InputHandler _inputHandler;

        private static readonly Dictionary<int, BGM> _sceneBGM = new Dictionary<int, BGM>
        {
            {(int)GameConstants.Scene.Title, BGM.TITLE },
            {(int)GameConstants.Scene.StageSelect, BGM.SELECT_STAGE },
            {(int)GameConstants.Scene.Stage1, BGM.STAGE },
            {(int)GameConstants.Scene.Stage2, BGM.STAGE },
            {(int)GameConstants.Scene.Stage3, BGM.STAGE },
            {(int)GameConstants.Scene.Clear, BGM.CLEAR },
            {(int)GameConstants.Scene.Options, BGM.ALL_CLEAR },
            {(int)GameConstants.Scene.Credits, BGM.CREDITS },
        };

        public static BGMManager instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;

                DontDestroyOnLoad(gameObject);

                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                    _audioSource = gameObject.AddComponent<AudioSource>();

                Debug.LogWarning(_audioSource.ToString());

                _inputHandler = InputHandler.Instance;

                return;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                Debug.LogWarning("ƒVƒ“ƒOƒ‹ƒgƒ“íœ");
                return;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayBGM();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            PlayBGM();
        }

        private void PlayBGM()
        {
            Scene scene = SceneManager.GetActiveScene();

            _audioSource.clip = SetBGM(scene.buildIndex);

            if (_audioSource.clip != null)
            {
                _audioSource.Play();
            }
            else
            {
                DebugManager.LogMessage($"{scene.name}‚ÌBGM‚ªÄ¶‚Å‚«‚Ü‚¹‚ñ‚Å‚µ‚½", DebugManager.MessageType.Error);
            }
        }

        private AudioClip SetBGM(int scene_index)
        {
            if (_sceneBGM.TryGetValue(scene_index, out BGM set_clip))
            {
                if (set_clip >= BGM.MAX_BGM) return null;

                return _audioClips[(int)set_clip];
            }

            return null;
        }

        private const float MIN_VOLUME = 0.0f;
        private const float MAX_VOLUME = 1.0f;
        private const float VARIABLE_VOLUME = 0.1f;

        public float VolumeChange(float volume)
        {
            if (volume != _audioSource.volume)
            {
                _audioSource.volume = volume;
                PlayerPrefs.SetFloat(BGM_PREF_KEY, volume);
                PlayerPrefs.Save();
            }
            else if (_inputHandler.IsActionPressing(InputConstants.Action.MENU_LEFT_SELECT) &&
                _audioSource.volume != MIN_VOLUME)
            {
                _audioSource.volume -= VARIABLE_VOLUME;
            }
            else if (_inputHandler.IsActionPressing(InputConstants.Action.MENU_RIGHT_SELECT) &&
                _audioSource.volume != MAX_VOLUME)
            {
                _audioSource.volume += VARIABLE_VOLUME;
            }

            return _audioSource.volume;
        }

        public void ApplyVolume(float volume)
        {
            _audioSource.volume = Mathf.Clamp01(volume);
        }

        public void LoadVolumeSettings()
        {
            if (PlayerPrefs.HasKey(BGM_PREF_KEY))
            {
                float bgmVolume = PlayerPrefs.GetFloat(BGM_PREF_KEY);
                ApplyVolume(bgmVolume);
            }

           
        }
    }
}
