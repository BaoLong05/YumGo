# Database Consistency Rules

This document is a guardrail over the schema.

## Every feature must answer

1. What table(s) are read?
2. What table(s) are written?
3. Which IDs are PK/FK NanoID(21)?
4. Which fields are required?
5. Which fields are nullable and why?
6. What length/range/enum rules apply?
7. What cross-field rules apply?
8. What cross-table rules apply?
9. What ownership/resource-scope rule applies?
10. What state must the record be in before mutation?
11. Which database constraints enforce the rule?
12. Which integration tests prove the rule?

No feature is implementation-ready until these questions are answered.
