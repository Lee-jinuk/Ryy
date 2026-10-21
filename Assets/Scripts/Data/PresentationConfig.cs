// ──────────────────────────────────────────────
// PresentationConfig.cs — 연출 간격 설정 에셋
// 말 이동·정산 효과 연출의 딜레이 단일 기준.
// 에셋 생성: Assets 우클릭 → Create → RYY → Presentation Config
// ──────────────────────────────────────────────
using UnityEngine;

[CreateAssetMenu(fileName = "PresentationConfig", menuName = "RYY/Presentation Config")]
public class PresentationConfig : ScriptableObject
{
    public float moveStepDelay = 0.15f;    // 말 이동 1칸당 딜레이(초)
    public float effectStepDelay = 0.35f;  // 정산 연출 단계 간 딜레이(초)
    public float greedStepPreDelay = 0.5f;   // 탐욕부 이유 표시 → 배율 갱신까지 대기(초)
    public float greedStepPostDelay = 0.5f;  // 탐욕부 배율 갱신 → 카드 표시 제거까지 대기(초)
}
