using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public enum YutResult { BackDo, Do, Gae, Geol, Yut, Mo, Nak }

public enum GameState { Start, Playing, Cleared, GameOver }

// 게임 총괄 — 게임 시작/종료, 전체 상태 전환, 시스템 간 실행 순서 조정,
// 투척부터 점수 정산까지의 흐름 제어, Unity Button OnClick 진입점 유지
// 실제 로직은 책임별 매니저에 위임한다 (모두 일반 C# 클래스 — Inspector 연결 영향 없음):
//   YutRollManager(윷 추첨·확률·스탯) / BoardManager(말판·말 이동) / ScoreManager(점수·저주)
//   BossManager(보스·관문) / BellManager(방울 이벤트) / GameUIManager(UI 표시·연출)
//   TalismanManager(부적 카드 — 뽑기 덱·손패·선택·사용·세션 효과)
public class YutGameManager : MonoBehaviour
{
    [Header("Data Assets — 설정 데이터 에셋 (단일 기준)")]
    // 밸런스·카드·보스 정의는 이 에셋만 편집한다 — Scene에 목록을 중복 저장하지 않는다.
    // 누락 시 게임 진입이 차단된다 (예전 Scene 저장 목록으로 조용히 대체하지 않음)
    public TalismanDatabase talismanDatabase;      // 부적 카드 정의 — Assets/ScriptableObjects/TalismanDatabase
    public BossDatabase bossDatabase;              // 보스 순서·이름·HP·능력 — Assets/ScriptableObjects/BossDatabase
    public GameBalanceConfig balanceConfig;        // 투척 횟수·윷 배율·보스 이동 범위 — Assets/ScriptableObjects/GameBalanceConfig
    public PresentationConfig presentationConfig;  // 연출 딜레이 — Assets/ScriptableObjects/PresentationConfig
    public BellDatabase bellDatabase;              // 방울 정의 20종 — Assets/ScriptableObjects/BellDatabase
    public ShopConfig shopConfig;                  // 시작 자금·장착 한도·상점 상품 수 — Assets/ScriptableObjects/ShopConfig
    public FortuneUpgradeDatabase fortuneDatabase; // 복주머니 점괘 강화 정의 — Assets/ScriptableObjects/FortuneUpgradeDatabase

    // 설정 에셋이 모두 연결됐는지 — Awake에서 경고, 게임 진입 경로에서 차단
    internal bool ConfigReady =>
        talismanDatabase != null && bossDatabase != null &&
        balanceConfig != null && presentationConfig != null;

    // ── UI 참조 구조 (2026-10-28 화면별 분리) ──────────────────────────────
    // 화면·팝업별 UI 연결의 기준은 아래 컴포넌트 — 사용자는 각 화면 오브젝트의
    // 컴포넌트 Inspector에서 Text·Image·Button·슬롯을 연결한다.
    // 아래 legacy* 필드는 기존 Scene 연결 보존용이다. 읽기 경로는 프로퍼티 하나 —
    // 컴포넌트에 연결이 있으면 컴포넌트 값이 우선하고, 컴포넌트 또는 해당 필드가
    // 비어 있을 때만 레거시 값을 읽는다 (배열은 1개 이상 연결 시 배열 전체가 컴포넌트 소유).
    // 참조 복사·컴포넌트 자동 부착은 메뉴 'YUTROLL/UI 참조 이전 (열린 씬)' 사용
    // (Assets/Scripts/Editor/UITransferTool.cs). 레거시 필드는 이전 완료 후 비운다.

    [Header("UI 컴포넌트 — 화면별 연결의 기준 (비어 있으면 레거시 필드 사용)")]
    public StartScreenUI startScreenUI;            // 시작 화면 (startPanel 부착 권장)
    public BossSelectScreenUI bossSelectScreenUI;  // 보스 소개 화면 (bossSelectPanel 부착 권장)
    public GameScreenUI gameScreenUI;              // 게임 화면 최상위 (gamePanel 부착 권장)
    public ClearScreenUI clearScreenUI;            // 클리어 화면 (clearPanel 부착 권장)
    public ShopScreenUI shopScreenUI;              // 상점 화면 (shopPanel 부착 권장)
    public GameOverScreenUI gameOverScreenUI;      // 게임오버 화면 (gameOverPanel 부착 권장)
    public YutThrowUI yutThrowUI;                  // 윷 투척 UI (gamePanel 부착 권장)
    public ScoreUI scoreUI;                        // 점수·능력치 UI (gamePanel 부착 권장)
    public BossCombatUI bossCombatUI;              // 전투 중 보스 정보 (gamePanel 부착 권장)
    public BoardUI boardUI;                        // 말판 UI (gamePanel 또는 boardPanel 부착 권장)
    public TalismanHandUI talismanHandUI;          // 부적 손패 UI (gamePanel 부착 권장)
    public EquippedBellUI equippedBellUI;          // 장착 방울 표시 (gamePanel 부착 권장)
    public TalismanChoiceUI talismanChoiceUI;      // 부적 선택창 (talismanChoicePanel 부착 권장)
    public MoveAdjustUI moveAdjustUI;              // 환위부 선택창 (moveAdjustPanel 부착 권장)
    public FortuneChoiceUI fortuneChoiceUI;        // 점괘 선택창 (fortuneChoicePanel 부착 권장)
    public UsedTalismanUI usedTalismanUI;          // 사용 부적 목록 (usedTalismanPanel 부착 권장)

    // ── UI 접근자 — 실제 동작의 기준은 이 프로퍼티 하나 (컴포넌트 우선·레거시 폴백) ──
    private static bool HasAny(Object[] arr)
    {
        if (arr == null) return false;
        foreach (Object e in arr)
        {
            if (e != null) return true;
        }
        return false;
    }

    // YutThrowUI — 윷 투척
    public Text mainResultText => yutThrowUI != null && yutThrowUI.mainResultText != null ? yutThrowUI.mainResultText : legacyMainResultText;
    public Image[] stickFaceImages => yutThrowUI != null && HasAny(yutThrowUI.stickFaceImages) ? yutThrowUI.stickFaceImages : legacyStickFaceImages;
    public Image[] flameImages => yutThrowUI != null && yutThrowUI.flameImages != null ? yutThrowUI.flameImages : new Image[0];
    public Sprite stickFrontSprite => yutThrowUI != null && yutThrowUI.stickFrontSprite != null ? yutThrowUI.stickFrontSprite : legacyStickFrontSprite;
    public Sprite stickBackSprite => yutThrowUI != null && yutThrowUI.stickBackSprite != null ? yutThrowUI.stickBackSprite : legacyStickBackSprite;
    public Button[] rerollButtons => yutThrowUI != null && HasAny(yutThrowUI.rerollButtons) ? yutThrowUI.rerollButtons : legacyRerollButtons;
    public Text throwStatusText => yutThrowUI != null && yutThrowUI.throwStatusText != null ? yutThrowUI.throwStatusText : legacyThrowStatusText;
    public Text rerollRemainText => yutThrowUI != null && yutThrowUI.rerollRemainText != null ? yutThrowUI.rerollRemainText : legacyRerollRemainText;
    public Button rollButton => yutThrowUI != null && yutThrowUI.rollButton != null ? yutThrowUI.rollButton : legacyRollButton;
    public Button confirmThrowButton => yutThrowUI != null && yutThrowUI.confirmThrowButton != null ? yutThrowUI.confirmThrowButton : legacyConfirmThrowButton;

    // ScoreUI — 점수·능력치
    public Text basicScoreText => scoreUI != null && scoreUI.basicScoreText != null ? scoreUI.basicScoreText : legacyBasicScoreText;
    public Text multScoreText => scoreUI != null && scoreUI.multScoreText != null ? scoreUI.multScoreText : legacyMultScoreText;
    public Text totalMultText => scoreUI != null && scoreUI.totalMultText != null ? scoreUI.totalMultText : legacyTotalMultText;
    public Text totalScoreText => scoreUI != null && scoreUI.totalScoreText != null ? scoreUI.totalScoreText : legacyTotalScoreText;
    public Text remainText => scoreUI != null && scoreUI.remainText != null ? scoreUI.remainText : legacyRemainText;
    public Text throwDataText => scoreUI != null && scoreUI.throwDataText != null ? scoreUI.throwDataText : legacyThrowDataText;
    public Text throwScoreText => scoreUI != null && scoreUI.throwScoreText != null ? scoreUI.throwScoreText : legacyThrowScoreText;
    public Text currentYutStatsText => scoreUI != null && scoreUI.currentYutStatsText != null ? scoreUI.currentYutStatsText : legacyCurrentYutStatsText;
    public Text currentAttributeStatsText => scoreUI != null && scoreUI.currentAttributeStatsText != null ? scoreUI.currentAttributeStatsText : legacyCurrentAttributeStatsText;
    public Text multiplierReasonText => scoreUI != null && scoreUI.multiplierReasonText != null ? scoreUI.multiplierReasonText : legacyMultiplierReasonText;

