# 🚀 GitHub Issue Slack Notification Setup Guide

이슈 생성 시 Gemini AI로 자동 요약하고 Slack으로 알림을 보내는 워크플로우가 추가되었습니다!

## ⚙️ 필수 설정

### 1. GitHub Repository Secrets 설정

Repository → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

#### Secret 1: `GEMINI_API_KEY`
- **발급 방법**: https://makersuite.google.com/app/apikey
- "Create API Key" 클릭 후 복사
- **형식**: `AIzaSy...`

#### Secret 2: `SLACK_WEBHOOK_URL`
- **발급 방법**: https://api.slack.com/apps
  1. "Create New App" → "From scratch"
  2. App 이름 입력, Workspace 선택
  3. 좌측 메뉴 "Incoming Webhooks" 클릭
  4. "Activate Incoming Webhooks" 토글 ON
  5. "Add New Webhook to Workspace" 클릭
  6. 알림 받을 채널 선택 후 Webhook URL 복사
- **형식**: `https://hooks.slack.com/services/...`

### 2. Actions 활성화 확인

Repository → **Actions** 탭
- "Issue Notification with AI Summary" 워크플로우 확인

## 📬 알림 형식

```
🎯 새로운 이슈가 생성되었습니다

제목: [이슈 제목]
요약: [Gemini AI 자동 요약]

담당자: [할당된 담당자]
작성자: [이슈 작성자]

내용: [이슈 본문 (최대 500자)]

[이슈 바로가기 →]
```

## 🧪 테스트

이슈를 하나 생성해보세요!

### 예시 이슈
```
제목: [Bug] Unity 플레이어 이동 오류

내용:
## 문제 설명
플레이어가 벽을 통과하는 현상이 발생합니다.

## 재현 방법
1. 플레이어를 벽 쪽으로 이동
2. 벽에 닿으면 통과함

## 환경
- Unity 2023.2 LTS
```

## ✅ 동작 확인

1. **Actions 탭**에서 워크플로우 실행 확인
2. **Slack 채널**에서 알림 수신 확인
3. AI 요약 품질 확인

## 🔧 커스터마이징

`.github/workflows/issue-notify.yml` 파일에서:

- **요약 프롬프트 변경**: `summarizeWithGemini` 함수의 `prompt` 수정
- **Slack 메시지 포맷 변경**: `sendSlackNotification` 함수의 `blocks` 수정
- **트리거 조건 변경**: `on.issues.types` 수정 (opened, edited, closed 등)

## 📚 참고 문서

- [GitHub Actions](https://docs.github.com/en/actions)
- [Gemini API](https://ai.google.dev/docs)
- [Slack Block Kit](https://api.slack.com/block-kit)

## 💡 주의사항

- Gemini API 무료 티어: 분당 60회 제한
- Slack Webhook URL은 절대 공개 금지
- Private 저장소에서도 동작

---

문제가 있으면 이슈를 생성해주세요!
