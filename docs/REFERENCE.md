# YouTube implementation reference

Current version: **1.12.0**. For the complete current action/event catalogue and connection setup, see [README.md](../README.md). Earlier version sections below describe the evolution of the implementation.

## Version 1.12.0: caption-based transcripts

### Get video transcript

**Get video transcript** is a caption-based action for videos you are authorized to manage. It returns plain transcript text, timed cue segments and the downloaded WebVTT export. It does not download audio/video, perform speech recognition, translate missing languages, repair caption wording or guarantee that captions cover all speech. Google requires OAuth and permission to edit the video for caption downloads. Existing OAuth scopes are sufficient; API-key connections cannot use this action.

| Input | Behavior |
| --- | --- |
| Video ID | Required. Shared OAuth video picker, direct ID, supported URL or mapped value. |
| Language | Optional exact BCP-47 caption tag. Contextual suggestions reflect the video and draft/ASR settings. Regional variants remain distinct. |
| Caption ID | Optional advanced override on that video. Must agree with Language and the draft/ASR flags. |
| Include drafts | Defaults to false. Explicit opt-in also applies to an overridden Caption ID. |
| Allow automatic captions | Defaults to false. Permits YouTube ASR tracks where actually accessible; does not generate ASR or imply that standard tracks are human-authored. |

An explicit Caption ID takes precedence as the selected track but never silently overrides conflicting language or opt-in settings. Without an override, an explicit Language is used exactly. Otherwise the action uses declared original audio language, then declared metadata language only if audio language is absent. If neither is declared, automatic selection occurs only when eligible tracks have exactly one language. A missing declared/requested language never falls back to another language or regional variant.

Within the selected language, ready tracks are considered first. Regular standard tracks take priority over ASR; non-draft tracks take priority within the type, followed by an ordinal Caption ID tie-break. A standard track is not a human-authorship guarantee. Forced and unknown track types are excluded from automatic selection. An explicit forced override is permitted with a coverage warning because it may contain only selected dialogue. The chosen ID, language, track kind/name, draft/ASR/forced flags and Selection reason are returned.

| Output | Purpose |
| --- | --- |
| Transcript available | Branch condition for downstream processing. |
| Transcript text | Cue text with formatting removed, ready for mapping into text-processing actions. |
| Segments | Start/end times in seconds and text, in source order. |
| Subtitle file | Exact bytes of Google's WebVTT export, present only for a non-empty successful transcript. |
| Status / Status message / Retry recommended | Explicit availability and next-step information. |
| Caption/selection metadata and Warnings | Provenance and coverage notes. |
| Available languages | All accessible caption tags, including tracks excluded by current selection settings. |

Availability statuses are `available`, `no_captions`, `language_required`, `language_unavailable`, `no_eligible_track`, `captions_processing`, `captions_failed`, `caption_not_ready` and `empty_transcript`. Missing/processing/not-ready captions can recommend a later attempt. Missing language, excluded tracks or failed processing require a configuration/content decision. Permission errors, download/conversion failures, malformed files and parsing/size failures raise errors rather than being disguised as unavailable or zero-length success. An explicit Caption ID not found on the video is a configuration error.

The action performs no automatic waiting/retry. **On new video added** does not fire again merely because captions become ready later. Use a bounded delay/retry branch in the Bird when Retry recommended is true, then route unresolved cases to a Checkpoint. Retries may never succeed if captions are never created or remain inaccessible.

The parser preserves overlapping cues, line breaks and repeated caption text, including rolling ASR repetition. It removes timing/formatting markup from text and keeps ruby base text rather than pronunciation annotations. It does not infer speakers, create word-level alignment or verify audio coverage. The subtitle export retains the downloaded markup and timing information. Unsupported segmented timestamp maps, ambiguous/malformed timing and invalid UTF-8 are errors, not partial transcripts.

Limits are explicit: the existing 100 MB download cap, 16,384 characters per line, 65,536 per cue, 10,000 non-empty segments and 250,000 combined text characters including separators (character limits use .NET string length). Exceeding a limit fails rather than truncating. Processing uses a delete-on-close temporary file instead of buffering the entire subtitle file in memory; only bounded parsed output is retained. Use Download captions when a full subtitle file exceeds transcript-parser limits. Platform-specific output/file limits still require staging verification.

Example Bird: **On new video added → Get video transcript → Decision: Transcript available**. If true, summarize/translate/extract terminology, then use a Checkpoint before publishing or preserving approved content in Blacklake. If false and Retry recommended, delay and retry a bounded number of times; otherwise request a language/track decision or caption repair through a Checkpoint. This action does not expand access to arbitrary third-party public-video transcripts.