    // BossCombatUI — 전투 중 보스 정보
    public Text bossNameText => bossCombatUI != null && bossCombatUI.bossNameText != null ? bossCombatUI.bossNameText : legacyBossNameText;
    public Text bossAbilityText => bossCombatUI != null && bossCombatUI.bossAbilityText != null ? bossCombatUI.bossAbilityText : legacyBossAbilityText;
    public Text bossCurrentHPText => bossCombatUI != null && bossCombatUI.bossCurrentHPText != null ? bossCombatUI.bossCurrentHPText : legacyBossCurrentHPText;
    public Text bossAffinityText => bossCombatUI != null && bossCombatUI.bossAffinityText != null ? bossCombatUI.bossAffinityText : legacyBossAffinityText;
    public Text goalScoreText => bossCombatUI != null && bossCombatUI.goalScoreText != null ? bossCombatUI.goalScoreText : legacyGoalScoreText;

    // 화면 6종 — Start·BossSelect·Game·Clear·Shop·GameOver
    public GameObject startPanel => startScreenUI != null && startScreenUI.startPanel != null ? startScreenUI.startPanel : legacyStartPanel;
    public GameObject bossSelectPanel => bossSelectScreenUI != null && bossSelectScreenUI.bossSelectPanel != null ? bossSelectScreenUI.bossSelectPanel : legacyBossSelectPanel;
    public Text bossSelectNameText => bossSelectScreenUI != null && bossSelectScreenUI.bossSelectNameText != null ? bossSelectScreenUI.bossSelectNameText : legacyBossSelectNameText;
    public Text bossSelectGoalText => bossSelectScreenUI != null && bossSelectScreenUI.bossSelectGoalText != null ? bossSelectScreenUI.bossSelectGoalText : legacyBossSelectGoalText;
    public Text bossSelectAbilityText => bossSelectScreenUI != null && bossSelectScreenUI.bossSelectAbilityText != null ? bossSelectScreenUI.bossSelectAbilityText : legacyBossSelectAbilityText;
    public Text bossSelectAffinityText => bossSelectScreenUI != null && bossSelectScreenUI.bossSelectAffinityText != null ? bossSelectScreenUI.bossSelectAffinityText : legacyBossSelectAffinityText;
    public GameObject skipBossButton => bossSelectScreenUI != null && bossSelectScreenUI.skipBossButton != null ? bossSelectScreenUI.skipBossButton : legacySkipBossButton;
    public GameObject gamePanel => gameScreenUI != null && gameScreenUI.gamePanel != null ? gameScreenUI.gamePanel : legacyGamePanel;
    public Text moneyText => gameScreenUI != null && gameScreenUI.moneyText != null ? gameScreenUI.moneyText : legacyMoneyText;
    public GameObject clearPanel => clearScreenUI != null && clearScreenUI.clearPanel != null ? clearScreenUI.clearPanel : legacyClearPanel;
    public Text clearBossNameText => clearScreenUI != null && clearScreenUI.clearBossNameText != null ? clearScreenUI.clearBossNameText : legacyClearBossNameText;
    public Text clearScoreText => clearScreenUI != null && clearScreenUI.clearScoreText != null ? clearScreenUI.clearScoreText : legacyClearScoreText;
    public Text clearMessageText => clearScreenUI != null && clearScreenUI.clearMessageText != null ? clearScreenUI.clearMessageText : legacyClearMessageText;
    public GameObject nextBossButton => clearScreenUI != null && clearScreenUI.nextBossButton != null ? clearScreenUI.nextBossButton : legacyNextBossButton;
    public GameObject shopPanel => shopScreenUI != null && shopScreenUI.shopPanel != null ? shopScreenUI.shopPanel : legacyShopPanel;
    public Text shopMoneyText => shopScreenUI != null && shopScreenUI.shopMoneyText != null ? shopScreenUI.shopMoneyText : legacyShopMoneyText;
    public BellSlotUI[] shopOfferSlots => shopScreenUI != null && HasAny(shopScreenUI.shopOfferSlots) ? shopScreenUI.shopOfferSlots : legacyShopOfferSlots;
    public BellSlotUI[] shopBellSlots => shopScreenUI != null && HasAny(shopScreenUI.shopBellSlots) ? shopScreenUI.shopBellSlots : legacyShopBellSlots;
    public Button refreshButton => shopScreenUI != null && shopScreenUI.refreshButton != null ? shopScreenUI.refreshButton : legacyRefreshButton;
    public Text refreshCostText => shopScreenUI != null && shopScreenUI.refreshCostText != null ? shopScreenUI.refreshCostText : legacyRefreshCostText;
    public GameObject gameOverPanel => gameOverScreenUI != null && gameOverScreenUI.gameOverPanel != null ? gameOverScreenUI.gameOverPanel : legacyGameOverPanel;
    public Text gameOverBossNameText => gameOverScreenUI != null && gameOverScreenUI.gameOverBossNameText != null ? gameOverScreenUI.gameOverBossNameText : legacyGameOverBossNameText;
    public Text gameOverScoreText => gameOverScreenUI != null && gameOverScreenUI.gameOverScoreText != null ? gameOverScreenUI.gameOverScoreText : legacyGameOverScoreText;
    public Text gameOverGoalText => gameOverScreenUI != null && gameOverScreenUI.gameOverGoalText != null ? gameOverScreenUI.gameOverGoalText : legacyGameOverGoalText;

    // BoardUI — 말판
    public GameObject boardPanel => boardUI != null && boardUI.boardPanel != null ? boardUI.boardPanel : legacyBoardPanel;
    public GameObject[] boardTiles => boardUI != null && HasAny(boardUI.boardTiles) ? boardUI.boardTiles : legacyBoardTiles;
    public RectTransform playerPiece => boardUI != null && boardUI.playerPiece != null ? boardUI.playerPiece : legacyPlayerPiece;
    public RectTransform bossPiece => boardUI != null && boardUI.bossPiece != null ? boardUI.bossPiece : legacyBossPiece;
    public RectTransform bossMovePreview => boardUI != null && boardUI.bossMovePreview != null ? boardUI.bossMovePreview : legacyBossMovePreview;
    public Text lapCountText => boardUI != null && boardUI.lapCountText != null ? boardUI.lapCountText : legacyLapCountText;
    public Text landingTileText => boardUI != null && boardUI.landingTileText != null ? boardUI.landingTileText : legacyLandingTileText;

    // TalismanHandUI / EquippedBellUI — 손패·장착 방울
    public Text talismanNoticeText => talismanHandUI != null && talismanHandUI.talismanNoticeText != null ? talismanHandUI.talismanNoticeText : legacyTalismanNoticeText;
    public BellSlotUI[] equipBellSlots => equippedBellUI != null && HasAny(equippedBellUI.equipBellSlots) ? equippedBellUI.equipBellSlots : legacyEquipBellSlots;

    // TalismanChoiceUI / MoveAdjustUI / FortuneChoiceUI / UsedTalismanUI — 팝업
    public GameObject talismanChoicePanel => talismanChoiceUI != null && talismanChoiceUI.talismanChoicePanel != null ? talismanChoiceUI.talismanChoicePanel : legacyTalismanChoicePanel;
    public Text[] talismanOptionNameTexts => talismanChoiceUI != null && HasAny(talismanChoiceUI.talismanOptionNameTexts) ? talismanChoiceUI.talismanOptionNameTexts : legacyTalismanOptionNameTexts;
    public Text[] talismanOptionDescTexts => talismanChoiceUI != null && HasAny(talismanChoiceUI.talismanOptionDescTexts) ? talismanChoiceUI.talismanOptionDescTexts : legacyTalismanOptionDescTexts;
    public Text[] talismanOptionRarityTexts => talismanChoiceUI != null && HasAny(talismanChoiceUI.talismanOptionRarityTexts) ? talismanChoiceUI.talismanOptionRarityTexts : legacyTalismanOptionRarityTexts;
    public Text talismanChoiceInfoText => talismanChoiceUI != null && talismanChoiceUI.talismanChoiceInfoText != null ? talismanChoiceUI.talismanChoiceInfoText : legacyTalismanChoiceInfoText;
    public Button talismanConfirmButton => talismanChoiceUI != null && talismanChoiceUI.talismanConfirmButton != null ? talismanChoiceUI.talismanConfirmButton : legacyTalismanConfirmButton;
    public GameObject moveAdjustPanel => moveAdjustUI != null && moveAdjustUI.moveAdjustPanel != null ? moveAdjustUI.moveAdjustPanel : legacyMoveAdjustPanel;
    public GameObject fortuneChoicePanel => fortuneChoiceUI != null && fortuneChoiceUI.fortuneChoicePanel != null ? fortuneChoiceUI.fortuneChoicePanel : legacyFortuneChoicePanel;
    public BellSlotUI[] fortuneOptionSlots => fortuneChoiceUI != null && HasAny(fortuneChoiceUI.fortuneOptionSlots) ? fortuneChoiceUI.fortuneOptionSlots : legacyFortuneOptionSlots;
    public Text fortuneChoiceText => fortuneChoiceUI != null && fortuneChoiceUI.fortuneChoiceText != null ? fortuneChoiceUI.fortuneChoiceText : legacyFortuneChoiceText;
    public GameObject usedTalismanPanel => usedTalismanUI != null && usedTalismanUI.usedTalismanPanel != null ? usedTalismanUI.usedTalismanPanel : legacyUsedTalismanPanel;
    public Transform usedTalismanContent => usedTalismanUI != null && usedTalismanUI.usedTalismanContent != null ? usedTalismanUI.usedTalismanContent : legacyUsedTalismanContent;
    public GameObject usedTalismanItemPrefab => usedTalismanUI != null && usedTalismanUI.usedTalismanItemPrefab != null ? usedTalismanUI.usedTalismanItemPrefab : legacyUsedTalismanItemPrefab;

