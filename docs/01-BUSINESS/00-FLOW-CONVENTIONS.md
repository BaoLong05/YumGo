# Feature documentation conventions

Every feature document must define:
1. Actor(s)
2. Goal
3. Preconditions
4. Inputs
5. Field validation
6. Cross-field validation
7. Cross-table validation
8. Authorization/scope validation
9. State requirements
10. Transaction boundary
11. Database rows changed
12. Side effects/events/jobs
13. Failure cases
14. Idempotency/concurrency behavior
15. Audit requirements
16. Acceptance criteria

All feature documents assume the database rules in `database/` are mandatory.
