# Blackbird.io YouTube

Version **1.13.1**

<!-- begin docs -->

Connect YouTube to Blackbird to discover videos, translate titles and descriptions, manage caption files, prepare publication and retrieve analytics. This app provides 28 actions and six events.

## Before setting up

Use a Google Cloud API key for public discovery. Use a Google OAuth connection for private content, captions, publication, playlists and analytics. You must have permission to manage the target channel.

## Connecting

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

### Playlists and reports

- **Find video** Find the first public video matching the search filters. Outputs Found as false when nothing matches.
  - **Channel URL**: Optional public channel URL. OAuth suggestions use the selected account; direct public URLs remain accepted. Supply Channel URL or Channel ID, not both. Without either, Search term searches public videos globally.
  - **Search term**: Optional YouTube search query. Supports YouTube's OR (|) and exclusion (-) operators. Without a term, retrieves the channel's uploads.
  - **Published after**: Include videos published at or after this time. Dates without a timezone are interpreted as UTC.
  - **Published before**: Include videos published at or before this time. Dates without a timezone are interpreted as UTC.
  - **Sort by**: Defaults to relevance for keyword searches and newest first for channel uploads. Relevance without a search term uses newest first.
  - **Maximum results**: Maximum number of matching videos to return, from 1 to 10,000. Defaults to 100. Pagination is automatic; larger searches consume more API quota.
  - **Caption availability**: Optional. Supply this value when needed.
  - **Duration**: Short means under four minutes, not the YouTube Shorts format.
  - **Channel ID**: Alternative to Channel URL. OAuth suggestions show owned channel IDs; public channel IDs can be entered or mapped. Supply one channel selector.
  - **Video definition**: Optional. Supply this value when needed.
  - **Video dimension**: Optional. Supply this value when needed.
  - **Video license**: Optional. Supply this value when needed.
  - **Safe search**: Optional. Supply this value when needed.
  - **Region code**: Two-letter country code, e.g. HU or US. Limits indexed results to videos viewable there.
  - **Relevance language**: Language preference for indexed search; does not guarantee the video's language.
  - **Video category ID**: Optional. Supply this value when needed.
- **Add video to playlist** Add a video to a managed playlist, optionally specifying its position and a note. Requires OAuth.
  - **Position**: Optional zero-based position. The playlist must use manual ordering. Omit to use YouTube's default placement; adding an existing video may create another distinct entry.
  - **Note**: Optional note, at most 280 characters.
- **Get report** Get channel or partner content-owner analytics for the selected dates and metrics. Requires Analytics API access and OAuth.
  - **Report type**: channel (default) or contentOwner. Content-owner reports require an authorized YouTube partner identity.
  - **Content owner ID**: Partner-only identity for Content owner reports. Use advanced metrics, breakdown and Filters for partner-specific combinations; normal channel OAuth does not grant partner access.
  - **Report breakdown**: Choose a compatible breakdown, or enter/map dimensions. Totals omits the breakdown. Monthly reports require both dates on the first of a month. Ranked video reports require a supported descending Sort. Custom combinations are validated by Google.
  - **Filters**: Advanced Analytics filter expression. Without helper fields it is passed unchanged. With helpers, only compatible video/country equalities can be merged; conflicts raise an error.
  - **Sort**: Choose a compatible sort or enter/map an Analytics sort expression. Omit to retain Google's default. Ranked video reports require a supported descending metric.
  - **Video ID**: Optional owned-video filter for common channel reports. Choose, enter or map an ID or URL. Not supported by the guided ranked-video breakdown. Omit when using advanced custom filters.
  - **Country**: Optional country filter for compatible channel reports. Cannot be combined with a country breakdown or conflicting advanced filters. Omit to keep all countries.
  - **Maximum rows**: Default 1,000; range 1–10,000. Pagination is internal.
- **API request** Send an authenticated text or JSON request to an allowed video or analytics endpoint. Credentials are restricted to those endpoints.
  - **Query parameters JSON**: Advanced JSON object of query key/value pairs. Dedicated actions provide guided fields; this action preserves custom API expressions.
  - **Additional headers JSON**: Authentication, routing and cookie headers cannot be overridden.
  - **Body**: Optional UTF-8 request body. For files use dedicated upload actions.
  - **Content type**: Optional. Supply this value when needed.
  - **Stop on error**: Defaults to true.

