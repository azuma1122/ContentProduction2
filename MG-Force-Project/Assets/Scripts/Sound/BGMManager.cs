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

        // 音源を流すAudioSouurce
        private AudioSource _audioSource;

        // 流す音源
        [SerializeField] private AudioClip[] _audioClips = new AudioClip[(int)BGM.MAX_BGM];

        private InputHandler _inputHandler;

        // 対応するBGMマッピング用の辞書
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

        #region -------- シングルトンの設定 --------

        private static BGMManager instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;

                // 破棄されないようにする
                DontDestroyOnLoad(gameObject);

                _audioSource = GetComponent<AudioSource>();

                _inputHandler = InputHandler.Instance;

                return;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region -------- イベント処理 --------

        /// <summary>
        /// シーンを読み込んだ時のイベント追加
        /// </summary>
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// シーンを読み込んだ時のイベント
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayBGM();  // 再生
        }

        /// <summary>
        /// シーンを読み込んだ時のイベントの削除
        /// </summary>
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        #endregion

        private void Start()
        {
            PlayBGM();
        }

        /// <summary>
        /// BGMの再生処理
        /// </summary>
        private void PlayBGM()
        {
            // 現在のシーン取得
            Scene scene = SceneManager.GetActiveScene();

            // BGMのセット
            _audioSource.clip = SetBGM(scene.buildIndex);

            // nullチェック
            if (_audioSource.clip != null)
            {
                _audioSource.Play();  // 再生
            }
            else
            {
                // エラー表示(Debug用)
                DebugManager.LogMessage($"{scene.name}のBGMが再生できませんでした", DebugManager.MessageType.Error);
            }
        }

        /// <summary>
        /// BGMのセット
        /// </summary>
        /// <param name="scene_index"></param>
        /// <returns></returns>
        private AudioClip SetBGM(int scene_index)
        {
            if (_sceneBGM.TryGetValue(scene_index, out BGM set_clip))
            {
                // 範囲外チェック
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
    }
}