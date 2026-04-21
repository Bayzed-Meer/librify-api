---
description: Push branch, open a GitHub PR, and trigger a multi-agent review
argument-hint: "[base-branch] -- defaults to main"
allowed-tools: Bash(git status:*), Bash(git diff:*), Bash(git log:*), Bash(git push:*), Bash(git branch:*), Bash(git fetch:*), Bash(gh pr:*), Bash(gh repo:*), Bash(dotnet:*)
---

# Create Pull Request

Push the current branch and open a GitHub PR with a well-structured description. Then trigger a multi-agent review.

## Step 1 — Pre-flight checks

Run in parallel:

```bash
git status
git branch --show-current
git log --oneline main..HEAD
git diff --stat main..HEAD
```

If the working tree is dirty (uncommitted changes), stop and tell the user to commit first (`/commit`).

If there are no commits ahead of main, stop — there is nothing to PR.

## Step 2 — Verify the build

Run in parallel:

```bash
dotnet build --no-restore
dotnet test --no-build
```

If anything fails, **stop and fix before pushing**. Never open a PR with a broken build.

## Step 3 — Determine base branch

Use the argument if provided, otherwise default to `main`.

Fetch the latest base branch state:

```bash
git fetch origin <base>
```

## Step 4 — Push the branch

```bash
git push -u origin HEAD
```

## Step 5 — Draft PR title and body

Examine `git log --oneline <base>..HEAD` and `git diff --stat <base>..HEAD` to understand the full set of changes.

Draft:

- **Title**: `<type>(<scope>): <concise description>` — follow Conventional Commits, ≤70 chars
- **Body**: use the template below

```markdown
## Summary
- <bullet 1>
- <bullet 2>
- <bullet 3>

## Changes
- **Files changed**: list key files and what changed in each
- **DB**: note any migrations or schema changes
- **Tests**: describe what is covered by new/updated tests

## Test plan
- [ ] Build succeeds (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] <feature-specific manual test step>
- [ ] <edge case to verify>
```

## Step 6 — Create the PR

```bash
gh pr create \
  --base <base-branch> \
  --title "<title>" \
  --body "$(cat <<'EOF'
<body>
EOF
)"
```

Output the PR URL when done.

## Step 7 — Trigger review (optional)

Ask the user: "Would you like me to run a multi-agent review on this PR now?"

If yes, invoke the `/review-pr` command with the PR number.
