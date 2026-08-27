# unity/ — Unity Projesi (henüz oluşturulmadı)

Buraya `HezarfenGame` adıyla Unity 6 LTS (6000.x) + **HDRP** boş projesi gelecek.

**Bloke:** Bu makinede Unity ve Unity Hub kurulu değil (bkz. docs/decisions/0001-versions.md).
Kurulum `[İNSAN]` görevidir (plan Görev 2). Unity gelmeden Faz 0 (uçuş prototipi) başlayamaz.

**Kurulum sonrası ilk adımlar:**
1. Unity Hub → Unity 6 LTS (6000.x) en güncel sürüm + Windows Build Support.
2. Yeni proje: HDRP şablonu, yol `unity/HezarfenGame`.
3. Tam sürüm numarasını `ProjectVersion.txt`ten ADR 0001'e yaz — sürüm kilitlenir, ara sürüm atlanmaz.
4. Paketler: Input System (yeni), Cinemachine 3.x, Addressables, Test Framework.
5. **Öneri:** Unity projesi oluşturulmadan ÖNCE `git init` yapılsın — `Library/` klasörü on binlerce
   dosyadır, `.gitignore` zaten hazır (bkz. ADR 0003).

**Klasör sözleşmesi (plan Bölüm 3):** bize ait her şey `Assets/_Project/` altında;
`Assets/_Import/` yalnızca Blender'dan gelen ham FBX iniş alanıdır.
