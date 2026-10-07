# System Architecture

**Status:** `<Implemented / Partially implemented / Planned>`

## 1. Overview

`<Describe the system in 1–3 paragraphs.>`

## 2. Context

```text
Users / External Systems
          |
          v
   <Web / Mobile / API>
          |
          v
      <Backend>
       /    \
      v      v
 <Database> <External Services>
```

## 3. Major components

| Component | Responsibility | Owns data? | Technology |
|---|---|---:|---|
| `<API>` | `<...>` | `<Y/N>` | `<...>` |
| `<Worker>` | `<...>` | `<Y/N>` | `<...>` |

## 4. Boundaries

Describe trust, data, and deployment boundaries.

## 5. Communication

| From | To | Protocol | Why |
|---|---|---|---|
| `<Client>` | `<API>` | `<REST/GraphQL/etc.>` | `<...>` |
| `<Service>` | `<Service>` | `<gRPC/Event/etc.>` | `<...>` |

## 6. Data ownership

Describe the source of truth for each important domain concept.

## 7. Scalability

Describe actual bottlenecks and chosen scaling strategies.

## 8. Failure isolation

Describe what happens when major dependencies fail.

## 9. Observability

Logs, metrics, traces, health checks, alerts.

## 10. Security boundaries

Describe authentication boundaries, authorization boundaries, external trust boundaries, and sensitive data paths.

## 11. Deployment

Link to `deployment.md`.

## 12. Related ADRs

- `<ADR-...>`
