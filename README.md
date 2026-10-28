# CrossApp

## Предметна область

Бібліотека.

## Основні сутності

- Book — видання.
- BookCopy — конкретний примірник.
- Reader — читач.
- Loan — видача.

## Призначення застосунку

Система призначена для обліку видач примірників книг читачам і повернень.

## Структура проєкту

```text
CrossApp/
├── data/
│   ├── sample.csv
│   └── sample.json
├── src/
│   ├── Cli/
│   │   ├── Cli.csproj
│   │   ├── ConsoleSink.cs
│   │   ├── FileSink.cs
│   │   └── Program.cs
│   │
│   └── Core/
│       ├── Core.csproj
│       ├── Abstractions/
│       ├── Domain/
│       ├── Dto/
│       ├── Events/
│       ├── Import/
│       ├── Services/
│       │   └── LendingService.cs
│       ├── Storage/
│       ├── EnvironmentInfo.cs
│       └── SampleData.cs
│
├── CrossApp.slnx
├── README.md
└── .gitignore
```

- **`src/Core`** (`classlib`) — бібліотека класів із доменною логікою (сутності, інваріанти, сховища, сервіс, події, імпорт) та кодом для отримання інформації про середовище. Підтримує multi-targeting (`net8.0;net10.0`). Не містить `Console` і запису в лог-файли.

- **`src/Cli`** (`console app`) — консольний застосунок (`net10.0`), який залежить від `Core` через `ProjectReference` та відповідає за форматування, відображення даних і обробку подій (консоль, файли логів).

## Запуск

Збірка проєкту:

```powershell
dotnet build
```

Запуск застосунку:

```powershell
dotnet run --project src/Cli
```

Запуск у режимі JSON:

```powershell
dotnet run --project src/Cli -- --json
```

## Публікація

Виконано self-contained публікацію застосунку для двох RID:

- win-x64 — 80 381 152 байти (~80,38 MB)
- linux-x64 — 82 616 665 байтів (~82,62 MB)

Linux-версія більша на 2 235 513 байтів (~2,24 MB).

Також виконано framework-dependent публікацію для win-x64:

- win-x64 — 203 638 байтів (~0,204 MB).

## Порівняння публікацій

| RID | Режим | Розмір | Чи потрібен встановлений runtime |
|---|---|---:|---|
| win-x64 | self-contained | 80 381 152 байти (~80,38 MB) | Ні |
| linux-x64 | self-contained | 82 616 665 байтів (~82,62 MB) | Ні |
| win-x64 | framework-dependent | 203 638 байтів (~0,204 MB) | Так |

Self-contained публікація містить .NET runtime, тому має значно більший розмір. Framework-dependent публікація не містить runtime і потребує встановленої сумісної версії .NET.

## Multi-targeting

Проєкт `Core` підтримує дві цільові версії .NET: `net8.0` та `net10.0`.

У `Core.csproj` використано:

```xml
<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
```

Версія .NET SDK: 10.0.400.

## Залежності між проєктами

`Cli` використовує `Core` через `ProjectReference`.

Напрямок залежності:

```text
Cli → Core
```

`Core` не залежить від `Cli`.

## Запуск опублікованого застосунку

Self-contained версію для Windows було запущено безпосередньо з папки `publish`.

Основний виконуваний файл — `Cli.exe`.

## Доменна модель та інваріанти

### Перелік доменних інваріантів

1. **Обов'язковість текстових ключів:**
   * `Id`, `Isbn` та `Title` для `BookCopy` не можуть бути порожніми або містити лише пробіли (`ArgumentException`).
   * `Id`, `BookCopyId` та `ReaderId` для `Loan` обов'язкові (`ArgumentException`).
2. **Валідація часових меж:**
   * Дата видачі `IssuedOn` та дата повернення `ReturnedOn` не можуть бути в майбутньому (`ArgumentOutOfRangeException`).
   * Дата повернення `ReturnedOn` не може бути раніше дати видачі `IssuedOn` (`ArgumentOutOfRangeException`).
