---
name: fix-bug
description: Reproduce, diagnose, minimally fix, review, test, and prepare a commit for one OrderHub bug. Use only when the user explicitly asks to fix a reported OrderHub defect or invokes $fix-bug.
---

# Fix an OrderHub bug

1. Restate the observable symptom and identify the page or workflow involved.
2. Obtain concrete reproduction evidence such as page number, amount, status, or
   stock before and after. Ask the user only when browser interaction cannot be
   performed locally.
3. Trace the request from controller to Core service to repository. Explain the
   root cause and identify the missing test coverage before editing.
4. Make the smallest production-code change that corrects the behavior. Do not
   mix in unrelated cleanup or refactoring.
5. Add a regression test that would fail with the original defect.
6. Use the project `code-reviewer` agent when delegation is available. Address
   correctness findings before continuing.
7. Use `test-runner` to run the complete test suite.
8. Ask the user to verify the original symptom in the browser when manual UI
   verification is required.
9. After verification, prepare one commit whose message states symptom, root
   cause, and fix. Do not push without explicit approval.
