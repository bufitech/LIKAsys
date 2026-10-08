<div align="center">

# LIKAsys

**Monitor i lehtë i sistemit për gamer-a — CPU · GPU · VRAM · RAM · FPS**

Një kartelë e vogël, elegante, gjithmonë sipër, në cepin e ekranit.
Jeton në ikonën afër orës. Pa reklama, pa pagesa, falas përgjithmonë.

**Made in Kosovo with ❤️ · [Likaapps.com](https://Likaapps.com)**

</div>

---

## Çfarë bën

| | |
|---|---|
| **CPU** | përqindja e ngarkesës, temperatura, shpejtësia në GHz |
| **GPU** | përqindja e ngarkesës, temperatura (NVIDIA / AMD / Intel) |
| **VRAM** | memoria e përdorur e kartës grafike |
| **RAM** | memoria e sistemit (GB dhe %) |
| **FPS** | kuadro në sekondë, reale, nga çdo lojë DX9/DX11/DX12/Vulkan |
| **1% / 0.1% LOW** | sa bien kuadrot në momentet më të këqija |
| **FRAME** | koha e një kuadri në ms |
| **NET · PING** | shkarkimi dhe ngarkimi në Mb/s, koha e përgjigjes |

## Veçoritë

- **Vetëm në tray** — asnjë dritare në taskbar, asnjë pengesë
- **Always on top** — rri sipër lojës (modaliteti borderless/windowed)
- **9 pozicione** — lart/poshtë, majtas/djathtas, qendër… ose tërhiqe me mouse dhe kapet vetë në cepin më të afërt
- **36 tema të gatshme** — Midnight Glass, Nord, Dracula, Tokyo Night, Cyberpunk, Matrix, Kosova, Snow… një klik dhe gjithçka ndryshon
- **Blur i vërtetë (akrilik)** — sfondi pas widget-it turbullohet si në Windows 11
- **11 ngjyra të ndara** — akcenti, sfondi, korniza, teksti, etiketat, shiritat… me HEX ose me paletën e Windows-it
- **Ikona 3D / outline / solid** — stil i zgjedhshëm, madhësi e veçantë, me efekt glow
- **Shirita të rrumbullakosur / katrorë / të segmentuar** — ose fare pa shirita
- **Font i lirë** — çdo font i sistemit, madhësi 8–34 px, 6 trashësi, vlerat dhe etiketat veç e veç
- **Përmasa 0.6× – 2.5×** — e vogël sa një shirit ose e madhe për TV
- **Shfaqe vetëm në lojë** — fshihet në desktop, kthehet kur nis loja
- **Click-through** — klikimet kalojnë përtej widget-it kur ti do
- **Update me një klik** — brenda programit, me njoftim automatik
- **Nis bashkë me Windows** — pa dritare UAC (me Task Scheduler)
- **E lehtë** — ~0.3% CPU, pa shërbime, pa telemetri

## Instalimi

1. Shkarko `LIKAsys-Setup-1.4.exe` nga [Likaapps.com](https://Likaapps.com)
2. Hape → *Next* → *Install*
3. Gati. Widget-i shfaqet në cep, ikona rri afër orës.

> **Windows 10 / 11 (64-bit).** Nuk ke nevojë për .NET — gjithçka është brenda.
> Programi kërkon të drejta administratori një herë në nisje: pa to Windows-i
> nuk e lejon asnjë program të lexojë FPS-në dhe temperaturat e harduerit.

## Si bëhet update

Nuk ke pse të bësh asgjë manualisht:

1. LIKAsys kontrollon vetë për version të ri (në nisje dhe çdo 6 orë)
2. Kur del version i ri, të shfaqet njoftim te ikona afër orës
3. Kliko njoftimin → shiko çfarë ka të re → **Instalo tani**
4. Shkarkohet, instalohet dhe rihapet vetvetiu — disa sekonda

Mund ta kontrollosh edhe vetë: *klik i djathtë mbi ikonë → Kontrollo për update*.

## Si funksionon (teknikisht)

| Matja | Burimi |
|---|---|
| Ngarkesa e CPU | `GetSystemTimes` (kernel, zero varësi) |
| RAM | `GlobalMemoryStatusEx` |
| Ngarkesa e GPU, VRAM | numëruesit WDDM `\GPU Engine` dhe `\GPU Adapter Memory` |
| Temperatura, GHz | LibreHardwareMonitor (MPL-2.0) |
| FPS | ngjarjet ETW `PresentStart` (DXGI / D3D9) — e njëjta teknikë si PresentMon |
| Update | serveri i versioneve i LIKAsys |

Nëse ndonjë burim nuk është i disponueshëm (p.sh. drajveri i sensorëve),
widget-i thjesht e fsheh atë vlerë — nuk rrëzohet kurrë.

## Ndërtimi nga burimi

Punon njësoj në **Linux** dhe **Windows** (Actions e ndërton në Ubuntu):

```bash
sudo apt-get install -y nsis python3-pil      # ose: pip install pillow
./build.sh 1.4
# -> dist/LIKAsys-Setup-1.4.exe
```

Kërkohet .NET SDK 8. Struktura:

```
src/LIKAsys/      kodi i aplikacionit (WPF, C#)
installer/        skripti NSIS + grafikat e instaluesit
tools/            gjenerimi i ikonës dhe bitmap-ave
build.sh          ndërton gjithçka
scripts/release.sh  publikon një version të ri
```

## Publikim i një versioni të ri

```bash
./scripts/release.sh 1.5 "Shtuar grafiku i vogël i FPS-së"
```

Kaq. Installer-i ndërtohet vetë, versioni publikohet, dhe të gjithë
përdoruesit marrin njoftimin automatikisht.

## Licenca

MIT — shih [LICENSE](LICENSE). Përdore, modifikoje, shpërndaje lirshëm.

<div align="center">

**LIKAsys** · Made in Kosovo with ❤️ · [Likaapps.com](https://Likaapps.com)

</div>
