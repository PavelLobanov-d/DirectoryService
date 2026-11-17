# DirectoryService.IntegrationTests

Интеграционные тесты поднимают реальное API через `WebApplicationFactory<Program>` и отдельный PostgreSQL в Testcontainers.

## Запуск

Из каталога `backend`:

```powershell
dotnet test .\DirectoryService.IntegrationTests\DirectoryService.IntegrationTests.csproj
```

Из корня репозитория:

```powershell
dotnet test .\backend\DirectoryService.IntegrationTests\DirectoryService.IntegrationTests.csproj
```

Требуется запущенный Docker Desktop.

## Что проверяется

- create, read и delete для locations и departments через HTTP;
- Envelope и коды ошибок;
- фильтры, сортировка и пагинация списка локаций;
- Dapper-запрос с CTE и `totalCount`;
- состояние БД после write-операций.

Перед каждым тестом БД очищается через Respawn. Миграции применяются один раз при старте fixture.
