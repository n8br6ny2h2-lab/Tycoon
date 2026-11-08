# Game Center configuration

Enable Game Center for the App ID (the Xcode capability is added automatically by `IosPostBuild`).

## Leaderboards (classic, "high score", integer)
| Id | Name (EN / TR) | Score |
|---|---|---|
| factoryking.company_level | Company Level / Şirket Seviyesi | company level |
| factoryking.prestige_count | Restructures / Yeniden Yapılanmalar | prestige count |

## Achievements (100 points total suggested; all visible)
| Id | EN | TR | Points |
|---|---|---|---|
| factoryking.first_upgrade | First Upgrade | İlk Yükseltme | 5 |
| factoryking.first_factory | First Factory | İlk Fabrika | 10 |
| factoryking.ten_machines | First 10 Machines | İlk 10 Makine | 5 |
| factoryking.first_million | First Million | İlk Milyon | 10 |
| factoryking.first_prestige | Fresh Start | Taze Başlangıç | 15 |
| factoryking.five_factories | Industrial Empire | Sanayi İmparatorluğu | 20 |
| factoryking.first_billion | Billionaire | Milyarder | 10 |
| factoryking.level_100 | Perfection | Kusursuzluk | 10 |
| factoryking.orders_10 | Reliable Supplier | Güvenilir Tedarikçi | 5 |
| factoryking.first_manager | Team Builder | Ekip Kurucusu | 5 |
| factoryking.research_10 | Innovator | Yenilikçi | 5 |
| factoryking.streak_7 | Loyal Boss | Sadık Patron | 5 |
| factoryking.ten_factories | Tycoon | Sanayi Devi | 15 |
| factoryking.twenty_factories | Factory King | Fabrika Kralı | 30 |
| factoryking.first_season | Season Veteran | Sezon Gazisi | 15 |

Descriptions are the `achievement.<id>.desc` entries in `strings.txt`. Achievement artwork (512×512 or 1024×1024) is still needed.
