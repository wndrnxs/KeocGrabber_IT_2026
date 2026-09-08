# KEOC Image Grabber (KeocGrabber)

라인스캔 카메라로 대상물을 촬상하고, Master PC(상위 비전 시스템)의 요청에 따라 GiGA 광링크 보드(APX-7402)를 통해 판정 PC로 이미지를 직접 전송하는 검사 설비용 그랩 프로그램입니다.

## 1. 개요

- **개발 언어 / 플랫폼**: C# / WPF (.NET Framework 4.8, x64 전용)
- **주요 역할**
  1. 카메라(2대 또는 4대)로부터 라인스캔 이미지를 그랩
  2. Master(상위 PC, 1~2대)와 TCP/IP로 통신하며 LotID/촬상 요청을 수신
  3. 촬상 완료 이미지를 GiGA 광링크 보드로 지정된 판정 노드(Node ID)에 직접 메모리 전송
  4. 조명 컨트롤러(2계열) 제어, 레시피 기반 노광/게인/조명값 자동 세팅
  5. 이미지·로그 파일 저장 및 보관

## 2. 하드웨어 구성

| 구분 | 내용 |
|---|---|
| 프레임 그래버 | **Euresys Coaxlink Quad G3**(CoaXPress, eGrabber SDK) 또는 **Matrox** 계열 보드(MIL SDK) 중 `SystemParam.UseEuresys`(bool)로 벤더 선택. Matrox 선택 시 `SystemParam.BoardType`(`MILBOARD_TYPE` enum: `EN_BT_SOLIOS`/`EN_BT_RADIENT`/`EN_BT_RADIENTCLHS`/`EN_BT_RADIENTEVCL`/`EN_BT_RADIENTCXP`/`EN_BT_RADIENTPRO`/`EN_BT_RAPIXOCXP`)로 세부 보드 모델 선택 |
| 카메라 | Vieworks 라인스캔 카메라. Matrox 경로에서는 시리얼(RS-232, `sxx`/`gxx` ASCII 커맨드)로 직접 제어, Euresys 경로에서는 GenICam Remote 레이어로 제어(카메라 시리얼 통신 불필요) |
| GiGA 광링크 보드 | Interface/APX-7402 (`apx7400Lib`), 광 Ch 1/2로 대상 Node에 이미지 메모리를 직접 Write |
| 조명 컨트롤러 | DAWOO 컨트롤러(상부, 최대 4개, 커스텀 바이너리 프로토콜) + VIT 컨트롤러(하부, ASCII 프로토콜) — 카메라 4대 구성 시에만 VIT 사용 |
| 통신 | TCP/IP(Master ↔ Grab PC, 최대 2계열) + Serial(조명/카메라) |

## 3. 프로젝트 구조

솔루션은 `KeocGrabber.sln` → `KeocGrabber\KeocGrabber.csproj` 단일 프로젝트이며, 폴더명 앞자리 숫자로 계층을 구분합니다.

```
KeocGrabber/
├─ 010_Common/        공통 유틸 (Localization, MVVMBase, XmlManager)
├─ 020_UserControl/   커스텀 WPF 컨트롤 (ImageViewer, UserButton/Combo/Param/Slider 등)
├─ 100_Define/         전역 상태(Global.cs = G 클래스) / 시스템·레시피 파라미터 정의
├─ 300_Class/
│   ├─ Comm/           TCP/IP 클라이언트, 프로토콜 파서, 요청 큐 매니저
│   ├─ GigaBoard/       APX-7402 SDK 래퍼
│   ├─ Image/           촬상 이미지 버퍼 관리(카메라별 라인 누적)
│   └─ Logger.cs / DiskManager.cs
├─ 400_SubPage/         WPF 페이지(운전/설정/권한/통신상태/시스템정보)
└─ 600_Device/
    ├─ Camera/          GrabberManager(추상화), EuresysGrabber, MatroxGrabber, VieworksCamera
    ├─ IO/              SensorIOManager (센서 입력 라인 모니터링)
    └─ Light/           LightManager, DawooLight, VitLight
```

