# TRADER Typography & Font Registry

This document catalogs all 21 custom typography packages integrated into the `TRADER` platform under `assets/fonts/`.

---

## Font Catalog Summary

| # | Font Family | Formats | Primary Files | License Type | Distribution Status | Source Link |
|---|-------------|---------|---------------|--------------|---------------------|-------------|
| 1 | **Aiden** | OTF | `Aiden-v7DO.otf` | Freeware, Non-Commercial | Non-Commercial / Personal | [FontSpace](https://www.fontspace.com/aiden-font-f25745) |
| 2 | **Ariana Violeta** | TTF | `ArianaVioleta-dz2K.ttf` | Freeware | Freeware | [FontSpace](https://www.fontspace.com/ariana-violeta-font-f34433) |
| 3 | **Baby Plums** | TTF | `BabyPlums-rv2gL.ttf` | Freeware, Non-Commercial | Personal / Demo | [FontSpace](https://www.fontspace.com/baby-plums-font-f109480) |
| 4 | **Becky Tahlia** | TTF | `BeckyTahlia-MP6r.ttf` | Freeware | Freeware | [FontSpace](https://www.fontspace.com/becky-tahlia-font-f34469) |
| 5 | **Believe It** | TTF | `BelieveIt-DvLE.ttf` | Freeware | Freeware | [FontSpace](https://www.fontspace.com/believe-it-font-f34515) |
| 6 | **Branda** | TTF | `Branda-yolq.ttf` | Freeware, Non-Commercial | Non-Commercial / Personal | [FontSpace](https://www.fontspace.com/branda-font-f30036) |
| 7 | **Brownie Stencil** | TTF | `BrownieStencil-8O8MJ.ttf` | Freeware, Non-Commercial | Personal / Demo | [FontSpace](https://www.fontspace.com/brownie-stencil-font-f107985) |
| 8 | **Chrusty Rock** | TTF | `ChrustyRock-ORLA.ttf` | Demo | Demo | [FontSpace](https://www.fontspace.com/chrusty-rock-font-f35152) |
| 9 | **Conquest** | TTF | `Conquest-8MxyM.ttf` | Demo | Demo | [FontSpace](https://www.fontspace.com/conquest-font-f61136) |
| 10 | **Cookie Crisp** | TTF | `CookieCrisp-L36ly.ttf` | Freeware, Non-Commercial | Personal / Demo | [FontSpace](https://www.fontspace.com/cookie-crisp-font-f98747) |
| 11 | **Debrosee** | TTF | `Debrosee-ALPnL.ttf` | Demo | Demo | [FontSpace](https://www.fontspace.com/debrosee-font-f40727) |
| 12 | **Freedom** | OTF, TTF | `Freedom-nZ4J.otf`, `Freedom-10eM.ttf` | CC BY-SA (Creative Commons) | Full Open Redistribution | [FontSpace](https://www.fontspace.com/freedom-font-f14832) |
| 13 | **Glorious Free** | TTF | `GloriousFree-dBR6.ttf` | Demo | Demo | [FontSpace](https://www.fontspace.com/glorious-free-font-f30878) |
| 14 | **Happy Swirly** | TTF | `HappySwirly-KVB7l.ttf` | Freeware, Non-Commercial | Personal / Demo | [FontSpace](https://www.fontspace.com/happy-swirly-font-f110063) |
| 15 | **Inflate PTX** | TTF | `InflateptxRegular-Wyg8V.ttf`, `InflateptxBase-ax3da.ttf` | Demo | Demo | [FontSpace](https://www.fontspace.com/inflate-ptx-font-f101671) |
| 16 | **Love Days** | TTF | `LoveDays-2v7Oe.ttf` | Freeware, Non-Commercial | Personal / Demo | [FontSpace](https://www.fontspace.com/love-days-love-font-f110142) |
| 17 | **Playful Time** | TTF | `PlayfulTime-BLBB8.ttf` | Freeware, Non-Commercial | Personal / Demo | [FontSpace](https://www.fontspace.com/playful-time-star-font-f109394) |
| 18 | **Shiny Crystal** | TTF | `ShinyCrystal-Yq3z4.ttf` | Freeware, Non-Commercial | Personal / Demo | [FontSpace](https://www.fontspace.com/shiny-crystal-font-f109434) |
| 19 | **Short Baby** | TTF | `ShortBaby-Mg2w.ttf` | Freeware | Freeware | [FontSpace](https://www.fontspace.com/short-baby-font-f34907) |
| 20 | **To The Point** | TTF | `ToThePointRegular-n9y4.ttf` | SIL Open Font License (OFL) | Full Open Redistribution / Embedding | [FontSpace](https://www.fontspace.com/to-the-point-font-f25644) |
| 21 | **Winter Song** | TTF | `WinterSong-owRGB.ttf` | Demo | Demo | [FontSpace](https://www.fontspace.com/winter-song-font-f59649) |

---

## Usage in App UI & MAUI Registration

- **Default Clean Interface:** Standard body text, data grids, tick charts, and trading tables use system sans-serif fonts (`OpenSans-Regular`, `OpenSans-Semibold`, `OpenSans-Bold`) located in `src/TraderApp/Resources/Fonts/`.
- **Open-Licensed Display Fonts:** Fonts with unrestricted open licensing (**Freedom** and **To The Point**) are configured for app branding, hero banners, and promotional trading cards.
- **Custom / Display Typography:** Additional artistic font families located in `assets/fonts/` can be registered inside `MauiProgram.cs` via `.ConfigureFonts()` for specialized dashboards, custom badge stylings, or themed chart overlays in private/evaluation deployments.
