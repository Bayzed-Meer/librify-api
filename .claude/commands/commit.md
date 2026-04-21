---
description: Verify, stage, and commit changes following Conventional Commits
argument-hint: "[files-to-stage] -- defaults to all modified tracked files"
allowed-tools: Bash(git status:*), Bash(git diff:*), Bash(git add:*), Bash(git commit:*), Bash(git log:*), Bash(dotnet:*)
---

# Commit Changes

Verify correctness, stage files, and commit with a Conventional Commit message.

## Step 1 — Inspect current state

Run these in parallel:

```bash
git status
git diff --stat HEAD
```

Report what has changed. If there is nothing to commit, stop and say so.

## Step 2 — Verify before committing

Run these in parallel:

```bash
dotnet build --no-restore
dotnet test --no-build
```

If any command fails, **stop and fix the issues before proceeding**. Do not commit broken code.

## Step 3 — Stage files

If the user passed specific files as arguments, stage only those:

```bash
git add <files>
```

Otherwise stage all modified tracked files (do NOT use `git add -A` or `git add .` to avoid accidentally staging untracked/sensitive files):

```bash
git add -u
```

Show a final `git diff --staged --stat` so the user can see exactly what will be committed.

## Step 4 — Draft the commit message

Examine the staged diff and draft a Conventional Commit message:

- Format: `<type>(<scope>): <short description>`
- Types: `feat`, `fix`, `refactor`, `test`, `chore`, `docs`, `style`
- Scope: the feature or layer being changed (e.g. `books`, `auth`, `loans`, `api`)
- Subject: imperative mood, lowercase, no period, ≤72 chars total
- Body (optional): explain *why*, not *what*, if the change needs context

Present the draft to the user and ask for confirmation or edits before committing.

## Step 5 — Commit

After the user confirms the message, commit using a heredoc to preserve formatting:

```bash
git commit -m "$(cat <<'EOF'
<confirmed message here>
EOF
)"
```

If any pre-commit hook fails, fix the underlying issue — never use `--no-verify`.

## Step 6 — Confirm

Show the result of:

```bash
git log --oneline -3
```

Report success with the commit SHA. Ask if the user wants to push the branch.