`G` 정적 클래스(`100_Define/Global.cs`)가 전역 싱글턴 허브 역할을 하며, `GRABBER`, `LIGHT`, `COMM`/`COMM2`, `GIGABOARD`, `IMAGEMANAGER`, `DISKMANAGER`, `MSGPROC`, `LOGGER`, `CURRRECIPE` 등 모든 매니저 인스턴스를 보유합니다.

## 4. 동작 시퀀스

1. **연결/대기**: `TCPIPClient`가 Master IP:Port로 접속을 유지하며(끊기면 재접속 루프), 1.5초 주기로 Heartbeat(`ATS.SEND.ANGLEVIEW.STATUS`)를 송신합니다.
2. **레시피 동기화**: Master가 `SYNC.0.<RecipeName>` 전송 → `G.SyncRecipe()`가 `C:/KEOC/Recipe/<RecipeName>.xml`을 파일 수정시각 기준으로 변경 시에만 재로드(노광/게인/조명값/Crop ROI 반영).
3. **촬상 시작**: Master가 `STATE.CHECK` 전송 → 장비 에러 상태(`CheckEqError`) 확인 후 이상 없으면 레시피 적용 → 조명 ON → 그래버 Grab Start.
4. **이미지 요청**: Master가 `ReceiveReady` 메시지(CellID, 이미지 인덱스, 메모리 영역/오프셋, Crop Offset/Height, 대상 Node ID 목록)를 전송 → `MessageManager` 큐에 적재.
5. **그랩 완료 대기**: 카메라별 라인 청크가 `ImageManager.AttachImage()`로 설정된 `GrabHeight`까지 누적되면 완료 플래그 세팅, 마지막 카메라 완료 시 조명 자동 OFF.
6. **전송 처리**: 큐에서 완료된 요청을 꺼내 필요 시 ROI Crop(2계열 Master 분할 촬상 지원) → GiGA 보드로 대상 Node에 이미지 메모리 Write(`fn_SendData`, 실패 시 최대 3회 재시도 + LinkCheck) → 로컬 저장 옵션(`LocalSave`) 처리.
7. **완료 응답**: `ATS.SEND.ANGLEVIEW.ImgReady.<idx>.<width>.<height>.<orgheight>.<nodecount>.<node:...>` 전송.
8. **안전 타이머**: `GrabTimeout`(기본 45초) 내 그랩이 끝나지 않으면 자동으로 GrabStop.

## 5. 통신 프로토콜 (Master ↔ Grab PC)

TCP 프레임은 고정 헤더 + 가변 길이 페이로드로 구성됩니다.

```
[STX(0x05)] [0000] [LLLL: 페이로드 길이 4자리] [페이로드 문자열] [ETX(0x0A)] [\0]
```

페이로드는 `.`으로 구분된 필드로 구성되며 대표 메시지는 다음과 같습니다.

| 방향 | 메시지 예 | 설명 |
|---|---|---|
| Master→Grab | `STA.SEND.ANGLEVIEW.ReceiveReady.<CellID>.<이미지idx>.<메모리영역>.<메모리offset>.<CropOffsetY>.<CropHeight>.<대상노드수>.<노드:...>` | 이미지 요청 |
| Master→Grab | `STA.SEND.ANGLEVIEW.STATE.CHECK` | 촬상 시작 지시 |
| Master→Grab | `STA.SEND.ANGLEVIEW.SYNC.0.<RecipeName>` | 레시피 동기화 |
| Grab→Master | `ATS.SEND.ANGLEVIEW.STATUS` | Heartbeat (1.5초 주기) |
| Grab→Master | `ATS.SEND.ANGLEVIEW.TEMP.<BoardTemp>.<FpgaTemp>` | GiGA 보드 온도 보고 (3분 주기) |
| Grab→Master | `ATS.SEND.ANGLEVIEW.ImgReady.<idx>.<width>.<height>.<orgheight>.<노드수>.<노드:...>` | 전송 완료 응답 |
| Grab→Master | `ATS.SEND.ANGLEVIEW.ERROR.EQP_ERROR.[CAMERA\|LIGHT\|GIGABOARD\|UNKNOWN]` | 장비 이상 보고 |
| Grab→Master | Ack (수신 메시지의 `STA.`→`ATS.`, `SEND.`→`RECV.` 치환) | 수신 확인 |

