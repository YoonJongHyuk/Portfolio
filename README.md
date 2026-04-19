# Portfolio

포트폴리오 제출용으로 정리한 주요 프로젝트 코드 모음입니다.

현재 저장소에는 Unity 기반 콘텐츠 구현 코드와 Python/FastAPI 기반 AI 서비스 프로토타입 코드가 포함되어 있습니다. 각 폴더는 프로젝트 단위로 구분되어 있으며, 핵심 기능을 빠르게 파악할 수 있도록 주요 파일과 역할을 정리했습니다.

## 폴더 구성

### `HangeulFriends`

Unity C#로 구성된 교육/콘텐츠형 애플리케이션 코드입니다. 영상 재생, 영상 퀴즈, QR 스캔, 화상 통화 UI 등 사용자 체험 흐름을 기능 단위로 분리했습니다.

#### `QR`

- `QRScanner.cs`
  - `ZXing`을 이용한 QR 코드 스캔 기능
  - 전면 카메라 우선 사용, 예외 상황에서는 후면 카메라 fallback 처리
  - 스캔 성공/실패 결과에 따라 Unity Event와 패널 UI 제어
- `QRJsonData.cs`
  - QR 데이터 파싱 또는 결과 처리를 위한 보조 데이터 구조

#### `Video_Call`

- `VideoCall.cs`
  - 영상 통화 진입 흐름 제어
  - 대기 패널에서 실제 재생 패널로 전환하고 영상 재생 시작
- `FrontCameraPreview.cs`
  - 전면 카메라 프리뷰 출력 처리
- `TimeSet.cs`
  - 통화 시간 또는 대기 시간 관련 UI와 로직 제어

#### `Video_Library`

- `VideoPlay.cs`
  - Unity `VideoPlayer` 준비, 재생, 일시정지, 종료 감지 처리
  - 기기별 종료 이벤트 누락에 대비한 frame/time fallback 로직 포함
- `VideoManager.cs`, `LibraryManager.cs`
  - 현재 선택된 영상/오디오 정보 관리
  - 메뉴 UI와 재생 로직을 연결하는 매니저 역할
- `AudioPlayer.cs`
  - `StreamingAssets` 경로의 mp3 파일 로드 및 재생
  - 재생, 일시정지, 곡 넘김, 반복 재생 상태 관리
- `AudioTimeBar.cs`, `ReplayButton.cs`, `ShuffleButton.cs`, `IconChange.cs`
  - 재생 UI 제어와 사용자 인터랙션 처리
- `VideoData.cs`, `VideoPathUtil.cs`
  - 영상 메타데이터와 파일 경로 처리

#### `Video_Quiz`

- `QuizTiming.cs`
  - 퀴즈가 호출될 시점 정보 관리
- `QuizeSliderSetting.cs`, `Video_Time_Bar.cs`, `Video_Timer.cs`, `Video_Volume.cs`
  - 재생 시간, 슬라이더, 볼륨 등 영상 컨트롤 UI 처리
- `Quiz_UI_Setting.cs`, `SliderSafeFill.cs`
  - 퀴즈 UI와 슬라이더 표현 안정화
- `StarCountEvent.cs`, `VideoPassEvent.cs`
  - 영상 완료/통과 이벤트 처리
- `Video_Screen_Manager.cs`
  - 전체화면/일반화면 전환
  - 자동 숨김 패널, 잠금 상태, 사용자 인터랙션 기반 UI 복원 처리

#### 요약

- 기술 스택: `Unity`, `C#`, `VideoPlayer`, `WebCamTexture`, `ZXing`
- 핵심 포인트:
  - 기능별 스크립트 분리
  - 교육형 콘텐츠에 필요한 영상 UX 로직 구현
  - 이벤트 기반 UI 전환과 기기 예외 대응 로직 포함

---

### `JurassicVerse`

Unity 기반 VR/메타버스형 공룡 콘텐츠 코드입니다. Meta Quest 컨트롤러 입력을 활용해 공룡을 선택하고, 선택한 공룡 정보를 UI에 표시한 뒤, 실제 맵 씬으로 이동하는 흐름을 구성합니다.

#### 주요 흐름

1. 메인 UI에서 공룡 선택 화면으로 진입합니다.
2. 오른손 조이스틱 좌우 입력으로 공룡 목록을 이동합니다.
3. A 버튼으로 공룡을 선택하면 해당 모델과 설명 UI가 활성화됩니다.
4. 선택 완료 상태에서 A 버튼을 누르면 `MapScene`으로 이동합니다.
5. 씬 로딩이 끝난 뒤 선택된 공룡 번호를 기반으로 공룡 생성 로직을 호출합니다.

#### 주요 파일

- `GameManager.cs`
  - 전역 싱글톤 매니저로 현재 선택된 공룡 번호를 유지합니다.
  - `DontDestroyOnLoad`를 사용해 씬이 바뀌어도 선택 정보를 보존합니다.
  - `MoveScene()`에서 비동기 씬 로딩을 수행하고, 로딩 완료 후 `DinosaurFactory`를 찾아 선택된 공룡 생성 이벤트를 호출합니다.
  - `DinosaurFactory`가 없는 씬에서도 오류로 중단되지 않도록 경고 로그만 남기고 흐름을 계속 진행합니다.