References: [caption download and editing permission](https://developers.google.com/youtube/v3/docs/captions/download), [caption track kinds and processing states](https://developers.google.com/youtube/v3/docs/captions), and [WebVTT syntax](https://www.w3.org/TR/webvtt1/).


---

## Version 1.11.0: guided inputs across actions

### Guided inputs and report choices

Version 1.13.0 makes **Get report → Report metrics** a required multi-select dropdown, defaulting to Views. Select individual metrics such as Views, Watch time in minutes and Average view duration in seconds. When mapping values, supply a collection of metric names, for example `["views", "estimatedMinutesWatched"]`. Predefined metric bundles have been removed. Comma-separated text is no longer accepted as a metric selection; each metric must be a separate item. Whitespace is trimmed and duplicates are removed without changing the order of first occurrence.

Metric suggestions consider the complete selected collection, Report type, Report breakdown, filters and Sort. Sort choices include selected metrics only; selecting a sort no longer prevents adding other compatible metrics. Common channel combinations are checked before a report request, including stale sorts, country breakdown/filter conflicts and ranked-video requirements. Advanced metrics, dimensions, filters and partner reports remain subject to Google's report tables and authorization; the curated guide does not enumerate every supported API report.

These are maintained choices from Google's documented channel-report tables, not an account-specific metric-discovery API. Revenue choices explicitly require monetary access; an OAuth connection does not prove that permission. No new scopes are requested. **Report breakdown** and **Sort** retain single-choice dropdowns: the guided breakdowns are mutually exclusive report shapes, and sort-key order matters. Supported advanced dimension and ordered sort expressions can still be entered or mapped directly.

**Report breakdown** suggests Totals, By day, By month, By country and ranked By video when compatible with the selected fields. Totals stores the value `none`, which the app translates into no dimensions parameter. Monthly reports require both Start date and End date to be the first day of a month. Dates are not adjusted automatically; choose a daily breakdown for arbitrary dates. Ranked video reports require an explicitly selected, supported descending Sort; Google limits each request to 200 rows and controls report availability. The existing report pagination remains in use. Sort suggestions use selected dimensions/metrics and the narrower ranking rules where applicable.

Changing parent values does not automatically clear existing selections. If a previous sort, breakdown or filter conflicts with a new selection, clear or update it. Contextual suggestions cover common channel reports only. Unsupported/custom metric families, extra advanced filter types, and Content owner reports retain typed/mapped expressions and Google's final validation rather than misleading guided choices. In those contexts a dropdown can have no suggestions while direct expressions remain available.

Two optional helper fields are added to Get report: **Video ID** uses the shared OAuth video picker and checks that the video belongs to the selected OAuth channel and any explicit Channel ID; **Country** suggests supported country codes for compatible report families. Country cannot be combined with a country breakdown, and the video helper is not offered as a valid combination with ranked-video breakdown. Helpers can merge matching video/country equality clauses from advanced Filters. Conflicting values or unsupported advanced filter families raise a clear error; nothing silently wins or gets discarded. Clear helpers to keep advanced Filters unchanged. Existing reports with no helpers retain their raw metrics, breakdown, sort and filter expressions.

**Channel ID** remains optional in Get report: omission uses the authenticated channel (`channel==MINE`). OAuth suggestions show the selected YouTube/Brand Account's channel name and ID, not every identity a person might manage. Selecting an ID does not grant Analytics access. Partner Content owner ID remains an advanced explicit input; normal channel suggestions are not offered for partner report mode.

OAuth channel suggestions also cover Search videos, Find video, Search channel videos, Search playlists and Get lifetime statistics. ID fields store IDs; URL fields store canonical channel URLs. API-key connections have no owner-channel suggestions but retain supported direct/mapped public selectors. Public/global video search is not silently restricted to the connected channel. Search channel videos and omitted-channel defaults keep their existing ownership rules.

**Search caption tracks → Language** now suggests only language tags actually present on the selected video, deduplicated case-insensitively and respecting Include drafts. Labels show the language name/code and matching track count; regional tags remain distinct. An empty video or empty inventory produces no suggestions. Upload captions, metadata localization and required-language inputs retain the full target-language catalogue, and valid directly entered tags remain accepted.

All other action groups retain their existing video/playlist/caption/category/language/region pickers. Guidance now makes source metadata language, original audio language and target localization language distinct; clarifies scheduling/cancellation, caption/video dependencies and playlist entry semantics; and retains omitted-update preservation. Fixed choices such as visibility, license, caption format and HTTP method stay static. Content text, dates, files and custom API expressions remain appropriate typed/free-form inputs.

Optional fields remain under Advanced where the platform places them. This SDK release does not promise conditional field hiding, automatic clearing or moving optional inputs into the Required section. Existing polling behavior and memory are unchanged.

References: [Google channel-report combinations](https://developers.google.com/youtube/analytics/channel_reports), [report parameters](https://developers.google.com/youtube/analytics/reference/reports/query), [monthly reporting dates](https://developers.google.com/youtube/analytics/dimensions#month), and [dependent data sources](https://docs.blackbird.io/sdk/datasources/).


---

## Version 1.10.0: analytics actions

### Analytics snapshots and date-range summaries

**Get lifetime statistics** outputs the Data API's current cumulative channel View count, rounded Subscriber count, Subscriber count hidden flag and Public video count, with channel identity and a UTC Retrieved at timestamp. Supply Channel URL with an API key, or omit it with OAuth to use the selected channel. Supported channel URL forms match the rest of the app. This action is a snapshot of channel counters, not an all-time Analytics watch-time report or an immutable historical total. Public video count excludes private/unlisted uploads even for owners; hidden or unavailable subscriber counts remain empty. YouTube rounds the subscriber counter to three significant figures.

**Get time-bound metrics** requires Start date and End date and the owning channel's OAuth connection with Analytics access. Omit Video ID for a channel-wide aggregate, or choose/map one owned video using the shared dynamic picker. The action verifies a supplied video belongs to the channel selected by the OAuth connection. It outputs Views, Engaged views, Estimated minutes watched, Average view duration in seconds, Average view percentage, Likes, Comments, Shares, Subscribers gained and Subscribers lost as directly mappable fields. These are Analytics values for the selected reporting period, not changes computed by subtracting two Data API counter snapshots. With a video filter, subscriber changes follow Google's video-attribution rules rather than representing all channel subscriber changes.

Dates are inclusive reporting-calendar dates in Pacific time; time-of-day is ignored without shifting the supplied calendar date. Requested start/end dates are echoed in the result. They do not certify the last available reporting day: Google may return data only through the latest day for which all requested metrics are available. Has data is false when no report row is returned, and unavailable metrics remain empty rather than becoming zero. A returned numeric zero remains zero. Average values retain their fractional precision. Retrieved at is UTC.

**Get video statistics by ID** outputs a video's current cumulative View count, Like count and Comment count, plus Video ID/URL, title, Channel ID, visibility and UTC Retrieved at. Video ID supports the existing OAuth dropdown, typed IDs, supported URLs and mapped values. API keys can read accessible public video statistics; private videos require an authorized OAuth connection. Missing counters remain empty, and inaccessible/missing videos raise an actionable error. These are Data API snapshots and do not include date-range watch time or revenue.

No new connection fields or OAuth scopes are introduced. Enable YouTube Data API v3 for counters and YouTube Analytics API for time-bound metrics. Standard reports access is sufficient for the new summary; no monetary metrics are requested. If Analytics permission was not granted, reconnect and approve the existing Analytics read-only permission. The existing **Get report** action is retained for custom metrics, daily/monthly breakdowns, geography and eligible revenue reports.

Implementation endpoints: channels.list with part=snippet,statistics; videos.list with part=snippet,status,statistics; and https://youtubeanalytics.googleapis.com/v2/reports with a supported aggregate metric pack and optional video filter. Reporting data can lag or differ from Data API counters. See [channel counters](https://developers.google.com/youtube/v3/docs/channels#statistics), [video counters](https://developers.google.com/youtube/v3/docs/videos#statistics), [channel reports](https://developers.google.com/youtube/analytics/channel_reports), [report queries](https://developers.google.com/youtube/analytics/reference/reports/query) and [reporting dates](https://developers.google.com/youtube/analytics/dimensions#day).


---

## Version 1.9.0: dynamic video and improved dependent pickers

### Choose videos, captions and playlist entries

With an OAuth connection, **Video ID** offers a searchable dropdown in Get video, Update video, Set video publication settings, Update video thumbnail, Get video localizations, Update video localization, Find missing languages, Search caption tracks, Upload captions, Download captions, Update captions and Add video to playlist.

Suggestions come from the one YouTube or Brand Account channel selected during OAuth authorization. Each label shows the video's current title, visibility and Video ID. Accessible public, unlisted and private uploads can be suggested. Search by title or Video ID, case-insensitively. Up to 50 matching videos are shown in upload-playlist order. Manual video IDs and supported video URLs, and Video ID bindings from prior steps, remain accepted by the actions. With an API-key connection, owner video suggestions are empty; manually entered or mapped public videos remain usable in actions that support API keys. OAuth-only actions still require OAuth.

For **Download captions** and **Update captions**, first select or map Video ID, then choose Caption ID from its dependent dropdown. Download captions keeps Video ID optional: a directly supplied Caption ID can still be used. Changing the video does not authorize a caption from a different video; existing action checks remain in effect. Caption labels show language, track name, Draft or Not draft, track type, processing status and Caption ID. Track types distinguish Automatic (ASR), Standard and Forced; Standard is not a guarantee of human authorship. Ready indicates the caption-processing status, not the video's publication visibility. Draft tracks are labeled separately. Unnamed tracks remain selectable.

Owned playlist suggestions show title and Playlist ID. **Playlist item ID** suggestions for Remove video from playlist depend on Playlist ID and show the video title, Video ID and distinct playlist-entry ID. A video appearing twice remains two separate entries. Search playlist entries by title, Video ID or playlist-entry ID; searches are case-insensitive. Stored field values remain IDs, not their display labels.

Video, playlist and playlist-entry searches load pages until 50 matching suggestions or the end of the accessible source. Searches are no longer limited to the first 250 items. An empty search generally loads only the first full page; sparse or unavailable video metadata can require further pages to fill suggestions. The handlers stream pages and retain at most 50 matching labels plus deduplication IDs. The shared 1,000-page safety guard still applies and raises an error rather than silently returning an incomplete search. Narrow searches on large channels or playlists can take longer and consume more quota; enter or map an exact ID directly to avoid enumerating suggestions. A failed page request surfaces as an error, not a successful partial suggestion list.

Visibility, license, caption format, sort order and other fixed-choice dropdowns remain static. Channel suggestion pickers and Analytics report presets are not added in this release. No new OAuth permissions or connection fields are required.


---

## Version 1.8.0: multiple watched channels

### Watch multiple channels for new videos

**On new video added** accepts the existing required **Channel URL** plus an optional **Additional channel URLs** list. Add one complete YouTube channel URL per list item. Up to **20 distinct channel URL selectors** are supported per Bird, including the primary channel. Supported forms are current @handle, /channel/UC... and /user/username URLs. Duplicate selectors are ignored; different URLs resolving to the same Channel ID are scanned once.

The existing Channel URL remains required. Leave Additional channel URLs empty to keep a single-channel Bird. Existing connections, event identity, Video outputs and visibility choices are preserved. Outputs already include Channel ID and Channel title, alongside Video ID, Video URL and the video's metadata. Each discovered video starts a separate Flight.

The first successful poll records existing videos without starting Flights. Adding a channel records that channel's existing videos silently while continuing to discover new videos in channels already being watched. Reordering URLs or switching to another supported URL for the same resolved channel preserves its history. Removing a channel stops monitoring it; adding it back later creates a fresh silent baseline. Changing Video visibility creates a silent baseline for every watched channel while retaining previously seen IDs.

A single connection and Video visibility setting apply to the whole event. Public only remains the default and can watch public channels using an API key. Unlisted only, Private only and All accessible videos require OAuth, with ownership verified for **every** watched channel. An OAuth login does not automatically grant access to every Brand Account managed by the same person. If channels require separate authorized identities, use a separate connection and Bird per identity. An inaccessible or invalid channel fails the poll rather than being silently skipped.

At most **100 videos total** are delivered in one poll. Capacity is shared in rotation among channels with unseen videos, preserving oldest-first upload order within each channel; there is no global chronological order across channels. Undelivered videos remain eligible on later polls while accessible. Seen-ID history is tracked independently per resolved channel and retained across temporary disappearance. If any channel scan, ownership check or metadata request fails, no advanced memory is returned and prior memory is not modified.

Monitoring more channels increases API quota usage and polling time. Upload inventories are paginated internally using channels.list and playlistItems.list, followed by videos.list for discovered metadata. The existing 1,000-page per-inventory safety limit remains in effect. Seen-ID history grows with watched uploads; use a representative staging Bird to validate runtime memory and polling duration before large-scale use.


---

## Version 1.7.0: channel comments

### Channel-wide comment monitoring

**On new comment on channel** monitors accessible published comments under videos in a specified Channel URL. The URL can use the current @handle, /channel/UC... ID or /user/username form. Use an API key for publicly accessible comments or an authorized OAuth connection where required. No new connection fields or OAuth scopes are introduced.

**Include replies** defaults to true. The event checks replies on older discussions, not only newly created threads. Set it to false to monitor top-level comments alone and reduce API calls. Threads without a Video ID are excluded: this event concerns video comments rather than other channel/community content. Held-for-review, spam or rejected comments are outside its published-comment scope.

Outputs include Channel ID and URL, Video ID and URL, Comment ID and URL, Parent comment ID, Is reply, Author, Text, Published at, Updated at and Like count. A new output type is used so the existing video-level event's output is unchanged.

The first complete scan establishes a silent baseline. A different resolved channel or changed Include replies setting establishes another silent baseline. Afterward, up to 100 previously unseen comments/replies are delivered per poll, oldest first with a stable ID tie-break. Undelivered discoveries remain eligible while they remain accessible; a comment removed before delivery can be missed. Each item starts one Flight. Edits do not retrigger the same Comment ID, and temporary disappearance does not erase remembered IDs.

Comment threads are paginated with allThreadsRelatedToChannelId. Replies are fully paginated using comments.list(parentId), because an embedded replies collection may be incomplete. No timestamp cutoff or unchanged-reply-count shortcut is used. Newly accessible or newly published comments can be discovered after their original creation date.

This reliability requires rescanning accessible history, which can be expensive for active channels. Read each page without retaining all historical comment text; only the 100 pending payloads and ID sets are retained in the event's working state. Persisted seen-ID history grows over time. Existing per-source 1,000-page limits apply, with an additional 2,000-logical-read limit across channel lookup, thread pages and reply pages per poll. HTTP read retries can add network calls beyond that logical-read limit. Exceeding a limit or encountering a failed read raises an error before new memory is returned; a partial scan is never treated as complete. For a very large channel, use a narrower video-level event or design a separate larger-volume scan strategy.

The existing **On new comment on video** event continues to require Video ID and retains its existing behavior, inputs, outputs and memory. Existing Birds can keep using it; use the new event for a channel-wide audience-feedback Bird.


---

# YouTube custom app for Blackbird

Version **1.6.0** · .NET 8 · Blackbird SDK Common 2.17.0 · FileManagement 1.1.0

This version reorganizes the app, adds contextual pickers, reduces file buffering and improves validation coverage. Publishing, reporting and advanced discovery were introduced in version 1.4.0. It contains **24 actions and 5 polling events**, retaining the localization, captions and visibility-aware channel polling from earlier releases. See `COMPARISON.md` and `RELEASE-NOTES.md`. Product, PackageId, AssemblyName, existing action method identifiers and polling memory remain stable for upgrades. The display name is **Get video**; its internal method is still `GetVideoUrl` for compatibility.

## Install or upgrade

- Existing app: choose **New version / Update app** and upload `YouTube-Blackbird-1.6.0.zip`.
- New app: open **Apps → My custom apps → Create app** and upload that deployment ZIP.
- The source ZIP is for development and is not the Blackbird deployment package.
- Existing API-key connections continue to support public discovery. Select a new OAuth connection on caption and write actions.

## Connection setup

### API key: public discovery

In Google Cloud, enable **YouTube Data API v3**, create an API key, restrict it to that API, and enter it in Blackbird's **YouTube Data API key** connection. Application restrictions must allow Blackbird server requests; browser HTTP-referrer restrictions are unsuitable. The validator calls `i18nLanguages.list`.

### OAuth: channel management

The ZIP contains no shared Google client ID or client secret. Supply your own OAuth client in the connection:

1. In Google Cloud, select a project with **YouTube Data API v3** enabled.
2. Configure the Google Auth Platform consent screen, branding and audience. For an external app in Testing, add the Google account you will connect as a test user.
3. Add the requested scopes:
   - `https://www.googleapis.com/auth/youtube.force-ssl`
   - `https://www.googleapis.com/auth/yt-analytics.readonly`
   - `https://www.googleapis.com/auth/youtube.readonly`
   - Optionally `https://www.googleapis.com/auth/yt-analytics-monetary.readonly` when choosing **Include revenue reports** in the connection's Analytics access field.
   Enable **YouTube Analytics API** in the same Google project to use reports. Existing connections must be reauthorized to grant newly requested scopes; token refresh alone does not add them. Existing video/caption actions continue to use their existing management permission.
4. Create an OAuth client with application type **Web application**.
5. Register the exact **Blackbird authorization-code redirect URI** for your environment as an Authorized redirect URI. The app obtains it from `InvocationContext.UriInfo.AuthorizationCodeRedirectUri`; it does not hard-code a region or hostname. Obtain this URI from Blackbird for your environment. It is also the `redirect_uri` in the generated Google authorization URL; Google's redirect mismatch error identifies it if it is not registered yet. Do not enter a YouTube channel URL or a guessed callback URL.
6. In Blackbird, create a **YouTube channel management (OAuth)** connection and enter that client's **OAuth client ID** and **OAuth client secret**.
7. Authorize with the Google account permitted to manage the target channel, choose the correct YouTube/Brand Account where applicable, and approve the requested permission. The connection validator requests `channels.list?mine=true` and checks that a channel is accessible. Actual video/caption authorization is enforced by Google for each operation; UI-only delegated permissions may not grant equivalent API access.

The authorization-code flow requests offline access and consent, stores tokens and client credentials in Blackbird's connection vault, refreshes access tokens before expiry, and preserves an existing refresh token when Google omits it from refresh responses. Only the Bearer access token is passed to action credentials. Blackbird owns callback-state validation; the app also rejects a mismatched state when it is echoed in values.

Deleting an OAuth connection invokes Google's token-revocation endpoint. Connections sharing the same Google authorization grant can be affected by revocation. External Google OAuth apps in **Testing** receive refresh tokens that expire after seven days for this scope; plan the appropriate Google publishing/verification process for long-lived production use. This is a Google OAuth setting, not a missing refresh implementation.

## Actions

| Action | Inputs and behavior | Connection |
| --- | --- | --- |
| **Get video** | Video ID or supported video URL; returns canonical URL, title, description, channel metadata, UTC publication date, duration, caption flag and view count where available. | API key for public/accessible unlisted metadata; OAuth for accessible private metadata. |
| **Search videos** | Channel URL, search term or both; publication dates, sort order, maximum results, caption availability and duration filters. | API key or OAuth; returns public videos. |
| **Get video localizations** | Video ID; returns source title/description, default language and the full list of localized titles/descriptions. | API key for accessible public metadata, or OAuth. |
| **Update video localization** | Video ID, target language, optional Title and Description; adds or patches one translation while preserving other languages. | OAuth. |
| **Search caption tracks** | Video ID, optional exact language, Include drafts (defaults true); returns track IDs, language, name, draft state, kind, status, failure reason and UTC update date. | OAuth. |
| **Download captions** | Caption ID and optional format (defaults SRT); returns a Blackbird File plus Caption ID. Formats: SRT, VTT, TTML, SBV, SCC. | OAuth and video edit permission. |
| **Upload captions** | Video ID, language, subtitle File, optional Name and Is draft. Returns the created track metadata. New tracks default to draft. | OAuth. |
| **Update captions** | Video ID, Caption ID, optional replacement File and optional Is draft. At least one change is required. Validates the track belongs to the supplied video; preserves draft state when omitted. | OAuth. |
| **Find missing languages** | Video ID, required language list, optional Count auto-generated captions (defaults false). Returns separate missing and available metadata/caption language lists. | OAuth. |

### Video metadata localization

`Update video localization` accepts a target language such as `fr`, `de` or `pt-BR`. Supply at least Title or Description. A new language requires a non-empty title; an omitted existing title/description is preserved, while an explicitly empty description clears it. Titles are validated to at most 100 .NET string characters and descriptions to 5,000 UTF-8 bytes, without angle brackets.

The action reads the full translation map and merges the requested language. It writes only `part=localizations` when the source metadata language is already set. If the source language is missing, provide **Default language** explicitly: the action includes the existing mutable snippet fields when setting it. It refuses to change an established default language or treat the source language as a target translation. An ETag precondition is sent; a precondition failure tells the caller to read the current state and retry. Concurrent edits should be coordinated at workflow level.

No localized entries are removed by design, and the API's full-map write is not treated as an individual-language PATCH. The updated metadata is read back after the write; if this read fails, the write may already have succeeded.

### Caption files and publishing

Downloads return Blackbird file references via `IFileManagementClient.UploadAsync`; uploads consume them through `DownloadAsync`. The subtitle bytes, cue boundaries and timecodes are passed unchanged. The app streams caption files with a **100,000,000-byte** bound, rejects empty files, and does not use YouTube's deprecated `sync` parameter. YouTube validates subtitle syntax and timing; provide a supported time-coded subtitle file rather than a plain transcript.

Upload and replacement requests use multipart/related media upload. A draft-only update uses the JSON caption endpoint. The app never automatically retries writes: after an uncertain network/server failure, inspect the track/video state before manually retrying, to avoid duplicate caption tracks. A language/name conflict directs the user to search tracks and update the existing one or choose another name. YouTube processes new/replacement tracks asynchronously; inspect **Status** and **Failure reason** in subsequent Search caption tracks calls. A successful HTTP upload does not guarantee the track is already serving.

Caption download is not a public transcript API: it requires authorized video edit permission. Automatic captions are returned only when Google makes the track accessible to the authorized caller. This release neither extracts arbitrary public transcripts nor generates translations inside YouTube.

### Missing-language rules

Language matching is exact and case-insensitive; `pt-BR`, `pt-PT` and `pt` are distinct. Google validates which language tags it supports.

Metadata coverage counts a non-empty localized title and, if the source has a description, a non-empty localized description. The video's default source language counts as available when its source fields meet that rule. Caption coverage counts only **serving, non-draft** tracks, excluding **ASR** automatic tracks unless Count auto-generated captions is true. Missing metadata and missing captions are reported independently. These are coverage checks, not translation-quality assessments.

## Existing polling event: On new video added

Required input: Channel URL, supporting `https://www.youtube.com/@handle`, `/channel/UC...`, and `/user/username`, including known channel tabs such as `/videos`. Legacy `/c/` vanity links must be replaced with the current @handle or channel-ID URL.

### Video visibility filter

| Option | Connection and behavior |
| --- | --- |
| **Public only** (default) | API key or OAuth. Discover uploads when they first become public, unless their ID was already remembered by this Bird. Existing Birds keep this default. |
| **Unlisted only** | Owning channel's OAuth connection. Process uploads while shared by link. |
| **Private only** | Owning channel's OAuth connection. Prepare metadata and subtitles before publication. |
| **All accessible videos** | Owning channel's OAuth connection. Process any accessible public, unlisted or private upload. Recommended for a single pre-publication localization pipeline. |

Non-public modes reject API-key connections before enumeration. On every non-public poll, `channels.list(mine=true)` verifies that the OAuth channel identity owns the channel in Channel URL. Connecting a different Google/Brand Account fails with a configuration error rather than silently creating an empty baseline. UI-only delegates and partner/content-owner authentication are not special-cased: this build requires the owning channel to appear in that endpoint. The existing `youtube.force-ssl` permission is sufficient for this channel-upload event. The additional 1.5.0 consent scopes apply to analytics reporting.

The first poll establishes a silent baseline of uploads matching the filter. Changing the channel creates a new baseline and starts a separate history. Changing only Video visibility establishes another silent baseline of currently matching uploads while preserving remembered IDs for that channel, so broadening the filter does not backfill an old private/unlisted archive. After switching from an older version's Public only memory, existing public uploads are not replayed; choosing All accessible videos baselines existing private/unlisted uploads too. Use separate, explicitly designed backfill workflows if existing content needs processing.

Each poll scans the complete accessible uploads playlist, up to 1,000 pages / 50,000 items, filters its privacy statuses and rechecks the hydrated video status before delivery. A video whose visibility changes between the playlist and video calls is deferred if it no longer matches. Metadata that is not yet available remains eligible for a future poll. Each matching new video starts a separate Flight via `MultipleEvents`, up to 100 per poll; excess discoveries remain eligible for subsequent polls. At a polling checkpoint, Blackbird returns the collection instead of splitting Flights. Non-public modes add an ownership-read request per poll (plus further ownership pages if needed).

The event output includes **Video visibility** (`public`, `unlisted` or `private`) alongside the existing metadata and URL. **Published at** follows Google's metadata semantics; for private uploads retrieved by their owner it can be the upload time, not a public-release time. This field is also exposed by Get video and Search videos. A private URL remains private: returning a URL does not grant viewing permission to another app or viewer.

### Visibility changes and duplicate suppression

Version 1.3.0 retains remembered Video IDs rather than pruning them when a video disappears from a filtered snapshot. Thus a video processed as private/unlisted is not processed again merely because it becomes public, nor when it leaves and later re-enters the selected visibility. In All accessible videos mode, a single private → unlisted → public lifecycle triggers once after the initial baseline. Public only can still trigger for a previously unseen private/unlisted video when it first becomes public. This event does not provide a separate publication notification.

The ledger grows with the number of remembered matching uploads over the Bird's lifetime. Historical IDs already pruned by older versions cannot be reconstructed. Unpublishing/recreating the Bird or otherwise resetting its polling memory can establish a new history. Failed scans or ownership checks return no updated memory. Videos added and removed entirely between polls cannot be detected, and YouTube pagination is not atomic. Use Video ID for downstream idempotency where needed; end-to-end exactly-once processing is not promised. No source-change or video-published event is added in this release.

## Search behavior

Provide Channel URL, Channel ID, Search term or a combination of one channel selector and a term. Maximum results defaults to 100 and accepts 1–10,000. All pagination is internal. Dates are inclusive UTC; dates without a timezone are interpreted as UTC. Sort choices: relevance, newest first, title, view count, rating. Caption filter: any / has captions / has no captions. Duration filter: any / under four minutes / four–20 minutes inclusive / over 20 minutes. The short-duration filter does not identify the YouTube Shorts format.

Without a keyword or an index-only advanced filter, the action scans the channel uploads playlist and hydrates all public metadata before filtering/sorting/limiting; this avoids the search index's 500-video channel ceiling but costs approximately one playlist call and one video call per 50 uploads, plus resolution. A low Maximum results does not reduce this full scan's cost.

With a keyword, rating order, or an index-only advanced filter, the action uses `search.list(type=video)`, forwards filters, restores search order after metadata hydration, removes duplicate IDs and skips inaccessible/non-public/non-video results. A lookahead match determines **More results available**. A channel keyword search stops at 500 unique candidates and flags **Search limit reached**, independently of the lookahead flag. Keyword search may be delayed or incomplete; it is not a replacement for polling the uploads playlist. Defensive pagination errors fail explicitly.

Outputs: Videos, Video IDs, Video URLs, Returned count, First video, First video URL, More results available, Search limit reached. Empty searches return empty collections, count zero, null First video and an empty First video URL.

## Additional actions in 1.5.0

### Upload video

Accepts a Blackbird Video file and Title/Description, with optional Thumbnail, Privacy status, Publish at, Tags, Default language, Default audio language, one localized Language code/Title/Description, Recording date, License, Allow embedding, Public statistics viewable, Category ID, Made for kids, Notify subscribers and Contains synthetic media.

Privacy defaults to **private**. Scheduled publication requires a future UTC timestamp and private visibility. The upload targets the OAuth-selected channel. The file must have a known length, be non-empty and fit YouTube's 256 GB limit; workflow/runtime limits can be lower. The action starts a resumable session, validates its returned HTTPS upload endpoint, and streams the complete file via PUT without buffering the video in app memory. It does not implement cross-Flight resume, automatic chunk recovery or automatic retries. After an uncertain upload failure, inspect channel uploads before retrying to avoid duplicates.

The result includes Video ID, URL, returned privacy/upload status and Warnings. Upload success is not processing completion. An optional thumbnail is applied after video creation; if that step fails, the result preserves the created Video ID with a warning so it can be retried through **Update video thumbnail** without uploading the video again. Google account/project eligibility, quota and processing restrictions remain authoritative.

### Update video thumbnail

Accepts a Video ID and Blackbird JPEG/PNG file, validates its signature and the current documented 50 MB maximum, and uploads it via `thumbnails.set`. Account-specific restrictions and thumbnail eligibility are enforced by YouTube. For Shorts, wait for video processing to complete before setting the thumbnail.

### Add video to playlist

Accepts Playlist ID or a YouTube URL containing `list=`, Video ID, optional zero-based Position and Note (up to 280 characters). Returns the created Playlist item ID and identifiers. Explicit placement requires a manually ordered playlist. The operation deliberately permits repeated videos, as YouTube playlist entries have separate IDs. It is not retried automatically.

### Find video and expanded Search videos

Find video accepts the Search videos filters and returns Found, Video, Video ID and Video URL for the first match, or Found=false with empty identifiers. It does not mutate the original input object. Search videos additionally exposes **Channel ID**, **Video definition**, **Video dimension**, **Video license**, **Safe search**, **Region code**, **Relevance language**, **Video category ID**, and **rating** order. Region is a viewing-country filter; relevance language is a preference, not a strict video-language filter.

Supplying an index-only filter without a keyword uses the search endpoint instead of silently ignoring it, so search-index limitations and the channel search ceiling then apply. Existing unfiltered channel inventory behavior remains available.

### Get report

Requires OAuth with Analytics and YouTube read-only permissions and the YouTube Analytics API enabled in your Google project. Reconnect older OAuth connections to grant these permissions. Standard metrics use read-only Analytics consent; revenue metrics additionally require selecting **Include revenue reports**, granting monetary read-only consent and having eligible report data.

Inputs: Start date, End date, multiple Report metrics (one metric per selection), Report type (channel or partner contentOwner), optional Channel ID / Content owner ID, Report breakdown (dimensions), Filters, Sort and Maximum rows (default 1,000, range 1–10,000). The default channel is `MINE`. Content-owner reports require an eligible linked partner identity; merely supplying an owner ID does not grant access. Google validates dimension/metric/filter combinations and permissions.

Returns column names, Rows JSON, Returned count and More results available. Pagination uses `startIndex` and a lookahead row, up to the requested maximum. It does not return an estimated total. Analytics availability can lag and small data groups may be suppressed by Google.

### API request

An advanced text/JSON request action with URL, HTTP method, JSON query/header objects, optional Body/Content type and Stop on error (default true). Supports GET/POST/PUT/PATCH/DELETE to HTTPS `www.googleapis.com/youtube/v3/…`, `www.googleapis.com/upload/youtube/v3/…` and `youtubeanalytics.googleapis.com/v2/reports` only. URLs outside these APIs, userinfo, non-default ports, path traversal, credential query overrides, and authentication/routing/cookie/method-override headers are rejected. Redirects are not followed. Non-GET requests require OAuth.

Responses include status, body text and response headers JSON; known connection secrets are redacted. Stop on error=false returns non-success responses for explicit handling in the Bird. Transport failures still raise errors. Text output is limited to 10 MB; use dedicated file actions for media. This action does not bypass OAuth scopes, Google API capabilities, account eligibility or quota.

## Additional polling events in 1.5.0

All new events establish a silent first baseline, reset the baseline on changed source/filter inputs, retain delivered identities and return up to 100 events per poll with `MultipleEvents`. Excess items remain undispatched for a later poll if still discoverable. Full scans fail explicitly at 1,000 pages instead of silently truncating. These are polling events, not instant delivery guarantees.

| Event | Behavior |
| --- | --- |
| **On new comment on video** | Required Video ID; Include replies defaults to true. Scans all top-level comment threads and paginates replies for threads that have them. Returns comment ID, parent ID, video ID, author, plain text, publication/update dates and like count. Comment edits do not retrigger an existing ID. Full scans, especially replies, can consume substantial quota; disabled comments fail explicitly. |
| **On new video matching search** | Required Search term with the Search videos filter set. Uses newest-first by default, with a bounded result window (Maximum results defaults to 100). Baselines existing matches, then discovers unseen matches published at/after activation. Newly indexed older content is not treated as a new upload. Search indexing delays, the result window and Google's channel ceiling can cause missed discoveries; this is not an exhaustive upload log. Prefer the uploads event for reliable channel monitoring. |
| **On new video in playlist** | Required Playlist ID/URL and optional Video visibility (public by default). Non-public modes require the playlist owner's OAuth identity. Hydrates video metadata; deduplicates on playlist-item ID, so removing and re-adding a video produces a new event. Reordering an existing entry does not. Results include the playlist item ID, position and Video. |
| **On new live stream** | Channel URL or ID; Events defaults to Created and started. A broadcast may produce one created/discovered event and one started event. Public mode combines upcoming/live search results and confirms current video status. Own channel=true verifies ownership and uses `liveBroadcasts.list(mine=true)` for accessible owned broadcasts, including non-public ones. First poll baselines existing states. A stream created and ended entirely between polls can be missed; public discovery is also subject to search indexing/ceiling limitations. |

## Endpoints used

All Data API paths use `https://www.googleapis.com`.

| Operation | Method/path |
| --- | --- |
| Resolve channel / validate OAuth | `GET /youtube/v3/channels` (`forHandle`, `id`, `forUsername`, or validator `mine=true`) |
| Poll/read uploads | `GET /youtube/v3/playlistItems` |
| Keyword search | `GET /youtube/v3/search` |
| Read video/localizations | `GET /youtube/v3/videos` |
| Write localization map | `PUT /youtube/v3/videos` |
| List caption tracks | `GET /youtube/v3/captions` |
| Download caption file | `GET /youtube/v3/captions/{id}` |
| Insert caption file | `POST /upload/youtube/v3/captions?uploadType=multipart` |
| Replace caption file | `PUT /upload/youtube/v3/captions?uploadType=multipart` |
| Update caption draft status | `PUT /youtube/v3/captions` |
| Validate API key | `GET /youtube/v3/i18nLanguages` |
| Google authorization | `https://accounts.google.com/o/oauth2/v2/auth` |
| Token exchange/refresh | `POST https://oauth2.googleapis.com/token` |
| Revoke connection | `POST https://oauth2.googleapis.com/revoke` |

Caption operations consume materially more quota than metadata reads. Check the current quotas in your Google Cloud project and Google documentation before processing a large multilingual archive. Read operations retry HTTP 429/5xx responses twice with bounded delays; writes do not. Error messages avoid raw API responses and request URLs to protect credentials.

## Example Bird

On new video added (All accessible videos + owning-channel OAuth) → Get video localizations → Find missing languages → translate titles/descriptions and subtitle files through your chosen Blackbird apps → human review → Update video localization → Upload captions as draft → check caption processing status → Update captions with Is draft=false.

Translation, QE, terminology, context reuse and review remain composable Bird steps. Dubbed-audio publishing and XLIFF/HTML interchange remain outside this release. Video upload, playlist insertion, thumbnail updates and analytics reports are available in 1.5.0.

## Build and validation

Requires a .NET 8 SDK at least 8.0.400 and Python 3.

```bash
dotnet run --project validation/RegressionChecks -c Release
dotnet build YouTube.sln -c Release
dotnet test tests/Tests.YouTube -c Release
bash build.sh
```

The build script restores locked dependencies, publishes a portable Release library with no runtime identifier, then zips the publish-folder contents at the archive root as `dist/YouTube-Blackbird-1.6.0.zip`. The archive must remain under the conservative 15 MB limit. Keep app identifiers and public method names stable for future updates; increase the project version.

The supplemental mocked regression checks cover publication metadata, resumable-session validation and byte transfer, thumbnail files, playlist insertion, Analytics pagination, additional scopes, raw-request destination/header restrictions and redaction, all four new event lifecycles, and advanced search filters. Tests cover visibility modes, owner checks, legacy memory migration, status transitions, temporary disappearance, filter changes, race handling and non-public backlog delivery, as well as existing polling/search behavior plus OAuth URL generation, code exchange, refresh-token preservation, expiry, state handling, scope checks and revocation; localized metadata merging and source-field preservation; multipart caption upload/download/update behavior; draft defaults and preservation; coverage rules; error redaction and no write retries. They use mocked Google responses. See `TEST-RESULTS.txt` for the executed checks. Live OAuth consent, real Google mutations, Blackbird file transfer and installation in a Blackbird organization have not been exercised with customer credentials.

## Official references

- https://docs.blackbird.io/sdk/connections/
- https://docs.blackbird.io/sdk/files/
- https://docs.blackbird.io/sdk/deploying/
- https://github.com/bb-io/TemplateApp
- https://developers.google.com/youtube/data/reference/rest/v3/videos/list
- https://developers.google.com/youtube/data/reference/rest/v3/videos/update
- https://developers.google.com/youtube/data/reference/rest/v3/captions/list
- https://developers.google.com/youtube/data/reference/rest/v3/captions/download
- https://developers.google.com/youtube/data/reference/rest/v3/captions/insert
- https://developers.google.com/youtube/data/reference/rest/v3/captions/update
- https://developers.google.com/identity/protocols/oauth2/web-server
- https://developers.google.com/identity/protocols/oauth2#expiration

- https://zapier.com/apps/youtube/integrations
- https://developers.google.com/youtube/data/reference/rest/v3/videos/insert
- https://developers.google.com/youtube/data/guides/using_resumable_upload_protocol
- https://developers.google.com/youtube/data/reference/rest/v3/thumbnails/set
- https://developers.google.com/youtube/data/reference/rest/v3/playlistItems/insert
- https://developers.google.com/youtube/data/reference/rest/v3/commentThreads/list
- https://developers.google.com/youtube/data/live/reference/rest/v3/liveBroadcasts/list
- https://developers.google.com/youtube/analytics/reference/reports/query

## Version 1.5.0 picker behavior

Language suggestions use `i18nLanguages.list`; additional valid BCP-47 tags may be entered directly. Region suggestions use `i18nRegions.list`. Category suggestions use assignable categories from `videoCategories.list`, scoped by Region code (US by default). Upload's Region code controls suggestions only and is not written into video metadata.

The playlist picker uses the OAuth account's playlists: up to 50 suggestions, inspecting the first 50 playlists when opened and up to 250 when searching. Enter another playlist ID or URL directly if needed. An API-key connection has no owned-playlist suggestions.

Update captions uses Video ID to populate Caption ID suggestions. Download captions adds an optional Video ID for the same purpose; existing Birds can continue supplying Caption ID alone. Caption suggestions inspect the selected video's caption tracks and show up to 50 matches.

## Scaling and validation limits

Playlist polling skips metadata hydration for previously processed playlist entries, while preserving the full entry scan. Channel, playlist and comment discovery still perform full inventory scans where needed for correctness. Comment replies are still enumerated when enabled. Historical seen-ID memory remains cumulative to preserve duplicate suppression across disappearance and visibility changes. This release does not introduce a timestamp cutoff, prune that history, or promise constant-cost polling. The existing 1,000-page safety limits remain.

Real Google API integration tests are in `tests/Tests.YouTube`; they have been compiled but not executed here because no live credentials were provided. `VALIDATION.md` separates completed checks from pending release gates. The platform's private `PluginInputMapper` is not available in the public SDK packages; restoration through the actual Blackbird runtime remains an explicit installation smoke test, rather than a JSON-only compatibility claim.

## Version 1.6.0 operations

### Publication and metadata updates

**Update video** changes source metadata and preserves omitted fields, translations and publication settings. Empty Description clears the description. An empty Tags collection or Clear tags removes all tags; Tags and Clear tags cannot be combined. Region code affects category suggestions only.

**Set video publication settings** changes visibility, scheduling, license, embedding, public statistics, audience and synthetic-media settings. Omitted settings are preserved. Publish at must be in the future for an already private, never-published video with private target visibility; Google enforces publication-history eligibility. Cancel scheduled publication removes the schedule. Selecting public or unlisted also clears an existing schedule. A stored, elapsed schedule must be explicitly canceled or replaced before changing unrelated publication settings.

Both actions read current values, preserve known editable fields in changed resource parts, send an ETag precondition and reread the result. A failed read after a successful write does not roll back the write. Concurrent conflicts require a fresh read and deliberate retry; writes are not automatically retried.

### Owned inventories and playlists

**Search channel videos** requires the owning channel's OAuth connection. It defaults to all accessible public, unlisted and private uploads, using the connected channel unless Channel URL is supplied. Text matching searches titles and descriptions literally and case-insensitively, without search-index operators. Publication dates are inclusive UTC filters. Results follow channel upload order. Existing Search videos continues to search public videos.

**Create playlist** defaults to private. **Search playlists** without a Channel URL searches the OAuth account's playlists; a supplied channel URL can use an API key for publicly accessible playlists. **Get playlist** and **Update playlist** output metadata and existing translations; updates preserve omitted fields, translations and podcast status when visibility is written. Empty descriptions clear existing descriptions.

**Search playlist videos** defaults to public videos. Select another visibility mode with OAuth to include accessible non-public videos. The connected account must have access to both the playlist and each video; a private playlist does not grant access to someone else's private video. Playlist order and repeated entries of the same video are preserved; deleted and inaccessible videos are omitted.

**Remove video from playlist** uses Playlist item ID, not Video ID. Its optional Playlist ID both populates entry suggestions and checks membership before removal. The video itself is not deleted. Entry suggestions show up to 50 matches, scanning up to 250 entries when searching; enter another entry ID directly if necessary. The action fails if an entry has already been removed.

New search actions default to 100 matching results, support 1-10,000, and handle pagination internally until enough matches or the end of the accessible source. A 1,000-page guard fails explicitly instead of pretending an incomplete scan is complete. Narrow filters can still require expensive scans. These actions do not expose continuation tokens.


### Added endpoints

| Action | Endpoint flow |
| --- | --- |
| Update video | GET /youtube/v3/videos; PUT /youtube/v3/videos?part=snippet; reread |
| Set video publication settings | GET /youtube/v3/videos; PUT /youtube/v3/videos?part=status; reread |
| Search channel videos | GET /youtube/v3/channels; GET /youtube/v3/playlistItems; GET /youtube/v3/videos |
| Create playlist | POST /youtube/v3/playlists |
| Get playlist | GET /youtube/v3/playlists |
| Search playlists | GET /youtube/v3/playlists, with channels lookup when a Channel URL is supplied |
| Update playlist | GET /youtube/v3/playlists; PUT /youtube/v3/playlists; reread |
| Search playlist videos | GET /youtube/v3/playlists; GET /youtube/v3/playlistItems; GET /youtube/v3/videos |
| Remove video from playlist | GET /youtube/v3/playlistItems?id=...; DELETE /youtube/v3/playlistItems?id=... |

All endpoints use the existing allowlisted Google hosts. No OAuth scopes or connection fields are added. The prior limits and guarantees remain except where this section describes new actions.