## 6. 그래버 추상화 (Euresys / Matrox)

`GrabberManager`가 `SystemParam.UseEuresys`(bool) 값으로 내부적으로 완전히 다른 두 구현체 리스트(`List<EuresysGrabber>` / `List<MatroxGrabber>`)를 선택적으로 운용하며, 상위 코드(`G.Init`, `SetCurrRecipe` 등)는 동일한 인터페이스로 호출합니다. Matrox 선택 시 세부 보드 모델은 `SystemParam.BoardType`(`MILBOARD_TYPE` enum, `MatroxGrabber.cs`에 정의)을 그대로 `MatroxGrabber.fn_Init`에 전달합니다 — 과거에는 자유 텍스트를 `Contains()`로 부분 매칭했으나(예: `"CXP"`만 입력하면 매칭 실패 후 조용히 기본값으로 대체되는 문제가 있었음), 현재는 enum 값이라 잘못된 값을 넣으면 설정 로드시 즉시 실패하고 기본값(`EN_BT_RADIENTEVCL`)으로 남습니다.

- **EuresysGrabber** (`600_Device/Camera/EuresysGrabber.cs`)
  - `EGrabberDiscovery`로 슬롯을 탐색해 요청 대수만큼 연결
  - **Live(Setup) 모드**: FreeRun, 256라인 버퍼, 33ms throttle로 화면 표시
  - **Production 모드**: 보드 CIC(RC 제어) + 카메라 LineStart 트리거(CXP) 조합으로 라인스캔 트리거링. 센서 1펄스(LIN1) → 보드가 내부 클럭으로 N라인 시퀀스를 생성(`SequenceLength`=GrabHeight)해 1024라인 청크 단위로 수신, `ImageManager`가 누적
  - 라인주기 목표값(카메라별 `SystemParam.CamLineRate1~4`, Hz 단위. 기본 11049.7Hz = 90.5us, 현장 200mm/s 스캔 조건 기준)에 맞춰 노광시간을 자동 캡핑. Setup 화면 CAM SETTING 탭에서 카메라별로 조정 가능(§6-1 참고)
  - Mono10/12/16 포맷은 CV_16UC1로 받아 8bit로 비트시프트 변환
  - 센서 입력 라인(`IIN11`)의 현재 레벨을 읽는 `fn_TryGetSensorInput()` 제공 (§11 참고)
- **MatroxGrabber** (`600_Device/Camera/MatroxGrabber.cs`)
  - MIL SDK 기반, 카메라별 DCF 파일(H/W 트리거 및 Grab Start IO 설정)로 초기화
  - `MdigProcess` 비동기 그랩 + Hook 콜백으로 프레임 수신
  - 노광/게인은 `VieworksCamera`(시리얼) 경유로 제어

### 6-1. 카메라별 라인레이트 (Euresys)

카메라(렌즈+센서 조합)마다 실제 픽셀 분해능이 다를 수 있어, 목표 라인레이트를 카메라별로 따로 둡니다.

```
라인레이트(Hz) = 1e6 / 라인주기(us)
라인주기(us)  = 물체 위 픽셀분해능(um) / 이송속도(mm/s)
```

- `SystemParam.CamLineRate1~4`(Hz)에 저장되며, Setup 화면 **CAM SETTING** 탭의 각 카메라 패널에 Exposure Time/Gain과 함께 `Line Rate` 항목으로 노출됩니다. 기본값은 기존 하드코딩 상수와 동일한 `11049.7Hz`(라인주기 90.5us).
- `EuresysGrabber.fn_SetExternalTrigger()`가 그랩을 시작할 때마다 자기 카메라 인덱스로 이 값을 새로 읽으므로, 재시작 없이 **다음 그랩부터 바로 반영**됩니다. 단, Setup 화면의 라이브뷰(`Grab` 버튼)는 FreeRun 모드라 이 트리거 경로를 타지 않아 변경 효과가 보이지 않습니다 — 실제 반영 확인은 Main 화면의 Manual Grab처럼 트리거 기반 촬상으로 해야 합니다.
- 값을 바꾸면 노출시간 캡(`라인주기 - 8us`)과 보드 CIC의 `CycleMinimumPeriod`도 함께 재계산됩니다.
- 적용 결과는 매 그랩 시작마다 로그로 남습니다 — `Euresys[CAM1] 라인레이트 목표:11049.7Hz 카메라:11049.7Hz 적용주기:90.50us (밴드합산 ON, raw 2배 수신 후 합산)`. **이 값은 §6-2의 밴드 합산 여부와 무관하게 그대로 씁니다** — 합산은 raw 줄 수만 조정할 뿐 사이클(=실제 이동거리 1칸)당 속도 자체는 바꾸지 않습니다.
- 물체가 늘어지거나 눌려 보이면(정사각 비율 안 맞음), 정사각형 물체를 찍어 결과 이미지의 H/W 비율을 재고 `현재 라인레이트 ÷ (H/W 비율)`로 보정값을 계산해 넣으면 됩니다.

