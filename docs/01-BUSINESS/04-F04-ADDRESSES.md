# F04 — Addresses

**Actor:** Customer


### Actions
Create, edit, delete, set default, list own addresses.

### Validation
- every new address requires latitude and longitude from the device's current-location flow;
- the client does not submit `addressLine`, ward, district, or city; the backend derives them with Google reverse geocoding;
- recipient name/phone and a short label are separate from the address and may be supplied by the customer;
- when coordinates change, the backend reverse-geocodes them again; metadata-only edits do not call Google;
- coordinates inside geographic ranges;
- only one default address per user;
- address ID in URL must belong to authenticated user.

### Google usage and cost guard
- The backend reserves each reverse-geocoding request in PostgreSQL before calling Google.
- The default application ceiling is 9,000 requests per UTC calendar month; configuration may lower, but not raise, the ceiling above 9,000.
- Requests are rejected after the ceiling is reached. Configure a matching or lower Google Cloud quota as an additional project-level safeguard; Google usage is aggregated per billing account, so this app's counter cannot account for calls from other projects.
- Google requires a billing account to be enabled even when requests remain under the current monthly free usage cap; configure Cloud billing budgets/alerts and a project quota before enabling the key.
- The API key is server-side configuration (`GoogleMaps__GeocodingApiKey`) and must not be shipped to the mobile app.
- The mobile client must obtain location permission and disclose that coordinates are sent to the backend and Google; publish the required Terms of Use and Privacy Policy before production use. Apply Google Maps attribution when displaying Google-derived content without a Google Map.

### Hidden field
Forms may carry `addressId` as hidden NanoID, but server must re-check ownership.


## Common security rule
All resource identifiers use NanoID(21). Route IDs and hidden form IDs are never trusted by themselves; the backend must re-check authorization and record relationships before mutation or response.

## Definition of Done
- DTO validation implemented.
- Cross-field validation implemented.
- Cross-table validation implemented.
- Authorization/scope validation implemented.
- Database constraints and indexes reviewed.
- Transaction boundary reviewed.
- Idempotency/concurrency behavior defined where applicable.
- Audit/notification side effects defined.
- Integration tests cover happy path and negative cases.
