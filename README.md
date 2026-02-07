# ⚔️ 이터널리턴 : 코발트 프로토콜

<div align="center">
  <a href="https://www.youtube.com/watch?v=eqiD9e247hg">
    <img src="https://img.youtube.com/vi/eqiD9e247hg/0.jpg" alt="프로젝트 영상" width="600">
  </a> 
  <br>
  <sub style="color: gray;">▶️ Click!</sub>
</div>

<br>

## 📅 1. 프로젝트 정보
- **개발 기간:** 2025.10 ~ 2025.12.12
- **프로젝트 개요:** 전략적 팀 전투와 거점 점령이 결합된 MOBA 장르의 멀티플레이 프로젝트입니다. 

<br>

## 🛠 2. 기술 스택
- **Engine:** Unity 6 
- **Language:** C#
- **IDE:** Visual Studio 2022
- **Version Control:** Git / GitHub
- **Collaboration:** Notion, Discord

<br>

## 👥 3. 역할 분담
| 이름 | 담당 역할 |
| :--- | :--- |
| **이나영 (ME)** | 플레이어(테오도르), 몬스터(AI), 이펙트/사운드 프레임워크, 환경 오브젝트 |
| **안정현** | 플레이어(아비게일), 서버, 맵, 스킬 충돌 처리 |
| **박준수** | 플레이어(유키, 프레임워크), 채팅, 카메라 |
| **박연진** | 플레이어(로지, 프레임워크), 환경 오브젝트 |
| **권용수** | 플레이어(현우), UI 전반 |

<br>

## 🚀 4. 핵심 구현 내용
- 캐릭터 스킬 연계와 은신(Bush)를 활용한 시야 심리전, 중립 몬스터 사냥 등 **전술적 전투를 필요로 하는 콘텐츠**를 담당했습니다.
  
| 👾 몬스터 (AI) | 🗡️ 플레이어 (Control) | 🔊 이펙트/사운드 | 🍄 환경/전술 (Env) |
| :--- | :--- | :--- | :--- |
| [**🧠 FSM / BT 구조**](링크)<br>`FSM` `Behavior Tree` | [**⚡ 선입력 & 캔슬**](링크)<br>`Input Buffer` `Anim Cancel` | [**💥 타격감 동기화**](링크)<br>`Anim Event` `Audio Sync` | [**🌿 은신 & 시야**](링크)<br>`Physics.Overlap` `Server Auth` |
| [**⚔️ 추적 알고리즘**](링크)<br>`NavMesh` `Funnel Modifier` | [**🔭 저격 모드 카메라**](링크)<br>`Dynamic Camera`  | | [**👁️ X-Ray 투시**](링크)<br>`Stencil Buffer` `Occlusion` |
| | [**🎯 Overlay 인디케이터**](링크)<br>`Camera Stacking` `URP` | | [**📐 확장 스킬**](링크)<br>`OBB Collision` `SAT` |

<br>

