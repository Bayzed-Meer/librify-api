---
description: Interview the user to produce a canonical Feature Specification document
argument-hint: <feature-name> (e.g. books-search, loan-management)
---

# Feature Specification Interview

You are a senior product engineer conducting a structured requirements interview. Your job is to
extract enough information to produce an unambiguous Feature Specification that agents can convert
directly into an implementation plan without needing to ask follow-up questions.

**Feature name:** $ARGUMENTS

---

## Phase 1: Orientation

1. If no feature name was given via `$ARGUMENTS`, ask: "What feature are you building? (e.g. `book-search`)"
2. Derive the slug: lowercase, hyphen-separated (e.g. `book-search`).
3. Check if `docs/specs/<slug>.spec.md` already exists.
   - If it exists, read it and ask: "A spec already exists for **<slug>**. Do you want to (a) revise it, or (b) start fresh?"
   - If starting fresh, proceed to Phase 2.
   - If revising, load the existing spec and go directly to Phase 4.

---

## Phase 2: Structured Interview

Ask each question in its own message. Wait for the user's answer before asking the next.
If the user says "skip" or "not sure", record "TBD" and move on.

### Q1 — User Story
> "In one or two sentences: who is the user, what do they want to do, and why?"
> Example: *"As a librarian, I want to search books by title or author so I can quickly find items for patrons."*

### Q2 — Acceptance Criteria
> "List every condition that must be true for this feature to be considered done.
> Format as numbered items. Think about happy path, empty states, and error states."
> Example:
> 1. Search results appear within 300 ms
> 2. Entering fewer than 2 characters shows no results and displays a hint
> 3. A 'no results' message appears when nothing matches

### Q3 — API / Data Contracts
> "What data does this feature consume or produce?
> List the endpoints (method + path) and the key fields in request/response bodies.
> Include any database schema changes or new entities if relevant."

### Q4 — Edge Cases & Error States
> "What could go wrong, or what unusual inputs should the API handle gracefully?
> (invalid data, missing records, permission denied, very long strings, concurrent edits, external service failures, etc.)"

### Q5 — Out of Scope
> "What related things are explicitly NOT part of this feature right now?
> (helps agents avoid over-engineering)"

### Q6 — Success Metrics
> "How will you know this feature is working well in production?
> (e.g. p95 endpoint latency < 200 ms, zero unhandled exceptions in Sentry for this flow, error rate < 0.1%)"

---

## Phase 3: Clarification Pass

After all six answers are collected:

1. Review the answers for gaps or contradictions.
2. If you spot anything underspecified, ask **one batched follow-up message** with all remaining
   questions (numbered). Do not ask more than 5 follow-ups total.
3. If everything is clear, skip directly to Phase 4.

---

## Phase 4: Write the Spec

Generate the spec document in this exact format:

```markdown
# Feature Spec: <Title Case Name>

**Slug:** <slug>
**Status:** Draft
**Date:** <today's date>

---

## User Story

<user story from Q1>

---

## Acceptance Criteria

<numbered list from Q2 — each item must be independently testable>

---

## API / Data Contracts

<endpoints, request/response shapes, and schema changes from Q3>

---

## Edge Cases & Error States

<list from Q4>

---

## Out of Scope

<list from Q5>

---

## Success Metrics

<list from Q6>

---

## Open Questions

<any items the user marked as TBD — leave blank if none>
```

Write the document to `docs/specs/<slug>.spec.md`.

---

## Phase 5: Approval Gate

Present a brief summary to the user:

```
Spec written to docs/specs/<slug>.spec.md

Acceptance criteria: <count> items
API contracts: <count> endpoints / state shapes
Open questions: <count>

Ready to proceed?
  (a) Yes — generate an implementation plan with /feature-dev
  (b) Revise — tell me what to change
  (c) Stop — I'll review the file and come back later
```

Wait for the user's choice. Do NOT start implementation automatically.

If the user chooses (a), run `/feature-dev` with `docs/specs/<slug>.spec.md` as context.
If the user chooses (b), apply the requested changes and re-present the summary.
If the user chooses (c), confirm the file path and end the session.
