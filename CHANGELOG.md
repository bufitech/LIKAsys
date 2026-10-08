## 2.0

**Tani lëviz.** Kartela më parë kërcente nga një numër te tjetri. Tani:

- **Shiritat rrëshqasin.** Matja e re nuk kërcen, shiriti shkon butë deri te vlera e re.
- **Rreshtat vijnë me radhë.** Kur kartela shfaqet, ose kthehet nga e minimizuara, rreshtat shfaqen njëri pas tjetrit.
- **Kartela ngrihet.** Çoje kursorin mbi të dhe ngrihet pak. Largohu dhe ulet prapë.
- **Pika e markës merr frymë.** Ngadalë te Gaming, edhe më ngadalë te IT.

Dy profilet nuk lëvizin njësoj. Gaming rrëshqet shpejt e me kërcim. IT rrëshqet ngadalë e me qetësi.

Çdo gjë është vetëm zhvendosje ose zbehje, prandaj dritarja nuk rimatet asnjëherë. Kur asgjë nuk lëviz, kohëmatësi ndalet vetë dhe widget-i kthehet në zero punë.

**Fike kur të duash.** Cilësimet > Pamja > **Animacione**. E fik dhe çdo ndryshim bëhet i menjëhershëm prapë.

**Dy profilet u ndanë më shumë.**

- **Gaming** tani ka fontin e vet, **Bahnschrift**, të ngushtë dhe këndor. Numrat u bënë edhe më të mëdhenj se etiketat. Kartela hyn nga lart.
- **IT** tani ka familjen e vet të plotë të ikonave. Disku, I/O, uptime, FPS, 1% low dhe frame kishin mbetur me ikonat e Gaming-ut. U vizatuan të gjitha nga e para, vetëm me katrorë dhe vija, si një diagram racku.

**Më shpejt se më parë.** Ngjyrat tani ruhen një herë e përdoren prapë. Më parë çdo sekondë bëheshin ngjyra të reja për secilin rresh, dhe teksti me shiritat rivizatoheshin edhe kur asgjë nuk kishte ndryshuar. Tani nuk rivizatohet asgjë nëse ngjyra është e njëjta.

**Kontroll i ri para çdo ndërtimi.** `check-ui.py` siguron që çdo lidhje e pamjes ekziston vërtet dhe që rrjeta e sigurisë e animacionit është ende aty. Nëse një animacion dështon, rreshtat kthehen në ekran brenda një sekonde e gjysmë.

## 1.9

**Temat u ndanë në dy: Lojëra dhe IT.** Te Cilësimet > Temat, dy grupet e para tani janë **Lojëra** dhe **IT**. Njëri për kur luan, tjetri për kur punon.

**Tetë tema të reja IT.** Si te lojërat, nuk janë ngjyra të tjera mbi të njëjtën kartelë. Secila ndërron shkronjat, ikonat, shiritat dhe qoshet:

- **Server Room** - LED-at e rackut mbi çelik të ftohtë. Consolas, shirita të segmentuar, qoshe 2px.
- **Blueprint** - vizatim teknik mbi blu të thellë. Vija të holla, ikona me vijë floku, qoshe të prera.
- **Ledger** - fletë e bardhë zyre me jeshile tabele. Calibri, shirita katrorë.
- **Helpdesk** - e lehtë dhe e qetë. Trebuchet MS, ikona 3D, qoshe të buta 14px.
- **Memo** - letër e shtypur, bojë e zezë, një vijë e kuqe. Georgia me serif.
- **E-Ink** - vetëm bardhë e zi, si lexuesi i librave. Verdana, pa asnjë efekt.
- **Phosphor** - ekran i vjetër qelibar që ende ndriçon. Lucida Console, shkëlqim dhe hije teksti.
- **Night Shift** - vjollcë e butë për punë natën vonë. Candara, shirita të rrumbullakët.

Katër prej tyre janë të ndritshme, për tavolinë me dritë. Katër të errëta.

- **Ndërrim i shpejtë nga ikona afër orës:** klik i djathtë > **Tema e lojës** ose **Tema e IT-së** > zgjidh. Pa hapur cilësimet.
- Gjithsej **61 tema** në 10 grupe.
- Rregullim në faqen Likaapps: ngjyrat e aplikacionit shkruhen `#AARRGGBB`, kurse shfletuesi i lexon `#RRGGBBAA`, prandaj disa korniza dhe shirita dilnin me ngjyrë të gabuar në galeri.

