# Deque UI 구조

`Assets/Scenes/DequeScene.unity`를 열고 Play → int/float/string 선택 → Start로 시작한다.
씬을 다시 구성하려면 Play Mode를 종료하고 `Structura > Build Deque UI`를 실행한다.
빌더는 DequeCanvas와 Deque 전용 프리팹을 다시 만들고 Controller 참조를 연결한다. 기존 카메라는 유지한다.

## 책임 분리

| 파일 | 역할 |
| --- | --- |
| `Assets/_Scripts/Deque/Deque.cs` | 실제 map/block/iterator, 연산, 구조 조회 API 및 debug 문자열 |
| `Assets/_Scripts/Deque/DequeIterator.cs` | 자료구조용 iterator의 Node/Curr/First/Last와 복사 생성자 |
| `Assets/_Scripts/Deque/DequeController.cs` | `Deque<object>` 소유, 타입·index 검증, 연산 실행, 변경 범위와 결과 전달 |
| `Assets/_Scripts/UI/Deque/DequeControls.cs` | 타입 선택, 입력 필드, 버튼 이벤트 연결·해제, 통계·결과 표시 |
| `Assets/_Scripts/UI/Deque/DequeUIView.cs` | 전달받은 Deque를 조회하여 map 행 재사용, Start/Finish 카드 표시 |
| `Assets/_Scripts/UI/Deque/DequeMapRowUIView.cs` | map 참조와 block의 가로 slot 배열 표시·재사용 |
| `Assets/_Scripts/UI/Deque/DequeSlotUIView.cs` | 값, 물리 slot 번호, 논리 index, S/F 및 변경·조회 상태 표시 |
| `Assets/_Scripts/UI/Deque/DequeSlotEffects.cs` | 변경·조회 slot의 색상 pulse, 중단 시 복구 |
| `Assets/_Scripts/Editor/DequeSceneBuilder.cs` | 씬 UI와 `DequeMapRowUI`, `DequeSlotUI` 프리팹 생성 |

HashTable과 동일한 Controller → 자료구조 → UIView → 행 → slot 흐름을 사용한다.
`ValueType`, `HashValueParser`와 Editor의 `HashUIFactory`를 그대로 재사용한다.
기존 HashTable 코드·씬·프리팹은 변경하지 않는다.

```text
DequeControls 버튼
    -> DequeController: HashValueParser 입력 변환 / index 범위 검사
    -> Deque<object> 연산
    -> DequeUIView.Refresh(deque, 변경 범위, 조회 index)
        -> GetStartIterator() / GetFinishIterator() 조회
    -> DequeMapRowUIView: HasBlock(node) / indexer 조회
    -> DequeSlotUIView.Bind(slot 번호, 논리 index, 값, S/F 여부)
    -> DequeSlotEffects
```

## 데이터 전달과 표시 규칙

- Controller가 소유한 `Deque<object>`를 View에 직접 전달한다. 별도의 snapshot DTO나 builder는 사용하지 않는다.
- HashTable처럼 Controller가 데이터를 변경하고 UIView → 행 → 슬롯이 표시를 담당한다. View는 공개 조회 API만 사용하며 Push/Pop/Set을 호출하지 않는다. 읽기 전용 인터페이스로 강제하는 방식은 아니다.
- `GetStartIterator()` / `GetFinishIterator()`는 자료구조용 iterator의 복사본을 반환하고, `HasBlock(node)`는 할당 여부만 반환한다. 내부 `_map`, `_start`, `_finish` 참조는 외부에 노출하지 않는다.
- 행에서 논리 index와 S/F 여부를 계산해 슬롯에 전달한다. `DequeSlotUIView`는 공통 `UIEntry<object>`를 사용하고 원소 값은 `Data`에 보관한다. 행·슬롯 데이터 복사본은 만들지 않는다.
- `[start, finish)`에서 start는 첫 실제 원소, finish는 마지막 실제 원소 다음 위치다. 빈 상태에서는 start와 finish가 같다.
- 각 map 행은 `[n] -> block` 또는 `[n] -> [] null`로 표시한다. 할당된 빈 block은 유지하고 모든 slot을 표시한다.
- 유효 원소 여부는 값이 0/null/빈 문자열인지가 아니라 논리 index 범위로 판정한다.
- slot에는 물리 `slot n`, 유효 원소의 논리 `i:n`, 값, 초록 S, 주황 F를 표시한다. 같은 위치의 S/F도 동시에 표시한다.
- 파란 배경은 유효 원소, 어두운 배경은 빈 slot이다. 금색 테두리·CHANGED는 최근 변경, 청록 테두리·READ는 최근 조회다. 다음 성공한 연산에서 이전 강조를 갱신한다.
- Pop 또는 RemoveAt으로 비워진 물리 slot도 CHANGED에 포함한다. map 확장 후 강조는 현재 Deque의 start 좌표로 계산한다.
- 왼쪽 Start/Finish 카드에는 `Node`, `Curr`, `First`, `Last`와 `map[Node][Curr]`를 표시한다. Last는 block 크기이며 배타적 상한이다. finish는 역참조하지 않고 `past-the-end (no value)`로 표시한다.
- MAP은 가로·세로 드래그와 스크롤이 가능하다. 작은 화면이나 확장된 map에서는 스크롤로 나머지 행을 탐색한다.

