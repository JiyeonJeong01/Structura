using UnityEngine;

// 리스트 UI의 공통 색상, 치수, 연출 시간을 한곳에서 관리한다.
// 씬 생성용 치수는 Builder 실행 시, 연출 값은 실행 중 UI에서 사용한다.
public static class DoublyLinkedListUISettings
{
    public const int Capacity = 5;
    public const int FirstLocation = 0;
    public const int LastLocation = 1;
    public const int OperationLocationCount = 3;
    public const int NoSelection = -1;

    public static readonly Color TextColor = new Color(0.87f, 0.93f, 0.98f); // 연한 하늘색
    public static readonly Color PanelColor = new Color(0.08f, 0.12f, 0.18f); // 짙은 남색
    public static readonly Color BackgroundColor = new Color(0.035f, 0.06f, 0.1f); // 검푸른색
    public static readonly Color ListPanelColor = new Color(0.055f, 0.085f, 0.13f); // 어두운 남색
    public static readonly Color SetupOverlayColor = new Color(0.01f, 0.02f, 0.04f, 0.96f); // 거의 불투명한 검푸른색
    public static readonly Color NodeColor = new Color(0.08f, 0.12f, 0.19f); // 짙은 남색
    public static readonly Color DividerColor = new Color(0.28f, 0.36f, 0.48f); // 회청색
    public static readonly Color ActiveColor = new Color(0.25f, 0.87f, 0.83f); // 청록색
    public static readonly Color LinkColor = new Color(0.4f, 0.5f, 0.64f); // 회청색
    public static readonly Color FoundColor = new Color(0.4f, 1f, 0.55f); // 연두색
    public static readonly Color SearchColor = new Color(1f, 0.78f, 0.28f); // 노란색
    public static readonly Color InvalidFeedbackColor = new Color(1f, 0.65f, 0.4f); // 주황색
    public static readonly Color FeedbackColor = new Color(0.55f, 0.87f, 0.81f); // 연한 청록색
    public static readonly Color MissOverlayColor = new Color(1f, 0.1f, 0.15f, 0f); // 빨간색, 평상시에는 투명

    // 좌표와 크기는 Canvas 기준 픽셀 단위다.
    public static readonly Vector2 ReferenceResolution = new Vector2(1600f, 900f);
    public static readonly Vector2 CenterPivot = new Vector2(0.5f, 0.5f);
    public static readonly Vector2 TopLeftAnchor = new Vector2(0f, 1f);
    public static readonly Vector2 LeftCenterPivot = new Vector2(0f, 0.5f);
    public static readonly Vector2 RightCenterPivot = new Vector2(1f, 0.5f);
    public static readonly Vector2 NodeSize = new Vector2(150f, 68f);
    public const float NodeCenterSpacing = 220f;
    public const float NodeRowY = -210f;
    public const float SentinelInset = 90f;
    public const float LinkLaneOffset = 10f;
    public const float LinkThickness = 3f;
    public const float HiddenLinkThreshold = 0.001f;
    public const float ArrowLength = 10f;
    public const float ArrowAngle = 35f;
    public const float ReverseLinkAngle = 180f;

    // 연출 시간은 초 단위이며 노드 이동과 알파 전환은 같은 시간에 끝난다.
    public const float NodeMoveDuration = 0.35f;
    public const float NodeVerticalTravel = 100f;
    public const float LinkDuration = 0.22f;
    public const float SelectedOutlineWidth = 3f;
    public const float PulseStartWidth = 2f;
    public const float PulsePeakWidth = 7f;
    public const float FoundPulseDuration = 0.3f;
    public const float SearchPulseDuration = 0.18f;
    public const int PulseLoopCount = 2; // 확대 후 원래 두께로 돌아오는 한 쌍
    public const float MissPeakAlpha = 0.22f;
    public const float MissFadeInDuration = 0.16f;
    public const float MissFadeOutDuration = 0.35f;

    public const int DefaultFontSize = 20;
    public const int DetailFontSize = 18;
    public const int TitleFontSize = 30;
    public const int PointerFontSize = 12;
    public const int NodeValueFontSize = 19;
    public const int EmptyFontSize = 22;
    public const int SearchFontSize = 24;
    public const float ButtonHeight = 46f;
    public const float ButtonTextPadding = 5f;
}
