# Blackbird.io YouTube

Blackbird is the new automation backbone for the language technology industry. Blackbird provides enterprise-scale automation and orchestration with a simple no-code/low-code platform. Blackbird enables ambitious organizations to identify, vet and automate as many processes as possible. Not just localization workflows, but any business and IT process. This repository represents an application that is deployable on Blackbird and usable inside the workflow editor.

## Introduction

<!-- begin docs -->

YouTube is an online video sharing platform owned by Google. Use this app to discover videos, translate video and playlist metadata, manage caption files, extract caption-based transcripts, publish videos and retrieve channel analytics.

## Before setting up

The app supports two connection types:

- **YouTube Data API key** – read-only access to public channels, videos, playlists and comments.
- **YouTube channel management (OAuth)** – required for private content, captions, transcripts, publishing, playlist management and analytics. You must have permission to manage the target channel.

### API key

1. In [Google Cloud Console](https://console.cloud.google.com/), select or create a project and enable **YouTube Data API v3**.
2. Go to **APIs & Services > Credentials** and create an API key.
3. Restrict the key to **YouTube Data API v3**. Do not use HTTP-referrer (browser) restrictions, because requests are sent from Blackbird servers.

### OAuth

The OAuth connection uses your own Google OAuth client:

1. In the same Google Cloud project, enable **YouTube Data API v3** and, to use reports, **YouTube Analytics API**.
2. Configure the **Google Auth Platform** consent screen. For an external app in testing mode, add the Google accounts you will connect as test users.
3. Add the following scopes:
   - `https://www.googleapis.com/auth/youtube.force-ssl`
   - `https://www.googleapis.com/auth/youtube.readonly`
   - `https://www.googleapis.com/auth/yt-analytics.readonly`
   - `https://www.googleapis.com/auth/yt-analytics-monetary.readonly` (only if you need revenue reports)
4. Create an OAuth client of type **Web application** and add `https://bridge.blackbird.io/api/AuthorizationCode` as an **Authorized redirect URI**.
5. Copy the **Client ID** and **Client secret**.

Google issues refresh tokens that expire after 7 days for OAuth apps in testing mode. Publish and verify your Google OAuth app for long-lived connections.

## Connecting

1. Navigate to **Apps** and search for **YouTube**.
2. Click **Add Connection**.
3. Name your connection for future reference, e.g. 'My YouTube connection'.
4. Select the connection type:
   - **YouTube Data API key**: enter the API key.
   - **YouTube channel management (OAuth)**: enter the **OAuth client ID** and **OAuth client secret**, choose **Analytics access** (**Standard reports** or **Include revenue reports**), click **Authorize connection** and sign in with the Google account that manages the channel. Select the correct Brand Account if prompted.
5. Click **Connect** and confirm that the connection appears and the status is **Connected**.

Reconnect the OAuth connection after changing **Analytics access**, so that Google can grant the updated scopes.

## Actions

### Videos

- **Search videos** searches public videos by keyword, channel, publication date and other filters. Without a keyword, it returns public channel uploads.
- **Get video** returns a video's metadata and watch URL by its ID or URL.

### Video management

- **Search channel videos** returns videos of the connected channel, including private and unlisted uploads, with metadata and publication-date filters.
- **Update video** updates the title, description, tags, category and languages of a video. Omitted fields, existing translations and publication settings are preserved.
- **Set video publication settings** changes visibility, schedules or cancels publication and updates license, embedding, audience and synthetic-media settings. Omitted settings are preserved.

### Publishing

- **Upload video** uploads a video file with metadata, an optional publication schedule and an optional thumbnail. New videos are private by default.
- **Update video thumbnail** sets a JPEG or PNG thumbnail for a video. The channel must be eligible for custom thumbnails.

### Localization

- **Get video localizations** returns the source title and description and all existing metadata translations of a video.
- **Update video localization** adds or updates the title and description for one language, preserving other translations.
- **Search caption tracks** returns caption tracks of a video with their language, status and draft information.
- **Find missing languages** returns the required languages that are missing from metadata translations or published captions of a video.

### Caption files

- **Download captions** downloads a caption track as a file in the selected subtitle format.
- **Upload captions** creates a caption track from a time-coded subtitle file. New tracks are drafts by default.
- **Update captions** replaces a caption file, changes its draft status, or both.

### Transcripts

- **Get video transcript** returns the transcript text, timed segments and a WebVTT file built from an existing caption track. Select a language or a Caption ID, or let the app pick the track based on the video's audio language. The action does not perform speech recognition; use the **Transcript available** and **Retry recommended** outputs to branch your workflow when captions are not ready.

### Playlists

- **Search playlists** returns playlists of a channel with optional title or description filters. Without a channel, it searches the connected account's playlists.
- **Get playlist** returns playlist metadata, visibility, video count and existing translations.
- **Create playlist** creates a playlist. New playlists are private by default.
- **Update playlist** updates a playlist's title, description, language or visibility, preserving omitted settings and existing translations.
- **Search playlist videos** returns videos from a playlist with optional text and visibility filters, preserving the playlist order.
- **Remove video from playlist** removes a playlist entry by its **Playlist item ID**. The video itself is not deleted.

### Playlists and reports

- **Find video** returns the first public video matching the search filters. **Found** is false when nothing matches.
- **Add video to playlist** adds a video to a playlist, optionally at a specific position and with a note.
- **Get report** returns YouTube Analytics data for a channel or content owner for the selected dates, metrics, breakdown, filters and sorting.
- **API request** sends an authenticated request to an allowed YouTube Data API or YouTube Analytics API endpoint. Use it for operations that are not covered by other actions.

### Analytics

- **Get lifetime statistics** returns the current cumulative channel views, subscribers and public video count.
- **Get time-bound metrics** returns views, watch time, engagement and subscriber changes of a channel or a single video for a date range.
- **Get video statistics by ID** returns the current views, likes and comments of a video.

## Events

### Channel videos

- **On new video added** triggers when a new video appears in one or more watched channels (up to 20) and matches the selected visibility. Non-public visibility modes require an OAuth connection of the channel owner.

### Channel comments

- **On new comment on channel** triggers when a new published comment, and optionally a reply, appears under any video of the selected channel.

### Discovery

- **On new comment on video** triggers when a new comment, and optionally a reply, is added to the selected video.
- **On new video matching search** triggers when a newly published public video matches a search query.
- **On new video in playlist** triggers when a new entry that matches the selected visibility is added to a playlist.
- **On new live stream** triggers when a broadcast of the channel is created, starts streaming, or both.

## Limitations

- All requests count against the [YouTube Data API quota](https://developers.google.com/youtube/v3/determine_quota_cost) of your Google Cloud project (10,000 units per day by default). Search-based actions and events (`search.list` costs 100 units per call) and comment events on active channels consume quota quickly; increase polling intervals or request a higher quota if needed.
- The first poll of every event records existing items without starting workflows. Changing the channel, visibility or reply settings of an event starts a new baseline.
- Caption downloads and transcripts require an OAuth connection with permission to edit the video.
- Monthly **Get report** breakdowns require **Start date** and **End date** to be the first day of a month.
- Caption files are limited to 100 MB, thumbnails to 50 MB (JPEG or PNG).
- Write actions are not retried automatically. If a write fails, check the current state of the video, caption or playlist before running it again.

## Feedback

Do you want to use this app or do you have feedback on our implementation? Reach out to us using the [established channels](https://www.blackbird.io/) or create an issue.

<!-- end docs -->
