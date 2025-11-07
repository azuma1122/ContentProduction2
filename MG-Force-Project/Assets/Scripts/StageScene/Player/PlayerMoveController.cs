using UnityEngine;
namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤー移動制御クラス
    /// - 射撃中は完全に移動を停止
    /// - ジャンプ中は横移動可能（空中制御）
    /// - 左右移動 / ジャンプ / 重力制御
    /// </summary>
    public class PlayerMoveController : PlayerControllerBase
    {
        #region ===== 定数 =====
        private const float MAX_SPEED = 3.5f;      // 横移動の最大速度
        private const float MOVE_SPEED = 3f;       // 1フレームあたりの移動加速度
        private const float MIN_SPEED = 0.0f;      // 最小速度（基本的に0）
        private const float JUMP_FORCE = 5.0f;     // ジャンプ時に与える上方向の力
        private const float RAYCAST_LENGTH = 0.2f; // 接地判定用のRayの長さ
        private const float GRAVITY_SCALE = 1.25f; // 重力の強さ（Unity標準重力の1.25倍）
        #endregion

        // ===== 公開プロパティ =====
        public Vector2 inputDir { get; set; } = Vector2.zero; // 入力方向（現在未使用）

        // ===== 内部変数 =====
        private Rigidbody _rigidbody;               // プレイヤーのRigidbodyコンポーネント
        private Vector3 moveDir = Vector3.zero;     // 現在の移動方向・速度ベクトル
        private Vector3 raycastDir = Vector3.down;  // 接地判定用のRay方向（真下）

        // ===== ジャンプ制御用フラグ =====
        private bool _hasJumped = false;    // ジャンプ力を与えたかどうか（連続ジャンプ防止）

        /// <summary>
        /// 初期化処理（プレイヤー生成時に1回だけ呼ばれる）
        /// </summary>
        public override void OnStart()
        {
            // Rigidbodyコンポーネントを取得
            _rigidbody = playerObject.GetComponent<Rigidbody>();

            // Unityの標準重力を無効化（自前で重力処理を実装するため）
            _rigidbody.useGravity = false;
        }

        /// <summary>
        /// 毎フレーム呼ばれる更新処理（移動・ジャンプ・重力の制御）
        /// </summary>
        public override void OnUpdate()
        {
            // ===== 1. 接地判定を更新 =====
            // Raycastを使って地面にいるかどうかをチェック
            CheckGrounded();

            // ===== 2. 着地判定：地面についたらジャンプ実行済みフラグをリセット =====
            if (isGrounded && _hasJumped)
            {
                _hasJumped = false; // 次回ジャンプボタンを押したときにジャンプできるようにする
            }

            // ===== 3. 射撃中の処理 =====
            // 射撃中はその場で停止し、重力のみ適用
            if (HasState(State.SHOOT))
            {
                moveDir.x = 0f;                     // 横方向の速度を0にして停止
                moveDir.z = 0f;                     // 奥行き方向も停止（2Dゲームなので基本0）
                moveDir.y = _rigidbody.velocity.y;  // 縦方向（Y軸）は現在の速度を維持（落下継続のため）

                // 空中にいる場合のみ重力を適用（地面にいる場合は重力不要）
                if (!isGrounded) GravityUpdate();

                // 計算した速度をRigidbodyに反映
                _rigidbody.velocity = moveDir;
                return; // 射撃中は他の移動処理を全てスキップ
            }

            // ===== 4. 停止処理 =====
            // 地面にいて、ジャンプ中でもなく、静止状態の場合は横移動を停止
            if (HasState(State.STILLNESS) && !HasState(State.JUMP))
            {
                StopMoving(); // 横方向の速度を0にする
            }

            // ===== 5. ジャンプ処理 =====
            // State.JUMPが立っていて、まだジャンプ力を与えていない場合のみ実行
            if (HasState(State.JUMP) && !_hasJumped)
            {
                JumpUpdate(); // ジャンプ力を与える（1回だけ）
            }

            // ===== 6. 左右移動処理 =====
            // 地面にいる場合も、空中にいる場合も横移動可能（空中制御あり）
            MoveInputUpdate();

            // ===== 7. 重力処理 =====
            if (!isGrounded)
            {
                GravityUpdate(); // 空中にいる場合は重力を適用（下方向に加速）
            }
            else if (moveDir.y < 0f)
            {
                moveDir.y = MIN_SPEED; // 地面にいる場合は下方向の速度を0にする（地面にめり込まないため）
            }

            // ===== 8. 最終的な速度をRigidbodyに反映 =====
            _rigidbody.velocity = moveDir;
        }

        /// <summary>
        /// 横移動を停止する
        /// </summary>
        private void StopMoving()
        {
            moveDir.x = 0f; // 横方向（X軸）の速度を0に
            moveDir.z = 0f; // 奥行き方向（Z軸）の速度を0に（2Dなので基本使わない）
        }

        /// <summary>
        /// 移動入力の処理（地面でもジャンプ中でも横移動可能）
        /// currentDirの方向に基づいて加速または減速を行う
        /// </summary>
        private void MoveInputUpdate()
        {
            // State.RUN（地面での移動）またはState.JUMP（空中制御）が立っている場合
            if (HasState(State.RUN) || HasState(State.JUMP))
            {
                // 右方向への移動
                if (currentDir == Direction.RIGHT)
                {
                    // 現在の速度にMOVE_SPEEDを加算し、MAX_SPEEDを超えないようにする
                    moveDir.x = Mathf.Min(moveDir.x + MOVE_SPEED, MAX_SPEED);
                }
                // 左方向への移動
                else if (currentDir == Direction.LEFT)
                {
                    // 現在の速度からMOVE_SPEEDを減算し、-MAX_SPEEDを下回らないようにする
                    moveDir.x = Mathf.Max(moveDir.x - MOVE_SPEED, -MAX_SPEED);
                }
            }
            else
            {
                // 入力がない場合は徐々に減速（慣性を止める）
                if (moveDir.x > 0f)
                {
                    // 右方向に動いている場合：減速して0に近づける
                    moveDir.x = Mathf.Max(moveDir.x - MOVE_SPEED, 0f);
                }
                else if (moveDir.x < 0f)
                {
                    // 左方向に動いている場合：減速して0に近づける
                    moveDir.x = Mathf.Min(moveDir.x + MOVE_SPEED, 0f);
                }
            }
        }

        /// <summary>
        /// ジャンプ処理（地面にいる時のみジャンプ力を与える）
        /// </summary>
        private void JumpUpdate()
        {
            // 地面にいる場合のみジャンプ可能
            if (isGrounded)
            {
                moveDir.y = JUMP_FORCE; // Y軸方向（上方向）に力を加える
                _hasJumped = true; // ジャンプ実行済みフラグを立てる（連続ジャンプ防止）
                Debug.Log("[MoveController] ジャンプ実行");
            }
        }

        /// <summary>
        /// 接地判定（Raycastを使って地面との接触を検出）
        /// </summary>
        private void CheckGrounded()
        {
            // プレイヤーの足元から少し上の位置（0.1f上）からRayを発射
            Vector3 rayStart = playerTransform.position + Vector3.up * 0.1f;

            // 真下に向かってRayを発射し、何かに当たったかチェック
            if (Physics.Raycast(rayStart, raycastDir, out RaycastHit hit, RAYCAST_LENGTH))
            {
                // トリガーコライダーではなく、かつタグがUNTAGGEDでない場合は地面と判定
                if (!hit.collider.isTrigger && hit.collider.tag != GameConstants.Tag.UNTAGGED)
                    isGrounded = true; // 地面に接地している
            }
            else
            {
                isGrounded = false; // 何にも当たらなければ空中にいる
            }

#if UNITY_EDITOR
            // エディタ上でRayを可視化（デバッグ用・赤い線で表示）
            Debug.DrawRay(playerTransform.position, raycastDir * RAYCAST_LENGTH, Color.red);
#endif
        }

        /// <summary>
        /// 重力処理（空中にいる時に下方向への加速度を与える）
        /// </summary>
        private void GravityUpdate()
        {
            // Unity標準の重力（Physics.gravity.y）にGRAVITY_SCALEを掛けて、Y軸速度に加算
            // Time.deltaTimeを掛けることでフレームレートに依存しない滑らかな落下を実現
            moveDir.y += Physics.gravity.y * GRAVITY_SCALE * Time.deltaTime;
        }
    }
}