### Analytics

- **Get lifetime statistics** Get a snapshot of cumulative channel views, rounded subscribers and public video count. Supply Channel URL or use the OAuth-connected channel.
- **Get time-bound metrics** Get date-range views, watch time, engagement and subscriber changes as directly mappable fields. Omit Video ID for channel totals. Requires the owning channel's Analytics OAuth access.
- **Get video statistics by ID** Get a snapshot of cumulative views, likes and comments for one video. Video ID supports an OAuth dropdown, direct ID, URL or a mapped value. Private videos require authorized OAuth access.

### Caption files

- **Download captions** Download an authorized caption track as a file in the selected subtitle format. Requires OAuth and permission to edit the video.
  - **Format**: Defaults to SRT. YouTube performs the format conversion.
- **Upload captions** Create a caption track from a time-coded subtitle file. New tracks default to draft. Requires OAuth.
  - **Name**: Track name, up to 150 characters. Defaults to the language code.
  - **Is draft**: Defaults to true, so a new track can be reviewed before publication.
- **Update captions** Replace a caption file, change its draft status, or both. Omitted draft status is preserved. Requires OAuth.
  - **File**: Optional replacement time-coded subtitle file. Omit to update only draft status.

### Localization

- **Get video localizations** Get the source title and description and all existing metadata translations.
- **Update video localization** Add or update one language’s title and description while preserving other translations. Requires OAuth.
  - **Title**: Required when adding a new language. Omit to preserve an existing localized title.
  - **Description**: Omit to preserve an existing description. An empty string clears the localized description.
  - **Default language**: Source metadata language. Required only when the video does not already have one. Existing source language cannot be changed by this action.
- **Search caption tracks** Find caption tracks and their language, status and draft information for a managed video. Requires OAuth.
  - **Language**: Optional exact language tag. Suggestions show tracks on the selected video and respect Include drafts. Omit for all languages; direct tags remain accepted.
  - **Include drafts**: Defaults to true.
- **Find missing languages** Identify required languages missing from complete metadata translations or published captions. Requires OAuth.
  - **Count auto-generated captions**: Defaults to false. Only serving, non-draft caption tracks count as available.

### Playlists

- **Create playlist** Create a playlist with a title, description and visibility. New playlists default to private. Requires OAuth.
  - **Privacy status**: Defaults to private.
- **Get playlist** Get playlist metadata, visibility, video count and existing metadata translations using its ID or URL.
- **Search playlists** Find playlists by channel and optional title or description text. Omit the channel to search the connected account's playlists with OAuth.
- **Update playlist** Update a playlist's title, description, source language or visibility while preserving omitted settings and existing translations. Requires OAuth.
- **Search playlist videos** Get accessible video entries from a playlist, with optional text and visibility filters. Preserves playlist order and repeated entries.
  - **Video visibility**: Defaults to public. Other visibility modes require OAuth.
- **Remove video from playlist** Remove a specific playlist entry using Playlist item ID. The video itself is preserved. Requires OAuth.
  - **Playlist ID**: Optional. Populates playlist-entry suggestions and verifies that the entry belongs to this playlist before removal.

### Publishing

- **Upload video** Upload a video file with metadata, an optional publication schedule and thumbnail. New videos default to private. Requires OAuth.
  - **Thumbnail**: Optional. If thumbnail upload fails after video creation, the result includes a warning and the created video ID.
  - **Publish at**: Future UTC publication time; requires private visibility.
  - **Tags**: Optional. Supply this value when needed.
  - **Default audio language**: Optional. Supply this value when needed.
  - **Language code**: Optional target language for one localized title/description at upload. This is separate from source metadata language and original audio language; supply the localized title when adding this language.
  - **Localized title**: Optional. Supply this value when needed.
  - **Localized description**: Optional. Supply this value when needed.
  - **Recording date**: Optional. Supply this value when needed.
  - **License**: Optional. Supply this value when needed.
  - **Allow embedding**: Optional. Supply this value when needed.
  - **Public statistics viewable**: Optional. Supply this value when needed.
  - **Category ID**: Defaults to 22 (People & Blogs).
  - **Made for kids**: Optional. Supply this value when needed.
  - **Notify subscribers**: Defaults to true.
  - **Contains synthetic media**: Optional. Supply this value when needed.
