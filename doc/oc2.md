# Overcooked! 2

This file contains random tidbits of information regarding the reverse engineering of Overcooked! 2 for Steam

## Steam (Stable Branch)

**Unity Version:** v2017.4.8.5945332

**CLR Runtime Version:** 2.something

**Architecture:** x86

**Known Good BeepInEx:** BepInEx_x86_5.4.21.0

**.NET Framework:** net35

**CodeVersion:** 17 *(Used when connecting to clients)*

### 2023-05-30 Update

This update broke the ability to use Unity Explorer which helps a lot for development. To downgrade Steam Windows to the previous release, use this command:

```
download_depot 728880 728881 4304367197445674355
```

following [this guide](https://www.reddit.com/r/Steam/comments/611h5e/guide_how_to_download_older_versions_of_a_game_on/).

## Epic Games

**Unity Version:** v2018.4.32.16491613

**CLR Runtime Version:** 4.0.30319.42000

**Architecture:** x64

**Known Good BeepInEx:** BepInEx 6.0.0-pre.1

**.NET Framework:** net46

**CodeVersion:** 17 *(Used when connecting to clients)*
