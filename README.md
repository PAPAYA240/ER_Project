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
| [**🧠 FSM / BT 구조**](https://gist.github.com/PAPAYA240/b00553e7e2e3fed6ba989559c3a53a7f)<br>`FSM` `Behavior Tree` | [**⚡ 선입력 & 캔슬**](https://gist.github.com/PAPAYA240/28c82e2124d1cd0b3a0f30fc90e4a821)<br>`Input Buffer` `Anim Cancel` | [**💥 타격감 동기화**](https://gist.github.com/PAPAYA240/af7c579b4aa6380699eb4a908345b667)<br>`Effect` `Sound` | [**🌿 은신 & 시야**](https://gist.github.com/PAPAYA240/48b1b1b04979cd8cbaa6d198346a024c)<br>`Physics.Overlap` `Server Auth` |
| [**⚔️ AI 길찾기**](https://gist.github.com/PAPAYA240/da494eef1ece7720d5d8770603122e37)<br>`NavMesh` `Funnel Modifier` | [**🔭 저격 모드 카메라**](https://gist.github.com/PAPAYA240/d9c3b4fe9d52355c7b5b4d7aa6b5c764)<br>`Dynamic Camera`  | | [**👁️ X-Ray 투시**](https://gist.github.com/PAPAYA240/47bc0b9259ed091a19ce7120429f0b58)<br>`Stencil Buffer` `Occlusion` |
| | [**🎯 Overlay 인디케이터**](https://gist.github.com/PAPAYA240/9f5bbce150539bbd86b2fbbe815cd2d1)<br>`Camera Stacking` `URP` | | [**📐 확장 스킬**](https://gist.github.com/PAPAYA240/9f5bbce150539bbd86b2fbbe815cd2d1)<br>`OBB Collision` `SAT` |

<br>