- **Update video thumbnail** Set a JPEG or PNG thumbnail for a managed video. Requires OAuth and a channel eligible for custom thumbnails.

### Transcripts

- **Get video transcript** Get caption-based text, timed segments and a WebVTT file for an authorized video. Select an exact language or Caption ID, or use deterministic source-language selection. Does not generate speech recognition.
  - **Caption ID**: Optional advanced override for a specific track on this video. Language and draft/automatic settings must agree. Forced tracks are allowed only through an explicit override.
  - **Allow automatic captions**: Defaults to false. Explicitly enable ASR tracks; the action does not generate speech recognition or correct caption text.

### Videos

- **Search videos** Search public videos by keyword, channel and filters. Without a keyword, retrieve public channel uploads.
- **Get video** Get a video’s metadata and watch URL using its ID or URL.

### Video management

- **Update video** Update source metadata while preserving omitted fields, existing translations and publication settings. Requires OAuth.
  - **Clear tags**: Set true to remove all tags. Cannot be combined with Tags.
- **Set video publication settings** Change visibility, schedule publication or update audience and distribution settings for an existing video. Omitted settings are preserved. Requires OAuth.
  - **Cancel scheduled publication**: Set true to remove the schedule. Cannot be combined with Publish at.
- **Search channel videos** Find existing videos in the connected channel, including private and unlisted uploads. Supports metadata and publication-date filters. Requires the owning channel's OAuth connection.

## Events

### Discovery

- **On new comment on video** Start a workflow when a new comment is added to the selected video, optionally including replies.
  - **Include replies**: Defaults to true. Full reply pagination adds API calls for threads with replies.
- **On new video matching search** Start a workflow when a newly published public video matches a search. Search indexing can be delayed.
- **On new video in playlist** Start a workflow when a new playlist entry matches the selected visibility.
- **On new live stream** Start a workflow when a broadcast is discovered, starts streaming, or both. Owner OAuth supports non-public broadcasts.
  - **Events**: Defaults to both creation/discovery and going live. A broadcast may generate one Flight for each event type.
  - **Own channel**: When true, use authorized Live Streaming API for the owning channel, including accessible non-public broadcasts. Otherwise monitor public upcoming/live search results.

### Channel comments

- **On new comment on channel** Start a workflow when a new published comment appears under any video in the selected channel, optionally including replies on older discussions.

### Channel videos

- **On new video added** Start a workflow when a new video in any watched channel matches the selected visibility. Public videos are selected by default; other modes require the owner’s OAuth connection.
  - **Additional channel URLs**: Optional additional YouTube channel URLs, one per list item. Watch up to 20 channels in total. New channels start with a silent baseline. The same connection and visibility filter apply to every channel.

## Additional information

### Upload and JSON request fix in version 1.13.1

Fixed a request-formatting defect that could make Upload video and other JSON write actions fail before reaching Google. JSON content now passes a valid media type to the HTTP library while preserving UTF-8 text. Existing inputs, outputs, scopes and upload behavior are unchanged. Update the existing app with YouTube-Blackbird-1.13.1.zip and test the same Bird. If an earlier attempt might have created a video, check the channel before retrying; uploads are not automatically retried.



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

### Analytics snapshots and date-range summaries

**Get lifetime statistics** outputs the Data API's current cumulative channel View count, rounded Subscriber count, Subscriber count hidden flag and Public video count, with channel identity and a UTC Retrieved at timestamp. Supply Channel URL with an API key, or omit it with OAuth to use the selected channel. Supported channel URL forms match the rest of the app. This action is a snapshot of channel counters, not an all-time Analytics watch-time report or an immutable historical total. Public video count excludes private/unlisted uploads even for owners; hidden or unavailable subscriber counts remain empty. YouTube rounds the subscriber counter to three significant figures.