### 6-2. 듀얼라인 센서 밴드 합산 (Euresys, GL3516)

`VL-16K3.5X2-M120-I-2`(GL3516 센서)는 물리적으로 줄이 2개(`BandSelector`: `M0`, `M1`)입니다. 실측으로 원인을 확정했습니다 — **두 밴드를 모두 켜면 트리거 1번에 M0/M1 줄이 각각 별도로 출력되어, 정사각형 물체가 세로로 정확히 2배 늘어집니다.** 감도 향상을 위해 두 밴드를 계속 사용하기로 하고, 카메라가 내부적으로 합쳐주는 기능이 없어(확인된 GenApi 트리 범위 내) **소프트웨어에서 직접 합산**합니다.

```
raw:  M0 M1 M0 M1 M0 M1 ...   (트리거마다 2줄)
        └──┴──┘
      픽셀 평균(INTER_AREA 2:1 축소)
        ↓
final: L0  L1  L2 ...          (원래 논리 줄 수로 복원)
```

- `EuresysGrabber.DUAL_BAND_COMBINE`(기본 `true`)로 켜져 있습니다. `false`로 바꾸면 M0 단일 밴드로 되돌아가며, raw 버퍼 배율도 자동으로 원래대로(×1) 계산됩니다.
- 켜져 있으면: 밴드 둘 다 활성화 → 보드/스트림은 raw로 논리 높이의 **2배**를 받음 → `GrabThreadProc`가 raw 청크를 세로 1/2로 합쳐(인접 두 줄 평균) 내보내므로 `ImageManager` 등 이후 파이프라인은 이 사실을 몰라도 됩니다.
  **라인레이트(`CamLineRate1~4`)는 건드리지 않습니다** — `SequenceLength`(보드 CIC "사이클 수")를 논리 높이 그대로 두는 한, "사이클 1번 = 실제 이동거리 1칸"이라는 관계는 밴드 수와 무관하게 유지되고, `GrabThreadProc`의 합산이 그 시점에 이미 raw 2줄을 1줄로 되돌리기 때문입니다. 여기서 라인레이트까지 낮추면 사이클당 이동거리가 2배로 늘어나 **이중 보정(결과가 가로로 눌려 보임)**이 됩니다 — 실제로 초기 구현에서 이 실수를 했다가 바로잡았습니다.
- **Setup 화면 라이브뷰에도 적용됩니다** — 원래는 FreeRun이라 센서가 그대로 M0+M1 두 줄을 내보내 라이브뷰도 늘어져 보였는데, 라이브 버퍼도 같은 방식으로 합산해 정상 비율로 보이게 했습니다.
- **메모리 비용**: raw 버퍼가 2배라 그랩 버퍼 메모리가 카메라당 약 256MB → **512MB**로 늘어납니다.
- Matrox나 단일 밴드 카메라에는 해당 없는 Euresys/이 센서 모델 전용 처리입니다.

## 7. 조명 제어

`LightManager`가 인덱스 0..N을 DAWOO(상부, `CtrlCount`개) → VIT(하부) 순서로 매핑합니다.

- **DawooLight**: 컨트롤러당 시리얼 포트 1개(최대 4개), `0xFF` 헤더 + 커맨드 + XOR 체크섬 바이너리 프로토콜
- **VitLight**: 시리얼 포트 1개로 16채널까지 제어, ASCII 프로토콜(`Dxxxyyy` 설정, `RxxDAT`/`ROONF` 조회, `ONN`/`OFF` + 채널 비트마스크)

