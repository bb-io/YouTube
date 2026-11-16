# YouTube 1.13.1 validation

Status: patch release candidate. No installation or real YouTube upload performed. Live Google and Blackbird runtime checks pending.

## Reproduction and completed checks

- Shipped 1.13.0 assembly reproduced a FormatException: RestSharp rejected application/json; charset=utf-8 as its media-type argument. The upload-session request never reached the simulated HTTP handler. Passing ContentType.MediaType fixes this deterministic pre-request defect; StringContent still encodes JSON as UTF-8.
- App and 62 real-API tests build in Release with zero warnings/errors; all 62 tests are discovered but not run without credentials.
- 243 retained regression checks pass on 1.13.1.
- 27 focused production-routing checks pass: upload-session creation through RestSharp rather than the custom-transport branch, exact OAuth/header metadata, UTF-8 and Unicode payload, selected visibility, validated session use, exact Content-Length and MP4 bytes via forward-only stream, public Upload video output/status, file disposal and no write retries; other JSON POST/PUT/PATCH methods and quota rejection classification remain correct.
- Total rerun for this patch: 270 local checks. Previous 1.13.0's 525-check results are historical and are not presented as a fresh full run here. Temporary focused harnesses are not shipped. Their check transcript is in TEST-RESULTS.txt; the supplied video and screenshots are excluded from deliverables.
- Source comparison against 1.13.0 confirms only YouTubeClient.cs (media-type fix) and the project version change among app implementation files. Reflection confirms all 34 action/event signatures and 489 exported property types/nullability/labels; app identity, OAuth and polling are unchanged. Metadata audit confirms 28 actions and six events.
- Packaging checks cover ZIP integrity/root layout, correct assembly/version/dependencies, embedded README/icon, size limit and exclusion of credentials, tests and intermediates from the deployment ZIP.

## Required upload/live checks

1. Update the existing custom app using YouTube-Blackbird-1.13.1.zip. Open the existing upload Bird and confirm file mapping and metadata remain unchanged. No connection/scope changes are required by this patch.
2. Inspect channel uploads before retrying a potentially completed prior operation. Then run one authorized test upload; confirm a returned Video ID/URL, expected privacy and successful processing in YouTube Studio. Local tests do not establish live service success.
3. Verify actual Google Drive → Blackbird File Management → YouTube file access and streaming, then any optional thumbnail flow. The focused test substitutes local file retrieval and simulated HTTP responses.
4. Test representative JSON management writes, including Unicode text. Errors after an external write still need resource inspection before retry; no automatic upload retries were added.
5. Existing live tests use disposable OAuth resources and may create/delete videos/captions/playlists/comments; review cleanup and avoid running the entire suite against a production channel.

## Report multi-select staging checks

1. Upload the new app version in staging and verify Report metrics renders as multi-select, defaults to Views and allows adding/removing individual choices. Check mapped collections, omitted/default/empty inputs and parent refresh in the actual Blackbird mapper. Old scalar test configurations must be reselected; no compatibility adapter is supplied.
2. Run supported totals/day/month/country/ranked-video combinations with multiple metrics. Verify column order/headers and values. Select -Views sort and then add Likes; confirm the extra compatible metric remains available. Remove the sorted metric and verify a clear validation error.
3. Verify no bundle choices remain; each returned option has one metric name. Check monetary labels and actual permission/eligible data using separately authorized connections. No scope introspection is claimed.
4. Verify custom metric names can be entered as separate items and mapped as collections, while comma-containing items are rejected. Advanced breakdown/filter/sort and content-owner combinations remain subject to Google's actual API tables and permissions; curated validation does not cover all families.
5. Breakdown and Sort deliberately remain single-choice dropdowns. Confirm supported advanced dimension and ordered sort expressions still work. Guided choices never advertise combining day and month. Changing parent inputs may require manually clearing incompatible saved child selections.

## Transcript staging/live checks