    // 보스 체력은 별도 HP 필드를 두지 않고 goalScore - totalScore로 계산한다.
    // totalScoreText = 보스에게 준 누적 피해, goalScoreText = 보스 최대 체력.

    [Header("레거시 UI 연결 — 이전용 (컴포넌트 연결 후 비우기)")]
    [FormerlySerializedAs("mainResultText")]            public Text legacyMainResultText;
    [FormerlySerializedAs("basicScoreText")]            public Text legacyBasicScoreText;
    [FormerlySerializedAs("multScoreText")]             public Text legacyMultScoreText;
    [FormerlySerializedAs("totalMultText")]             public Text legacyTotalMultText;
    [FormerlySerializedAs("totalScoreText")]            public Text legacyTotalScoreText;
    [FormerlySerializedAs("goalScoreText")]             public Text legacyGoalScoreText;
    [FormerlySerializedAs("remainText")]                public Text legacyRemainText;
    [FormerlySerializedAs("throwDataText")]             public Text legacyThrowDataText;
    [FormerlySerializedAs("throwScoreText")]            public Text legacyThrowScoreText;
    [FormerlySerializedAs("bossNameText")]              public Text legacyBossNameText;
    [FormerlySerializedAs("bossAbilityText")]           public Text legacyBossAbilityText;
    [FormerlySerializedAs("bossCurrentHPText")]         public Text legacyBossCurrentHPText;
    [FormerlySerializedAs("bossAffinityText")]          public Text legacyBossAffinityText;
    [FormerlySerializedAs("startPanel")]                public GameObject legacyStartPanel;
    [FormerlySerializedAs("gamePanel")]                 public GameObject legacyGamePanel;
    [FormerlySerializedAs("clearPanel")]                public GameObject legacyClearPanel;
    [FormerlySerializedAs("gameOverPanel")]             public GameObject legacyGameOverPanel;
    [FormerlySerializedAs("bossSelectPanel")]           public GameObject legacyBossSelectPanel;
    [FormerlySerializedAs("clearBossNameText")]         public Text legacyClearBossNameText;
    [FormerlySerializedAs("clearScoreText")]            public Text legacyClearScoreText;
    [FormerlySerializedAs("clearMessageText")]          public Text legacyClearMessageText;
    [FormerlySerializedAs("gameOverBossNameText")]      public Text legacyGameOverBossNameText;
    [FormerlySerializedAs("gameOverScoreText")]         public Text legacyGameOverScoreText;
    [FormerlySerializedAs("gameOverGoalText")]          public Text legacyGameOverGoalText;
    [FormerlySerializedAs("bossSelectNameText")]        public Text legacyBossSelectNameText;
    [FormerlySerializedAs("bossSelectGoalText")]        public Text legacyBossSelectGoalText;
    [FormerlySerializedAs("bossSelectAbilityText")]     public Text legacyBossSelectAbilityText;
    [FormerlySerializedAs("bossSelectAffinityText")]    public Text legacyBossSelectAffinityText;
    [FormerlySerializedAs("skipBossButton")]            public GameObject legacySkipBossButton;
    [FormerlySerializedAs("boardPanel")]                public GameObject legacyBoardPanel;
    [FormerlySerializedAs("boardTiles")]                public GameObject[] legacyBoardTiles = new GameObject[30];
    [FormerlySerializedAs("playerPiece")]               public RectTransform legacyPlayerPiece;
    [FormerlySerializedAs("bossPiece")]                 public RectTransform legacyBossPiece;
    [FormerlySerializedAs("bossMovePreview")]           public RectTransform legacyBossMovePreview;
    [FormerlySerializedAs("lapCountText")]              public Text legacyLapCountText;
    [FormerlySerializedAs("landingTileText")]           public Text legacyLandingTileText;
    [FormerlySerializedAs("moneyText")]                 public Text legacyMoneyText;
    [FormerlySerializedAs("equipBellSlots")]            public BellSlotUI[] legacyEquipBellSlots = new BellSlotUI[4];
    [FormerlySerializedAs("shopPanel")]                 public GameObject legacyShopPanel;
    [FormerlySerializedAs("shopMoneyText")]             public Text legacyShopMoneyText;
    [FormerlySerializedAs("shopOfferSlots")]            public BellSlotUI[] legacyShopOfferSlots = new BellSlotUI[4];
    [FormerlySerializedAs("shopBellSlots")]             public BellSlotUI[] legacyShopBellSlots = new BellSlotUI[4];
    [FormerlySerializedAs("refreshButton")]             public Button legacyRefreshButton;
    [FormerlySerializedAs("refreshCostText")]           public Text legacyRefreshCostText;
    [FormerlySerializedAs("currentYutStatsText")]       public Text legacyCurrentYutStatsText;
    [FormerlySerializedAs("currentAttributeStatsText")] public Text legacyCurrentAttributeStatsText;
    [FormerlySerializedAs("talismanChoicePanel")]       public GameObject legacyTalismanChoicePanel;
    [FormerlySerializedAs("moveAdjustPanel")]           public GameObject legacyMoveAdjustPanel;
    [FormerlySerializedAs("talismanOptionNameTexts")]   public Text[] legacyTalismanOptionNameTexts = new Text[7];
    [FormerlySerializedAs("talismanOptionDescTexts")]   public Text[] legacyTalismanOptionDescTexts = new Text[7];
    [FormerlySerializedAs("talismanOptionRarityTexts")] public Text[] legacyTalismanOptionRarityTexts = new Text[7];
    [FormerlySerializedAs("talismanChoiceInfoText")]    public Text legacyTalismanChoiceInfoText;
    [FormerlySerializedAs("talismanConfirmButton")]     public Button legacyTalismanConfirmButton;
    [FormerlySerializedAs("talismanNoticeText")]        public Text legacyTalismanNoticeText;
    [FormerlySerializedAs("multiplierReasonText")]      public Text legacyMultiplierReasonText;
    [FormerlySerializedAs("usedTalismanPanel")]         public GameObject legacyUsedTalismanPanel;
    [FormerlySerializedAs("usedTalismanContent")]       public Transform legacyUsedTalismanContent;
    [FormerlySerializedAs("usedTalismanItemPrefab")]    public GameObject legacyUsedTalismanItemPrefab;
    [FormerlySerializedAs("fortuneChoicePanel")]        public GameObject legacyFortuneChoicePanel;
    [FormerlySerializedAs("fortuneOptionSlots")]        public BellSlotUI[] legacyFortuneOptionSlots = new BellSlotUI[3];
    [FormerlySerializedAs("fortuneChoiceText")]         public Text legacyFortuneChoiceText;
    [FormerlySerializedAs("stickFaceImages")]           public Image[] legacyStickFaceImages = new Image[4];
    [FormerlySerializedAs("stickFrontSprite")]          public Sprite legacyStickFrontSprite;
    [FormerlySerializedAs("stickBackSprite")]           public Sprite legacyStickBackSprite;
    [FormerlySerializedAs("rerollButtons")]             public Button[] legacyRerollButtons = new Button[4];
    [FormerlySerializedAs("throwStatusText")]           public Text legacyThrowStatusText;
    [FormerlySerializedAs("rerollRemainText")]          public Text legacyRerollRemainText;
    [FormerlySerializedAs("rollButton")]                public Button legacyRollButton;
    [FormerlySerializedAs("confirmThrowButton")]        public Button legacyConfirmThrowButton;
    [FormerlySerializedAs("nextBossButton")]            public GameObject legacyNextBossButton;

    // 말 이동 1칸당 딜레이(초) — 기준은 PresentationConfig 에셋
    public float moveStepDelay => presentationConfig != null ? presentationConfig.moveStepDelay : 0.15f;

    // 부적 카드 정의 — 기준은 TalismanDatabase 에셋(talismanDatabase.cards).
    // Scene에 목록을 저장하지 않는다 — 에셋 하나만 편집하면 모든 씬에 동일 적용.
    // 덱 규칙: 카드 1장씩, 사용 시 런에서 소멸. 레어도 배정 미확정 → 전부 '미정'
    public List<TalismanData> talismanPool => talismanDatabase != null ? talismanDatabase.cards : null;

