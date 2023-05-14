# Changelog

All notable changes to this project will be documented in this file.<br>
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [v3.0.1](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v3.0.1) - 15 May 2023

- Added descriptions and value checkers for config
- Updated admin hammer
- Fixed empty stone slab drop
- Admin hammer, vanilla hammers and chisels can be stored in mining bag

### Lang
- Renamed Admin hammer to Moderator hammer (key: `item-adminhammer`)
- Added `itemdesc-adminhammer`

[Changes][v3.0.1]


## [v3.0.0](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v3.0.0) - 04 May 2023

- Feature: Removed create plug network particles
- Feature: Updated sounds
- Feature: Drop plugs at quarry center
- Feature: Changed particles and added sound when slab created
- Feature: Changed block quantity for different slab size
- Feature: Plugs not usable in claims without privileges, fix #5
- Feature: Improve plug placement behavior (look for a wall, follow a row)
- Feature: Removed plug collision box
- Feature: Rubble hammer, chisels, plugs, slabs and rubble storage can be stored in mining bag
- Feature: Chisels requires hammer
- Fixed: Quarry particles
- Fixed: Exception if not correct BlockEntity
- Fixed: Tool swap behavior (different tool types for chisels and rubble hammer)
- Fixed: Rubble storage world interactions

### Dev:
- Assets refactoring and cleanup (move all chisels to one item)
- Removed MoreMetals compatibility
- Removed legacy code
- More refactoring
- Added modicon, license
- Rewrited mod for ModBuilder and CommonLib

