using UnityEngine;

namespace Game.StageScene.Player
{
    /// <summary>
    /// プレイヤーのアニメーション制御クラス
    /// - ジャンプアニメーションを最優先（地面についたら解除）
    /// - 優先順位: JUMP > SHOOT > RUN > IDLE
    /// - アニメーションの遷移とループ制御を管理
    /// </summary>
    public class PlayerAnimationController : PlayerControllerBase
    {
        #region -------- Animation 定数 --------
        // Animatorのパラメータ名（Animatorウィンドウで設定した名前と一致させる）
        private const string CURRENT_STATE = "CurrentState";       // 現在の状態（0=NONE, 1=IDLE, 2=RUN, 3=JUMP, 4=SHOOT）
        private const string CURRENT_DIRECTION = "CurrentDirection"; // 射撃方向（角度）
        #endregion

        /// <summary>
        /// アニメーション状態の種類
        /// AnimatorのCurrentStateパラメータに対応
        /// </summary>
        private enum AnimationState
        {
            NONE = 0,  // 状態なし
            IDLE = 1,  // 待機アニメーション
            RUN = 2,   // 走行アニメーション
            JUMP = 3,  // ジャンプアニメーション
            SHOOT = 4, // 射撃アニメーション
        }

        /// <summary>
        /// アニメーションレイヤー（左右の向きに対応）
        /// </summary>
        private enum AnimationLayer
        {
            BASE = 0,  // ベースレイヤー（使用しない）
            RIGHT = 1, // 右向き用レイヤー
            LEFT = 2,  // 左向き用レイヤー
        }

        // ===== 内部変数 =====
        private Animator _animator;                      // プレイヤーのAnimatorコンポーネント
        private AnimationState _currentAnimationState;   // 現在再生中のアニメーション状態
        private AnimationLayer _currentAnimationLayer;   // 現在アクティブなレイヤー（左 or 右）
        private float _currentAnimationTime;             // 現在のアニメーション再生時間（0.0～1.0の正規化された値）

        /// <summary>
        /// 初期化処理（プレイヤー生成時に1回だけ呼ばれる）
        /// </summary>
        public override void OnStart()
        {
            // プレイヤーオブジェクトからAnimatorコンポーネントを取得
            _animator = playerObject.GetComponent<Animator>();

            // 初期状態を設定
            _currentAnimationState = AnimationState.IDLE;     // 待機アニメーションからスタート
            _currentAnimationLayer = AnimationLayer.RIGHT;    // 右向きからスタート
        }

        /// <summary>
        /// 毎フレーム呼ばれる更新処理
        /// アニメーションの状態を決定し、Animatorに反映する
        /// </summary>
        public override void OnUpdate()
        {
            // ===== 1. アニメーションレイヤーの切り替え =====
            // 現在のレイヤーのウェイトを0にする（非表示）
            _animator.SetLayerWeight((int)_currentAnimationLayer, 0);

            // プレイヤーの向き（currentDir）に応じて新しいレイヤーを決定
            _currentAnimationLayer = currentDir == Direction.RIGHT ? AnimationLayer.RIGHT : AnimationLayer.LEFT;

            // 新しいレイヤーのウェイトを1にする（表示）
            _animator.SetLayerWeight((int)_currentAnimationLayer, 1);

            // ===== 2. モデルの向き設定 =====
            // 3Dモデルを左右に回転させる
            SetAnimationDir();

            // ===== 3. アニメーション状態の更新 =====
            // プレイヤーの状態（State）に基づいて、どのアニメーションを再生するか決定
            StateUpdate();

            // ===== 4. Animatorパラメータの反映 =====
            // 決定したアニメーション状態をAnimatorに送信
            UpdateAnimatorParameters();
        }