    // 보스 목록 — 기준은 BossDatabase 에셋(bossDatabase.bosses). Scene에 목록을 저장하지 않는다
    public List<BossData> bosses => bossDatabase != null ? bossDatabase.bosses : null;
    public int selectedBossIndex = 0;   // 보스 선택 화면에서 선택한 보스 (0~4)

    [Header("Throw State")]
    // 기본 투척 횟수 — 기준은 GameBalanceConfig 에셋
    public int maxThrows => balanceConfig != null ? balanceConfig.maxThrows : 6;
    public int currentThrows = 0;
    public int goalScore = 1000;     // 현재 보스 목표 점수 = 보스 최대 HP

    [Header("Score — 이번 투척 표시용 임시값 (투척마다 초기화)")]
    public int basicScore = 0;      // 이번 투척 계산의 베이직 = 윷 기본점수 (효과 단계마다 갱신)
    public float multiplier = 0f;   // 이번 투척 윷 배율 — 성장 반영값 표시 (소수 가능). 전체배율은 totalMult에 분리
    public float totalMult = 0f;    // 이번 투척 전체배율 — 발동 효과 곱셈 단계마다 갱신 (소수 가능)
    public int throwScore = 0;    // 이번 투척 확정 점수
    public int totalScore = 0;    // 보스전 전체 누적 총점 (보스 HP와 대응 — 백도 차감 기준이기도 함)

    // 보스 말 이동 거리 — 기준은 GameBalanceConfig 에셋.
    // 보스 말 이동 — 기본은 고정 범위(4~7칸). useBossMoveDamageStages 활성 시에만 누적 피해 비율 단계 배열 사용 (구조 보존·비활성)
    public bool bossMoveUseDamageStages => balanceConfig != null && balanceConfig.useBossMoveDamageStages;
    public int bossMoveFixedMin => balanceConfig != null ? balanceConfig.bossMoveMin : 4;   // 고정 이동 최소 칸 (단계 미사용 시)
    public int bossMoveFixedMax => balanceConfig != null ? balanceConfig.bossMoveMax : 7;   // 고정 이동 최대 칸 (단계 미사용 시, 상한 포함)
    // 배열 길이 = 단계 수. 변경된 범위는 다음 투척 세션의 최초 던지기부터 적용. 새 보스전(피해 0)은 0단계
    public int[] bossMoveStageMin => balanceConfig != null ? balanceConfig.bossMoveStageMin : null;   // 단계별 이동 최소 칸 수
    public int[] bossMoveStageMax => balanceConfig != null ? balanceConfig.bossMoveStageMax : null;   // 단계별 이동 최대 칸 수 (최소~최대 정수 균등 추첨, 상한 포함)

    [Header("Boss / Curse (임시 테스트값)")]
    public int backDoPenalty = 30;   // 보스 1~5 백도 차감량 (미확정 수치 — 테스트값)

    [Header("Probabilities — 임시 테스트값 (합계 100% 유지)")]
    // 낙 추가로 개 37.5→32.5 조정분과 낙 5%는 테스트값 (확정 확률 아님)
    public List<YutProbabilityEntry> baseProbabilities = new List<YutProbabilityEntry>
    {
        new YutProbabilityEntry { result = YutResult.BackDo, percent = 6.25f },
        new YutProbabilityEntry { result = YutResult.Do,     percent = 18.75f },
        new YutProbabilityEntry { result = YutResult.Gae,    percent = 32.5f },
        new YutProbabilityEntry { result = YutResult.Geol,   percent = 25f },
        new YutProbabilityEntry { result = YutResult.Yut,    percent = 6.25f },
        new YutProbabilityEntry { result = YutResult.Mo,     percent = 6.25f },
        new YutProbabilityEntry { result = YutResult.Nak,    percent = 5f },
    };

    // 윷별 스탯 — 기준은 GameBalanceConfig 에셋(yutStats).
    // basicBonus는 현재 공격 공식(칸 공격점수 × 윷 배율)에 적용되지 않는다 — 향후 강화 효과용으로 예약된 필드
    public List<YutStatEntry> baseYutStats => balanceConfig != null ? balanceConfig.yutStats : null;

    // 정산 연출 단계 간 딜레이(초) — 기준은 PresentationConfig 에셋
    public float effectStepDelay => presentationConfig != null ? presentationConfig.effectStepDelay : 0.35f;

    // 30칸 원형 속성 말판 — 런당 1회 생성 후 게임오버까지 유지 (보스 전환·건너뛰기에서 재추첨 없음)
    public YutBoard board = new YutBoard();

    public GameState currentState = GameState.Start;

    // 최근 투척 정산 데이터 — 디버그 인스펙터 노출 + 향후 연출/UI 코드가 읽는다
    public ThrowSettlement currentSettlement;

    // ============ 책임별 매니저 (런타임 생성, 일반 C# 클래스) ============
    internal YutRollManager rollManager;     // 윷 추첨·확률·윷 스탯
    internal BoardManager boardManager;      // 말판·말 이동
    internal ScoreManager scoreManager;      // 점수·저주 정산
    internal BossManager bossManager;        // 보스·관문
    internal BellManager bellManager;        // 방울 장착·효과 (런 단위 장착 상태·성장·지속 효과)
    internal EconomyManager economyManager;  // 냥(화폐) — 시작 자금·구매·판매·수입
    internal ShopManager shopManager;        // 방울 상점 — 보스 소개 화면의 구매·판매
    internal TalismanManager talismanManager; // 부적 카드 — 뽑기 덱·손패·선택·사용·세션 효과
    internal TileEffectManager tileEffects;  // 속성 착지 효과 — 불×2·물 연속·풀 도트 (보스전 단위 상태)
    internal FortuneUpgradeManager fortuneManager;  // 복주머니 점괘 강화 — 획득 횟수·누적 강화·선택 대기 (런 단위 상태)
    internal GameUIManager uiManager;        // UI 표시·연출

    // 표시·연출용 런타임 상태 (매니저들이 접근)
    internal YutResult lastResult;
    internal bool hasRolled;
    internal int displayTotal;          // 화면에 보이는 총점 — 정산 연출의 '토탈 합산' 단계에서만 갱신
    internal bool isPresenting;
    internal Coroutine activePresentation;
    internal string landingTileInfo = "";   // 착지 칸 표시 문자열 — 연출이 말 이동 완료 시점에 채운다
    internal bool dotPaidThisTurn;      // 이번 차례 풀 도트 지급 여부 — 차례 준비 시 리셋, 정상 최초 던지기 접수 시 1회 지급

    // 일반 결과 5종 — 저주(백도·낙) 제외. 보스 특수능력의 비례 차감 대상 등에 사용
    internal static readonly YutResult[] NormalResults =
        { YutResult.Do, YutResult.Gae, YutResult.Geol, YutResult.Yut, YutResult.Mo };

    // 저주(Curse) 계열 판별 — 부적/방울/주술이 이 함수로 저주 결과를 구분한다
    public static bool IsCurse(YutResult result) =>
        result == YutResult.BackDo || result == YutResult.Nak;

    internal static string ToKorean(YutResult result) => result switch
    {
        YutResult.BackDo => "백도",
        YutResult.Do => "도",
        YutResult.Gae => "개",
        YutResult.Geol => "걸",
        YutResult.Yut => "윷",
        YutResult.Mo => "모",
        YutResult.Nak => "낙",
        _ => result.ToString(),
    };

    // ============ 버튼 OnClick 연결용 Public 함수 ============

    // '게임 시작' 버튼: 현재 진행 순서의 보스 소개 화면을 열고, '다음' 버튼(EnterBossBattle)에서 실제 시작
    public void StartGame()
    {
        if (currentState != GameState.Start)
        {
            return;
        }
        if (!ConfigReady)
        {
            Debug.LogError("설정 데이터 에셋 미연결 — 게임을 시작할 수 없습니다. Inspector에서 에셋 4종을 연결하세요.", this);
            return;
        }
        bossManager.ShowBossSelect(bossManager.pendingBossIndex);
    }

    // 보스 소개 화면의 '다음' 버튼 — 소개 중인 보스전을 시작하고 바로 투척 가능 상태로 진행
    public void EnterBossBattle()
    {
        if (currentState != GameState.Start && currentState != GameState.Cleared)
        {
            return;
        }
        if (shopManager.isOpen || fortuneManager.isChoosingFortune)
        {
            return;   // 상점 이용 중이거나 점괘 선택 대기 중 — 선택을 완료해야 보스전에 진입할 수 있다
        }
        if (!ConfigReady)
        {
            Debug.LogError("설정 데이터 에셋 미연결 — 보스전을 시작할 수 없습니다.", this);
            return;
        }
        if (bosses == null || bossManager.pendingBossIndex < 0 || bossManager.pendingBossIndex >= bosses.Count)
        {
            Debug.LogWarning($"EnterBossBattle: 잘못된 보스 인덱스 {bossManager.pendingBossIndex}");
            return;
        }
        selectedBossIndex = bossManager.pendingBossIndex;
        GameUIManager.SetPanel(bossSelectPanel, false);
        StartBossBattle();
    }

