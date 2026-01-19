using Game.StageScene;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ギミック全体の動作を制御するクラス
/// - 指定されたgimmickIdを持つボタンの状態に応じて複数ブロックを制御
/// - ボタンが上がっている(UP)時: ブロック表示
/// - ボタンが下がっている(DOWN)時: ブロック非表示
/// - Scene内のFixed_Not_Block_<gimmickId>を自動取得
/// </summary>
public class GimmickController : MonoBehaviour
{
    [Header("ギミック設定")]
    [SerializeField] private string _gimmickId; // このギミック専用のID

    private ButtonController _button;
    [SerializeField] private List<GameObject> _fixedBoxes = new List<GameObject>(); // 複数ブロック対応

    [Header("自動取得設定")]
    [SerializeField] private bool _autoFindBlocks = false; // Scene内のブロックを自動取得（StageCreaterから設定する場合はfalse）

    [Header("デバッグ")]
    [SerializeField] private bool _showDebugLogs = true; // デバッグを有効に

    // 前フレームのボタン状態を記憶（チカチカ防止）
    private bool? _previousButtonState = null; // null = 未初期化

    private void Start()
    {
        if (_showDebugLogs)
        {
            Debug.Log($"[GimmickController] 起動 - GimmickID={_gimmickId}, アタッチ先={gameObject.name}, ブロック数={_fixedBoxes.Count}");
        }

        // 自動取得が有効な場合のみ実行（StageCreaterから動的生成する場合は不要）
        if (_autoFindBlocks && string.IsNullOrEmpty(_gimmickId) == false)
        {
            AutoFindFixedBlocks();
        }

        TryFindButton();
    }

    private void Update()
    {
        if (_fixedBoxes.Count == 0)
        {
            // 毎フレームではなく、5秒に1回だけ警告
            if (Time.frameCount % 300 == 0 && _showDebugLogs)
            {
                Debug.LogWarning($"[GimmickController({_gimmickId})] ❌_fixedBoxes が空です！StageCreaterからAddFixedBoxが呼ばれていない可能性があります");
            }
            return;
        }

        if (_button == null)
        {
            TryFindButton();
            return;
        }

        // 現在のボタン状態を取得
        bool currentButtonState = _button.GetIsUpButton();

        // 初回または状態が変化した時だけブロックを制御
        if (_previousButtonState == null || currentButtonState != _previousButtonState.Value)
        {
            // すべてのブロックに対してボタンの状態を反映
            foreach (GameObject block in _fixedBoxes)
            {
                if (block != null)
                {
                    // ボタンが上がっている時はブロック表示、下がっている時は非表示
                    block.SetActive(currentButtonState);
                }
            }

            if (_showDebugLogs)
            {
                Debug.Log($"[GimmickController({_gimmickId})] 状態変化検出！ " +
                         $"ボタン: {(currentButtonState ? "UP" : "DOWN")} → " +
                         $"ブロック({_fixedBoxes.Count}個): {(currentButtonState ? "表示" : "非表示")} " +
                         $"(前回: {(_previousButtonState.HasValue ? (_previousButtonState.Value ? "UP" : "DOWN") : "未初期化")})");
            }

            // 状態を更新
            _previousButtonState = currentButtonState;
        }
    }