## 조작

| 영역 | 입력 | 버튼 |
| --- | --- | --- |
| End Operations | 상단 Value | PushFront, PushBack, PopFront, PopBack, Clear |
| Index Access | Index, Value | Get, Set |
| Cost Demo | Index Access의 Index, Value 공유 | InsertAt, RemoveAt |

- Get/Set/RemoveAt은 `0 <= index < Count`, InsertAt은 `0 <= index <= Count`를 받는다.
- 잘못된 타입·index·빈 deque의 Pop은 데이터 변경 없이 안내 문구로 처리한다.
- Get/Pop/RemoveAt 결과는 Feedback에 표시한다. Clear는 현재 MapSize/BlockSize로 재초기화하고 가운데 block 하나만 할당한다.
- 타입은 세션 시작 시 고정한다. 숫자 입력 규칙과 실제 파싱은 HashTable과 같으며 NaN/무한대는 거부한다. 문자열은 rich text로 해석하지 않는다.
- 모든 연산은 즉시 완료한다. 색상 pulse는 데이터 변경과 독립적이므로 연출 대기나 입력 잠금은 없다.

## Cost Demo 범위

중간 InsertAt은 뒤쪽부터 원소를 오른쪽으로 한 칸씩 밀고, 중간 RemoveAt은 뒤쪽 원소를 왼쪽으로 한 칸씩 당긴다.
현재 단계에서는 **최종 결과 refresh, 변경 범위 강조, 방향과 이동 원소 수 안내**를 구현했다.
중간 삽입·삭제의 순차 이동 애니메이션은 Controller의 TODO로 남겼다.

기존 Deque가 index 0 및 양끝을 Push/Pop으로 최적화하므로 해당 경우에는 이동 원소 수 0과 end operation을 안내한다.
중간 조작은 O(n)이며, 양끝 조작도 map 재배치가 일어나는 개별 호출은 선형 비용이 생길 수 있다.

## Deque 최소 수정

- 표시용 DTO와 builder는 제거했다. Deque에는 iterator 복사본과 block 할당 여부 조회 API만 두고, `GetDebugView()`는 자체 상태로 문자열을 만든다.
- Initialize에서 0 이하 map/block 크기를 거부한다.
- RecenterMap에서 finish가 있는 block도 보존한다. BlockSize=1에서 PushBack 직후 Curr=0인 실제 원소를 제외하던 문제를 수정했다.
- MapSize=1 확장 시 앞뒤 여유가 생기도록 최소 4칸으로 확장하고 시작 노드를 최소 1로 둔다.
- Push/Pop/indexer/InsertAt/RemoveAt의 기존 연산 흐름은 유지한다.

## 검증 (2026-09-21)

- Snapshot 제거 후 Unity 6000.3.20f1 컴파일 통과.
- Play Mode에서 실제 `Button.onClick` 경로로 필수 9개 연산, int/float/string, 잘못된 입력, 빈 상태, 블록 경계 및 map 확장을 검증했다 (30,895 assertions).
- 각 map 행의 block 할당 표시, 슬롯 값·논리 index, S/F 마커 위치와 개수, 삽입·삭제 후 변경 슬롯 강조를 확인했다. 삭제 후 빈 슬롯도 강조된다.
- DequeController는 RefreshView의 코드 4줄만 변경했으며 사용자가 작성한 주석은 모두 그대로 보존했다. Deque 본체, DequeControls, 씬 및 슬롯 프리팹도 수정 전과 동일하다.
- 자료구조 본체는 앞선 작업에서 map 크기 1/4, block 크기 1/2/8로 17,400회 연산을 List와 비교해 검증했다. 이번 변경에서는 본체를 수정하지 않아 해당 검증은 다시 실행하지 않았다.
- 순차 이동 애니메이션은 미구현이다. 이번 변경에서 물리 마우스·키보드 수동 조작 및 Player 빌드는 검증하지 않았다.