    // 보스 소개 화면의 '건너뛰기' 버튼 — 전투 없이 다음 보스 소개로 이동 (판별·제한은 BossManager)
    public void SkipBoss()
    {
        if (shopManager.isOpen || fortuneManager.isChoosingFortune)
        {
            return;   // 상점 이용 중이거나 점괘 선택 대기 중 — 선택을 완료해야 건너뛸 수 있다
        }
        bossManager.SkipBoss();
    }

    // 실제 보스전 시작 (소개 화면 '다음' 버튼에서 호출)
    private void StartBossBattle()
    {
        BeginBossFight();
        EnterPlaying();
        talismanManager.OpenStartPick();   // 매 보스전 시작 — 후보 7장 중 5장 선택 (선택 완료 전 최초 투척 차단)
    }

    // 클리어 화면의 '다음 관문' 버튼 — 다음 보스 소개 화면 표시 (전투 진입은 진입 버튼에서)
    public void GoToNextBoss()
    {
        if (currentState != GameState.Cleared || !bossManager.HasNextBoss)
        {
            return;   // 클리어 상태에서만, 다음 보스가 있을 때만
        }
        if (shopManager.isOpen || fortuneManager.isChoosingFortune)
        {
            return;   // 상점 이용 중이거나 점괘 선택 대기 중 — 미완료 거래·선택을 초기화하지 않는다
        }
        GameUIManager.SetPanel(clearPanel, false);
        bossManager.ShowBossSelect(selectedBossIndex + 1);
    }

    // 클리어·게임오버 화면의 '처음으로' 버튼 — 런 데이터를 전부 초기화하고 시작 화면으로 복귀
    public void RestartRun()
    {
        ResetRunState();
        GameUIManager.SetPanel(clearPanel, false);
        GameUIManager.SetPanel(gameOverPanel, false);
        GameUIManager.SetPanel(talismanChoicePanel, false);
        GameUIManager.SetPanel(moveAdjustPanel, false);
        GameUIManager.SetPanel(bossSelectPanel, false);
        GameUIManager.SetPanel(shopPanel, false);
        GameUIManager.SetPanel(gamePanel, false);
        GameUIManager.SetPanel(startPanel, true);
        currentState = GameState.Start;
        uiManager.UpdateUI();
    }

    // 차례 준비 — 새 세션 생성(미투척 상태) + 이전 투척의 '이번 투척' 부적 효과 해제.
    // 보스전 시작·정산 종료 후 다음 차례에서 한 번만 호출된다. 준비 단계에서는 부적 사용만 가능하다
    private void PrepareTurn()
    {
        rollManager.PrepareSession();
        talismanManager.ClearSessionEffects();
        talismanManager.OnTurnStart();           // 족쇄부·추적부 등 차례 지속 효과의 남은 턴 차감
        dotPaidThisTurn = false;             // 새 차례 — 풀 도트 지급 여부 리셋 (정상 최초 던지기에서 1회 지급)
        bossManager.PrepareBossMove();   // 다음 보스 이동 계획 — 거리를 1회 추첨해 예고 표시, 실제 이동은 최초 투척 시
    }

    // '윷 던지기' 버튼 — 준비된 세션의 윷 4개 최초 투척 (이동·득점은 '결과 확정'에서만 발생)
    public void RollYut()
    {
        if (currentState != GameState.Playing)
        {
            Debug.Log("윷을 던질 수 있는 상태가 아닙니다. (GameState.Playing 아님)");
            return;
        }
        if (isPresenting || talismanManager.isChoosing || talismanManager.isChoosingMoveAdjust)
        {
            return;   // 정산 연출·부적 선택창·환위부 선택창 표시 중 연타 방지 — 중복 투척/득점 불가
        }
        if (currentThrows >= maxThrows)
        {
            return;
        }
        if (rollManager.session == null)
        {
            PrepareTurn();   // 안전망 — 정상 흐름에서는 차례 준비에 이미 생성돼 있다
        }
        if (rollManager.session.rolled)
        {
            return;   // 이미 던진 세션 — 재투척 선택 또는 '결과 확정' 필요
        }
        // 풀 도트 — 정상 접수된 최초 던지기에서 차례당 1회, 투척·보스 이동 전에 지급.
        // 도트로 보스 처치 시 즉시 클리어 → 이번 투척 소모·보스 이동 없이 종료 (사용자 확정)
        if (!dotPaidThisTurn)
        {
            dotPaidThisTurn = true;
            ApplyTurnStartDot();
            if (currentState != GameState.Playing)
            {
                return;
            }
        }
        // 최초 투척 낙 판정 — 접수된 최초 투척에서만 1회, 일반 결과 5종 분포와 별개 추첨.
        // 방울·점괘·공령명·보스 낙 능력은 적용하지 않고 설정값(initialNakPercent)만 사용 (2026-10-27 확정).
        // 도트 처치 클리어는 위에서 이미 반환 — 여기 도달하면 전투가 계속 중이다
        float nakPct = balanceConfig != null ? balanceConfig.initialNakPercent : 1f;
        if (Random.value * 100f < nakPct)
        {
            rollManager.session.InitialRollNak();   // 낙 — 면 추첨 없이 결과 고정, 재투척 불가·확정만 가능
        }
        else
        {
            rollManager.session.InitialRoll();   // 면만 던진다 — 준비 단계 부적 효과는 세션에 보존
        }
        bossManager.MoveBossPiece();   // 최초 투척 접수 시 보스 말 1회 이동 (낙 포함 — 기존 최초 투척 1회 경로 유지, 재투척·확정에서는 이동 없음)
        hasRolled = false;   // 이전 확정 결과 표시 지우기 — 새 세션은 확정 전까지 결과 미표시
        uiManager.UpdateUI();
    }

    // 윷별 재투척 버튼(int): index 0~3 — 선택한 윷 하나만 다시 던진다
    public void RerollStick(int index)
    {
        if (currentState != GameState.Playing || isPresenting || talismanManager.isChoosing || talismanManager.isChoosingMoveAdjust)
        {
            return;
        }
        if (rollManager.session != null && rollManager.session.Reroll(index))
        {
            uiManager.UpdateUI();
        }
    }
    public void RerollStick1() => RerollStick(0);
    public void RerollStick2() => RerollStick(1);
    public void RerollStick3() => RerollStick(2);
    public void RerollStick4() => RerollStick(3);

    // '결과 확정' 버튼 — 확정된 결과로 말 이동·공격 정산 시작 (기본 투척 1회 소모는 여기서만)
    public void ConfirmThrow()
    {
        if (currentState != GameState.Playing || isPresenting || talismanManager.isChoosing || talismanManager.isChoosingMoveAdjust)
        {
            return;
        }
        YutThrowSession session = rollManager.session;
        if (session == null || !session.RollInProgress)
        {
            return;   // 확정할 세션 없음·아직 미투척·이미 확정 — 연타 차단
        }
        session.Confirm();

        ThrowSettlement settlement = SettleThrow(session.CurrentResult);
        currentSettlement = settlement;
        lastResult = settlement.result;
        hasRolled = true;

        activePresentation = StartCoroutine(uiManager.PresentSettlement(settlement));
    }

    // 연출 스킵 — 정산 데이터는 이미 확정되어 있으므로 스킵해도 같은 결과가 표시된다
    public void SkipPresentation()
    {
        if (!isPresenting || currentSettlement == null)
        {
            return;
        }
        if (activePresentation != null)
        {
            StopCoroutine(activePresentation);
        }
        FinishPresentation(currentSettlement);
    }

    // '상점' 버튼 — 보스 소개 화면에서 상점 진입 (첫 보스전 이전 Start 상태에서는 불가)
    public void OpenShop() => shopManager.OpenShop();

    // '새로고침' 버튼 — 상점 상품 전체를 비용을 내고 새로 구성 (비용 2냥부터 성공마다 2배)
    public void RefreshShop() => shopManager.RefreshShop();

    // '상점 닫기' 버튼 — 보스 소개 화면으로 복귀
    public void CloseShop() => shopManager.CloseShop();

    // 부적 후보 선택 토글(int): index 0~6 — 시작 7택5·땅 3택1 공용. 확정은 ConfirmTalismanChoice()
    public void SelectTalismanOption(int index) => talismanManager.SelectCandidate(index);
    public void SelectTalismanOption1() => SelectTalismanOption(0);
    public void SelectTalismanOption2() => SelectTalismanOption(1);
    public void SelectTalismanOption3() => SelectTalismanOption(2);
    public void SelectTalismanOption4() => SelectTalismanOption(3);
    public void SelectTalismanOption5() => SelectTalismanOption(4);
    public void SelectTalismanOption6() => SelectTalismanOption(5);
    public void SelectTalismanOption7() => SelectTalismanOption(6);