- `Select_DinoList.cs`
  - 공룡 선택 화면의 목록 이동과 모델 활성화를 담당합니다.
  - 오른손 조이스틱 X축 입력으로 선택 인덱스를 순환시킵니다.
  - `delayTime`을 두어 조이스틱 입력이 너무 빠르게 반복되지 않도록 제어합니다.
  - 선택된 공룡만 보이도록 `Twinkle()`에서 목록 오브젝트 활성 상태를 갱신합니다.
  - A 버튼 입력 시 선택 모델을 켜고, `UIManager`와 `GameManager`에 선택 번호를 동기화합니다.

- `TurnModel.cs`
  - 왼손 조이스틱 X축 입력을 받아 현재 모델을 Y축으로 회전시킵니다.
  - `rotationSpeed` 값으로 회전 속도를 조절할 수 있습니다.
  - 작은 입력 흔들림을 무시하기 위해 dead zone을 적용합니다.

- `UIManager.cs`
  - UI 패널 전환, 버튼 선택, 공룡 정보 UI 표시를 담당하는 싱글톤 매니저입니다.
  - `number` 상태값을 기준으로 현재 UI 단계에서 어떤 입력을 처리할지 분기합니다.
  - 로그인/회원가입 선택 화면에서는 오른손 조이스틱 좌우 입력으로 버튼 포커스를 이동하고 A 버튼으로 클릭을 실행합니다.
  - 공룡 선택 완료 상태에서는 A 버튼으로 맵 씬 이동, B 버튼으로 선택 취소 및 이전 UI 복귀를 처리합니다.
  - `SetOnOffUI()`와 `SetDinosaurUI()`를 통해 패널과 공룡 정보 UI를 활성/비활성화합니다.

#### 코드 구조 특징

- `GameManager`는 씬 간 데이터 유지와 씬 전환을 담당합니다.
- `UIManager`는 현재 화면 상태와 UI 입력 처리를 담당합니다.
- `Select_DinoList`는 공룡 선택 목록과 선택 결과 반영을 담당합니다.
- `TurnModel`은 선택된 모델을 직접 관찰할 수 있는 회전 인터랙션을 제공합니다.

#### 요약

- 기술 스택: `Unity`, `C#`, `Meta Quest`, `OVRInput`, `SceneManager`
- 핵심 포인트:
  - VR 컨트롤러 기반 UI 선택 흐름 구현
  - 씬 전환 후에도 선택 공룡 정보를 유지하는 싱글톤 구조
  - 선택 UI, 모델 표시, 모델 회전, 맵 씬 진입을 분리한 구성

---

### `Living_Brush`

Python 기반 AI 브러시 텍스처 생성 프로토타입 코드입니다. FastAPI 서버, RAG 검색, 컬러 추천, Ollama LLM 연동, ComfyUI 워크플로우 연결 등 생성형 AI 실험 코드를 포함합니다.

#### 주요 파일

- `main.py`
  - 전체 서비스의 중심 API
  - 사용자 프롬프트를 받아 브러시 유형을 분류하고 이름을 생성
  - 영문 텍스처 프롬프트를 만들고 ComfyUI 워크플로우에 주입
  - 진행 상태 확인과 결과 반환 처리
- `rag_chain.py`
  - `FAISS`, `HuggingFace Embeddings`, `CrossEncoder Reranker`, `Ollama` 기반 RAG 체인 구성
  - `docs/UIDocs.csv`를 벡터 DB로 만들거나 로드해서 UI 관련 질의응답에 사용
- `color_api.py`
  - `TheColorAPI`를 호출해 입력 색상 기반 컬러 안내 추천
- `color_prompt_api.py`
  - 태그 기반 색상 추천 API
  - 사전 정의 팔레트가 있으면 바로 반환하고, 없으면 Ollama로 HEX 팔레트를 생성
- `convert_prompt.py`
  - 프롬프트 정제와 변환용 보조 스크립트

#### 데이터 및 설정 파일

- `.env`
  - 로컬 환경 변수 설정 파일
- `color.json`
  - 색상 관련 데이터 저장 파일
- `tiled_workflow.json`
  - ComfyUI에 전달할 이미지 생성 워크플로우 정의
- `tokenizer_config.json`
  - 토크나이저 설정 파일

#### 문서/실험 파일

- `docs/UIDocs.csv`
  - UI 문서 기반 질의응답 데이터
- `docs/PronunciationCorrection.csv`
  - 발음 보정 관련 데이터 문서
- `tcttocsv.ipynb`, `TTS.ipynb`
  - 데이터 전처리와 음성 관련 실험 노트북

#### 요약

