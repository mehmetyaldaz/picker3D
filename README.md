# Picker3D

![Picker3D ana ekranı](Docs/main-screen.png)

Picker3D, oyuncunun otomatik ilerleyen bir toplayıcıyı sağa ve sola
sürükleyerek farklı şekilleri topladığı mobil bir arcade oyunudur. Her
partın sonunda toplanan objeler Dropbox alanına bırakılır. Gerekli sayı
sağlandığında kapılar açılır ve oyuncu bir sonraki parta devam eder.

Level sonunda oyuncu final rampasına çıkar. Rampada yapılan hızlı
dokunuşlar karakterin hızını ve atlayış mesafesini artırır. Oyuncunun
ulaştığı ödül bölgesine göre gem kazanılır.

## Oynanış

1. `Tap to Play` ekranına dokunarak levelı başlat.
2. Parmağını sağa ve sola sürükleyerek objeleri topla.
3. Part sonundaki Dropbox alanına yeterli sayıda obje bırak.
4. Tüm partları tamamlayarak final rampasına ulaş.
5. Rampada hızlıca dokunarak atlayış gücünü artır.
6. Ulaşılan ödül bölgesine göre gem kazan ve sonraki levela ilerle.

## Özellikler

- Sonsuz ve birbirine bağlı level ilerlemesi
- Mobil sürükleme kontrollü sağ-sol hareket
- Rastgele seçilen Easy, Normal ve Hard zorluklar
- Zorluğa ve part sırasına göre değişen gerekli obje sayıları
- Manuel hazırlanmış A, B ve C collectible layoutları
- Layout başına 60 adede kadar spawn noktası
- Sphere, Cube, Cylinder ve Capsule collectible çeşitleri
- Her part için rastgele seçilen tek şekil ve renk
- Oyuncuya temas ettiğinde küçük parçalara ayrılan büyük objeler
- Part başına rastgele çıkabilen spinner güçlendirmesi
- Dropbox gereksinim ve ilerleme göstergesi
- Dokunma hızına bağlı final rampası ve fizik tabanlı atlayış
- Yedi farklı ödül bölgesi
- Gem kazanma, toplam gem bakiyesi ve uçan gem animasyonu
- Açılabilir ve seçilebilir skin/color materyalleri
- Store, Mission, Failed ve Level Finished ekranları
- Collect Memes, Finish Levels ve Collect Shapes görevleri
- Uygulama kapalıyken de süreyi koruyan görev yenileme sistemi
- Level, gem, görev ve kozmetik ilerlemesini kaydetme

## Level Uzunluğu İlerlemesi

Oyunun level yapısı, oyuncu ilerledikçe daha uzun levellar sunacak
şekilde tasarlanmıştır:

- Başlangıç levellarında 3 part ve final rampası
- Orta levellarda rastgele 3 veya 4 part ve final rampası
- İleri levellarda rastgele 3, 4 veya 5 part ve final rampası

Üç, dört ve beş partlık level template prefabları hazırlanmıştır. Level
numarasına göre uygun template seçiminin `LevelManager` üzerinden
yapılması planlanmaktadır. Aynı level yeniden başlatıldığında template
seçiminin değişmemesi için seçim level numarasından üretilen sabit bir
seed kullanacaktır.

## Kontroller

- **Sağ-sol hareket:** Ekrana basılı tutup sağa veya sola sürükle
- **Rampada hızlanma:** Ekrana art arda dokun
- **Menüler:** İlgili UI butonuna dokun

## Kullanılan Teknolojiler

- Unity `6000.0.79f1`
- Universal Render Pipeline `17.0.4`
- Unity Input System `1.19.0`
- Cinemachine `3.1.7`
- C#
- Hedef platformlar: Android ve iOS

## Projeyi Çalıştırma

1. Projeyi bilgisayarına indir veya klonla.
2. Unity Hub üzerinden projeyi Unity `6000.0.79f1` ile aç.
3. `Assets/Scenes/Proto.unity` sahnesini aç.
4. Unity Editor içindeki Play butonuna bas.
5. Mobil görünümü test etmek için Device Simulator kullan.

## Temel Proje Yapısı

```text
Assets/
├── Data/          Oyun ayarları, zorluklar, görevler ve level tanımları
├── Materials/     Oyun ve görev materyalleri
├── Prefab/        Level, part, collectible, UI ve çevre prefabları
├── Scenes/        Unity sahneleri
└── Scripts/
    ├── Collectibles/
    ├── Core/
    ├── Level/
    ├── Missions/
    ├── Player/
    └── UI/
```

## Geliştirme Durumu

Proje aktif geliştirme aşamasındadır. Sıradaki temel çalışma, hazır
3/4/5 partlık template prefablarını level ilerlemesine bağlamak ve
mobil cihazlarda input davranışlarını test etmektir.

