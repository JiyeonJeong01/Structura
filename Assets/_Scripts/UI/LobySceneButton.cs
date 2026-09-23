using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>로비 버튼의 강조, 개별 미리보기 전환과 씬 이동을 담당한다.</summary>
[RequireComponent(typeof(Button))]
public class LobySceneButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string _sceneName;
    [SerializeField] private RectTransform _visual;
    [SerializeField] private RectTransform _preview;
    [SerializeField] private CanvasGroup _previewGroup;
    [SerializeField] private Image _previewImage;
    [SerializeField, Min(1f)] private float _hoverScale = 1.06f;
    [SerializeField, Min(0.01f)] private float _duration = 0.24f;
    [SerializeField, Min(0f)] private float _slideDistance = 100f;

    private Button _button;
    private Vector3 _baseScale;
    private Vector2 _shownPosition;
    private float _progress;
    private bool _hovered;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _baseScale = _visual.localScale;
        _shownPosition = _preview.anchoredPosition;
        _previewGroup.blocksRaycasts = false;
        _previewGroup.interactable = false;
        _button.onClick.AddListener(LoadScene);
        ApplyPose();
    }

    private void Update()
    {
        // 방향이 바뀌어도 현재 진행도에서 이어가며, 일시정지 상태에서도 UI는 반응한다.
        float target = _hovered && _button.IsInteractable() ? 1f : 0f;
        _progress = Mathf.MoveTowards(_progress, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, _duration));
        ApplyPose();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        // 새 미리보기가 사라지는 이전 미리보기보다 앞에 나타나게 한다.
        _preview.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
    }

    private void ApplyPose()
    {
        // Sprite가 없으면 흰 사각형 대신 안내 문구를 보여준다. Play 중 이미지 교체도 반영한다.
        _previewImage.enabled = _previewImage.sprite != null;
        float eased = Mathf.SmoothStep(0f, 1f, _progress);
        // 입력 영역은 고정하고 자식 비주얼만 확대해 경계에서 hover가 반복되는 것을 막는다.
        _visual.localScale = _baseScale * Mathf.Lerp(1f, _hoverScale, eased);
        _preview.anchoredPosition = _shownPosition + Vector2.right * (_slideDistance * (1f - eased));
        _previewGroup.alpha = eased;
    }

    private void OnDisable()
    {
        _hovered = false;
        _progress = 0f;
        if (_button != null)
            ApplyPose();
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(LoadScene);
    }

    private void LoadScene()
    {
        if (!Application.CanStreamedLevelBeLoaded(_sceneName))
        {
            Debug.LogError($"Scene '{_sceneName}' is missing from the active build scene list.", this);
            return;
        }

        SceneManager.LoadScene(_sceneName);
    }
}