**Get time-bound metrics** requires Start date and End date and the owning channel's OAuth connection with Analytics access. Omit Video ID for a channel-wide aggregate, or choose/map one owned video using the shared dynamic picker. The action verifies a supplied video belongs to the channel selected by the OAuth connection. It outputs Views, Engaged views, Estimated minutes watched, Average view duration in seconds, Average view percentage, Likes, Comments, Shares, Subscribers gained and Subscribers lost as directly mappable fields. These are Analytics values for the selected reporting period, not changes computed by subtracting two Data API counter snapshots. With a video filter, subscriber changes follow Google's video-attribution rules rather than representing all channel subscriber changes.

Dates are inclusive reporting-calendar dates in Pacific time; time-of-day is ignored without shifting the supplied calendar date. Requested start/end dates are echoed in the result. They do not certify the last available reporting day: Google may return data only through the latest day for which all requested metrics are available. Has data is false when no report row is returned, and unavailable metrics remain empty rather than becoming zero. A returned numeric zero remains zero. Average values retain their fractional precision. Retrieved at is UTC.

**Get video statistics by ID** outputs a video's current cumulative View count, Like count and Comment count, plus Video ID/URL, title, Channel ID, visibility and UTC Retrieved at. Video ID supports the existing OAuth dropdown, typed IDs, supported URLs and mapped values. API keys can read accessible public video statistics; private videos require an authorized OAuth connection. Missing counters remain empty, and inaccessible/missing videos raise an actionable error. These are Data API snapshots and do not include date-range watch time or revenue.

No new connection fields or OAuth scopes are introduced. Enable YouTube Data API v3 for counters and YouTube Analytics API for time-bound metrics. Standard reports access is sufficient for the new summary; no monetary metrics are requested. If Analytics permission was not granted, reconnect and approve the existing Analytics read-only permission. The existing **Get report** action is retained for custom metrics, daily/monthly breakdowns, geography and eligible revenue reports.

Implementation endpoints: channels.list with part=snippet,statistics; videos.list with part=snippet,status,statistics; and https://youtubeanalytics.googleapis.com/v2/reports with a supported aggregate metric pack and optional video filter. Reporting data can lag or differ from Data API counters. See [channel counters](https://developers.google.com/youtube/v3/docs/channels#statistics), [video counters](https://developers.google.com/youtube/v3/docs/videos#statistics), [channel reports](https://developers.google.com/youtube/analytics/channel_reports), [report queries](https://developers.google.com/youtube/analytics/reference/reports/query) and [reporting dates](https://developers.google.com/youtube/analytics/dimensions#day).

### Choose videos, captions and playlist entries

With an OAuth connection, **Video ID** offers a searchable dropdown in Get video, Update video, Set video publication settings, Update video thumbnail, Get video localizations, Update video localization, Find missing languages, Search caption tracks, Upload captions, Download captions, Update captions and Add video to playlist. Get video statistics by ID and optional Video ID in Get time-bound metrics and Get report also use the shared picker.

Suggestions come from the one YouTube or Brand Account channel selected during OAuth authorization. Each label shows the video's current title, visibility and Video ID. Accessible public, unlisted and private uploads can be suggested. Search by title or Video ID, case-insensitively. Up to 50 matching videos are shown in upload-playlist order. Manual video IDs and supported video URLs, and Video ID bindings from prior steps, remain accepted by the actions. With an API-key connection, owner video suggestions are empty; manually entered or mapped public videos remain usable in actions that support API keys. OAuth-only actions still require OAuth.

For **Download captions** and **Update captions**, first select or map Video ID, then choose Caption ID from its dependent dropdown. Download captions keeps Video ID optional: a directly supplied Caption ID can still be used. Changing the video does not authorize a caption from a different video; existing action checks remain in effect. Caption labels show language, track name, Draft or Not draft, track type, processing status and Caption ID. Track types distinguish Automatic (ASR), Standard and Forced; Standard is not a guarantee of human authorship. Ready indicates the caption-processing status, not the video's publication visibility. Draft tracks are labeled separately. Unnamed tracks remain selectable.