    // 부적 선택 확정 버튼 — 정해진 수를 골랐을 때만 손패에 추가하고 창 닫기
    public void ConfirmTalismanChoice() => talismanManager.ConfirmChoice();

    // 손패 부적 사용(int): index = 손패 슬롯 번호 — 최초 투척 후~확정 전에만 동작
    public void UseTalisman(int index) => talismanManager.UseTalisman(index);
    public void UseTalisman1() => UseTalisman(0);
    public void UseTalisman2() => UseTalisman(1);
    public void UseTalisman3() => UseTalisman(2);
    public void UseTalisman4() => UseTalisman(3);
    public void UseTalisman5() => UseTalisman(4);
    public void UseTalisman6() => UseTalisman(5);
    public void UseTalisman7() => UseTalisman(6);
    public void UseTalisman8() => UseTalisman(7);
    public void UseTalisman9() => UseTalisman(8);
    public void UseTalisman10() => UseTalisman(9);

    // 방울 슬롯 1칸 확장 — 테스트·OnClick 연결용 래퍼.
    // 확장 아이템(종류·수치·지속 범위)은 미확정 — 확정 시 이 경로에서 호출한다
    public void ExpandBellSlots() => bellManager.ExpandBellSlots(1);

    // 환위부 ±1 선택창 버튼 — +1/−1 보정 확정 또는 취소 (확정 전에는 카드 미소모)
    public void ChooseMoveAdjustPlus() => talismanManager.ChooseMoveAdjust(+1);
    public void ChooseMoveAdjustMinus() => talismanManager.ChooseMoveAdjust(-1);
    public void CancelMoveAdjust() => talismanManager.CancelMoveAdjust();

    // 보스 특수능력·방울·주술의 확률 조작용 API — runtimeWeights만 변경되고 baseProbabilities는 오염되지 않음
    public void ModifyProbability(YutResult result, float deltaPercent) =>
        rollManager.ModifyProbability(result, deltaPercent);

    // 런타임 확률 변경을 모두 되돌리고 기본 확률로 재구축 (보스 효과도 함께 제거됨)
    public void ResetProbabilities() => rollManager.ResetProbabilities();

    // 주술 등 영구 강화용 API — 다음 투척부터 적용. baseYutStats는 오염되지 않음
    public void UpgradeYutStat(YutResult result, int newBasicBonus, float newMult) =>
        rollManager.UpgradeYutStat(result, newBasicBonus, newMult);

    // ============ 내부 로직 (게임 흐름 조정) ============

    private void Awake()
    {
        // 매니저 생성 — 참조 보관 순서 (roll → board/score → boss/bell → ui)
        rollManager = new YutRollManager(this);
        boardManager = new BoardManager(this);
        scoreManager = new ScoreManager(this);
        bossManager = new BossManager(this);
        bellManager = new BellManager(this);
        economyManager = new EconomyManager(this);
        shopManager = new ShopManager(this);
        talismanManager = new TalismanManager(this);
        tileEffects = new TileEffectManager(this);
        fortuneManager = new FortuneUpgradeManager(this);
        uiManager = new GameUIManager(this);

        EnsureUIComponents();   // 화면별 UI 컴포넌트 자동 확보 — 에디터 이전 도구 미실행 씬에서도 표시 갱신이 동작한다

        currentState = GameState.Start;
        ValidateDataAssets();
        rollManager.BuildWeights();
        rollManager.BuildYutStats();
        if (bossManager.CurrentBoss != null)
        {
            goalScore = bossManager.CurrentBoss.maxHP;   // 선택 화면 미리보기
        }
        WarnIfPanelNested();
        if (bossMovePreview != null)
        {
            // 예고 표식은 버튼 입력을 가리지 않게 Raycast를 끄고 시작 시 숨겨 둔다
            var previewImage = bossMovePreview.GetComponent<UnityEngine.UI.Image>();
            if (previewImage != null)
            {
                previewImage.raycastTarget = false;
            }
            bossMovePreview.gameObject.SetActive(false);
        }
        economyManager.ResetForNewRun();   // 첫 런 시작 자금 지급 (이후 지급은 RestartRun→ResetRunState에서만)
        GameUIManager.SetPanel(startPanel, true);
        GameUIManager.SetPanel(gamePanel, false);
        GameUIManager.SetPanel(clearPanel, false);
        GameUIManager.SetPanel(gameOverPanel, false);
        GameUIManager.SetPanel(bossSelectPanel, false);
    }

    // 화면별 UI 컴포넌트 확보 — 매니저 필드가 비어 있으면 해당 패널 오브젝트(없으면 이 오브젝트)에서
    // 찾거나 실행 중 부착한다. 실행 중 부착은 씬 파일을 건드리지 않고, 컴포넌트 필드가 비어 있으면
    // 접근자가 레거시 연결을 읽으므로 기존 연결만으로도 표시가 유지된다.
    // 씬에 영구 반영하려면 에디터 메뉴 'YUTROLL/UI 참조 이전 (열린 씬)' 사용.
    private void EnsureUIComponents()
    {
        EnsureComp(ref startScreenUI, startPanel);
        EnsureComp(ref bossSelectScreenUI, bossSelectPanel);
        EnsureComp(ref gameScreenUI, gamePanel);
        EnsureComp(ref clearScreenUI, clearPanel);
        EnsureComp(ref shopScreenUI, shopPanel);
        EnsureComp(ref gameOverScreenUI, gameOverPanel);
        EnsureComp(ref yutThrowUI, gamePanel);
        EnsureComp(ref scoreUI, gamePanel);
        EnsureComp(ref bossCombatUI, gamePanel);
        EnsureComp(ref boardUI, boardPanel != null ? boardPanel : gamePanel);
        EnsureComp(ref talismanHandUI, gamePanel);
        EnsureComp(ref equippedBellUI, gamePanel);
        EnsureComp(ref talismanChoiceUI, talismanChoicePanel);
        EnsureComp(ref moveAdjustUI, moveAdjustPanel);
        EnsureComp(ref fortuneChoiceUI, fortuneChoicePanel);
        EnsureComp(ref usedTalismanUI, usedTalismanPanel);
    }

    private void EnsureComp<T>(ref T field, GameObject host) where T : Component
    {
        if (field != null) return;
        GameObject h = host != null ? host : gameObject;
        field = h.GetComponent<T>();
        if (field == null) field = h.AddComponent<T>();
    }

    // 설정 에셋 연결·내용 검증 — Awake에서 1회 실행. 누락/오류는 로그로 알리고
    // 실제 진입 차단은 ConfigReady를 확인하는 게임 시작 경로가 담당한다
    private void ValidateDataAssets()
    {
        if (!ConfigReady)
        {
            Debug.LogError(
                $"설정 데이터 에셋이 연결되지 않았습니다 — Gamemanager Inspector에서 연결 필요: " +
                $"{(talismanDatabase == null ? "talismanDatabase " : "")}" +
                $"{(bossDatabase == null ? "bossDatabase " : "")}" +
                $"{(balanceConfig == null ? "balanceConfig " : "")}" +
                $"{(presentationConfig == null ? "presentationConfig" : "")}", this);
            return;
        }
        if (bossDatabase.bosses == null || bossDatabase.bosses.Count == 0)
        {
            Debug.LogWarning("BossDatabase에 보스가 없습니다 — 보스전을 시작할 수 없습니다.", bossDatabase);
        }
        if (talismanDatabase.cards == null || talismanDatabase.cards.Count == 0)
        {
            Debug.LogWarning("TalismanDatabase에 카드가 없습니다 — 부적 선택이 생략됩니다.", talismanDatabase);
        }
        if (balanceConfig.maxThrows <= 0)
        {
            Debug.LogWarning("GameBalanceConfig.maxThrows가 0 이하입니다 — 투척할 수 없습니다.", balanceConfig);
        }
        if (balanceConfig.useBossMoveDamageStages)
        {
            if (balanceConfig.bossMoveStageMin == null || balanceConfig.bossMoveStageMax == null ||
                balanceConfig.bossMoveStageMin.Length != balanceConfig.bossMoveStageMax.Length)
            {
                Debug.LogWarning("GameBalanceConfig의 보스 이동 단계 배열이 비어 있거나 길이가 맞지 않습니다 — 단계 모드에서 보스 말 이동이 차단됩니다.", balanceConfig);
            }
        }
        else if (balanceConfig.bossMoveMin > balanceConfig.bossMoveMax || balanceConfig.bossMoveMin < 1)
        {
            Debug.LogWarning("GameBalanceConfig의 고정 이동 범위(bossMoveMin/bossMoveMax)가 올바르지 않습니다 — 추첨 시 보정됩니다.", balanceConfig);
        }
        if (bellDatabase == null)
        {
            Debug.LogWarning("BellDatabase 에셋 미연결 — 상점에 판매할 방울이 없습니다.", this);
        }
        if (shopConfig == null)
        {
            Debug.LogWarning("ShopConfig 에셋 미연결 — 시작 자금 50·장착 4·상품 4 기본값으로 동작합니다.", this);
        }
        // 점괘 강화 — 활성인데 적용 가능하지 않은 정의를 미리 경고한다
        // (미구현 확률 종류·백도/낙 대상 윷 배율·불 외 속성 배율 — 후보에서 자동 제외되지만 설정 실수를 알린다)
        if (fortuneDatabase != null && fortuneDatabase.upgrades != null)
        {
            foreach (FortuneUpgradeData u in fortuneDatabase.upgrades)
            {
                if (u != null && u.effectEnabled && !FortuneUpgradeManager.IsSupported(u))
                {
                    Debug.LogWarning($"FortuneUpgrade '{u.upgradeId}'가 활성이지만 적용 가능한 효과·대상이 아닙니다 — 점괘 후보에서 제외됩니다. kind·targetResult·targetAttribute를 확인하세요.", fortuneDatabase);
                }
            }
            int supported = fortuneManager != null ? fortuneManager.SupportedCandidateCount() : 0;
            int need = shopConfig != null ? shopConfig.fortuneCandidateCount : 3;
            if (supported > 0 && supported < need)
            {
                Debug.LogWarning($"점괘 후보 부족 — 활성·적용 가능 강화가 {supported}개로 {need}택1을 구성할 수 없습니다. 복주머니 구매가 거부됩니다.", fortuneDatabase);
            }
        }
    }