## 1.8

**Tetë tema të reja, të gjitha për lojëra.** Grupi i ri **Lojëra** te Cilësimet > Temat. Nuk janë vetëm ngjyra të tjera: secila ndërron edhe shkronjat, ikonat, shiritat dhe qoshet.

- **Night City** - verdhë neoni mbi të zezë, qoshe të prera, ikona të mbushura, shirita të segmentuar.
- **Corpo** - e kuqe korporate, pa shkëlqim fare, shkronja të ngushta, shirita katrorë.
- **Los Santos** - jeshile dhe perëndim dielli, ikona 3D, qoshe të buta 9px.
- **Vice** - rozë synthwave mbi vjollcë, shkëlqim i butë, qoshe 12px.
- **Dust** - rërë dhe blu, rreshta të ngjeshur, ikona me vijë floku. Për ata që duan vetëm numrat.
- **Raid** - ushtarake, shkronja makine shkrimi, pa asnjë efekt.
- **Overworld** - blloqe dhe ngjyra pikseli, shirita si kuba.
- **Ashen** - ar i vjetër dhe shkronja me serif, pa shkronja të mëdha. E qetë, por jo e zbehtë.

- **Ndërrim i shpejtë nga ikona afër orës:** klik i djathtë > **Tema e lojës** > zgjidh. Pa hapur cilësimet, pa dalë nga loja.
- Kartela e temës tani tregon një rresht se çka bën ajo temë, jo vetëm emrin.
- Mbrojtje e re në ndërtim: `scripts/check-themes.py` kontrollon çdo ngjyrë, çdo emër dhe çdo grup para se të dalë versioni. Një gabim shkronje te një temë nuk arrin më te ti.

## 1.7

- **RREGULLIM KRITIK: aplikacioni nuk hapej fare.** Tabela e përkthimeve kishte dy çelësa të përsëritur; kjo hidhte një gabim brenda konstruktorit statik dhe LIKAsys vdiste para se të vizatonte asgjë - pa dritare, pa mesazh. Tani përsëritjet janë të padëmshme, `build.sh` e ndal ndërtimin nëse shfaqet ndonjëra, dhe çdo gabim fatal tregohet me mesazh në vend që të zhduket në heshtje.
- **Profili "IT Apple Style" u hoq si profil më vete.** Pamja Apple tani është **temë brenda IT-së**: Cilësimet > Temat > Dritë > **Apple Clean**. E bardhë e ngrirë, pa korniza, pa shkëlqim, qoshe 18px, ikona me vijë floku, etiketa pa shkronja të mëdha.
- Profilet janë prapë vetëm dy: **Gaming** dhe **IT**.
- Tema e re **Terminal** (Dev) - pamja me të cilën vjen profili IT, tani edhe në listën e temave.
- Stil i ri ikonash **"Vijë floku (e qetë)"** - përdoret vetë nga Apple Clean, por mund ta zgjedhësh kudo.
- "Shkronja të mëdha" e fikur tani vërtet ndryshon diçka: `Disk`, `Network`, `Uptime` në vend të `DISK`, `NET`, `UPTIME`.
- Animacioni i hyrjes mbeti, me një mbrojtje shtesë që widget-i të mos mbetet kurrë i padukshëm.

## 1.6

- **Profil i ri: IT - Apple Style.** Jashtëzakonisht i pastër - qelq i bardhë i ngrirë, pa korniza, pa shkëlqim, qoshe të buta 18px, ikona me vijë floku dhe etiketa pa shkronja të mëdha.
- **Çdo profil ndryshon gjithçka.** Jo vetëm cilat rreshta shfaqen: tani ndryshon edhe shkronjat (Segoe UI / monospace / Segoe UI Variable), madhësitë, trashësia, ikonat, shiritat, qoshet, hapësirat, hija dhe ngjyrat.
- **Lëvizje kur shfaqet.** Widget-i vjen me "drop" nga lart, rrëshqitje nga anash, ngritje nga poshtë ose shfaqje të butë. Zgjidhet te Cilësimet > Sistemi; secili profil ka lëvizjen e vet si parazgjedhje.
- **Ndërrim i shpejtë i profilit nga ikona afër orës.** Klik i djathtë > Profili > Gaming / IT / IT Apple.
- Instaluesi tani pyet për të tre profilet dhe flet shqip ose anglisht - e zgjedh vetë në fillim.
- Rregullim: një ndryshim i pamjes nuk e hap më vetë kartelën që e kishe lënë të minimizuar.