카메라 4대 구성에서는 상부 촬상 시 하부 조명 간섭 방지를 위한 별도 제어(`fn_Vit_ON`)가 있습니다.

### 카메라 대수와 조명 구성

`SystemParam.CamCount`는 **1~4** 범위로 제한되며(범위를 벗어난 XML 값은 잘림), 조명 구성은 카메라 대수에서 파생됩니다.

| 카메라 | 상부(DAWOO) | 하부(VIT) | 조명 대수(`LightManager.LightCount`) | 조명 인덱스 매핑 |
|---|---|---|---|---|
| 1대 | 1 | 없음 | 1 | 0 = 상부1 |
| 2대 | 2 | 없음 | **2** | 0~1 = 상부 |
| 3대 | 3 | 1대(4채널) | 4 | 0~2 상부, 3~6 하부 |
| 4대 | 4 | 1대(4채널) | **5** | 0~3 상부, 4~7 하부 |

2대·4대 값(굵게)은 기존 현장 구성과 동일하며, 1대·3대는 같은 규칙을 일반화한 값입니다. 연결 상태 판정(`MainWindow.fn_UpdateState`, `Page_Communication`, `ProtocallManager`의 조명 에러 보고)은 모두 `LightManager.LightCount` 하나를 기준으로 씁니다.

카메라 대수에 따라 자동으로 조정되는 항목:

- **Main 화면** — 카메라 뷰어, GrabState, 센서 I/O 표시 개수
- **Setup 화면** — 카메라 선택 콤보, 상부 조명 선택 콤보, CAM1~4 설정 패널 활성화, 하부 조명 패널 활성화
- **내부** — 카메라 시리얼 포트 연결(Matrox), 레시피의 노광/게인/광량 적용 범위, 마지막 카메라 완료 시 조명 OFF 시점

## 8. 레시피 / 설정 파일

- **시스템 설정**: 실행 파일 위치의 `ImageGrabber.xml` (없으면 최초 실행 시 기본값으로 생성). 카메라 대수, Master IP/Port(최대 2계열), 그래버 벤더(`UseEuresys`)/Matrox 보드 모델(`BoardType`), GrabHeight, GiGA 보드 Node/Link/Mailbox 번호, 시리얼 포트 매핑, 이미지/로그 경로, 카메라별 라인레이트(`CamLineRate1~4`, §6-1), 센서 I/O 설정(§11) 등을 포함. 구버전 XML의 `<BoardType>Coaxlink Quad G3</BoardType>` 같은 자유 텍스트 값은 `MILBOARD_TYPE` enum 이름이 아니므로 로드 시 해당 필드만 무시되고 기본값으로 대체됩니다(다른 설정에는 영향 없음). Matrox 보드를 쓰는 현장은 업그레이드 시 `BoardType` 값을 enum 이름(예: `EN_BT_RADIENTCXP`)으로 갱신해야 합니다.
- **레시피**: `C:/KEOC/Recipe/<RecipeName>.xml`. 카메라별 노광/게인(최대 4채널), 상/하부 조명값, Crop ROI 테이블(2계열 Master 분할 촬상 시 이미지당 2개 ROI) 포함.
- XML 직렬화는 리플렉션 기반 커스텀 매니저(`010_Common/XmlManager.cs`, `FalconWpf` 네임스페이스)를 사용하며 `DataTable` 프로퍼티(ROI 등)도 자동 저장/복원합니다.

## 9. 이미지/로그 저장

- **이미지 저장 경로**는 앱 자체 설정이 아니라 외부 `D:\MAVT\INI\MAVT.ini`의 `[General] Image Save Path` 값을 `GetPrivateProfileString`으로 매 저장 시점마다 읽어와 사용합니다(상위 비전 시스템과 경로 연동).
- 저장은 `DiskManager`의 백그라운드 스레드가 큐를 통해 비동기로 처리(`Cv2.ImWrite`).
- 로그는 카테고리별 CSV로 `yyyy/MM/dd` 폴더 구조에 저장되며(`Trace`/`Communication`/`GiGABoard`/`Error`), `CallStack` 옵션 활성화 시 호출 스택(클래스/메서드) 정보가 함께 기록됩니다.
- ⚠️ 드라이브 용량 기반 자동 삭제/보관 기능(파일 감시 + 30일 보관 정책)은 현재 코드상 **비활성화**되어 있습니다(`DiskManager.fn_StartThread` 주석 참고, 2025-04-01 현장 반영). 필요 시 재활성화 검토 필요.