### Translations:
- Updated it lang from crowdin
- Fixed lang error
- Removed obsolete MoreMetals strings
- Added ru guide translation by Mayadzuko
- Added japanese translation by [@macoto](https://github.com/macoto)_hino

[Changes][v3.0.0]


## [v3.0.0-pre.1](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v3.0.0-pre.1) - 22 Apr 2023

WIP. Works on 1.18.0, but interaction with stone slabs is buggy.
A full release on moddb will be after fixing anegostudios/VintageStory-Issues#2606

[Changes][v3.0.0-pre.1]


## [v2.0.0-rc.4](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v2.0.0-rc.4) - 07 Oct 2022

- Fixed stacking never placed stone slabs [#12](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/12) 

[Changes][v2.0.0-rc.4]


## [v2.0.0-rc.3](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v2.0.0-rc.3) - 17 Sep 2022

- Fixed RockManager crash in some worlds

[Changes][v2.0.0-rc.3]


## [v2.0.0-rc.2](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v2.0.0-rc.2) - 11 Sep 2022

- Fixed empty stone slabs after placing

[Changes][v2.0.0-rc.2]


## [v2.0.0-rc.1](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v2.0.0-rc.1) - 11 Sep 2022

- Updated to 1.17

### 2.0.0-pre changes:
- Updated models
- Updated guides
- Fixed plug preview
- Fixed tool durability modifier in world settings does not affect mod tools
- Fixed rubble storage selection boxes
- Dynamic rock textures for RubbleStorage
- Removed ItemSlabTool class
- Rubble hammer can turn rock into gravel and gravel into sand in the world (left-click on block)
- Added a separate common system for checking rock variants (stone, sand, gravel, bricks, etc) with wildcard support
- Fixed admin hammer lang
- Added creative-only admin plugs (128x128x128)
- Fixed Stone Slabs stack merging with different content stacksize [#9](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/9)
- Added additional world interactions for stone slabs in creative
- Fixed Rubble Storage world interactions
- Fixed Rubble Storage block info
- Fixed Rubble Storage desync when adding resources
- Fixed Rubble Storage particles
- Fixed Rubble Storage buttons world interactions order 
- Added Rubble Storage correct pick block way
- Enabled latest CSharp features
- Fixed drops and particles always only on core block
- Fixed broken interaction with stone slab core block [#8](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/8) 
- Updated lang files [#10](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/10) 
- Fixed quantum entangled slabs [#7](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/7) (slabs placed from the same stack synced all changes)
- Fixed loss of items in placed stone slab after re-entering the world
- Added more accurate visualization of stone slab contents (multiple stones based on quantity)
- Slab in inventory now looks like in the world
- Removed rock variants for stone slabs (added remap for old worlds)
- [WIP] Slabs rewritten from MonolithicSmall to Modular, this should fix particle and drop issues later
- Multiblocks rewritten to vanilla way (IMultiBlockMonolithicSmall)
- Visual amount of rubble storage contents, correct collision
- Slone slabs with multiple rocks
- Completely rewritten hell in plugs. Easier, faster, more readable, fewer bugs. Now the plug will actually dig out the cube inside its maximum size. Only 3x3x3 on copper, no 5x3x3, 5x4x4 and other errors
- Changed the system for adding new stones for stone slabs and rubble storages. Stone slabs will dig not only stones, but also ore (WIP). And the stones of other mods too. I removed the hard-coded dependency on game:rock-, game:sand, etc.
- Land claim checking (reinforcement later) [#5](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/5)
- Most of the code has been rewritten

[Changes][v2.0.0-rc.1]


## [v2.0.0-pre.5](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v2.0.0-pre.5) - 30 May 2022

- Updated models
- Updated guides
- Fixed plug preview
- Fixed tool durability modifier in world settings does not affect mod tools
- Fixed rubble storage selection boxes
- Dynamic rock textures for RubbleStorage
- Removed ItemSlabTool class
- Rubble hammer can turn rock into gravel and gravel into sand in the world (left-click on block)

[Changes][v2.0.0-pre.5]


## [v2.0.0-pre.4](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v2.0.0-pre.4) - 14 May 2022

- Added a separate common system for checking rock variants (stone, sand, gravel, bricks, etc) with wildcard support
- Fixed admin hammer lang
- Added creative-only admin plugs (128x128x128)

### Stone Slabs
- Fixed stack merging with different content stacksize [#9](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/9)
- Added additional world interactions for stone slabs in creative

### Rubble Storage
- Fixed world interactions
- Fixed block info
- Fixed desync when adding resources
- Fixed particles
- Fixed buttons world interactions order 
- Added correct pick block way

### Dev
- Enabled latest CSharp features

[Changes][v2.0.0-pre.4]


## [v2.0.0-pre.3](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v2.0.0-pre.3) - 10 May 2022

- Fixed drops and particles always only on core block
- Fixed broken interaction with stone slab core block [#8](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/8) 
- Updated lang files [#10](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/10) 

[Changes][v2.0.0-pre.3]


## [v2.0.0-pre.2](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v2.0.0-pre.2) - 29 Apr 2022

- Fixed quantum entangled slabs [#7](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/7) (slabs placed from the same stack synced all changes)
- Fixed loss of items in placed stone slab after re-entering the world
- Added more accurate visualization of stone slab contents (multiple stones based on quantity)
- Slab in inventory now looks like in the world
- Removed rock variants for stone slabs (added remap for old worlds)
- [WIP] Slabs rewritten from MonolithicSmall to Modular, this should fix particle and drop issues later

[Changes][v2.0.0-pre.2]


## [v2.0.0-pre.1](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v2.0.0-pre.1) - 10 Apr 2022

- Multiblocks rewritten to vanilla way (IMultiBlockMonolithicSmall)
- Visual amount of rubble storage contents, correct collision
- Slone slabs with multiple rocks
- Completely rewritten hell in plugs. Easier, faster, more readable, fewer bugs. Now the plug will actually dig out the cube inside its maximum size. Only 3x3x3 on copper, no 5x3x3, 5x4x4 and other errors
- Changed the system for adding new stones for stone slabs and rubble storages. Stone slabs will dig not only stones, but also ore (WIP). And the stones of other mods too. I removed the hard-coded dependency on game:rock-, game:sand, etc.
- Land claim checking (reinforcement later) [#5](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/5)
- Most of the code has been rewritten, MANY bugs are expected :smile:


[Changes][v2.0.0-pre.1]


## [v1.6.5](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.6.5) - 12 Mar 2022

- Fixed ProtoBuf crash

[Changes][v1.6.5]


## [v1.6.4](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.6.4) - 11 Mar 2022

- Added config sync from the server to the client. Should fix the issue with invisible blocks on servers

[Changes][v1.6.4]


## [v1.6.3](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.6.3) - 17 Feb 2022

- Fixed desync display of stone amount in slab in multiplayer [#4](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/4)
- Fixed stone slab crash when it broken [#3](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/3)

[Changes][v1.6.3]


## [v1.6.2](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.6.2) - 14 Feb 2022

- Fixed client-only broken stone slab
- Fixed lang (changed exact values to wildcards)
- Fixed sound infinity area (changed stereo to mono)
- Fixed giant stone storage drop [#2](https://github.com/DArkHekRoMaNT/StoneQuarry/issues/2)


[Changes][v1.6.2]


## [v1.6.1](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.6.1) - 31 Jan 2022

- Fixed no remap for new tool molds
- Fixed PlugSize config value don't work
- Added config value PlugSizesMoreMetals
- Updated lang (including add MoreMetals lang)
- [DEV] Updated launchSettings for fix /expclang path (added workingDirectory)

[Changes][v1.6.1]


## [v1.6.0](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.6.0) - 26 Jan 2022

- Added [MoreMetals](https://mods.vintagestory.at/show/mod/2164) compatibility
- Added config value for break plug chance after used it
- Changed own toolmold block to vanilla block patch (fixed display of Metal molding for plugs in handbook)
- Changed tool for rubble storage (pickaxe -> axe)
- Changed rchances and *rate to NatFloat (more vanilla random for stones)
- Fixed click with flint on empty rubble storage crash
- Fixed water portion consumption for muddy gravel
- Fixed order of rubble storage button wi help
- Slightly improved sound
- Group tools in handbook
- Now not compatible with 1.15, only 1.16.0+

[Changes][v1.6.0]


## [v1.5.0](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.5.0) - 27 Dec 2021

- Added simple config (more on forum/moddb)
- Rewritten interaction with slabs, now it takes time and uses not only Start, but also Step and Stop interaction methods. The default speed is 0.2 seconds, which is almost identical to the old version. The sound and animation of the instruments has also been changed.
- Fixed the size of various things on the ground (they were too small)
- Refactoring (will require updating in old worlds)
- Updated language files
- Changed IDE from VSCode to Visual Studio

[Changes][v1.5.0]


## [v1.4.2](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.4.2) - 13 Dec 2021

- Allow adding slabs to mining bag
- Slabs have a maximum stack size depending on their size (12 for small, 1 for giant)
- Rubble storage has a maximum stack size of 1
- Fixed a typo in the name of the tin bronze chisel
- Updated to 1.16.0-pre.8 (still works on 1.15.10 and below)

[Changes][v1.4.2]


## [v1.4.1](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.4.1) - 28 Nov 2021

- Fixed plugandfeather tooltip
- Fixed plug phantom drop
- Changed slabs and plugs size

[Changes][v1.4.1]


## [v1.4.0](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.4.0) - 28 Nov 2021

- Fixed RubbleStorage without buttons (refactoring)
- Added remap for plugandfeather tool mold
- Fixed plugandfeather tool mold output drop
- Fixed slabs don't place in replaceable blocks like tail grass
- Fixed plugandfeather tool mold transform on mold rack
- Improve sand and gravel distribution mechanics for unlocked state
- Working with Rubble Storage and hammering the plug takes durability, but the durability of the mod tools is slightly increased (like pickaxes now)
- Increased the size of plugs, now they fall into the block where they were

[Changes][v1.4.0]


## [v1.3.0](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.3.0) - 24 Nov 2021

- Fixed only rock drops from slabs
- Fixed crash of incorrect interaction with RubbleStorage
- Fixed slab content setter debug tool
- Slabs no longer react to the wrong tool
- Slabs with a non-existent variant use the error reporter instead of being displayed in the chat
- Refactoring:
  - Changed plugnfeather name to plugandfeather (remapping for old worlds)
  - Vanilla style class naming (keep block entity registration for old worlds)
  - Combined the Caps and Core classes into one

[Changes][v1.3.0]


## [v1.2.0](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.2.0) - 22 Nov 2021

- Added tooltip when hovering over plug and slab in the world
- Reworked Rubble Storage:
  - Improved tooltip in the world and on hover in GUI
  - Muddy gravel production now consumes water from the bucket
  - Added hints on hover
  - Capacity increased from 500 to 512
  - Refactoring

[Changes][v1.2.0]


## [v1.1.1](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.1.1) - 21 Nov 2021

- Added tooltip info for: plug max size, amount of stone in the slab and result for this tool

[Changes][v1.1.1]


## [v1.1.0](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.1.0) - 20 Nov 2021

- Added check for unavailable variants (polished obsidian for example) and notification in chat about it
- Fixed unbreakable plug 
- Changed mold recipe to pit kiln
- Added tool heads
- Added meteoric iron and steel plugs
- Assets refactoring
- Updated lang

[Changes][v1.1.0]


## [v1.0.1](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.0.1) - 17 Nov 2021

- Probably fixed other players crash in multiplayer (remove static from particleProps)

[Changes][v1.0.1]


## [v1.0.0](https://github.com/DArkHekRoMaNT/StoneQuarry/releases/tag/v1.0.0) - 17 Nov 2021

- Changed name and modid

Changes from Quarry Works! for 1.14:
- Fixed plug drop pos
- Fixed all stack placement
- Fixed crash with interact (check ItemAttributes)
- Fixed pallet placement crash
- Fixed PlugnFeatherBlock.OnBlockBroken crash
- Fixed PlugnFeatherBlock.CheckDone crash
- Fixed rubble hammer head recipe
- Refactoring
- Added ru lang

[Changes][v1.0.0]


[v3.0.1]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v3.0.0...v3.0.1
[v3.0.0]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v2.0.0-rc.4...v3.0.0
[v3.0.0-pre.1]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v2.0.0-rc.4...v3.0.0-pre.1
[v2.0.0-rc.4]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v2.0.0-rc.3...v2.0.0-rc.4
[v2.0.0-rc.3]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v2.0.0-rc.2...v2.0.0-rc.3
[v2.0.0-rc.2]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v2.0.0-rc.1...v2.0.0-rc.2
[v2.0.0-rc.1]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v2.0.0-pre.5...v2.0.0-rc.1
[v2.0.0-pre.5]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v2.0.0-pre.4...v2.0.0-pre.5
[v2.0.0-pre.4]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v2.0.0-pre.3...v2.0.0-pre.4
[v2.0.0-pre.3]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v2.0.0-pre.2...v2.0.0-pre.3
[v2.0.0-pre.2]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v2.0.0-pre.1...v2.0.0-pre.2
[v2.0.0-pre.1]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.6.5...v2.0.0-pre.1
[v1.6.5]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.6.4...v1.6.5
[v1.6.4]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.6.3...v1.6.4
[v1.6.3]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.6.2...v1.6.3
[v1.6.2]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.6.1...v1.6.2
[v1.6.1]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.6.0...v1.6.1
[v1.6.0]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.5.0...v1.6.0
[v1.5.0]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.4.2...v1.5.0
[v1.4.2]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.4.1...v1.4.2
[v1.4.1]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.4.0...v1.4.1
[v1.4.0]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.3.0...v1.4.0
[v1.3.0]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.2.0...v1.3.0
[v1.2.0]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.1.1...v1.2.0
[v1.1.1]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.1.0...v1.1.1
[v1.1.0]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.0.1...v1.1.0
[v1.0.1]: https://github.com/DArkHekRoMaNT/StoneQuarry/compare/v1.0.0...v1.0.1
[v1.0.0]: https://github.com/DArkHekRoMaNT/StoneQuarry/tree/v1.0.0