- 기술 스택: `Python`, `FastAPI`, `Ollama`, `FAISS`, `LangChain`, `HuggingFace`, `ComfyUI`
- 핵심 포인트:
  - 생성형 AI 기반 브러시 텍스처 생성 파이프라인 프로토타입
  - LLM 분류, 프롬프트 생성, 이미지 워크플로우 실행 구조
  - RAG와 색상 추천 API를 함께 구성한 멀티 기능 백엔드

---

### `CleanOceanCompany`

Unity 기반 메타버스/미니게임 환경에서 손 인식 기반 상호작용과 실시간 채팅을 결합한 프로젝트 코드입니다. 정리된 코드는 `Manager`, `Mediapipe`, `Network` 영역으로 나뉘며, Python Mediapipe 프로세스와 Unity 게임 로직을 연결하는 구조가 특징입니다.

#### `Manager`

- `BrushManager.cs`
  - 브러시가 `Trash` 오브젝트와 충돌하면 청소 점수를 차감
  - 브러시 색상을 점점 흐리게 바꾸며 소모 상태를 시각적으로 표현
  - `MediapipeTrash`의 `CleanTrashEvent()`를 호출해 청소 이벤트 처리
- `ChatManager.cs`
  - Photon Chat 서버 연결, 채널 구독, 메시지 발행/수신 처리
  - Enter 키 기반 채팅 입력 상태 전환, 스크롤 UI, 말풍선 UI 관리
  - 일반 채팅과 `/w` 형식의 귓속말 구분 처리
- `MediapipeManager.cs`
  - Unity 내부에서 Python 실행 파일과 Mediapipe 스크립트를 별도 프로세스로 실행
  - Python 표준 출력 JSON 데이터를 파싱해 브러시 위치 갱신
  - 숨은 쓰레기 수를 관리하고 미니게임 종료 후 결과 처리와 프로세스 종료 수행
- `MediapipeMiniGameTimeManager.cs`
  - 제한 시간 UI, 슬라이더, 성공/실패 결과 패널 관리
  - 남은 시간 비율에 따라 포인트를 차등 지급
  - 게임 종료 후 기존 UI와 플레이어 조작 상태 복구

#### `Mediapipe`

- `BrushTriggerEvent.cs`
  - 브러시 사용 횟수를 줄이고 RawImage 색상을 변경해 브러시 소모를 표현
- `MediapipePorpoise.cs`
  - 브러시 위치를 3D 모델 표면에 Raycast로 투영
  - Dirt Mask 텍스처를 직접 수정해 닦이는 효과 구현
  - 청소 진행률 계산, 파티클 생성, 완료 후 브러시 제거와 점수 처리 연결
- `MediapipeThirdManager.cs`
  - Mediapipe JSON 입력을 받아 손 상태(`isFist`)에 따라 브러시 조작과 카메라 회전을 분기
  - 브러시 생성/삭제 이벤트와 카메라 반지름 고정 로직 관리
- `MediapipeThirdTimeManager.cs`
  - 3차 상호작용 미니게임의 타이머, 결과 UI, 카메라 UI 복귀 흐름 관리
  - 성공 시 애니메이션 트리거와 포인트 지급 수행
- `Mediapipe_CreateBrush_Trigger.cs`
  - 손 이미지가 특정 영역에 들어오면 브러시 프리팹 생성
- `Mediapipe_DeleteBrush_Trigger.cs`
  - 브러시가 삭제 영역에 들어오면 브러시 제거 및 상태 초기화
- `MediapipeThirdBrushEvent.cs`, `MediapipeThirdEventController.cs`
  - 확장용 구조가 준비된 스크립트

#### `Network`

- `ChatItem.cs`
  - 채팅 메시지를 TMP 텍스트에 반영
  - 텍스트 높이에 맞춰 UI 크기를 갱신하고 자동 스크롤 호출

#### 요약

- 기술 스택: `Unity`, `C#`, `Photon Chat`, `TextMeshPro`, `Python`, `Mediapipe`
- 핵심 포인트:
  - Python 기반 손 인식 결과를 Unity 실시간 인터랙션에 연결
  - 브러시 조작, 카메라 회전, 청소 미니게임, 채팅 UI를 하나의 플레이 흐름으로 통합
  - 충돌 기반 청소와 텍스처 마스크 기반 청소 연출을 함께 구현

---

## 한눈에 보는 특징

- `HangeulFriends`: Unity 기반 사용자 인터랙션, 영상 재생, 퀴즈, 카메라 QR 처리 로직
- `JurassicVerse`: Meta Quest 입력 기반 공룡 선택, 모델 확인, 씬 전환, 선택 데이터 유지 구조
- `Living_Brush`: AI 텍스처 생성, 프롬프트 엔지니어링, RAG, 색상 추천 API 프로토타입
- `CleanOceanCompany`: 손 인식 기반 미니게임, 청소 인터랙션, 실시간 채팅, Unity-Python 연동 구조

## 정리 목적

- 포트폴리오 제출용 코드 샘플 정리
- 프로젝트별 핵심 기능과 역할을 빠르게 파악할 수 있도록 구조화
- 실제 구현 경험을 보여주는 기능 단위 코드 중심으로 보관