1. Verify contextual video/language/Caption ID fields in the Blackbird UI with OAuth. Language suggestions follow Video ID and draft/ASR flags. Changing parents may require clearing an obsolete child value; dependent pickers do not automatically clear saved values.
2. Run with owned serving captions and permission to edit the video. Verify returned text, segment times/provenance and exported WebVTT through actual Blackbird File Management. Existing caption download API permissions still apply; public playback does not grant caption-download access.
3. Check exact regional language selection, declared audio/metadata language behavior, multiple eligible language decision, Caption ID mismatch, forced/ASR/draft opt-ins, syncing/failed/empty states and permission errors against representative live data. ASR track accessibility is subject to Google's actual API response.
4. Validate large responses against Blackbird runtime/output limits. Local parser limits are 100 MB download, 16,384 characters per line, 65,536 per cue, 10,000 non-empty segments and 250,000 transcript characters including separators. Failures never silently truncate. Unsupported formatting/conversion errors require inspecting the subtitle file.
5. Confirm bounded delay/retry behavior in a Bird when Retry recommended is true. On new video added does not fire again just because captions become ready. Use a Checkpoint for unavailable language/track or exhausted retries. Transcript output reflects captions and is not proof of complete speech coverage.

## Required staging/live checks

1. Update the existing custom app using YouTube-Blackbird-1.13.1.zip in staging. Open old Birds and confirm requiredness, saved raw strings, metric collections, mapped IDs, outputs, connections and non-empty polling state. No external installation has been performed here.
2. Verify metric, breakdown and sort dropdowns preserve stored values. Select multiple individual metrics, inspect the collection and run supported aggregate/day/month/country queries. When changing parent values, clear incompatible child choices; the SDK does not automatically clear fields or hide them. Check custom expressions not in suggestions remain usable.
3. Test valid and invalid monthly boundaries. Both dates must be first-of-month calendar dates; invalid values fail before Google and must not be silently rounded. Verify Pacific reporting-calendar interpretation in the actual platform mapper.
4. Test ranked-video reports with an explicitly supported descending sort, selected metric and Google's 200-row per-request restriction. Verify live pagination/availability on representative data. All report combinations still depend on Google's table rules; advanced families are deliberately outside the curated guide.
5. Confirm revenue choices are marked as requiring monetary access. A normal OAuth token is not proof of that permission. Test standard-only and monetary connections separately, including actual consent/eligible report data. No scope introspection, new scopes or partner-discovery access is introduced.
6. Check Get report helpers independently and together: valid Video ID/URL, country normalization, same-value raw equality merge, conflicts, wrong owner, explicit Channel ID mismatch, advanced filter families, incompatible dimensions and content-owner mode. Existing raw Filters must pass unchanged when helpers are absent. No partial or zero-valued success should replace permission/read errors.
7. Verify OAuth channel labels and value forms in Get report, Search videos, Find video, Search channel videos, Search playlists and Get lifetime statistics. API-key public/global searches and manual/mapped selectors remain supported. Do not assume one token exposes all Brand Accounts. The shared search input also supplies suggestions to its existing search event; event execution/memory remain unchanged.
8. In Search caption tracks choose/map a video, toggle Include drafts and verify language suggestions, deduplication, region-specific tags, empty/no-caption videos, names and track counts. Changing the parent requires clearing an obsolete selection. Verify manual unknown/not-present language filters still run as explicit filters; uploads/localization use the full target-language catalogue.
9. Recheck scheduling/cancellation, omitted-update preservation, source/audio/target-language distinctions, caption/video dependencies and distinct playlist-entry removal. These changes primarily improve descriptions and retain existing action behavior.
10. Execute the real-API suite only with a disposable OAuth channel configured from appsettings.json.example. Existing fixtures create private/unlisted videos, captions, playlists and comments, briefly schedule a video and attempt cleanup; interrupted runs may leave resources. New UX tests reuse fixtures and perform read queries, allowing empty Analytics rows. Check leftovers.
11. Previous gates remain: file transfers, OAuth callback/refresh, standard advanced reporting, existing typed Analytics actions, static pickers, other dynamic sources and runtime restoration of all existing event memories.

Google's common channel-report tables, report parameters, monthly dimension rule and SDK dependent-source documentation are linked in README.md and docs/REFERENCE.md.
