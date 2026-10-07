# Documentation Standard

## 1. Principle

Documentation is part of the product. It must be:

- accurate
- current
- concise enough to read
- detailed enough to operate and change the system safely
- tied to real code and real behavior

## 2. Source of truth

Prefer this order when resolving contradictions:

1. Executable behavior and tests
2. API/schema definitions and migrations
3. Current source code/configuration
4. Documentation
5. Historical decisions

When documentation conflicts with implementation, fix the documentation if the implementation is correct; otherwise fix the implementation and its tests.

## 3. Avoid documenting implementation trivia

Do document:
- boundaries
- responsibilities
- contracts
- invariants
- security rules
- important trade-offs
- failure behavior
- operational constraints

Do not document every private helper, obvious getter, or line-by-line code behavior.

## 4. Naming

Use stable business names. Prefer `orders.md` over `order-service-v2-new.md`.

## 5. Status labels

Use one of:

- **Implemented**
- **Partially implemented**
- **Planned**
- **Deprecated**
- **Removed**

Never present planned behavior as implemented.

## 6. Feature documentation minimum

Every feature file should cover:
- overview
- actors
- user flow
- business rules
- authorization
- domain model
- API
- persistence
- integrations
- failure cases
- tests
- edge cases

## 7. Architecture changes

If a change affects system boundaries, data ownership, protocols, deployment, security model, or major dependencies, update the relevant architecture document and consider an ADR.

## 8. API changes

For every API change, document:
- endpoint or operation
- authentication
- authorization
- request
- response
- errors
- idempotency
- pagination/filtering/sorting if applicable
- rate limits if applicable
- versioning/compatibility impact

## 9. Security changes

Document changes to:
- authentication
- authorization
- permissions
- tenancy/scope
- secrets
- personal/sensitive data
- external trust boundaries
- file uploads
- payment flows
- webhooks

## 10. Completion gate

A feature is not considered fully documented until the implementation, tests, and relevant docs all describe the same behavior.