Owned playlist suggestions show title and Playlist ID. **Playlist item ID** suggestions for Remove video from playlist depend on Playlist ID and show the video title, Video ID and distinct playlist-entry ID. A video appearing twice remains two separate entries. Search playlist entries by title, Video ID or playlist-entry ID; searches are case-insensitive. Stored field values remain IDs, not their display labels.

Video, playlist and playlist-entry searches load pages until 50 matching suggestions or the end of the accessible source. Searches are no longer limited to the first 250 items. An empty search generally loads only the first full page; sparse or unavailable video metadata can require further pages to fill suggestions. The handlers stream pages and retain at most 50 matching labels plus deduplication IDs. The shared 1,000-page safety guard still applies and raises an error rather than silently returning an incomplete search. Narrow searches on large channels or playlists can take longer and consume more quota; enter or map an exact ID directly to avoid enumerating suggestions. A failed page request surfaces as an error, not a successful partial suggestion list.

Visibility, license, caption format, sort order and other fixed-choice dropdowns remain static. Channel suggestion pickers and Analytics report presets are not added in this release. No new OAuth permissions or connection fields are required.

### Watch multiple channels for new videos

**On new video added** accepts the existing required **Channel URL** plus an optional **Additional channel URLs** list. Add one complete YouTube channel URL per list item. Up to **20 distinct channel URL selectors** are supported per Bird, including the primary channel. Supported forms are current @handle, /channel/UC... and /user/username URLs. Duplicate selectors are ignored; different URLs resolving to the same Channel ID are scanned once.

The existing Channel URL remains required. Leave Additional channel URLs empty to keep a single-channel Bird. Existing connections, event identity, Video outputs and visibility choices are preserved. Outputs already include Channel ID and Channel title, alongside Video ID, Video URL and the video's metadata. Each discovered video starts a separate Flight.

The first successful poll records existing videos without starting Flights. Adding a channel records that channel's existing videos silently while continuing to discover new videos in channels already being watched. Reordering URLs or switching to another supported URL for the same resolved channel preserves its history. Removing a channel stops monitoring it; adding it back later creates a fresh silent baseline. Changing Video visibility creates a silent baseline for every watched channel while retaining previously seen IDs.

A single connection and Video visibility setting apply to the whole event. Public only remains the default and can watch public channels using an API key. Unlisted only, Private only and All accessible videos require OAuth, with ownership verified for **every** watched channel. An OAuth login does not automatically grant access to every Brand Account managed by the same person. If channels require separate authorized identities, use a separate connection and Bird per identity. An inaccessible or invalid channel fails the poll rather than being silently skipped.

At most **100 videos total** are delivered in one poll. Capacity is shared in rotation among channels with unseen videos, preserving oldest-first upload order within each channel; there is no global chronological order across channels. Undelivered videos remain eligible on later polls while accessible. Seen-ID history is tracked independently per resolved channel and retained across temporary disappearance. If any channel scan, ownership check or metadata request fails, no advanced memory is returned and prior memory is not modified.

Monitoring more channels increases API quota usage and polling time. Upload inventories are paginated internally using channels.list and playlistItems.list, followed by videos.list for discovered metadata. The existing 1,000-page per-inventory safety limit remains in effect. Seen-ID history grows with watched uploads; use a representative staging Bird to validate runtime memory and polling duration before large-scale use.

### Channel-wide comment monitoring

**On new comment on channel** monitors accessible published comments under videos in a specified Channel URL. The URL can use the current @handle, /channel/UC... ID or /user/username form. Use an API key for publicly accessible comments or an authorized OAuth connection where required. No new connection fields or OAuth scopes are introduced.

**Include replies** defaults to true. The event checks replies on older discussions, not only newly created threads. Set it to false to monitor top-level comments alone and reduce API calls. Threads without a Video ID are excluded: this event concerns video comments rather than other channel/community content. Held-for-review, spam or rejected comments are outside its published-comment scope.

