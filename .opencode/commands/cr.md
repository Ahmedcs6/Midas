---
description: Review git diff with code-review skill, report-only
agent: build
---

Load skill `code-review` via skill tool, then review:

Recent status:
!`git status --short`
!`git diff --stat`
!`git diff`

Focus on $ARGUMENTS if given. Report-only with `file:line` + severity.
