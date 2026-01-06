using UnityEngine;

namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤーの物理的な移動を制御するクラス。
    /// - Rigidbodyを操作し、横移動、ジャンプ、カスタム重力処理を行います。
    /// - PlayerStateControllerから受け取った状態（RUN, JUMP, SHOOT）に基づいて動作します。
    /// - ゴール後は移動処理を完全停止
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
        public Vector2 inputDir { get; set; } = Vector2.zero;

        private Rigidbody _rigidbody;
        private Vector3 moveDir = Vector3.zero;
        private bool _hasJumped = false;
        private CapsuleCollider _capsuleCollider;

        /// <summary>
        /// 初期化処理。RigidbodyとColliderを取得し、重力を無効化します。
        /// </summary>
        public override void OnStart()
        {
            _rigidbody = playerObject.GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _capsuleCollider = playerObject.GetComponent<CapsuleCollider>();

            if (playerTransform == null)
                playerTransform = playerObject.transform;
        }

        /// <summary>
        /// 毎フレームの更新処理。接地判定、状態チェック、移動処理、カスタム重力処理を行います。
        /// </summary>
        public override void OnUpdate()
        {
            // ===== ゴール状態の場合は移動処理を完全停止 =====
            if (HasState(State.GOAL))
            {
                // kinematicエラーを防ぐため、velocity設定を一切行わない
                return;
            }

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
                moveDir.x = 0f;
                moveDir.z = 0f;
                moveDir.y = _rigidbody.velocity.y;

                if (!isGrounded) GravityUpdate();
                _rigidbody.velocity = moveDir;
                return;
            }

            // ===== 2. 静止状態の処理 =====
            if (HasState(State.STILLNESS) && !HasState(State.JUMP))
                StopMoving();

            // ===== 3. ジャンプ入力の処理 =====
            if (HasState(State.JUMP) && !_hasJumped)
                JumpUpdate();

            // ===== 4. 横移動の処理 =====
            MoveInputUpdate();

            // ===== 5. 重力処理 =====
            if (!isGrounded)
                GravityUpdate();
            else if (moveDir.y < 0f)
                moveDir.y = MIN_SPEED;

            // 最終的な移動ベクトルをRigidbodyに適用
            _rigidbody.velocity = moveDir;
        }

        private void StopMoving()
        {
            moveDir.x = 0f;
            moveDir.z = 0f;
        }

        private void MoveInputUpdate()
        {
            if (HasState(State.RUN) || HasState(State.JUMP))
            {
                if (currentDir == Direction.RIGHT)
                    moveDir.x = Mathf.Min(moveDir.x + MOVE_SPEED, MAX_SPEED);
                else if (currentDir == Direction.LEFT)
                    moveDir.x = Mathf.Max(moveDir.x - MOVE_SPEED, -MAX_SPEED);
            }
            else
            {
                if (moveDir.x > 0f) moveDir.x = Mathf.Max(moveDir.x - MOVE_SPEED, 0f);
                else if (moveDir.x < 0f) moveDir.x = Mathf.Min(moveDir.x + MOVE_SPEED, 0f);
            }
        }

        private void JumpUpdate()
        {
            if (isGrounded)
            {
                moveDir.y = JUMP_FORCE;
                _hasJumped = true;
                SEManager.instance.PlaySE(SEManager.Player.PLAYER_JUMP);
                Debug.Log("[MoveController] ジャンプ実行");
            }
        }

        private void CheckGrounded()
        {
            if (_capsuleCollider == null)
                _capsuleCollider = playerObject.GetComponent<CapsuleCollider>();

            Vector3 rayStart = new Vector3(
                _capsuleCollider.bounds.center.x,
                _capsuleCollider.bounds.min.y + 0.01f,
                _capsuleCollider.bounds.center.z
            );

            isGrounded = Physics.Raycast(
                rayStart,
                Vector3.down,
                RAYCAST_LENGTH,
                ~0,
                QueryTriggerInteraction.Ignore
            );

#if UNITY_EDITOR
            Debug.DrawRay(rayStart, Vector3.down * RAYCAST_LENGTH,
                isGrounded ? Color.green : Color.red);
#endif
        }

        public void EnableMovement() => this.enabled = true;
        public void DisableMovement() => this.enabled = false;

        private void GravityUpdate()
        {
            moveDir.y += Physics.gravity.y * GRAVITY_SCALE * Time.deltaTime;
        }

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