Outputs include Channel ID and URL, Video ID and URL, Comment ID and URL, Parent comment ID, Is reply, Author, Text, Published at, Updated at and Like count. A new output type is used so the existing video-level event's output is unchanged.

The first complete scan establishes a silent baseline. A different resolved channel or changed Include replies setting establishes another silent baseline. Afterward, up to 100 previously unseen comments/replies are delivered per poll, oldest first with a stable ID tie-break. Undelivered discoveries remain eligible while they remain accessible; a comment removed before delivery can be missed. Each item starts one Flight. Edits do not retrigger the same Comment ID, and temporary disappearance does not erase remembered IDs.

Comment threads are paginated with allThreadsRelatedToChannelId. Replies are fully paginated using comments.list(parentId), because an embedded replies collection may be incomplete. No timestamp cutoff or unchanged-reply-count shortcut is used. Newly accessible or newly published comments can be discovered after their original creation date.

This reliability requires rescanning accessible history, which can be expensive for active channels. Read each page without retaining all historical comment text; only the 100 pending payloads and ID sets are retained in the event's working state. Persisted seen-ID history grows over time. Existing per-source 1,000-page limits apply, with an additional 2,000-logical-read limit across channel lookup, thread pages and reply pages per poll. HTTP read retries can add network calls beyond that logical-read limit. Exceeding a limit or encountering a failed read raises an error before new memory is returned; a partial scan is never treated as complete. For a very large channel, use a narrower video-level event or design a separate larger-volume scan strategy.

The existing **On new comment on video** event continues to require Video ID and retains its existing behavior, inputs, outputs and memory. Existing Birds can keep using it; use the new event for a channel-wide audience-feedback Bird.

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

Language and region pickers suggest supported values. Category suggestions use Region code, defaulting to US. With OAuth, Video ID suggests accessible uploads from the selected channel and Playlist ID suggests owned playlists. Caption ID suggestions depend on Video ID; Download captions keeps that field optional. Video, caption and playlist labels show identifying details while preserving stored ID values. IDs and additional valid language tags can still be entered directly.

Caption files must be non-empty and no larger than 100 MB. Thumbnail files must be JPEG or PNG and no larger than 50 MB. Video files must have a known size and fit the 256 GB API limit; platform limits can be lower. A partially completed write may already have created or changed content, so inspect its current state before retrying.

## Feedback

For feedback or implementation requests, contact your Blackbird team or use the [established channels](https://www.blackbird.io/).

<!-- end docs -->

## Installation and development

Upload `YouTube-Blackbird-1.13.1.zip` using the existing custom app’s **New version / Update app** option. Keep the current app identity to preserve existing Birds and connections. The source ZIP is for development.

Read [the detailed reference](docs/REFERENCE.md) for visibility transitions, silent baselines, quotas, search limits, caption processing and publication safeguards. Read [validation status](VALIDATION.md) before production rollout and [release notes](RELEASE-NOTES.md) for version changes.

```bash
dotnet build YouTube.sln -c Release
dotnet run --project validation/RegressionChecks -c Release
cp tests/Tests.YouTube/appsettings.json.example tests/Tests.YouTube/appsettings.json
dotnet test tests/Tests.YouTube -c Release
bash build.sh
```

The live tests require a disposable OAuth-managed channel with Analytics API access, comments and live streaming available, custom thumbnails enabled, and the appropriate scopes. They create a private video, caption tracks and private playlists and attempt cleanup at assembly completion. The channel-comment integration test also uploads an unlisted disposable video, posts test comments and a reply under that video, and deletes the video in cleanup. An interrupted test can leave that unlisted video and its comments. The scheduling test temporarily schedules its disposable video two days ahead, then attempts cancellation and deletion; an interrupted run can leave a scheduled video. The suite is explicitly non-parallel to protect its shared test resources. Interrupted or failed runs may leave test resources; inspect the test channel afterward. Do not use a production channel. Caption processing and search indexing can delay live results.

The icon is derived from the service’s public favicon at https://www.youtube.com/favicon.ico. No credentials are included.
