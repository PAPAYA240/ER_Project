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
- **Language:** C#
- **Graphics API:** Unity
- **Tools:** Visual Studio 2022,

<br>

## 👥 3. 역할 분담
| 이름 | 담당 역할 |
| :--- | :--- |
| **이나영** | 플레이어(테오도르), 몬스터(AI), 이펙트/사운드 프레임워크, 환경 오브젝트 |
| **안정현** | 플레이어(아비게일), 서버, 맵, 스킬 충돌 처리 |
| **박준수** | 플레이어(유키, 프레임워크), 채팅, 카메라 |
| **박연진** | 플레이어(로지, 프레임워크), 환경 오브젝트 |
| **권용수** | 플레이어(현우), UI 전반 |

<br>

## 🚀 4. 주요 구현 내용
- 캐릭터 스킬 연계와 부쉬(은신)를 활용한 시야 심리전, 중립 몬스터 사냥 등 다양한 전술적 전투를 필요로 하는 콘텐츠를 담당했습니다.
  
| 👾 몬스터 (Monster) | 🗡️ 플레이어 (Player) | 🔊 이펙트/사운드 | 🍄 **기타 환경 오브젝트** |
| :--- | :--- | :--- | :--- |
| **Client**: Behavior Tree <br> [코드 보러가기](https://gist.github.com/PAPAYA240/b00553e7e2e3fed6ba989559c3a53a7f) | **Player Input Manager** <br> [코드 보러가기](https://github.com/PAPAYA240/ER_Project/blob/b2cc2ae85f6b1ed3ea839192021584c4d4da0eec/Client/Assets/Scripts/Controllers/Player/PlayerInput/TheodoreInputController.cs#L110) | **이펙트 시스템** <br> [코드 보러가기](https://github.com/PAPAYA240/ER_Project/blob/b2cc2ae85f6b1ed3ea839192021584c4d4da0eec/Client/Assets/Scripts/Fx/EffectFXManager.cs#L50) | **Bush (은신)** <br> [코드 보러가기](https://github.com/PAPAYA240/ER_Project/blob/b2cc2ae85f6b1ed3ea839192021584c4d4da0eec/Client/Assets/Scripts/Environment/Env_Bush.cs#L29) |
| **Server**: FSM : IdleState 예시 <br> [코드 보러가기](https://github.com/PAPAYA240/ER_Project/blob/b2cc2ae85f6b1ed3ea839192021584c4d4da0eec/Server/Server/Game/Object/Monster/FSM/BaseFSM/IdleState.cs#L8) | **Camera : 조준 스킬** <br> [코드 보러가기](https://github.com/PAPAYA240/ER_Project/blob/b2cc2ae85f6b1ed3ea839192021584c4d4da0eec/Client/Assets/Scripts/Controllers/CameraController.cs#L170) | **사운드 시스템** <br> [코드 보러가기](주소) | **X-Ray 시스템 세팅** <br> [코드 보러가기](https://github.com/PAPAYA240/ER_Project/blob/b2cc2ae85f6b1ed3ea839192021584c4d4da0eec/Client/Assets/Scripts/Controllers/PlayerController.cs#L945) |
| **AI**: A* 알고리즘 <br> [코드 보러가기](https://github.com/PAPAYA240/ER_Project/blob/b2cc2ae85f6b1ed3ea839192021584c4d4da0eec/Server/Server/Game/Object/Monster/AStar/Pathfinding.cs#L51) | **Player Indicator : Overay Camera** <br> [코드 보러가기](https://github.com/PAPAYA240/ER_Project/blob/b2cc2ae85f6b1ed3ea839192021584c4d4da0eec/Client/Assets/Scripts/Controllers/CameraController.cs#L51) | | **OBB 충돌** <br> [코드 보러가기](https://github.com/PAPAYA240/ER_Project/blob/b2cc2ae85f6b1ed3ea839192021584c4d4da0eec/Server/Server/Game/Collision/CollisionManager.cs#L1135) |
| **AI**: Funnel 알고리즘 <br> [코드 보러가기](https://github.com/PAPAYA240/ER_Project/blob/b2cc2ae85f6b1ed3ea839192021584c4d4da0eec/Server/Server/Game/Object/Monster/AStar/Funnel.cs#L50) | | | |
<br>

## 🚀 5. 문제 해결

<br>

