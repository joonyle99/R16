# 콤보 러쉬 - 만화식 날려보내기 연출

## 목표
콤보 러쉬 중 플레이어에게 닿은 대상이 빙글빙글 돌며 좌/우 화면 밖으로 날아간다.
- 적: 즉시 파괴 대신 날아가는 연출로 교체
- 지면: "마커 스크립트가 붙은 지면"만 날아가고 플레이어는 관성 유지하며 통과
- 일반 지면/벽: 평소대로 막힘/바운스
- 방향: 좌/우 번갈아

## 구현 항목
- [ ] `RushLaunchable.cs` 신규 — 날려보내기 연출 + 지면 마커 겸용
- [ ] `EnemyBehaviour.LaunchAway()` — currHp=0(AI/카운팅 정지) 후 RushLaunchable 동적 부착·발사
- [ ] `PlayerBehaviour.OnPropelledHit(enemy)` — 러쉬 중이면 TakeDamage 대신 LaunchAway
- [ ] `PlayerBehaviour.OnCollisionEnter2D` — 러쉬 중 RushLaunchable 지면 감지 시 발사 + 관성 복원 + 바운스 스킵
- [ ] `PlayerBehaviour.FixedTick` — 충돌 직전 속도 캐시

## 검증
- [ ] 컴파일 (IDE diagnostics)
- [ ] 로직 점검 (미션 카운팅 유지, 마커/비마커 지면 분기)

## 리뷰
(구현 후 작성)