3. **Захист від повторної видачі:**
   * Неможливо видати примірник, який уже має статус виданого (`InvalidOperationException`).
4. **Контроль повернення:**
   * Неможливо повернути примірник, який знаходиться в бібліотеці і не видавався (`InvalidOperationException`).
   * Неможливо закрити видачу, яка вже закрита (`InvalidOperationException`).

## Сховища, події та сповіщення

### Сховища даних (`ILibraryStore`)

* **`InMemoryLibraryStore`** — зберігання даних у пам'яті (`ConcurrentDictionary`).
* **`FileLibraryStore`** — зчитування та збереження стану у JSON-файл на диску.
* **`CachingLibraryStore`** — декоратор для кешування результатів зчитування.
* **`StoreFactory`** — фабрика для зручного створення сховищ.

### Доменні події (`LendingService`)

Сервіс `LendingService` піднімає подію `event EventHandler<LibraryChangedEventArgs>? Changed` строго **після** успішної модифікації стану у сховищі. Подія піднімається в одному приватному методі `OnChanged` через `Changed?.Invoke(this, args)`.

Якщо інваріант сутності або сховище кинули виняток (дублікат id, повторна видача, неіснуючий id), подія не генерується.

Core лише повідомляє, що сталося. Спосіб обробки визначає Cli, тому в `src/Core` немає `Console` і запису у файли логів.

| Операція сервісу | Вид події |
|---|---|
| `AddBook` | `Added` |
| `ChangeBookStatus(..., Issued)` | `Issued` (`DueDate` = +14 днів) |
| `ChangeBookStatus(..., Available)` | `Returned` |

Аргументи події `LibraryChangedEventArgs` містять:

* `BookId` — ідентифікатор книги.
* `Kind` — тип зміни (`Added`, `Issued`, `Returned`).
* `ReaderId` — ідентифікатор читача.
* `DueDate` — граничний термін повернення.
* `At` — точний timestamp події.

### Підписники та механізми логування

1. **Обробники подій у CLI (`Program.cs`):**
   * Локальний метод `OnCatalogChanged` виводить рядок у консоль і дописує його у `logs/app.log`.
   * Лямбда-вираз із замиканням над `changes` рахує кількість подій.
   * Відписка через `-=`: після неї сервіс виконує операції «тихо» (`app.log` не змінюється), а лічильник-лямбда продовжує працювати.
2. **Канали сповіщень (`INotificationSink`), додаткове завдання:**
   * `ConsoleSink` та `FileSink` (реалізації в `Cli`) передаються в конструктор `LendingService` (ручний DI).
   * `FileSink` пише у `logs/sink.log`.
   * Збій одного sink не ламає операцію і не заважає іншим sinks (`try/catch` у `OnChanged`).
3. **Streaming-логування імпорту (`Action<string>`):**
   * Параметр `Action<string>? onSkipped` у `BookCsvImporter.Load` для миттєвого сповіщення про некоректні/пропущені рядки CSV.
4. **Фільтрація:**
   * Метод `Search(Func<BookCopy, bool> predicate)` для пошуку книг за довільними предикатами. Тут потрібне значення у відповідь, тому `Func`, а не подія.

### Подія проти sink

| | Подія (`Changed`) | Sink (`INotificationSink`) |
|---|---|---|
| Підключення | `+=` у Cli | через конструктор сервісу |
| Відписка | `-=` | неможлива, живе разом із сервісом |
| Сервіс знає про одержувачів | ні | так (тримає список) |
| Файл логу | `logs/app.log` | `logs/sink.log` |

### Логи

Каталог `logs` створюється кодом (`Directory.CreateDirectory`), шляхи будуються через `Path.Combine(AppContext.BaseDirectory, "logs", ...)`. Файли лежать поруч зі збіркою: `src/Cli/bin/Debug/net*/logs/`.

### Очікуваний результат сценарію сервісу

```text
Подій отримано: 2     (Added + Issued)
changes = 2           (після відмов події немає)
Подій отримано: 3     (після -=: операція виконана, app.log не змінився, лічильник +1)
```