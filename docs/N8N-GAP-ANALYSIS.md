# YouTube 1.6.0 scope against n8n

Comparison baseline: n8n's documented native YouTube operations and the Blackbird 1.5.0 app. This release addresses publication updates, owned inventory and practical playlist management, rather than claiming complete parity.

| Prior gap | Version 1.6.0 |
| --- | --- |
| General source-video metadata editing | Update video |
| Publication of an already uploaded video | Set video publication settings |
| On-demand owned private/unlisted inventory | Search channel videos |
| Playlist creation and metadata management | Create playlist, Get playlist, Search playlists, Update playlist |
| Playlist entry inventory and removal | Search playlist videos, Remove video from playlist |
| Standalone retrieval of one playlist entry | Entries are available through Search playlist videos; a separate action is deferred |
| Channel retrieval/search/editing/localization | Deferred |
| Banners | Deferred |
| Video/playlist deletion | Deferred |
| Video ratings | Deferred |
| Standalone category enumeration | Existing picker retained; standalone action deferred |
| Separate playlist localization actions | Deferred; existing translations are retrieved and preserved |
| Structured Blacklake metadata round trip | Deferred |

Existing caption files, video localizations, missing-language checks, thumbnail updates, analytics and five polling events remain in the app. Their presence here does not imply n8n cannot implement similar workflows through generic HTTP nodes or other integrations.

## Documentation sources

- [n8n YouTube operations](https://docs.n8n.io/integrations/builtin/app-nodes/n8n-nodes-base.youtube/)
- [Video updates](https://developers.google.com/youtube/v3/docs/videos/update)
- [Video resource and scheduling](https://developers.google.com/youtube/v3/docs/videos)
- [Playlist creation](https://developers.google.com/youtube/v3/docs/playlists/insert)
- [Playlist retrieval](https://developers.google.com/youtube/v3/docs/playlists/list)
- [Playlist updates](https://developers.google.com/youtube/v3/docs/playlists/update)
- [Playlist entry retrieval](https://developers.google.com/youtube/v3/docs/playlistItems/list)
- [Playlist entry removal](https://developers.google.com/youtube/v3/docs/playlistItems/delete)
- [Channel retrieval](https://developers.google.com/youtube/v3/docs/channels/list)
