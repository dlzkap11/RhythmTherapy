# RhythmTherapy 개발 규약

Unity 6000.3.22f1 기반 횡스크롤 2D 리듬게임.

상세 아키텍처·판정 규칙·폴더 구조 표준은 `rhythm-therapy-dev` 스킬 문서를 정본으로 참조한다.
이 문서는 **커밋 컨벤션**과 **C# 코드 컨벤션**만 다룬다.

---

## 1. 커밋 컨벤션

### 형식

```
[Tag] 한글 요약

(필요할 때만) 무엇을 / 왜 바꿨는지 본문
```

- 제목은 50자 내외, 마침표 없이
- 본문은 필요할 때만. 제목과 빈 줄로 분리
- PR 제목도 같은 형식

### 태그 (7종)

| 태그 | 용도 |
|---|---|
| `[Feat]` | 새 기능 추가 |
| `[Fix]` | 버그 수정 |
| `[Update]` | 기존 기능의 동작 변경·개선 (버그 아님) |
| `[Refactor]` | 동작 불변, 구조/이름만 정리 |
| `[Remove]` | 코드/파일/에셋 삭제 |
| `[Test]` | 테스트 추가·수정 |
| `[Chore]` | 설정, 패키지, 빌드, 프로젝트 세팅, 문서/주석 |

**판단 팁**

- 의도한 동작이 아니었으면 → `[Fix]`
- 원래 의도대로였는데 더 낫게 바꾸면 → `[Update]`
- 플레이 결과가 안 바뀌면 → `[Refactor]`
- 단순 파일/에셋 추가는 성격에 따라 `[Feat]` 또는 `[Chore]`
- 애매하면 `[Update]`

**예시**

```
[Feat] 결과창 UI 추가
[Fix] 자동 Miss 시 콤보가 안 끊기는 문제
[Update] Perfect 판정창 25ms → 30ms
[Refactor] JudgeSystem 약어 메서드명 정리
[Chore] FMOD 패키지 업데이트
```

`[Docs]`는 문서 분량이 쌓이면 `[Chore]`에서 분리한다.

### AI 커밋

Claude가 만드는 커밋·PR은:

- 커밋 메시지 끝: `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`
- PR 본문 끝: `🤖 Generated with [Claude Code](https://claude.com/claude-code)`

---

## 2. C# 코드 컨벤션

기준: Unity 공식 C# 스타일 (*"Create a C# style guide"* e-book + Unity 예제 코드).
아래는 이 프로젝트에서 확정한 규칙.

### 네이밍

| 대상 | 규칙 | 예 |
|---|---|---|
| 클래스, 메서드, 프로퍼티, 이벤트 | `PascalCase` | `ComboSystem`, `RegisterHit` |
| enum 타입 + 멤버 | `PascalCase`, 타입명은 단수형 | `JudgeType.Perfect` |
| 네임스페이스 | `PascalCase` | `RhythmTherapy.Core` |
| `const` / `static readonly` 상수 | `PascalCase` (ALL_CAPS 금지) | `HpMax`, `ApproachMs` |
| 지역 변수, 매개변수 | `camelCase` | `comboAfter`, `error` |
| private / protected 인스턴스 필드 | `_camelCase` (밑줄 접두사) | `_combo`, `_hpDepletedFired` |

- 두 글자 초과 약어는 PascalCase로 (`SongId`, `HttpClient` 스타일)
- 의미가 불분명한 축약 금지 (`AccAss`, `JudgeAvg` 같은 이름 지양)

```csharp
// Good
private readonly ComboSystem _combo = new ComboSystem();
public int MaxCombo => _combo.Max;

// Bad
private readonly ComboSystem combo = new ComboSystem();   // 밑줄 없음
public int MAX_COMBO => combo.Max;                        // ALL_CAPS
```

### 포매팅

- Allman 중괄호 (여는 중괄호는 새 줄)
- 4스페이스 들여쓰기, 한 줄 최대 120자 목표
- 한 줄짜리 `if`는 중괄호 생략 허용. 단 중첩되거나 여러 줄이면 중괄호

```csharp
// Good
if (Current > Max)
    Max = Current;

// Good
if (hp.IsDepleted && !_hpDepletedFired)
{
    _hpDepletedFired = true;
    HpDepleted?.Invoke();
}
```

- `using` 정렬: `System.*` → `UnityEngine.*` / `Unity.*` → 프로젝트(`RhythmTherapy.*`), 그룹 사이 빈 줄

```csharp
// Good
using System;

using UnityEngine;

using RhythmTherapy.Core;
```

- 특성(attribute)은 짧으면 같은 줄, 여러 개거나 인자가 길면 별도 줄

```csharp
[SerializeField] private int _lane;

[Header("Note")]
[SerializeField] private NoteData _data;
```

### 타입 / 언어 기능

- **`var` 사용 지양** — 명시적 타입이 기본. `foreach (KeyValuePair<TKey, TValue> ...)`처럼
  타입명이 지나치게 길어 오히려 가독성을 해치는 예외적 경우에만 허용
