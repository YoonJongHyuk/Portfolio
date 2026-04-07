# Portfolio

포트폴리오용으로 정리한 주요 코드 모음입니다.  
현재 저장소에는 Unity 기반 콘텐츠 기능 구현 코드와 Python/FastAPI 기반 AI 서비스 프로토타입 코드가 포함되어 있습니다.

## 폴더 구성

### `HangeulFriends`
Unity C#으로 구성한 기능별 스크립트 모음입니다.  
교육/콘텐츠형 애플리케이션에서 사용할 수 있는 영상 재생, 퀴즈, QR 스캔, 화상 통화 UI 관련 로직을 기능 단위로 정리했습니다.

#### `QR`
- `QRScanner.cs`
  - `ZXing`을 이용한 QR 코드 스캔 기능
  - 전면 카메라 우선 사용, 예외 시 후면 카메라 fallback 처리
  - 스캔 성공/실패 결과에 따라 Unity Event와 패널 UI를 제어
- `QRJsonData.cs`
  - QR 데이터 파싱 또는 결과 처리용 보조 데이터 구조 스크립트

#### `Video_Call`
- `VideoCall.cs`
  - 영상 통화 진입 흐름 제어
  - 대기 패널에서 실제 재생 패널로 전환하고 영상 재생 시작
- `FrontCameraPreview.cs`
  - 전면 카메라 프리뷰 출력 처리
- `TimeSet.cs`
  - 통화 시간 또는 대기 시간 관련 UI/로직 제어

#### `Video_Library`
- 영상/오디오 라이브러리 재생 기능을 분리해 둔 폴더입니다.
- `VideoPlay.cs`
  - `VideoPlayer` 준비, 재생/일시정지, 종료 감지 처리
  - 기기별 종료 이벤트 누락에 대비한 frame/time fallback 로직 포함
- `VideoManager.cs`, `LibraryManager.cs`
  - 현재 선택된 영상/오디오 정보 관리
  - 외부 UI와 재생 로직을 연결하는 매니저 역할
- `AudioPlayer.cs`
  - `StreamingAssets` 경로의 mp3를 로드해 재생
  - 재생/일시정지/재개/반복 재생 상태 관리
- `AudioTimeBar.cs`, `ReplayButton.cs`, `ShuffleButton.cs`, `IconChange.cs`
  - 재생 UI 제어 및 사용자 인터랙션 처리
- `VideoData.cs`, `VideoPathUtil.cs`
  - 영상 메타데이터 및 파일 경로 처리

#### `Video_Quiz`
- 영상 시청 중 퀴즈나 진행 UI를 제어하는 스크립트 모음입니다.
- `QuizTiming.cs`
  - 퀴즈가 노출될 시점 정보 관리
- `QuizeSliderSetting.cs`, `Video_Time_Bar.cs`, `Video_Timer.cs`, `Video_Volume.cs`
  - 재생 시간, 슬라이더, 볼륨 등 영상 컨트롤 UI 처리
- `Quiz_UI_Setting.cs`, `SliderSafeFill.cs`
  - 퀴즈 UI 및 슬라이더 표현 안정화
- `StarCountEvent.cs`, `VideoPassEvent.cs`
  - 영상 완료/통과 이벤트 처리
- `Video_Screen_Manager.cs`
  - 전체화면/일반화면 전환
  - 자동 숨김 패널, 잠금 상태, 사용자 인터랙션 기반 UI 복원 처리

#### 요약
- 기술 스택: `Unity`, `C#`, `VideoPlayer`, `WebCamTexture`, `ZXing`
- 핵심 포인트:
  - 기능별 스크립트 분리
  - 교육용/콘텐츠형 앱에서 필요한 영상 UX 로직 구현
  - 이벤트 기반 UI 전환과 기기 예외 대응 로직 포함

---

### `Living_Brush`
Python 기반의 AI 브러시/텍스처 생성 프로토타입 코드입니다.  
FastAPI 서버, RAG 검색, 컬러 추천, Ollama LLM 연동, ComfyUI 워크플로우 연결 등 생성형 AI 실험 코드를 포함하고 있습니다.

#### 주요 파일
- `main.py`
  - 전체 서비스의 중심 API
  - 사용자 프롬프트를 받아 브러시 유형을 분류하고 이름을 생성
  - 영문 텍스처 프롬프트를 만들고 ComfyUI 워크플로우에 주입
  - 진행 상태 확인 및 결과 반환까지 처리
- `rag_chain.py`
  - `FAISS`, `HuggingFace Embeddings`, `CrossEncoder Reranker`, `Ollama` 기반 RAG 체인 구성
  - `docs/UIDocs.csv`를 벡터 DB로 만들거나 로드해서 UI 관련 질의응답에 활용
- `color_api.py`
  - `TheColorAPI`를 호출해 입력 색상 기준 컬러 스킴 추천
- `color_prompt_api.py`
  - 태그 기반 색상 추천 API
  - 사전 정의된 팔레트가 있으면 바로 반환하고, 없으면 Ollama로 HEX 팔레트 생성
- `convert_prompt.py`
  - 프롬프트 정제/변환용 보조 스크립트

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
  - UI 문서 기반 질의응답용 데이터
- `docs/PronunciationCorrection.csv`
  - 발음 보정 관련 데이터 문서
- `tcttocsv.ipynb`, `TTS.ipynb`
  - 데이터 전처리 및 음성 관련 실험용 노트북

#### 요약
- 기술 스택: `Python`, `FastAPI`, `Ollama`, `FAISS`, `LangChain`, `HuggingFace`, `ComfyUI`
- 핵심 포인트:
  - 생성형 AI 기반 브러시/텍스처 생성 파이프라인 프로토타이핑
  - LLM 분류 + 프롬프트 생성 + 이미지 워크플로우 실행 구조
  - RAG와 색상 추천 API를 함께 구성한 멀티 기능 백엔드

---

## 한눈에 보는 특징
- `HangeulFriends`: Unity 기반 사용자 인터랙션, 영상 재생, 퀴즈, 카메라/QR 처리 로직
- `Living_Brush`: AI 텍스처 생성, 프롬프트 엔지니어링, RAG, 색상 추천 API 프로토타입

## 정리 목적
- 포트폴리오 제출용 코드 샘플 정리
- 프로젝트별 핵심 기능과 역할이 빠르게 보이도록 구조화
- 실제 구현 경험이 드러나는 기능 단위 코드 중심으로 보관
