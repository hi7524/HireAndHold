# 📊 일간보고 자동화 설정 가이드

매일 오전 9시에 지난 24시간 동안의 GitHub 이슈 활동을 자동으로 요약하여 Notion에 일간보고 페이지를 생성합니다.

## 🎯 기능

- ✅ **완료된 이슈** - 24시간 내 Close된 이슈
- 🆕 **새로 생성된 이슈** - 24시간 내 생성된 이슈
- 🔄 **업데이트된 이슈** - 제목/본문/라벨/담당자 등이 변경된 이슈
- 💬 **활발한 논의** - 댓글이 추가된 이슈
- 🤖 **AI 요약** - Gemini API가 전체 활동을 3-4문단으로 요약

## ⚙️ 필수 설정

### 1. Repository Secrets

HireAndHold → Settings → Secrets and variables → Actions

#### `NOTION_TOKEN` (새로 추가 필요)

**발급 방법:**
1. https://www.notion.so/my-integrations 접속
2. "+ New integration" 클릭
3. 정보 입력:
   - Name: `GitHub Daily Report`
   - Associated workspace: 본인 워크스페이스 선택
   - Type: Internal integration
4. Submit 클릭
5. "Internal Integration Secret" 복사 (starts with `secret_`)
6. GitHub Secrets에 추가

**Notion 페이지 권한 설정:**
- 일간보고를 생성할 Notion 워크스페이스에서
- 우측 상단 `...` → `Connections` → `GitHub Daily Report` 연결

#### 기존 Secrets (이미 설정됨)
- ✅ `GEMINI_API_KEY` - 이미 설정됨
- ✅ `GITHUB_TOKEN` - 자동 제공됨 (설정 불필요)

## 📅 실행 시간

- **자동 실행**: 매일 오전 9시 (KST)
- **수동 실행**: Actions 탭 → "Daily Report to Notion" → "Run workflow"

## 📄 생성되는 페이지 구조

```markdown
# 25.11.09 일간보고

## 📊 오늘의 활동 요약
[Gemini AI가 생성한 전체 요약]
- 완료된 작업 요약
- 새로운 이슈 요약
- 진행 중인 작업 상황
- 주요 논의 내용

## ✅ 완료된 이슈 (N건)
- [#17] 이슈 제목 - 바로가기
- [#16] 이슈 제목 - 바로가기

## 🆕 새로 생성된 이슈 (N건)
- [#18] 이슈 제목 - 바로가기

## 🔄 진행 중인 이슈 업데이트 (N건)
- [#15] 이슈 제목 - 바로가기

## 💬 활발한 논의 (N건)
- [#12] 이슈 제목 (댓글 5개) - 바로가기
```

## 🧪 수동 테스트 방법

1. GitHub → HireAndHold → Actions 탭
2. 좌측 "Daily Report to Notion" 워크플로우 선택
3. 우측 "Run workflow" 버튼 클릭
4. "Run workflow" 확인
5. 실행 완료 후 로그 확인
6. Notion 워크스페이스에서 새 페이지 확인

## 🔍 트러블슈팅

### Notion API 오류

**증상**: "Could not create page" 에러

**해결방법**:
1. Notion Integration이 올바르게 생성되었는지 확인
2. Integration을 워크스페이스에 연결했는지 확인
3. NOTION_TOKEN Secret이 올바른지 확인

### 이슈가 없을 때

24시간 동안 활동이 없으면 각 섹션이 비어있을 수 있습니다. 이는 정상입니다.

### Gemini API 오류

**증상**: 요약이 "요약 생성 중 오류가 발생했습니다"로 표시

**해결방법**:
1. GEMINI_API_KEY가 올바른지 확인
2. Gemini API 할당량 확인 (무료: 분당 60회)
3. https://makersuite.google.com 에서 API 상태 확인

## 🎨 커스터마이징

### 실행 시간 변경

`.github/workflows/daily-report.yml` 파일의 cron 표현식 수정:

```yaml
schedule:
  # 매일 오후 6시 (KST) = 매일 오전 9시 (UTC)
  - cron: '0 9 * * *'
```

### 요약 프롬프트 변경

`daily-report.js`의 `generateSummaryWithGemini()` 함수에서 `prompt` 변수 수정

### Notion 페이지 포맷 변경

`daily-report.js`의 `createNotionPage()` 함수에서 `children` 배열 수정

## 📊 활동 통계

워크플로우는 다음 정보를 수집합니다:
- 완료된 이슈 개수
- 새로 생성된 이슈 개수
- 업데이트된 이슈 개수
- 댓글 활동이 있는 이슈 개수

## 🔐 보안

- 모든 Secrets는 GitHub에서 암호화되어 저장됩니다
- Notion Token은 읽기 전용이 아닌 쓰기 권한이 필요합니다
- Actions 로그에서 Secret 값은 자동으로 마스킹됩니다

## 📚 참고 문서

- [GitHub Actions 문서](https://docs.github.com/en/actions)
- [Notion API 문서](https://developers.notion.com/)
- [Gemini API 문서](https://ai.google.dev/docs)

---

문제가 있으면 이슈를 생성해주세요!
