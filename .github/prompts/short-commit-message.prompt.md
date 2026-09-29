\---

name: short-commit-message

description: Generate one concise git commit message for the changes made in the current task.

agent: ask

\---



Based on the changes made in the current task, propose one concise git commit message.



Rules:

\- lowercase type

\- one line only

\- no period at the end

\- use one of: ui, fix, feat, refactor, docs, test, chore

\- describe the actual completed change, not the task request

\- do not commit anything



Return only the commit message.

