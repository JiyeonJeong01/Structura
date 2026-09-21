using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 전체 흐름의 시작점이다. 
/// </summary>
public class HashTableController : MonoBehaviour
{
    public bool IsStarted { get; private set; }
    public int Capacity => _hashTable.Capacity;
    public int Count => _hashTable.Count;
    public bool IsBusy { get; private set; } // 연출 도중 다음 데이터 변경이 겹치지 않도록 잠근다.

    [SerializeField] private bool _printDebug = true;
    [SerializeField] private int _initialCapacity = 8;
    [SerializeField] private string _inputKey;
    [SerializeField] private string _inputValue;
    [SerializeField] private HashTableUIView _tableView;
    [SerializeField] private HashTableControls _controls;
    [ShowInInspector, ReadOnly] private string _debugStats;
    [ShowInInspector, ReadOnly] private string[] _debugBuckets;


    // 선택한 타입으로 파싱하여 저장한다. object로 보관해도 숫자의 비교와 해시 동작은 유지된다.
    private readonly HashTable<object, object> _hashTable = new HashTable<object, object>();
    private ValueType _keyType;
    private ValueType _valueType;

    private Tween _activeEffect; // 완료 대기와 중단 처리를 담당하는 현재 조작의 연출이다.


    private void Awake()
    {
        _controls.Connect(this);
        _controls.ShowSetupPanel(true);
    }

    [Button]
    public void Initialize()
    {
        if (IsBusy)
            return;

        // controls에서 설정한 key, value의 타입 
        _keyType = _controls.KeyType;
        _valueType = _controls.ValueType;

        _hashTable.Initialize(Mathf.Max(1, _initialCapacity));
        IsStarted = true;
        _controls.StartShowTable(_keyType, _valueType);

        RefreshView();
        Report("Ready. Insert a key and value.");
    }

    // 입력을 변환해 삽입 또는 값 갱신을 수행하고 결과에 맞는 연출을 재생한다.
    [Button]
    public void Add()
    {
        if (!IsStarted || IsBusy) 
            return;

        _inputKey = _controls.KeyInput;
        _inputValue = _controls.ValueInput;

        if (!Parse(_inputKey, _keyType, "Key", out object key)
            || !Parse(_inputValue, _valueType, "Value", out object value)) 
            return;

        bool existed = _hashTable.Find(key, out _);

        _hashTable.Insert(key, value);

        RefreshView();
        Report(existed ? "Existing key: value updated." : "Entry inserted.");
        PlayEffect(_tableView.AnimateInsert(key, !existed));
    }

    [Button]
    public void Find()
    {
        Find(_controls.KeyInput);
    }

    // 키를 변환해 검색하고 성공한 엔트리만 강조 연출한다.
    public bool Find(string keyText)
    {
        if (!IsStarted || IsBusy) 
            return false;

        _tableView.Highlight(null);

        if (!Parse(keyText, _keyType, "Key", out object key))
            return false;

        bool found = _hashTable.Find(key, out object value);
        _tableView.Highlight(found ? key : null);

        Report(found ? $"Found {HashValueParser.Format(key)} : {HashValueParser.Format(value)}" : "Key not found.");

        if (found)
            PlayEffect(_tableView.AnimateEntry(key, false));
        return found;
    }

    [Button]
    public void Remove()
    {
        Remove(_controls.KeyInput);
    }

    // 데이터에서 키를 제거하되 화면의 엔트리는 삭제 연출이 끝난 뒤 정리한다.
    public bool Remove(string keyText)
    {
        if (!IsStarted || IsBusy || !Parse(keyText, _keyType, "Key", out object key)) 
            return false;

        bool removed = _hashTable.Remove(key);
        Report(removed ? "Entry removed." : "Key not found.");

        if (removed) 
            PlayEffect(_tableView.AnimateEntry(key, true), true);

        return removed;
    }

    [Button]
    public void Clear()
    {
        if (!IsStarted || IsBusy) 
            return;

        _hashTable.Clear();

        Report("All entries cleared. Heads retained.");
        PlayEffect(_tableView.AnimateClear(), true);
    }

    // 조작을 잠그고 연출 완료를 기다리며 필요하면 종료 후 UI를 데이터와 동기화한다.
    private void PlayEffect(Tween effect, bool refreshAfter = false)
    {
        // 연출할 대상이 없어 Tween을 만들지 못한 경우
        if (effect == null)
        {
            if (refreshAfter) 
                RefreshView();
            return;
        }

        _activeEffect = effect;

        IsBusy = true;
        _controls.SetBusy(true);
        StartCoroutine(FinishEffect(refreshAfter));
    }

    // 삭제 연출이 끝나면 슬롯을 정리하고 조작 잠금을 해제한다.
    private IEnumerator FinishEffect(bool refreshAfter)
    {
        yield return _activeEffect.WaitForCompletion();

        _activeEffect = null;
        if (refreshAfter)
            RefreshView();

        IsBusy = false;
        _controls.SetBusy(false);
    }

    // 비활성화로 연출이 끊겨도 이동 중인 UI와 조작 잠금이 남지 않도록 정리한다.
    private void OnDisable()
    {
        StopAllCoroutines();
        if (_activeEffect != null && _activeEffect.IsActive()) 
            _activeEffect.Kill();

        _activeEffect = null;
        if (_tableView != null) 
            _tableView.StopEffects();

        IsBusy = false;
        if (_controls != null)
            _controls.SetBusy(false);
    }

    private void OnEnable()
    {
        // 삭제 데이터는 반영됐지만 UI 갱신 전에 중단됐을 수 있으므로 다시 동기화한다.
        if (IsStarted) 
            RefreshView();
    }

    // 공통 파서를 사용하고 변환 실패는 예외 대신 입력 안내로 전달한다.
    private bool Parse(string text, ValueType type, string label, out object value)
    {
        if (HashValueParser.TryParse(text, type, out value)) 
            return true;

        Report($"{label}: enter a valid {type.ToString().ToLowerInvariant()} (decimal separator: .).", true);
        return false;
    }

    // 실제 테이블 상태를 기준으로 버킷 UI와 디버그 통계를 함께 갱신한다.
    private void RefreshView()
    {
        _tableView.Refresh(_hashTable.GetBuckets());
        _debugStats = $"Count: {Count}    Capacity: {Capacity}    Load: {_hashTable.LoadFactor:0.00}";
        _debugBuckets = _hashTable.GetDebugBuckets();
        _controls.SetStats(_debugStats);
    }

    // 조작 결과를 UI에 전달하고 설정에 따라 Console에도 출력한다.
    private void Report(string message, bool invalid = false)
    {
        _controls.SetFeedback(message, invalid);
        if (_printDebug) 
            Debug.Log(message);
    }
}
