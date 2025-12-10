using UnityEngine;

namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤーの物理的な移動を制御するクラス。
    /// - Rigidbodyを操作し、横移動、ジャンプ、カスタム重力処理を行います。
    /// - PlayerStateControllerから受け取った状態（RUN, JUMP, SHOOT）に基づいて動作します。
    /// </summary>
    public class PlayerMoveController : PlayerControllerBase
    {
        #region ===== 定数 =====
        private const float MAX_SPEED = 3.5f;      // 横移動の最大速度
        private const float MOVE_SPEED = 0.3f;     // 1フレームあたりの移動加速度（加速/減速の速さ）
        private const float MIN_SPEED = 0.0f;      // 最小速度
        private const float JUMP_FORCE = 5.0f;     // ジャンプ時に与える上方向の力
        private const float GRAVITY_SCALE = 1.25f; // 重力の強さ（Unity標準重力の1.25倍）
        private const float RAYCAST_LENGTH = 0.2f; // 接地判定用Rayの長さ
        #endregion

        // ===== 内部変数 =====
        public Vector2 inputDir { get; set; } = Vector2.zero; // 外部からの入力方向（現在は未使用の可能性あり）

        private Rigidbody _rigidbody;
        private Vector3 moveDir = Vector3.zero; // 現在の移動ベクトル（Rigidbody.velocityに設定される）

        private bool _hasJumped = false; // ジャンプが実行されたかどうかのフラグ（ジャンプ入力の重複防止）

        private CapsuleCollider _capsuleCollider;

        /// <summary>
        /// 初期化処理。RigidbodyとColliderを取得し、重力を無効化します。
        /// </summary>
        public override void OnStart()
        {
            // Rigidbodyコンポーネントを取得
            _rigidbody = playerObject.GetComponent<Rigidbody>();
            // カスタム重力処理を行うため、Unityの標準重力を無効化
            _rigidbody.useGravity = false;

            // CapsuleColliderコンポーネントを取得
            _capsuleCollider = playerObject.GetComponent<CapsuleCollider>();

            if (playerTransform == null)
                playerTransform = playerObject.transform;
        }

        /// <summary>
        /// 毎フレームの更新処理。接地判定、状態チェック、移動処理、カスタム重力処理を行います。
        /// </summary>
        public override void OnUpdate()
        {
            // デバッグログ（動作確認用）
            //Debug.Log($"[PlayerMove] enabled={enabled}, isGrounded={isGrounded}, " +
            // $"State.RUN={HasState(State.RUN)}, currentDir={currentDir}, " +
            // $"moveDir={moveDir}, velocity={_rigidbody?.velocity}");

            // 接地判定を更新
            CheckGrounded();

            // 着地した瞬間、ジャンプフラグをリセット
            if (isGrounded && _hasJumped)
            {
                _hasJumped = false;
            }

            // ===== 1. 射撃状態の処理 =====
            if (HasState(State.SHOOT))
            {
                // 射撃中は横移動を停止
                moveDir.x = 0f;
                moveDir.z = 0f;
                moveDir.y = _rigidbody.velocity.y; // 縦方向の速度は維持

                if (!isGrounded) GravityUpdate(); // 空中にいる場合は重力処理
                _rigidbody.velocity = moveDir;
                return; // 射撃中は以降の移動・ジャンプ処理をスキップ
            }

            // ===== 2. 静止状態の処理 =====
            // STILLNESS状態かつJUMP状態でない場合、移動を停止
            if (HasState(State.STILLNESS) && !HasState(State.JUMP))
                StopMoving();

            // ===== 3. ジャンプ入力の処理 =====
            // JUMP状態がセットされており、かつまだジャンプが実行されていない場合
            if (HasState(State.JUMP) && !_hasJumped)
                JumpUpdate();

            // ===== 4. 横移動の処理 =====
            MoveInputUpdate();

            // ===== 5. 重力処理 =====
            if (!isGrounded)
                GravityUpdate(); // 空中にいる場合はカスタム重力を適用
            else if (moveDir.y < 0f)
                moveDir.y = MIN_SPEED; // 地面にいる場合はY速度をリセット（めり込み防止）

            // 最終的な移動ベクトルをRigidbodyに適用
            _rigidbody.velocity = moveDir;
        }

        /// <summary>
        /// 横方向の移動を停止します。
        /// </summary>
        private void StopMoving()
        {
            moveDir.x = 0f;
            moveDir.z = 0f;
        }

        /// <summary>
        /// 移動入力（RUN状態）に基づいて横方向の速度を更新します。
        /// </summary>
        private void MoveInputUpdate()
        {
            // RUN状態またはJUMP状態の場合
            if (HasState(State.RUN) || HasState(State.JUMP))
            {
                // 加速処理
                if (currentDir == Direction.RIGHT)
                    // 右方向に加速し、最大速度を超えないように制限
                    moveDir.x = Mathf.Min(moveDir.x + MOVE_SPEED, MAX_SPEED);
                else if (currentDir == Direction.LEFT)
                    // 左方向に加速し、最大速度を超えないように制限
                    moveDir.x = Mathf.Max(moveDir.x - MOVE_SPEED, -MAX_SPEED);
            }
            else
            {
                // 入力がない場合、減速処理
                if (moveDir.x > 0f) moveDir.x = Mathf.Max(moveDir.x - MOVE_SPEED, 0f);
                else if (moveDir.x < 0f) moveDir.x = Mathf.Min(moveDir.x + MOVE_SPEED, 0f);
            }
        }

        /// <summary>
        /// ジャンプを実行します。
        /// </summary>
        private void JumpUpdate()
        {
            if (isGrounded)
            {
                // 上方向に力を加える
                moveDir.y = JUMP_FORCE;
                _hasJumped = true;

                //SEプレイヤージャンプはこの一行（必要時にコメントアウト
                SEManager.instance.PlaySE(SEManager.Player.PLAYER_JUMP);

                //ここまで

                Debug.Log("[MoveController] ジャンプ実行");
            }
        }

        // =====================================================
        //  接地判定処理
        // =====================================================
        /// <summary>
        /// Raycastを使用してプレイヤーの接地判定を行います。
        /// </summary>
        private void CheckGrounded()
        {
            if (_capsuleCollider == null)
                _capsuleCollider = playerObject.GetComponent<CapsuleCollider>();

            // Rayの始点をカプセルコライダーの下端から少し上に設定
            Vector3 rayStart = new Vector3(
                _capsuleCollider.bounds.center.x,
                _capsuleCollider.bounds.min.y + 0.01f, // 0.01fはコライダーの表面から開始するためのオフセット
                _capsuleCollider.bounds.center.z
            );

            // 下方向にRayを飛ばし、地面との接触を判定
            isGrounded = Physics.Raycast(
                rayStart,
                Vector3.down,
                RAYCAST_LENGTH,
                ~0, // すべてのレイヤーを対象
                QueryTriggerInteraction.Ignore // トリガーColliderは無視
            );
          

            // Unity Editorでのデバッグ表示
#if UNITY_EDITOR
            Debug.DrawRay(rayStart, Vector3.down * RAYCAST_LENGTH,
                isGrounded ? Color.green : Color.red);
#endif
        }

        /// <summary>
        /// 移動処理を有効化します。
        /// </summary>
        public void EnableMovement() => this.enabled = true;

        /// <summary>
        /// 移動処理を無効化します。
        /// </summary>
        public void DisableMovement() => this.enabled = false;

        /// <summary>
        /// カスタム重力処理。Y軸の速度に重力を加算します。
        /// </summary>
        private void GravityUpdate()
        {
            // moveDir.y = moveDir.y + (Physics.gravity.y * GRAVITY_SCALE * Time.deltaTime)
            moveDir.y += Physics.gravity.y * GRAVITY_SCALE * Time.deltaTime;
        }

        /// <summary>
        /// プレイヤーの位置と速度を初期化します。
        /// </summary>
        public void InitPlayer(Vector3 spawnPos, Quaternion spawnRot)
        {
            playerTransform.position = spawnPos;
            playerTransform.rotation = spawnRot;

            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }
    }
}