- 접근 제어자 항상 명시 (`private` 생략 금지)
- 상속을 열 의도가 없는 클래스는 `sealed`
- 공개 필드 대신 프로퍼티. 인스펙터 노출은 `[SerializeField] private`
- 튜닝 상수·매직넘버는 `GameConfig`에 `const`로 모으고 XML 주석으로 의미를 적는다

```csharp
// Bad — 매직넘버
_judge.JudgeAC = _judge.AccAss(200);

// Good
_judge.JudgeAC = _judge.AccAss(GameConfig.AutoMissErrorMs);
```

### 파일 / 구조

- 1파일 1 public 타입, 파일명 = 타입명
- 모든 스크립트는 네임스페이스 안에 둔다. 네임스페이스는 폴더 경로와 맞춘다
  (`RhythmTherapy.Core`, `RhythmTherapy.Managers`, `RhythmTherapy.UI`, `RhythmTherapy.Data` …)
- enum·인터페이스도 반드시 네임스페이스 안에. 파일 최상단 전역 선언 금지
- 폴더 배치는 `rhythm-therapy-dev` 스킬의 폴더 구조 표준을 따른다

### Unity 특화

- **MonoBehaviour 멤버 순서**: 직렬화 필드 → private 필드 → 프로퍼티 → 이벤트 →
  Unity 생명주기 메서드(실행 순: `Awake` → `OnEnable` → `Start` → `Update` → `OnDisable` → `OnDestroy`)
  → 나머지 메서드
- `Update`에서 매 프레임 `GetComponent` / `Find` / 힙 할당 지양. 참조는 캐싱
- `GameObject.Find` / `FindObjectOfType`는 부트스트랩 등 예외 상황에만
- 이벤트 구독은 `OnEnable` 또는 `Start`에서, 해제는 반드시 `OnDisable` / `OnDestroy`에서
- 생성·소멸이 잦은 오브젝트(노트 등)는 `PoolManager` 경유 풀링
- 싱글턴: `Instance` 프로퍼티 + `[RuntimeInitializeOnLoadMethod]` 부트스트랩 + `@Managers` GameObject

### 주석

- 주석과 `<summary>`는 한글, 식별자는 영어
- public 클래스/메서드에 `<summary>`
- 계산식 코드에는 근거 문서(`formulas-and-tests.md` 등)의 해당 항목을 주석으로 참조

---

## 3. 아키텍처 가이드라인

컨벤션(기계적 규칙)이 아니라 설계 판단 기준. 상세는 `rhythm-therapy-dev` 스킬 `architecture.md` 참조.

- 판정 / 점수 / 콤보 / HP 계산은 MonoBehaviour·씬 상태에 의존하지 않는 순수 C# 클래스로 분리한다 (테스트 가능성)
- 오디오는 `Conductor` / `AudioManager` 뒤에 캡슐화한다 — FMOD 교체를 대비해 호출부는 추상에 의존
- 데모곡(`SongDataSO`)과 에디터 생성곡(`SongData`)은 `ISongData`로 통합하고, UI는 두 타입을 구분하지 않는다
- 판정은 노래 재생 시간 기준이다. 노트의 화면 이동은 순수 시각 요소이며 판정 로직에 관여하지 않는다

---

## 4. 개선 백로그

현재 코드가 위 컨벤션과 어긋난 지점. **새 코드는 컨벤션을 따르고**, 아래 항목은 해당 파일을 만질 때
또는 전용 커밋으로 하나씩 정리한다.

- [ ] private 인스턴스 필드를 `_camelCase`로 리네임 (`ComboSystem`, `GameManager`, `Note` 등 전 파일) — `[Refactor]`
- [ ] Runtime 코드에 네임스페이스 부여: `RhythmTherapy.*` (`GameManager`, `Note`, `LaneManager`, 각 Manager/UI)
- [ ] 전역 enum을 네임스페이스 안으로: `JudgeType`(`JudgeSystem.cs`), `NoteType` 등. 가능하면 전용 파일로 분리
- [ ] 약어 이름 정리: `AccAss` → `AssessAccuracy`, `SumScore` → `AddScore`, `JudgeAvg` → 의미 있는 이름, `JudgeAC` 필드명 재검토
- [ ] 매직넘버를 `GameConfig`로: `GameManager`의 `AccAss(200)`, 자동 Miss error 값 등
- [ ] `JudgeSystem`의 `PerfectMS` / `GreatMS` / `GoodMS` / `BadMS` — PascalCase private + 비-const →
      `const` + 컨벤션 이름, 또는 `GameConfig`로 이동
- [ ] `using` 정렬 (`GameManager` 등에서 `System`이 중간에 있음)
- [ ] 오타 수정: `ResoureceManager.cs` → `ResourceManager.cs`, `Assets/Scripts/Utills/` → `Utils/`
      (`.meta` 동반 이동, 참조 갱신 — 단독 커밋)
- [ ] 빈 `Define.cs` — 쓸 계획 없으면 삭제, 쓸 거면 용도 주석 추가
