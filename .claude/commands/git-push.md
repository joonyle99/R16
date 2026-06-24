---
description: 변경사항을 작업 단위로 나눠 커밋하고 push
allowed-tools: Bash(git status:*), Bash(git diff:*), Bash(git add:*), Bash(git commit:*), Bash(git push:*), Bash(git branch:*), Bash(git fetch:*), Bash(git pull:*), Bash(git log:*)
---

## 현재 상태
- 변경 파일: !`git status --short`
- 변경 요약: !`git diff --stat`
- 현재 브랜치: !`git branch --show-current`

## 작업
위 변경사항을 작업 단위로 나눠 커밋한 뒤 push한다.

1. 변경사항이 없으면 그 사실만 알리고 종료한다.
2. 변경사항을 논리적 단위로 묶어 커밋 분리
   - 스크립트(.cs) / 씬(.unity) / ProjectSettings 변경은 별도 커밋
   - 연관된 파일끼리 묶어 커밋
   - **Unity .meta 파일은 항상 해당 에셋과 같은 커밋에 포함**한다
3. 커밋 메시지는 한 줄 요약(+필요 시 본문). 예: `feat: 적 이동 경로 스플라인 적용`
4. 각 단위별로 파일을 명시해 `git add <파일>` → `git commit` 반복 (`git add .` 금지)
5. push 전 원격 동기화 확인:
   - `git fetch` 후 로컬이 원격보다 뒤처져 있으면 `git pull`(merge)로 먼저 동기화한다
   - merge 중 충돌이 나면 중단하고 충돌 파일을 보여준 뒤 사용자 판단을 기다린다
6. `git push` (upstream 없으면 `-u origin <브랜치>`)
7. 커밋/pull/push가 실패하면 중단하고 오류 원문을 그대로 보여준다. `--no-verify`나 강제 push로 임의 우회하지 않는다.

별도 확인 없이 바로 진행한다.