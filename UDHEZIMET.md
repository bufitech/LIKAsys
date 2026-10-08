# LIKAsys — udhëzime për ty

Ky skedar është vetëm për ty, jo për përdoruesit.

---

## 1. Si nxjerrim një version të ri

Thuaj thjesht **"Push për update të ri"** dhe shkruaj çfarë do të shtohet.

Unë bëj gjithçka:

1. e shkruaj kodin e veçorisë,
2. e ndërtoj dhe e provoj,
3. ngre numrin e versionit dhe shkruaj shënimet e ndryshimeve,
4. e publikoj.

Pastaj, vetvetiu:

- installer-i `LIKAsys-Setup-x.y.exe` ndërtohet,
- versioni i ri publikohet në serverin e përditësimeve,
- çdo përdorues që e ka LIKAsys të hapur merr njoftimin brenda 6 orësh,
- ai kliko **Instalo tani** dhe mbaroi puna.

Ti nuk ke nevojë të prekësh asgjë.

---

## 2. Ndërtimi me dorë (nëse do vetë)

```bash
sudo apt-get install -y nsis python3-pil
./build.sh 1.4
# -> dist/LIKAsys-Setup-1.4.exe
```

Kërkohet .NET SDK 8.

---

## 3. Si e provon një version para se ta nxjerrim

Instalues-i më i fundit gjendet gjithmonë te dosja `instalimi/`.
Hape atë në një PC me Windows 10 ose 11, kliko *Next* → *Install*.

Nëse diçka nuk shkon, dërgoma skedarin e regjistrit:
klik i djathtë mbi ikonën afër orës → **Hap regjistrin**.
Aty shkruhet gjithçka që ka ndodhur dhe e gjej problemin shpejt.

---

## 4. Çfarë duhet të dinë përdoruesit

- Kërkon **Windows 10 ose 11, 64-bit**.
- Nuk kërkon .NET të instaluar — gjithçka është brenda.
- Në nisje kërkon të drejta administratori **një herë**: pa to Windows-i nuk e
  lejon asnjë program të lexojë FPS-në dhe temperaturat.
- Rri vetëm te ikona afër orës, kurrë në shiritin e detyrave.
- Është **falas përgjithmonë**, pa reklama, pa pagesa.

---

**LIKAsys** · Made in Kosovo with ❤️ · [Likaapps.com](https://Likaapps.com)