# LIKAsys - historiku i versioneve

## v1.5 - 2026-10-08

**Dy profile, matje te reja per IT, dhe pamja e minimizuar.**

- **Profili Gaming dhe profili IT.** Instaluesi pyet nje here se per cfare e
  perdor kompjuterin, dhe LIKAsys vjen i gatshem. Ndryshohet kurdo te
  Cilesimet -> Metrikat -> Profili.
  - **Gaming**: FPS, 1% low, VRAM, temperatura. Numra te medhenj, ikona 3D,
    theks cyan.
  - **IT**: disku, lexim/shkrim, rrjeti, ping, uptime. Rreshta te ngjeshur,
    nje familje e tere ikonash teknike, theks i gjelber.
- **DISK** - sa eshte mbushur disku i sistemit, me GB te lire si detaj.
- **I/O** - lexim dhe shkrim ne MB/s, me shkalle qe ulet ngadale.
- **UPTIME** - sa kohe eshte ndezur kompjuteri.
- **Pamja e minimizuar.** Nje buton i ri ne kokë e mbledh karteln ne nje shirit
  te vetem me numrat kryesore - mbetet siper, por nuk ze vend. Kthehet me nje
  klikim, ose nga menuja e ikones prane ores.
- **Si te hapet** - zgjidh nese widget-i hapet si e le heren e fundit,
  gjithmone i plote, ose gjithmone i minimizuar.
- **Anglishtja u plotesua.** Cdo njoftim, mesazh gabimi, dritare diagnostike
  dhe tekst i dritares se update-it ishin mbetur shqip edhe kur gjuha ishte
  English. Tani jane te gjitha te perkthyera, dhe nje kontroll automatik nuk e
  lejon me asnje tekst te dale pa perkthim.

## v1.4 - 2026-10-08

Versioni i pare publik i LIKAsys.

**Cfare eshte**

Nje monitor i vogel sistemi qe rri ne qoshe te ekranit, gjithmone siper dritareve
te tjera, dhe te tregon ne kohe reale se cfare po ben kompjuteri yt.

**Matjet**

- **CPU** - ngarkesa, temperatura dhe shpejtesia ne GHz
- **GPU** - ngarkesa, temperatura dhe emri i kartes
- **VRAM** - memoria e kartes grafike
- **RAM** - memoria e sistemit
- **FPS** - kuadro ne sekonde per cdo loje, me emrin e lojes
- **1% LOW dhe 0.1% LOW** - sa bien kuadrot ne momentet me te keqija
- **FRAME** - koha e nje kuadri ne ms
- **NET** - shkarkimi dhe ngarkimi ne Mb/s
- **PING** - koha e pergjigjes se rrjetit

**Pamja**

- Dizajn Dark Glass me theks neon, qoshe te rrumbullakosura
- Pozicionim i shpejte: qoshet, anet, ose kudo me zvarritje
- Madhesia e shkronjave, fonti, stili i ikonave, trashesia e shiritave
- Tema e ngjyrave dhe theksi qe e zgjedh vete
- Menyra kompakte, vertikale ose horizontale
- Hapesira rezervohet qe numrat te mos e dridhin pamjen

**Ikona prane ores**

- Rri vetem ne tray, jo ne shiritin e detyrave
- Mund te vizatoje numrin e vertete: CPU, temperature, GPU, RAM ose FPS
- Ngjyra nderrohet vete kur ngarkesa kalon 75% dhe 90%

**Te tjera**

- Gjuha shqip dhe anglisht, nderrohet menjehere
- Nisje automatike me Windows
- Perditesim me nje klikim brenda programit, me njoftim kur del versioni i ri
- Falas pergjithmone, pa reklama, pa pagesa