    // 보스전 시작: 선택 보스 적용 + 전투 상태 초기화 + 보스 특수능력 적용
    private void BeginBossFight()
    {
        StopPresentation();
        BossData boss = bossManager.CurrentBoss;
        goalScore = boss != null ? boss.maxHP : 0;
        currentThrows = 0;
        totalScore = 0;
        displayTotal = 0;
        basicScore = 0;
        multiplier = 0;
        totalMult = 0;
        throwScore = 0;
        hasRolled = false;
        currentSettlement = null;
        bossManager.pendingMoveSeal = false;            // 미소비 봉쇄부는 보스전을 넘기지 않는다 (미확정 임시 규칙)
        talismanManager.ResetForBoss();                 // 부적 덱·손패는 보스전 단위 — 이전 손패·효과 정리 후 새 덱 구성 예약
        landingTileInfo = "";
        bossManager.ResetBossPiece();                   // 모든 보스전 시작 시 보스 말을 START로 초기화 + 이전 이동 계획 폐기
        boardManager.SetupBoard();                      // 배치는 런당 1회 생성·유지, 말 위치·이동 진행도만 초기화 — 누적 완주는 런 단위 유지
        boardManager.MovePieceVisual(board.pieceIndex); // 말을 시작점(START) 위치에 배치
        PrepareTurn();                                  // 이전 투척 세션·부적 효과 정리 + 첫 차례 준비 — 보스 말 초기화 후 이동 계획을 세운다
        economyManager.ResetClearReward();              // 이번 보스의 클리어 보상 지급 여부 초기화 — 보스전당 1회 지급
        tileEffects.ResetForBoss();                     // 물 연속·풀 도트는 보스전 단위 — 보스 전환 시 제거
        rollManager.BuildWeights();                     // 기본 확률로 재구축 → 보스 효과 중복 누적 방지
        // 보스 낙 확률 능력은 신규 윷 4개 면 조합 방식에 적용 불가 — 적용 호출을 하지 않는다 (미확정 사항 보고)
        if (boss != null && boss.nakBonusPercent > 0f)
        {
            Debug.Log($"참고: {boss.bossName}의 '낙 확률 +{boss.nakBonusPercent}%p' 능력은 신규 투척 방식에서 발동하지 않습니다 (미확정)");
        }
        Debug.Log($"--- 보스전 시작: {boss?.bossName} | 목표 {goalScore} | {maxThrows}회 ---");
    }

    // 본 게임 진입 — 보스전 시작 시 패널 전환 처리
    private void EnterPlaying()
    {
        GameUIManager.SetPanel(gamePanel, true);
        GameUIManager.SetPanel(startPanel, false);
        GameUIManager.SetPanel(clearPanel, false);
        GameUIManager.SetPanel(gameOverPanel, false);
        GameUIManager.SetPanel(bossSelectPanel, false);
        GameUIManager.SetPanel(shopPanel, false);
        GameUIManager.SetPanel(talismanChoicePanel, false);
        GameUIManager.SetPanel(moveAdjustPanel, false);
        hasRolled = false;
        currentState = GameState.Playing;
        uiManager.UpdateUI();
    }

    // 보스 클리어: TotalScore >= GoalScore
    private void EnterClear()
    {
        economyManager.GrantBossClearReward();   // 클리어 보상 — 기본+남은 투척 보상, 보스전당 1회 (게임오버·건너뛰기는 이 경로 미도달)
        fortuneManager.OnBossCleared();          // 적용 대기 상점 할인 활성화 — 다음 상점부터 할인가 적용 (건너뛰기·재진입으로는 활성화 안 됨)
        currentState = GameState.Cleared;
        GameUIManager.SetPanel(gameOverPanel, false);            // 클리어/게임오버 동시 표시 방지
        GameUIManager.SetPanel(talismanChoicePanel, false);
        GameUIManager.SetPanel(moveAdjustPanel, false);
        GameUIManager.ActivatePanel(clearPanel, nameof(clearPanel));
        // 다음 관문 버튼: 마지막 보스 클리어(최종 클리어)면 숨기고 메시지 표시
        GameUIManager.SetPanel(nextBossButton, !bossManager.IsFinalClear);
        if (clearMessageText != null)
        {
            clearMessageText.text = bossManager.IsFinalClear ? "최종 클리어" : "";
        }
        uiManager.UpdateUI();
        BossData boss = bossManager.CurrentBoss;
        Debug.Log($"--- 보스 클리어! {boss?.bossName} | 최종 점수 {totalScore} / 목표 {goalScore}{(bossManager.IsFinalClear ? " | 최종 클리어" : "")} ---");
    }

    // 게임오버: 기본 투척 소진 후에도 목표 미달
    private void EnterGameOver()
    {
        currentState = GameState.GameOver;
        GameUIManager.SetPanel(clearPanel, false);
        GameUIManager.SetPanel(talismanChoicePanel, false);
        GameUIManager.SetPanel(moveAdjustPanel, false);
        GameUIManager.SetPanel(shopPanel, false);
        GameUIManager.ActivatePanel(gameOverPanel, nameof(gameOverPanel));
        uiManager.UpdateUI();
        BossData boss = bossManager.CurrentBoss;
        Debug.Log($"--- 게임 오버. {boss?.bossName} | 최종 점수 {totalScore} / 목표 {goalScore} ---");
    }

    private void StopPresentation()
    {
        if (activePresentation != null)
        {
            StopCoroutine(activePresentation);
            activePresentation = null;
        }
        isPresenting = false;
    }

    // 확정 투척 1회 정산: 확정 결과(윷 4개 면 조합·낙) → 정산 계산(scoreManager) → 정산 데이터 확정
    // 낙은 전용 정산(SettleNakThrow) — 이동·착지 효과·피해 연출 없이 공격 0 마감.
    // 백도는 세션이 생성하지 않는다 (HandleCurseResult는 API로 유지)
    private ThrowSettlement SettleThrow(YutResult confirmed)
    {
        // 이전 투척의 표시용 임시값만 초기화 (윷별 영구 스탯·누적 총점은 유지)
        basicScore = 0;
        multiplier = 0f;
        totalMult = 0f;
        throwScore = 0;

        ThrowSettlement settlement = new ThrowSettlement
        {
            result = confirmed,
        };

        if (confirmed == YutResult.Nak)
        {
            scoreManager.SettleNakThrow(settlement);
        }
        else
        {
            scoreManager.SettleNormalThrow(settlement, confirmed);
        }
        return settlement;
    }