    /// <summary>
    /// Scene内のFixed_Not_Block_<gimmickId>を自動取得
    /// </summary>
    private void AutoFindFixedBlocks()
    {
        if (string.IsNullOrEmpty(_gimmickId))
        {
            if (_showDebugLogs)
            {
                Debug.LogWarning("[GimmickController] GimmickIDが設定されていないため、自動取得をスキップします");
            }
            return;
        }

        // 命名規則パターン（複数対応）:
        // 1. Fixed_Not_Block_<gimmickId> (例: Fixed_Not_Block_will_1)
        // 2. Fixed_Not_Block(<gimmickId>) (例: Fixed_Not_Block(will_1))
        // 3. Fixed_Not_Block_<gimmickId>_数字 (例: Fixed_Not_Block_will_1_1)
        // 4. Fixed_Not_Block (Clone)で、親がGimmick(<gimmickId>)配下

        string pattern1 = $"Fixed_Not_Block_{_gimmickId}";
        string pattern2 = $"Fixed_Not_Block({_gimmickId})";

        // Scene内の全GameObjectを検索
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int foundCount = 0;

        if (_showDebugLogs)
        {
            Debug.Log($"[GimmickController] ブロック検索開始: GimmickID={_gimmickId}");
            Debug.Log($"  検索パターン1: {pattern1}*");
            Debug.Log($"  検索パターン2: {pattern2}*");
        }

        foreach (GameObject obj in allObjects)
        {
            bool isMatch = false;
            string matchReason = "";

            // パターン1: Fixed_Not_Block_<gimmickId>で始まる
            if (obj.name.StartsWith(pattern1))
            {
                isMatch = true;
                matchReason = "パターン1一致";
            }
            // パターン2: Fixed_Not_Block(<gimmickId>)で始まる
            else if (obj.name.StartsWith(pattern2))
            {
                isMatch = true;
                matchReason = "パターン2一致";
            }
            // パターン3: 親階層をチェック（Gimmick配下のFixed_Not_Block）
            else if (obj.name.Contains("Fixed_Not_Block"))
            {
                Transform parent = obj.transform.parent;
                while (parent != null)
                {
                    if (parent.name.Contains(_gimmickId) || parent.name.Contains($"Gimmick({_gimmickId})"))
                    {
                        isMatch = true;
                        matchReason = $"親階層一致: {parent.name}";
                        break;
                    }
                    parent = parent.parent;
                }
            }

            if (isMatch)
            {
                if (!_fixedBoxes.Contains(obj))
                {
                    _fixedBoxes.Add(obj);
                    foundCount++;

                    if (_showDebugLogs)
                    {
                        Debug.Log($"[GimmickController] ブロック自動取得: {obj.name} ({matchReason})");
                    }
                }
            }
        }

        if (_showDebugLogs)
        {
            if (foundCount > 0)
            {
                Debug.Log($"[GimmickController] GimmickID({_gimmickId})のブロックを{foundCount}個自動取得しました（合計: {_fixedBoxes.Count}個）");
            }
            else
            {
                Debug.LogWarning($"[GimmickController] GimmickID({_gimmickId})に対応するブロックが見つかりませんでした");
                Debug.LogWarning("  Hierarchy内の'Fixed_Not_Block'オブジェクト一覧:");
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("Fixed_Not_Block"))
                    {
                        string parentInfo = obj.transform.parent != null ? $" (親: {obj.transform.parent.name})" : " (親なし)";
                        Debug.LogWarning($"    - {obj.name}{parentInfo}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 指定されたgimmickIdを持つButtonを探して取得
    /// </summary>
    private void TryFindButton()
    {
        // 全てのButtonControllerを取得
        ButtonController[] allButtons = FindObjectsOfType<ButtonController>();

        if (_showDebugLogs)
        {
            Debug.Log($"[GimmickController] 全ボタン数: {allButtons.Length}");
            foreach (var btn in allButtons)
            {
                Debug.Log($"  - ボタン: {btn.gameObject.name}, gimmickId: '{btn.gimmickId}'");
            }
        }

        foreach (ButtonController btn in allButtons)
        {
            // gimmickIdが一致するボタンを探す
            if (btn.gimmickId == _gimmickId)
            {
                _button = btn;

                if (_showDebugLogs)
                {
                    Debug.Log($"[GimmickController] GimmickID({_gimmickId})のボタンを取得！ - {btn.gameObject.name}");
                }

                // ボタン取得時に初期状態を設定
                _previousButtonState = _button.GetIsUpButton();
                foreach (GameObject block in _fixedBoxes)
                {
                    if (block != null)
                    {
                        block.SetActive(_previousButtonState.Value);
                    }
                }

                if (_showDebugLogs)
                {
                    Debug.Log($"[GimmickController] 初期状態設定: ボタン={((_previousButtonState.Value) ? "UP" : "DOWN")}, ブロック({_fixedBoxes.Count}個)={(_previousButtonState.Value ? "表示" : "非表示")}");
                }

                return;
            }
        }

        // 見つからなかった場合
        _button = null;

        if (_showDebugLogs)
        {
            Debug.LogWarning($"[GimmickController] GimmickID({_gimmickId})のボタンが見つかりません！");
        }
    }

    /// <summary>
    /// StageCreaterから呼ばれる：GimmickIDを設定
    /// </summary>
    public void SetGimmickId(string gimmickId)
    {
        _gimmickId = gimmickId;

        if (_showDebugLogs)
        {
            Debug.Log($"[GimmickController] GimmickID設定 - {gimmickId}");
        }

        // GimmickID設定後、自動取得を実行
        if (_autoFindBlocks)
        {
            AutoFindFixedBlocks();
        }
    }

    /// <summary>
    /// StageCreaterから呼ばれる：ボタンを直接設定
    /// </summary>
    public void SetButton(ButtonController button)
    {
        _button = button;

        if (_button != null)
        {
            // ボタン設定時に初期状態を同期
            _previousButtonState = _button.GetIsUpButton();
            foreach (GameObject block in _fixedBoxes)
            {
                if (block != null)
                {
                    block.SetActive(_previousButtonState.Value);
                }
            }

            if (_showDebugLogs)
            {
                Debug.Log($"[GimmickController] ボタン設定: {button.gameObject.name}, gimmickId={button.gimmickId}, 初期状態={((_previousButtonState.Value) ? "UP" : "DOWN")}, ブロック数={_fixedBoxes.Count}");
            }
        }
    }

    /// <summary>
    /// StageCreaterから呼ばれる：ブロックを設定（単一・後方互換性用）
    /// </summary>
    public void SetFixedBox(GameObject block)
    {
        if (block != null && !_fixedBoxes.Contains(block))
        {
            _fixedBoxes.Add(block);

            // ブロック設定時に現在のボタン状態に合わせる
            if (_button != null)
            {
                _previousButtonState = _button.GetIsUpButton();
                block.SetActive(_previousButtonState.Value);
            }

            if (_showDebugLogs)
            {
                Debug.Log($"[GimmickController] ブロック設定: {block.name}（合計: {_fixedBoxes.Count}個）");
            }
        }
    }

    /// <summary>
    /// StageCreaterから呼ばれる：複数ブロックを追加
    /// </summary>
    public void AddFixedBox(GameObject block)
    {
        Debug.Log($"[GimmickController] ★AddFixedBox呼び出し★ block={(block != null ? block.name : "null")}");

        if (block != null && !_fixedBoxes.Contains(block))
        {
            _fixedBoxes.Add(block);
            Debug.Log($"[GimmickController] ✅ブロック追加成功: {block.name}（合計: {_fixedBoxes.Count}個）");

            // ブロック追加時に現在のボタン状態に合わせる
            if (_button != null && _previousButtonState.HasValue)
            {
                block.SetActive(_previousButtonState.Value);
                Debug.Log($"[GimmickController] ブロック初期状態設定: {block.name} → {(_previousButtonState.Value ? "表示" : "非表示")}");
            }
            else
            {
                Debug.LogWarning($"[GimmickController] ⚠️ボタン未設定のため初期状態を設定できません（ブロック: {block.name}）");
            }
        }
        else
        {
            if (block == null)
            {
                Debug.LogError("[GimmickController] ❌ブロックがnullです！");
            }
            else
            {
                Debug.LogWarning($"[GimmickController] ⚠️ブロックは既に追加済み: {block.name}");
            }
        }
    }

    /// <summary>
    /// すべてのブロックを一度に設定
    /// </summary>
    public void SetFixedBoxes(List<GameObject> blocks)
    {
        _fixedBoxes.Clear();
        foreach (GameObject block in blocks)
        {
            if (block != null)
            {
                _fixedBoxes.Add(block);
            }
        }

        // 設定時に現在のボタン状態に合わせる
        if (_button != null && _previousButtonState.HasValue)
        {
            foreach (GameObject block in _fixedBoxes)
            {
                if (block != null)
                {
                    block.SetActive(_previousButtonState.Value);
                }
            }
        }

        if (_showDebugLogs)
        {
            Debug.Log($"[GimmickController] 複数ブロック一括設定: {_fixedBoxes.Count}個");
        }
    }
}