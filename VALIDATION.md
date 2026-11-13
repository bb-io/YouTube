# YouTube 1.13.0 validation

Status: release candidate. Local builds/checks completed; live Google API execution and Blackbird installation/UI/runtime validation pending. No installation was performed.

## Completed locally

- App and live-test project build in Release with zero warnings/errors; warnings treated as errors. All 62 real-API tests compile/discover but have not run without credentials.
- 525 local checks pass: 243 retained regression, 35 multi-channel, 39 picker, 50 Analytics, 79 guided UX and 79 transcript checks. Focused harnesses use simulated responses/reflection and are not a substitute for actual Google/Blackbird execution. Temporary harnesses are not shipped; logs are in TEST-RESULTS.txt.
- Guided UX checks verify collection input/dependent-context metadata, individual options without bundles, complete-selection dependencies, valid additional metrics with an existing sort, stable trimming/deduplication/query serialization without mutating the caller, empty/null/invalid metric collections, stale sorts, ranked-video constraints and existing advanced/helper behavior.
- Updated monthly real-API report test selects Views, Watch time in minutes and Average view duration in seconds and checks expected headers. The time-bound Analytics action internally passes a metric collection; its typed result behavior remains unchanged.
- Compared with 1.12.0, 34 action/event method signatures and identities and 487 exported properties remain unchanged. Two exported Metrics properties intentionally change from text to collections: ReportInput.Metrics and ReportContext.Metrics. There is no scalar adapter by user decision because no existing Birds exist. Labels, requiredness, report outputs, app identity, OAuth scopes and polling source/state remain unchanged.
- App exposes 28 actions, six multiflight events and 15 dynamic providers. Shared video picker serves 16 actions across 14 input models. No new scopes or endpoints.
- Deployment ZIP integrity, root layout, version/dependencies, embedded README/icon, size and exclusion of tests/credentials/build intermediates checked during packaging.

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

1. Update the existing custom app using YouTube-Blackbird-1.13.0.zip in staging. Open old Birds and confirm requiredness, saved raw strings, metric collections, mapped IDs, outputs, connections and non-empty polling state. No external installation has been performed here.
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