    // 정산 연출 종료 지점 — GameUIManager.PresentSettlement 마지막과 SkipPresentation에서 호출
    internal void FinishPresentation(ThrowSettlement settlement)
    {
        basicScore = settlement.finalBasic;
        multiplier = settlement.finalMult;   // 윷 배율 표시 — 성장 반영값. 탐욕부 취소는 전체배율만 0이고 윷 배율은 정산값 유지
        totalMult = settlement.talismanCancelled ? 0f : settlement.totalMult;    // 전체배율 표시 — 취소 시 유효 배율 0
        throwScore = settlement.throwScore;
        displayTotal = totalScore;
        isPresenting = false;
        activePresentation = null;
        if (settlement.landingTileIndex >= 0)
        {
            boardManager.MovePieceVisual(settlement.landingTileIndex);   // 이동 중 스킵해도 말을 최종 착지 칸에 맞춘다
        }
        uiManager.ShowLandingInfo(settlement);   // 스킵 경로 — 이동 연출 없이도 착지 정보 표시
        uiManager.ClearUsedTalismanFeedback();   // 이유 텍스트 비우고 남은 사용 카드 표시 정리 — 다음 투척에 잔류 없음
        uiManager.UpdateUI();

        if (currentState != GameState.Playing)
        {
            return;
        }
        if (totalScore >= goalScore)
        {
            EnterClear();
            return;
        }
        if (currentThrows >= maxThrows)
        {
            EnterGameOver();   // 기본 투척 소진 후에도 보스 생존 — 마지막 투척 처치는 위 클리어 판정이 먼저 잡는다
            return;
        }
        PrepareTurn();   // 다음 차례 준비 — 새 세션 생성 + 이전 '이번 투척' 부적 효과 해제. 선택창이 닫히면 부적 사용 가능
        // 땅·START 칸 착지 — 정산 연출이 끝나고 보스전이 계속될 때 부적 후보 3택1 선택창 표시 (방울 선택 대체)
        if (settlement.landedOnBellTile)
        {
            talismanManager.OpenLandPick();
        }
        uiManager.UpdateUI();   // 최종 상태로 갱신 — 새 준비 세션·선택창 반영 후 CanUseNow 기준으로 손패 버튼 활성 판정
    }

    // 풀 도트 차례 지급 — 정상 접수된 최초 던지기에서 차례당 1회 (윷 투척·보스 이동 전).
    // 정산 종료·재투척·확정·선택창 닫기·UI 갱신에서는 지급하지 않는다 (2026-10-22 오후 변경).
    // 도트는 소모되지 않고 차례마다 반복 적용, 등록한 차례에는 틱 없음.
    // 도트 피해로 보스를 처치하면 즉시 클리어 — 그 차례 투척은 소모되지 않는다 (사용자 확정)
    internal void ApplyTurnStartDot()
    {
        if (currentState != GameState.Playing)
        {
            return;
        }
        int dot = tileEffects.TurnStartDotDamage;
        if (dot <= 0)
        {
            return;
        }
        // 잔향령 — 정상 도트 지급 시 유효 확률(공령명 반영)로 같은 도트를 한 번 더 지급한다.
        // 저장된 도트량은 그대로 두고, 추가 지급분에는 잔향령을 다시 판정하지 않는다 (연쇄 없음)
        int extra = 0;
        OwnedBell dotBell = bellManager.GetBell(BellEffectKind.DotDoubleChance);
        if (dotBell != null && Random.value < bellManager.EffectiveChance(dotBell))
        {
            extra = dot;
        }
        // 목박령 — 도트 지급마다 유효 확률(공령명 반영)로 다음 예정 보스 이동을 1회 봉쇄한다.
        // 정상 지급과 잔향령 추가 지급에서 각각 판정 (추가 지급이 있으면 기회 2회).
        // 봉쇄부와 같은 pendingMoveSeal 경로를 사용 — 속박령의 최소 1칸 보정이 0칸을
        // 되살리지 않고, 같은 다음 이동을 막는 효과를 중첩해 봉쇄 턴을 추가하지 않는다
        OwnedBell sealBell = bellManager.GetBell(BellEffectKind.DotSealChance);
        if (sealBell != null)
        {
            int payments = 1 + (extra > 0 ? 1 : 0);
            for (int i = 0; i < payments; i++)
            {
                if (Random.value < bellManager.EffectiveChance(sealBell))
                {
                    bossManager.pendingMoveSeal = true;
                    Debug.Log("목박령 발동 — 다음 예정 보스 이동 1회 봉쇄 (풀 도트 지급)");
                }
            }
        }
        totalScore += dot + extra;
        displayTotal = totalScore;
        uiManager.SetMultiplierReason(extra > 0
            ? $"풀 도트 피해 +{dot} +{extra} (잔향령)"
            : $"풀 도트 피해 +{dot}");   // 이번 윷 공격과 구분된 별도 표시
        Debug.Log($"풀 도트 틱: {dot + extra} 피해 (등록 도트 {tileEffects.forestDot}{(extra > 0 ? $", 잔향령 추가 +{extra}" : "")}) | Total {totalScore}/{goalScore}");
        if (totalScore >= goalScore)
        {
            EnterClear();
            return;
        }
        uiManager.UpdateUI();
    }

    // 런 전체 초기화 — 게임오버/보스 선택 복귀로 런이 끝날 때 호출
    // 관문 이동(GoToNextBoss→EnterBossBattle)은 여기를 거치지 않으므로 런 성장 요소가 유지된다
    private void ResetRunState()
    {
        StopPresentation();                  // 이전 보스전의 잔여 연출 정리
        bellManager.ResetForNewRun();        // 보유 방울 등 런 단위 장착 상태 정리
        fortuneManager.ResetForNewRun();     // 점괘 강화 횟수·누적 강화·선택 대기 정리 — 보스 전환에서는 호출되지 않음
        shopManager.ResetForNewRun();        // 상점 열림 상태·상품 후보 정리
        economyManager.ResetForNewRun();     // 돈 초기화 + 시작 자금 재지급 (런당 1회)
        talismanManager.ResetForNewRun();    // 부적 덱 재구성·손패·선택창·세션 효과 전부 초기화
        hasRolled = false;                   // 이전 윷 결과 표시용 데이터
        currentSettlement = null;
        scoreManager.lastBackDoLoss = 0;
        rollManager.session = null;          // 진행 중이던 투척 세션 폐기
        landingTileInfo = "";
        selectedBossIndex = 0;                 // 새 런은 첫 번째 보스부터 — 전투·소개 인덱스 모두 초기화
        bossManager.pendingBossIndex = 0;
        bossManager.ResetBossPiece();        // 보스 말 위치 초기화 — 이전 런 상태 잔존 방지
        tileEffects.ResetForBoss();          // 물 연속·풀 도트는 런 종료에도 제거
        currentThrows = 0;
        totalScore = 0;
        displayTotal = 0;
        basicScore = 0;
        multiplier = 0f;
        totalMult = 0f;
        throwScore = 0;
        boardManager.ResetForNewRun();       // 말판 배치·말 위치·완주 초기화 (다음 런 첫 보스전에서 새 배치 생성)
        scoreManager.ResetForNewRun();       // 런타임 기본 전체배율을 설정값으로 초기화 (보스 전환에는 유지)
        // 런 전체 성장 요소 초기화 — 런이 끝났으므로 윷 강화·보스 특수능력 원복
        rollManager.BuildYutStats();
        rollManager.ResetProbabilities();
        goalScore = bossManager.CurrentBoss != null ? bossManager.CurrentBoss.maxHP : 0;   // 선택 화면 미리보기
    }

    // 패널 중첩 경고 — 부모가 꺼지면 함께 숨겨지는 배치를 미리 진단한다
    private void WarnIfPanelNested()
    {
        if (startPanel == null)
        {
            return;
        }
        if (bossSelectPanel != null && clearPanel != null && bossSelectPanel.transform.IsChildOf(clearPanel.transform))
        {
            Debug.LogWarning("[YutGameManager] bossSelectPanel이 clearPanel의 자식입니다. 다음 관문 클릭 시 clearPanel이 꺼지면 함께 숨겨지니 Canvas 바로 아래로 분리하세요.", bossSelectPanel);
        }
        if (shopPanel != null && clearPanel != null && shopPanel.transform.IsChildOf(clearPanel.transform))
        {
            Debug.LogWarning("[YutGameManager] shopPanel이 clearPanel의 자식입니다. 결과 화면이 꺼지면 함께 숨겨지니 Canvas 바로 아래로 분리하세요.", shopPanel);
        }
        if (fortuneChoicePanel != null && clearPanel != null && fortuneChoicePanel.transform.IsChildOf(clearPanel.transform))
        {
            Debug.LogWarning("[YutGameManager] fortuneChoicePanel이 clearPanel의 자식입니다. 부모가 꺼지면 선택창이 보이지 않아 점괘 선택이 잠길 수 있으니 Canvas 바로 아래로 분리하세요.", fortuneChoicePanel);
        }
    }

    private void Start()
    {
        uiManager.UpdateUI();
    }

    private void Update()
    {
        // 스페이스바 테스트: 화면 플로우를 순서대로 진행
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentState == GameState.Start)
            {
                // 보스 소개 화면이 열려 있으면 스페이스바가 진입 버튼 역할
                if (bossSelectPanel != null && bossSelectPanel.activeSelf)
                {
                    EnterBossBattle();
                }
                else
                {
                    StartGame();
                }
            }
            else if (currentState == GameState.Cleared && bossSelectPanel != null && bossSelectPanel.activeSelf)
            {
                EnterBossBattle();
            }
            else if (currentState == GameState.Playing)
            {
                // 투척 진행 중이면 스페이스바 = 결과 확정 (준비 단계·확정 후에는 최초 투척)
                if (rollManager.session != null && rollManager.session.RollInProgress)
                {
                    ConfirmThrow();
                }
                else
                {
                    RollYut();
                }
            }
        }
    }
}
