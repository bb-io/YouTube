# YouTube — Version 1.13.1

- Fixed **Upload video** failing during upload-session creation because of an invalid JSON media-type argument.
- Applied the fix to the shared JSON request path, covering other affected metadata and management writes while preserving UTF-8 text.
- Kept existing Bird inputs, outputs, app identity, OAuth scopes and upload retry behavior unchanged.
- Release builds pass with zero warnings/errors. All 243 retained regression checks and 27 focused production-routing checks pass; 62 live API tests compile/discover but have not run.
- Live YouTube upload and Blackbird runtime verification remain pending. Install **YouTube-Blackbird-1.13.1.zip** as a new version of the existing app, then retest the upload Bird.
