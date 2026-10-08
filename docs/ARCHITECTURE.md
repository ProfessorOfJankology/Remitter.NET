# Remitter.NET Architecture

## Purpose

Remitter.NET is the Windows desktop client for the Remitter backend service.

The desktop client is intentionally not authoritative for payment eligibility, HCN financial state, allocation, receipting, locking, idempotency, reconciliation or audit history. Those responsibilities remain in the Remitter backend.

## Repository boundary

### Remitter.NET owns

- Windows UI and operator interaction.
- Presentation and selection state.
- Queue display and refresh behaviour.
- Manual entry and import UX.
- HCN search UX exposed through backend APIs.
- Remittance and email viewing.
- ESCompose integration.
- HTTP client behaviour, authentication and client-side error presentation.

### Remitter owns

- Shared queue state.
- Background invoice checks.
- Authoritative HCN validation.
- Service-level financial state and allocation.
- Server-side eligibility.
- Atomic claims and locking.
- HCN receipting.
- Idempotency and duplicate protection.
- Uncertain-outcome reconciliation.
- Audit and processing history.

## Projects

### Remitter.App

WPF executable and presentation layer.

### Remitter.Client

Typed HTTP client for the Remitter Hub API. This layer should contain transport concerns, authentication and API DTO mapping, but not receipting business rules.

### Remitter.Domain

Client-side domain and presentation-safe models shared by the application and client libraries.

## ESCompose

ESCompose is a separate WPF/.NET application. Remitter.NET should integrate through reusable ESCompose libraries rather than shelling through `cmd.exe`.

The preferred direction is to extract or expose reusable compose functionality from ESCompose behind a library boundary. Remitter.NET should not directly depend on ESCompose's main WPF application assembly.

## Security and financial integrity

The client must assume displayed data can become stale. A successful recent check in the UI is not authority to receipt. The backend must revalidate the request and retain all existing safeguards immediately before any financial write.

The client must preserve request identity across failed or uncertain submissions and must not silently retry a potentially committed receipt with a new idempotency key.
