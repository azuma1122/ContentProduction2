using Game.GameSystem;
using UnityEngine;

namespace Game
{
    public class SEManager : MonoBehaviour
    {
        /// <summary>
        /// メニュー画面
        /// </summary>
        public enum Menu
        {
            SELECT,
            DECISION,
            CANCEL,
            MAX_SE,
        }

        /// <summary>
        /// プレイヤーの動作関連のSE
        /// </summary>
        public enum Player
        {

            PLAYER_MOVE,   // プレイヤー移動時のSE
            PLAYER_JUMP,   // プレイヤージャンプ時のSE
            PLAYER_LAND, // プレイヤー着地時のSE
            MAX_SE,
        }


        /// <summary>
        /// ステージ関連
        /// </summary>
        public enum Stage
        {
            STAGE_TRANSITION, // ステージ移行時のSE
            STAGE_START, // ステージ開始時のSE
            STAGE_RETRY,  // ステージリトライ時のSE
            STAGE_CLEAR,  // ステージリトライ時のSE
            MAX_SE,

        }

        /// <summary>
        /// 弾発射関連
        /// </summary>
        public enum Bullet
        {
            BULLET_CHARGE,           // 弾をチャージするときのSE
            BULLET_SHOT,             // 弾を撃つ時のSE
            BULLET_MOVE,             // 弾の移動中のSE
            BULLET_HIT_BLOCK,         // 弾がブロックに当たった時のSE
            MAX_SE,

        }

        /// <summary>
        /// 磁力関連
        /// </summary>
        public enum Magnet
        {
            MAGNET_ACTIVATE,// 磁力起動時のSE
            MAGNET_RESET,// 磁力リセット時のSE
            MAX_SE,

        }

        /// <summary>
        /// 障害物関連
        /// </summary>
        public enum Obstacle
        {
            ObstacleCollision,      // 障害物同士がぶつかった時のSE
            ButtonPress,            // 障害物のボタンを押した時のSE
            MAX_SE,
        }


        [NamedSerializeField(
            new string[]
            {
                "選択音",
                "決定音",
                "キャンセル音",

            }
        )]

        [SerializeField]
        private AudioClip[] _menuClips = new AudioClip[(int)Menu.MAX_SE];

        [NamedSerializeField(
            new string[]
            {
                "プレイヤー移動時のSE",
                "プレイヤージャンプ時のSE",
                "プレイヤー着地時のSE",


            }
        )]

        [SerializeField]
        private AudioClip[] _PlayerClips = new AudioClip[(int)Player.MAX_SE];

        [NamedSerializeField(
           new string[]
           {
               "ステージ移行時のSE" ,
               " ステージ開始時のSE ",
               "ステージリトライ時のSE",
               "ステージクリア時のSE"

           }
       )]

        [SerializeField]
        private AudioClip[] _StageClips = new AudioClip[(int)Stage.MAX_SE];

        [NamedSerializeField(
          new string[]
          {
               "弾をチャージするときのSE" ,
               " 弾を撃つ時のSE ",
               "弾の移動中のSE",
               "弾がブロックに当たった時のSE"

          }
      )]

        [SerializeField]
        private AudioClip[] _BulletClips = new AudioClip[(int)Bullet.MAX_SE];

        [NamedSerializeField(
         new string[]
         {
               "磁力起動時のSE" ,
               " 磁力リセット時のSE ",

         }
     )]
        [SerializeField]
        private AudioClip[] _MagnetClips = new AudioClip[(int)Magnet.MAX_SE];

        [NamedSerializeField(
         new string[]
         {
               "障害物同士がぶつかった時のSE" ,
               " 障害物のボタンを押した時のSE ",

         }
     )]
        [SerializeField]
        private AudioClip[] _ObstacleClips = new AudioClip[(int)Obstacle.MAX_SE];


        private InputHandler _inputHandler;
        public AudioSource _audioSource;

        private const float MIN_VOLUME = 0.0f;
        private const float MAX_VOLUME = 1.0f;
        private const float VARIABLE_VOLUME = 0.1f;

        #region -------- シングルトンの設定 --------

        public static SEManager instance;

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

        /// <summary>
        /// SEの再生(MenuSE)
        /// </summary>
        /// <param name="clip_index"></param>
        public void PlaySE(Menu clip_index)
        {
            AudioClip set_clip = _menuClips[(int)clip_index];

            _audioSource.PlayOneShot(set_clip);  // 再生
        }

        /// <summary>
        /// SEの再生(ActioinSE)
        /// </summary>
        /// <param name="clip_index"></param>
        public void PlaySE(Player clip_index)
        {
            AudioClip set_clip = _PlayerClips[(int)clip_index];

            _audioSource.PlayOneShot(set_clip);  // 再生
        }

        /// <summary>
        /// ステージ
        /// </summary>
        /// <param name="clip_index"></param>
        public void PlaySE(Stage clip_index)
        {
            AudioClip set_clip = _PlayerClips[(int)clip_index];

            _audioSource.PlayOneShot(set_clip);  // 再生
        }

        /// <summary>
        /// 障害物
        /// </summary>
        /// <param name="clip_index"></param>
        public void PlaySE(Obstacle clip_index)
        {
            AudioClip set_clip = _PlayerClips[(int)clip_index];

            _audioSource.PlayOneShot(set_clip);  // 再生
        }

        /// <summary>
        /// 磁力
        /// </summary>
        /// <param name="clip_index"></param>
        public void PlaySE(Magnet clip_index)
        {
            AudioClip set_clip = _PlayerClips[(int)clip_index];

            _audioSource.PlayOneShot(set_clip);  // 再生
        }

        /// <summary>
        /// 弾発射
        /// </summary>
        /// <param name="clip_index"></param>
        public void PlaySE(Bullet clip_index)
        {
            AudioClip set_clip = _PlayerClips[(int)clip_index];

            _audioSource.PlayOneShot(set_clip);  // 再生
        }
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