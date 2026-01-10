using UnityEngine;

namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤーの物理的な移動を制御するクラス
    /// 
    /// 【役割】
    /// ・Rigidbody を使って横移動・ジャンプ・重力を制御する
    /// ・PlayerStateController が管理する状態（RUN / JUMP / SHOOT / GOAL 等）を見て動作を切り替える
    /// ・アニメーションではなく「物理的な動き」だけを担当する
    /// </summary>
    public class PlayerMoveController : PlayerControllerBase
    {
        #region ===== 定数 =====
        private const float MAX_SPEED = 3.5f;      // 横移動の最大速度
        private const float MOVE_SPEED = 0.3f;     // 1フレームあたりの加速・減速量
        private const float MIN_SPEED = 0.0f;      // 停止時の最小速度
        private const float JUMP_FORCE = 5.0f;     // ジャンプ時に上方向へ加える力
        private const float GRAVITY_SCALE = 1.25f; // カスタム重力（Unity標準重力の倍率）
        private const float RAYCAST_LENGTH = 0.2f; // 接地判定用Rayの長さ
        #endregion

        // ===== 入力 =====
        // 入力システムなどから設定される移動方向
        public Vector2 inputDir { get; set; } = Vector2.zero;

        // ===== 内部状態 =====
        private Rigidbody _rigidbody;              // プレイヤーのRigidbody
        private Vector3 moveDir = Vector3.zero;    // 現在の移動ベクトル
        private bool _hasJumped = false;           // 1回のジャンプで多重ジャンプしないためのフラグ
        private CapsuleCollider _capsuleCollider; // 接地判定に使うコライダー

        /// <summary>
        /// 初期化処理
        /// ・RigidbodyとColliderを取得
        /// ・重力を無効化して、カスタム重力を使うようにする
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
        /// 毎フレーム実行されるメインの移動処理
        /// 
        /// 【流れ】
        /// 1. ゴールしていたら何もしない
        /// 2. 接地判定を更新
        /// 3. 射撃中なら移動を停止
        /// 4. STILLNESSなら停止
        /// 5. ジャンプ処理
        /// 6. 横移動処理
        /// 7. 重力処理
        /// 8. Rigidbody に反映
        /// </summary>
        public override void OnUpdate()
        {
            // ゴール中は完全に移動停止
            if (HasState(State.GOAL))
                return;

            // 地面にいるかどうかを判定
            CheckGrounded();

            // 着地したらジャンプ済みフラグをリセット
            if (isGrounded && _hasJumped)
                _hasJumped = false;

            // ===== 射撃中 =====
            // 射撃中は横移動を完全に止める
            if (HasState(State.SHOOT))
            {
                moveDir.x = 0f;
                moveDir.z = 0f;
                moveDir.y = _rigidbody.velocity.y; // 落下中ならそのまま落ち続ける

                if (!isGrounded)
                    GravityUpdate();

                _rigidbody.velocity = moveDir;
                return;
            }

            // ===== 静止状態 =====
            if (HasState(State.STILLNESS) && !HasState(State.JUMP))
                StopMoving();

            // ===== ジャンプ処理 =====
            if (HasState(State.JUMP) && !_hasJumped)
                JumpUpdate();

            // ===== 横移動処理 =====
            MoveInputUpdate();

            // ===== 重力処理 =====
            if (!isGrounded)
                GravityUpdate();
            else if (moveDir.y < 0f)
                moveDir.y = MIN_SPEED;

            // 計算した移動量をRigidbodyに反映
            _rigidbody.velocity = moveDir;
        }

        /// <summary>
        /// 横移動を完全に止める
        /// </summary>
        private void StopMoving()
        {
            moveDir.x = 0f;
            moveDir.z = 0f;
        }

        /// <summary>
        /// 入力と状態に応じて左右移動を加速・減速させる
        /// </summary>
        private void MoveInputUpdate()
        {
            if (HasState(State.RUN) || HasState(State.JUMP))
            {
                // 走り・ジャンプ中は加速
                if (currentDir == Direction.RIGHT)
                    moveDir.x = Mathf.Min(moveDir.x + MOVE_SPEED, MAX_SPEED);
                else if (currentDir == Direction.LEFT)
                    moveDir.x = Mathf.Max(moveDir.x - MOVE_SPEED, -MAX_SPEED);
            }
            else
            {
                // 入力がないときは自然に減速
                if (moveDir.x > 0f) moveDir.x = Mathf.Max(moveDir.x - MOVE_SPEED, 0f);
                else if (moveDir.x < 0f) moveDir.x = Mathf.Min(moveDir.x + MOVE_SPEED, 0f);
            }
        }

        /// <summary>
        /// ジャンプ処理
        /// 地面にいる時だけ上方向に力を加える
        /// </summary>
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

        /// <summary>
        /// 接地判定
        /// カプセルコライダーの底の「中央・右・左」からRayを飛ばし、
        /// どれかが地面に当たっていれば接地とみなす
        /// </summary>
        private void CheckGrounded()
        {
            if (_capsuleCollider == null)
                _capsuleCollider = playerObject.GetComponent<CapsuleCollider>();

            Vector3 center = new Vector3(
                _capsuleCollider.bounds.center.x,
                _capsuleCollider.bounds.min.y + 0.01f,
                _capsuleCollider.bounds.center.z
            );

            float radius = _capsuleCollider.radius * 0.7f;

            Vector3 centerPoint = center;
            Vector3 frontPoint = center + Vector3.right * radius;
            Vector3 backPoint = center + Vector3.left * radius;

            bool centerHit = Physics.Raycast(centerPoint, Vector3.down, RAYCAST_LENGTH, ~0, QueryTriggerInteraction.Ignore);
            bool frontHit = Physics.Raycast(frontPoint, Vector3.down, RAYCAST_LENGTH, ~0, QueryTriggerInteraction.Ignore);
            bool backHit = Physics.Raycast(backPoint, Vector3.down, RAYCAST_LENGTH, ~0, QueryTriggerInteraction.Ignore);

            isGrounded = centerHit || frontHit || backHit;
        }

        /// <summary>
        /// 移動処理を有効化
        /// </summary>
        public void EnableMovement() => this.enabled = true;

        /// <summary>
        /// 移動処理を無効化
        /// </summary>
        public void DisableMovement() => this.enabled = false;

        /// <summary>
        /// カスタム重力を加算する
        /// </summary>
        private void GravityUpdate()
        {
            moveDir.y += Physics.gravity.y * GRAVITY_SCALE * Time.deltaTime;
        }

        /// <summary>
        /// プレイヤーの初期スポーン処理
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