        /// <summary>
        /// 状態に基づいてアニメーションを決定
        /// 優先順位：JUMP > SHOOT > RUN > IDLE
        /// </summary>
        private void StateUpdate()
        {
            // ===== 優先度1: ジャンプアニメーション =====
            // 空中にいる場合は必ずジャンプアニメーションを再生
            // State.RUNが立っていてもジャンプアニメーションを優先
            if (!isGrounded)
            {
                // 別のアニメーションからジャンプに切り替わった場合、再生時間をリセット
                if (_currentAnimationState != AnimationState.JUMP)
                {
                    _currentAnimationTime = 0.0f;
                }

                _currentAnimationState = AnimationState.JUMP; // ジャンプアニメーションに設定
                JumpUpdate(); // ジャンプアニメーションのループ制御
                return; // ここでreturnするので、State.RUNがあっても走行アニメーションは再生されない
            }

            // ===== 優先度2: 射撃アニメーション =====
            // 地面にいる時のみ射撃アニメーションが有効
            // State.RUNが立っていても射撃アニメーションを優先
            if (HasState(State.SHOOT))
            {
                // 別のアニメーションから射撃に切り替わった場合、再生時間をリセット
                if (_currentAnimationState != AnimationState.SHOOT)
                {
                    _currentAnimationTime = 0.0f;
                }

                _currentAnimationState = AnimationState.SHOOT; // 射撃アニメーションに設定
                ShootUpdate(); // 射撃アニメーションのループ制御
                return; // ここでreturnするので、State.RUNがあっても走行アニメーションは再生されない
            }

            // ===== 優先度3: 走行アニメーション =====
            // 地面にいて、ジャンプ中でも射撃中でもなく、RUN状態の時のみ走行アニメーションを再生
            if (isGrounded && HasState(State.RUN))
            {
                _currentAnimationState = AnimationState.RUN;
                return;
            }

            // ===== 優先度4: 待機アニメーション =====
            // 上記のどれにも当てはまらない場合はアイドル（待機）
            _currentAnimationState = AnimationState.IDLE;
        }

        /// <summary>
        /// モデルの向きを設定（3Dモデルを左右に回転）
        /// </summary>
        private void SetAnimationDir()
        {
            // 右向きの場合：Y軸を90度回転
            // 左向きの場合：Y軸を270度回転
            playerTransform.eulerAngles = _currentAnimationLayer == AnimationLayer.RIGHT
                ? new Vector3(0f, 90f, 0f)   // 右向き
                : new Vector3(0f, 270f, 0f); // 左向き
        }

        /// <summary>
        /// Animatorパラメータを更新
        /// 決定したアニメーション状態と射撃方向をAnimatorに送信
        /// </summary>
        private void UpdateAnimatorParameters()
        {
            // 現在Animatorに設定されている値を取得
            int currentDirValue = _animator.GetInteger(CURRENT_DIRECTION);
            int currentStateValue = _animator.GetInteger(CURRENT_STATE);

            // 射撃方向が変わっている場合のみ更新（無駄な更新を避ける）
            if (currentDirValue != (int)shootDir)
            {
                _animator.SetInteger(CURRENT_DIRECTION, (int)shootDir);
            }

            // アニメーション状態が変わっている場合のみ更新
            if (currentStateValue != (int)_currentAnimationState)
            {
                _animator.SetInteger(CURRENT_STATE, (int)_currentAnimationState);
            }
            _animator.Update(0); // 即時反映
        }

        /// <summary>
        /// ジャンプアニメーションのループ制御
        /// アニメーションが80%まで進んだら60%の位置にループバックさせる
        /// これにより、ジャンプの頂点付近のアニメーションを繰り返す
        /// </summary>
        private void JumpUpdate()
        {
            // 現在アクティブなレイヤーのアニメーション状態を取得
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo((int)_currentAnimationLayer);

            // 正規化された再生時間を取得（0.0～1.0）
            _currentAnimationTime = stateInfo.normalizedTime;

            // アニメーションが80%まで進んだら、60%の位置に巻き戻す
            //if (_currentAnimationTime >= 0.8f)
            //{
            //    // stateInfo.shortNameHash: 現在のアニメーションステートのハッシュ値
            //    // 第3引数の0.6f: 再生位置を60%に設定
            //    _animator.Play(stateInfo.shortNameHash, (int)_currentAnimationLayer, 0.6f);
            //}
        }

        /// <summary>
        /// 射撃アニメーションのループ制御
        /// アニメーションが70%まで進んだら37.5%の位置にループバックさせる
        /// これにより、射撃の構えと発射のモーションを繰り返す
        /// </summary>
        private void ShootUpdate()
        {
            // 現在アクティブなレイヤーのアニメーション状態を取得
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo((int)_currentAnimationLayer);

            // 正規化された再生時間を取得（0.0～1.0）
            _currentAnimationTime = stateInfo.normalizedTime;

            // アニメーションが70%まで進んだら、37.5%の位置に巻き戻す
            //if (_currentAnimationTime >= 0.7f)
            //{
            //    // stateInfo.shortNameHash: 現在のアニメーションステートのハッシュ値
            //    // 第3引数の0.375f: 再生位置を37.5%に設定
            //    if (_currentAnimationState == AnimationState.SHOOT && _currentAnimationTime >= 0.7f)
            //    {
            //        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo((int)_currentAnimationLayer);
            //        _animator.Play(stateInfo.shortNameHash, (int)_currentAnimationLayer, 0.375f);
            //    }
            //}
        }
    }
}