## 10. 사용자 권한

`EN_AUTHORITY`: `EN_OPERATOR`(0) < `EN_MAINTENANCE`(1) < `EN_ENGINEER`(2). 비밀번호는 `100_Define/Global.cs`의 `Define` 클래스에 정의(`PASSWORD_EN = "keoc"`, OP/MA는 비워짐). Operator 권한으로는 설정 화면 진입 및 프로그램 종료가 제한됩니다(`MainWindow.Window_Closing`에서 강제 차단).

## 11. 센서 입력 I/O 모니터링

물체 감지 센서가 프레임그래버까지 실제로 신호를 보내고 있는지 화면에서 바로 확인할 수 있습니다.

### 배선

| 항목 | 값 |
|---|---|
| 커넥터 | Euresys Coaxlink 15pin D-Sub |
| Pin 3 | `IIN11+` (Isolated input #11 – Positive pole) |
| Pin 12 | `IIN11-` (Isolated input #11 – Negative pole) |

이 라인은 `EuresysGrabber.fn_SetExternalTrigger()`에서 `LineInputTool` 설정으로 논리라인 **LIN1**에 매핑되며, LIN1의 상승 에지 1펄스가 `StartOfSequenceTriggerSource`로 들어가 **`SequenceLength`(= `GrabHeight`) 라인만큼 스캔**을 시작시킵니다. 즉 화면의 램프가 켜지는 신호와 스캔을 시작시키는 신호는 **동일한 물리 라인**입니다.

### 동작

- `SensorIOManager`(`600_Device/IO/SensorIOManager.cs`)가 백그라운드 스레드로 라인 레벨을 폴링합니다. 읽기는 Euresys Interface 모듈의 `LineSelector`(= `IIN11`) → `LineStatus` 조합이며, 트리거를 소비하지 않으므로 grab 중에도 그대로 사용할 수 있습니다.
- 상승 에지를 누적 카운트하고 마지막 검출 시각·펄스 폭을 보관합니다. 신호가 짧아도 눈에 보이도록 검출 후 일정 시간(코드 고정 1000ms) 동안 램프를 켜 둡니다.
- 한 보드(Interface)에 여러 카메라(Device)가 붙어 있으면 I/O 커넥터는 하나이므로 해당 카메라들은 하나의 채널을 공유하고, 보드 접근도 보드 수만큼만 발생합니다.
- `SensorLogEnable`이 켜져 있으면 검출 시 `[SENSOR] IIN11 신호 검출 (BOARD1, #12)` 형태로 로그를 남깁니다.

### 화면

Main 화면 우측 GrabState와 Log 사이에 **한 줄짜리 상태 표시줄**로 붙어 있습니다(높이 22px). 좌측에 `SENSOR IIN11`, 우측에 카메라별 램프 + 누적 횟수(`CAM1 12`)가 표시되며, 램프는 신호 검출 시 초록으로 켜집니다. 마우스를 올리면 마지막 검출 시각과 펄스 폭이 툴팁으로 나오고, 더블클릭하면(Operator 권한 제외) 카운트를 초기화합니다.

| 표시 | 의미 |
|---|---|
| 회색 램프 + `CAM1 0` | 모니터링 중이며 아직 신호 없음 → 센서/배선 또는 대상물 통과 여부 확인 |
| 초록 램프 + `CAM1 n` | 신호가 보드까지 들어옴. 그래도 스캔이 안 되면 보드 이후(트리거/카메라 설정) 문제 |
| `SENSOR OFF`, `CAM1 -` | 모니터링 비활성(`UseSensorIO=false` 또는 Matrox 사용 중) |
| 툴팁 `라인 상태를 읽지 못함` | 그래버 미연결 또는 `LineStatus` 미지원 |

### 설정 (`ImageGrabber.xml`)

| 키 | 기본값 | 설명 |
|---|---|---|
| `UseSensorIO` | `true` | 센서 I/O 모니터링 사용 여부 |
| `SensorInputLine` | `IIN11` | 센서가 물린 Interface 라인. 트리거 소스(LIN1)에도 함께 적용됨 |
| `SensorLogEnable` | `true` | 검출 시 로그 기록 여부 |
| `SensorTriggerDelay1~4` | `0` | 카메라별(0:Front 1:Rear 2:InSide 3:OutSide) 센서 ON 후 스캔 시작까지 지연(**ms**, 1000 = 1초). 0 = 지연 없음. `CamExposure1~4`와 동일한 관례 |
| `SensorDelayTool` | `DEL1` | 지연에 사용할 IOToolbox 블록 이름 (DEL1~DEL4). 카메라마다 자기 보드의 블록을 쓰므로 공용 이름을 써도 충돌 없음 |

### 촬상 지연 (센서 ON → 스캔 시작)

센서와 카메라 시야 사이의 거리를 보정하기 위해, 센서 신호가 들어온 뒤 일정 시간 기다렸다가 스캔을 시작할 수 있습니다. 지연은 **보드 하드웨어(IOToolbox DelayTool)**가 처리하므로 소프트웨어 지터가 없고, **카메라(보드)별로 독립적으로** 설정됩니다 — 카메라마다 센서-시야 간 거리가 다를 수 있기 때문입니다.

```
IIN11 ──▶ LIN1 ──▶ DelayTool(DEL1) ──▶ StartOfSequenceTriggerSource ──▶ N라인 시퀀스
                    (SensorTriggerDelay1~4 ms, 카메라별)
```

- `SensorTriggerDelay1~4`는 카메라 인덱스(0:Front 1:Rear 2:InSide 3:OutSide)별 값이며 **ms 단위**입니다(`1000` = 1초). 보드가 실제로 요구하는 단위는 us이므로, `SystemParam.fn_GetSensorTriggerDelay(idx)`가 XML의 ms 값을 1000배 해 us로 환산한 뒤 `EuresysGrabber`에 넘깁니다 — 이후 계산(클럭 선택, 틱 환산, 로그)은 모두 이 us 단위 기준입니다.
- 카메라는 각자 자기 보드에서 지연을 계산·적용하므로 다른 카메라의 값에 영향을 주지 않습니다.
- 해당 카메라의 값이 `0`이면 지연 블록을 거치지 않고 기존대로 `LIN1`이 직접 시퀀스를 시작합니다.
- `DelayToolDelayValue`는 시간이 아니라 `DelayToolClockSource`의 **틱 수**이므로, 코드가 요청 지연을 담을 수 있는 가장 분해능 높은 클럭을 자동으로 골라 환산합니다. `DelayToolClockSource`는 `TIME8NS`/`TIME200NS`/`TIME1US`처럼 주기를 직접 이름에 담은 형식과 `MHz100` 같은 주파수 형식을 모두 인식합니다. 설정 후 readback으로 반영 여부를 확인하고, 값이 잘리면(레지스터 폭 초과) 다음 클럭으로 재시도합니다.
- 설정 결과는 카메라별로 로그에 남습니다(us 단위) — `Euresys[CAM1] 트리거 지연 설정: 10000000.0us (요청 10000000us, DEL1 clk:TIME1US 10000000tick, 분해능 1.00us) → DEL11` (= XML `SensorTriggerDelay1 = 10000`, 10초)
- **설정에 실패하면 해당 카메라만 지연 없이(LIN1 직결) 동작하며 그 사실을 로그에 남깁니다.** 다른 카메라의 지연 설정에는 영향을 주지 않고, 조용히 잘못된 지연이 적용되는 일도 없습니다. 실패 로그에는 `DelayToolClockSource`의 실제 열거값이 함께 찍히므로, 보드/드라이버 버전이 달라 이름 형식이 다르면 바로 확인할 수 있습니다.
- UI의 센서 램프는 지연 전 **물리 라인(IIN11)**을 그대로 읽으므로, 램프가 켜지는 시점은 지연과 무관하게 센서가 실제로 감지한 순간입니다.
- 참고: 이송 200mm/s 기준 `1ms = 0.2mm`.

### 제약

- 폴링 방식이라 폴링 주기보다 짧은 펄스는 놓칠 수 있습니다. 또한 grab 스레드가 버퍼를 pop 하는 동안에는 보드 접근이 잠시 막혀(`EGrabber is busy in another thread`) 폴링이 몇 회 건너뛸 수 있으며, 이때는 마지막 값을 유지합니다. **카운트는 신호 유입 확인용 참고값이며 실제 트리거 횟수와 정확히 일치하지 않을 수 있습니다.**
- **Matrox는 미지원**입니다. `GrabberManager.fn_TryGetSensorInput()`의 `TODO(Matrox)` 위치에 `fn_GetSpecificIO(idx, MIL.M_AUX_IO*)`를 연결하면 동일한 UI를 그대로 사용할 수 있으며, 사용 핀은 실제 배선 확인 후 결정해야 합니다. Matrox 선택 시에는 모니터링이 자동으로 비활성화됩니다.

## 12. 빌드 방법

1. Visual Studio 2019/2022 + .NET Framework 4.8 Developer Pack
2. NuGet 패키지 복원 (`OpenCvSharp4`, `OpenCvSharp4.runtime.win`, `OpenCvSharp4.WpfExtensions` 등, `packages.config` 참조)
3. 다음 SDK/드라이버가 별도로 설치되어 있어야 합니다.
   - **Euresys eGrabber** (Coaxlink 드라이버 + `EGrabber.NETFramework.dll`) — `bin\x64\Debug\` 경로에 배치 필요
   - **Matrox Imaging Library(MIL)** — `Lib\Matrox.MatroxImagingLibrary.dll` (Matrox 보드 사용 시)
   - **Interface APX-7402 SDK** (`apx7400Lib`, GiGA 광링크 보드)
4. 플랫폼은 `x64`만 지원(AnyCPU 빌드 불가), 출력 경로는 `bin\x64\Debug` / `bin\x64\Release`

## 13. 알려진 제약 / TODO

- 센서 입력 I/O 모니터링과 촬상 지연은 Euresys 전용이며 Matrox 지원은 미구현(§11 참고)
- 촬상 지연의 `DelayTool` 출력 이름(`StartOfSequenceTriggerSource`의 열거값)은 보드/드라이버 버전에 따라 다를 수 있어 코드가 열거값을 검색해 매칭합니다. 현장 첫 적용 시 로그로 실제 선택된 이름을 확인할 것
- 카메라 1대·3대 구성은 코드상 지원되나 현장 검증 이력이 없음(§7 참고). 하부 조명 채널 매핑(`CamCount + n`)은 4대 구성 기준을 일반화한 것이라 실제 배선 확인 필요
- 카메라별 `CamLineRate1~4`는 Setup 화면에서 값을 바꿔도 그 자리에서 결과를 볼 수 없음(라이브뷰가 FreeRun이라 트리거 경로를 안 탐) — 실제 촬상(Manual Grab 등)으로만 확인 가능(§6-1 참고)
- §6-2 밴드 합산은 소프트웨어(픽셀 평균) 방식입니다. 카메라/센서가 하드웨어 자체적으로 두 밴드를 합쳐 내보내는 기능(TDI류)을 지원하는지는 확인되지 않았습니다 — 있다면 그쪽이 더 정확할 수 있어 카메라 매뉴얼/제조사 확인 필요
- `GrabberManager.IsGrabbing`은 "전부 그랩 중"의 부정(=하나라도 멈춤)을 반환해 이름과 의미가 반대. `ImageManager.ComplateImage`에서 종료 판정에 쓰이므로 수정 시 동작 확인 필요
- 드라이브 용량 기반 이미지 자동 삭제 기능 비활성화 상태(§9 참고)
- Setup 화면에 `UseEuresys`/`BoardType` 편집 UI가 없어 현재는 `ImageGrabber.xml` 파일을 직접 수정해야 함
- 2계열 Master(`JavasCount == 2`) 운용 시 ROI 인덱싱은 `이미지idx * 2 (+1)` 규칙에 의존하므로 레시피의 `CropROI` 행 순서가 중요
