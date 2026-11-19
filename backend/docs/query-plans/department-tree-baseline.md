# Планы запросов дерева подразделений: до и после индексов

Дата: 2026-11-18

СУБД: PostgreSQL 14.23, локальный Docker.

Данные: 11 121 подразделение, 306 локаций, 1 122 должности, 33 344 связи
`department_locations`, 22 223 связи `department_positions`.

Ниже baseline до добавления индексов и результат после добавления GIN-индекса
по имени. В таблице уже есть существующие индексы:

- `PK_department` — btree по `departments.id`;
- `IX_departments_parentid` — btree по `departments.parentid`;
- `IX_departments_path` — GiST по `departments.path`;
- `UX_departments_parent_name_active`, `UX_departments_parent_slug_active` —
  частичные уникальные btree;
- индексы таблиц связей.

## Методика

Для каждого запроса выполнен `EXPLAIN (ANALYZE, BUFFERS)`. Запрос выполнен
дважды. В таблицу попал второй запуск, то есть прогретый кэш.

Тестовые параметры:

- корень: `path = 'aaa'`, 10 прямых детей и 1 110 потомков;
- лист: `path = 'aaa.aaa.aaa.aaa'`;
- поиск: `lower(name) LIKE '%bulk level three%'`;
- лимит выдачи: 20 строк.

Сырые планы: `department-tree-baseline.txt`, `department-tree-after.txt`,
`department-name-search-selective.txt`.
SQL запросов: `department-tree-baseline.sql`.

## Добавленные индексы

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX "IX_departments_name_trgm"
ON departments USING gin (lower(name) gin_trgm_ops)
WHERE deleted_at IS NULL;
```

Остальные индексы из списка уже существуют:

| Индекс | Статус | Причина |
|---|---|---|
| `departments.path` GIST | есть | `IX_departments_path` |
| `departments.parentid` btree | есть | `IX_departments_parentid` |
| `departments.slug` unique | есть | `UX_departments_parent_slug_active`; slug уникален в пределах родителя |
| `department_locations.location_id` | есть | `IX_department_locations_location_id` |
| `department_locations.department_id` | покрыт | ведущая колонка `UX_department_locations_department_location` |
| `department_positions.position_id` | есть | `IX_department_positions_position_id` |
| `department_positions.department_id` | покрыт | ведущая колонка `UX_department_positions_department_position` |

Глобальный unique-индекс по `departments.slug` не добавлен. Домен требует
уникальность slug в пределах родителя. В данных slug повторяются у разных
родителей.

## До/после

| № | Запрос | Оператор плана (до) | Фактическое время (до) | Оператор плана (после) | Время (после) |
|---:|---|---|---:|---|---:|
| 1 | `GET /departments/tree` | `Bitmap Heap Scan` + `Bitmap Index Scan IX_departments_parentid` | 0.343 ms | `Bitmap Heap Scan` + `Bitmap Index Scan IX_departments_parentid` | 0.313 ms |
| 2 | `GET /departments/{id}/children` | `Bitmap Heap Scan` + `Bitmap Index Scan IX_departments_parentid` | 0.181 ms | `Bitmap Heap Scan` + `Bitmap Index Scan IX_departments_parentid` | 0.217 ms |
| 3 | `GET /departments/{id}/ancestors` | `Bitmap Heap Scan` + `Bitmap Index Scan IX_departments_path` | 0.238 ms | `Bitmap Heap Scan` + `Bitmap Index Scan IX_departments_path` | 0.602 ms |
| 4 | `GET /departments/tree/search`, широкий шаблон | `Seq Scan departments m` + `Sort` + `Bitmap Index Scan IX_departments_path` на каждого кандидата | 21.057 ms | `Bitmap Index Scan IX_departments_name_trgm` + `Sort` + `Bitmap Index Scan IX_departments_path` | 21.821 ms |
| 5 | Список, первая страница без фильтра | `Index Scan PK_department` | 0.172 ms | `Index Scan PK_department` | 0.050 ms |
| 6 | Список по пути, EF-обработчик | `Merge Join` + `Seq Scan departments d3` (11 121 строка) | 4.435 ms | `Merge Join` + `Seq Scan departments d3` (11 121 строка) | 2.861 ms |
| 7 | Список по пути, Dapper, страница | `Recursive Union` + `CTE Scan` + `Merge Join` | 4.675 ms | `Recursive Union` + `CTE Scan` + `Merge Join` | 5.527 ms |
| 8 | Список по пути, Dapper, count | `Recursive Union` + `Seq Scan departments` + `HashAggregate` | 7.989 ms | `Recursive Union` + `Seq Scan departments` + `HashAggregate` | 6.582 ms |
| 9 | Фильтр по имени, count, широкий шаблон | `Seq Scan departments` | 8.079 ms | `Bitmap Index Scan IX_departments_name_trgm` | 9.546 ms |
| 10 | Фильтр по имени, избирательный шаблон `%aab aaj%` | `Seq Scan departments` | 6.514 ms | `Bitmap Index Scan IX_departments_name_trgm` | 0.784 ms |

## Наблюдения

- `tree`, `children` и `ancestors` работают по существующим индексам.
  Время осталось ниже 1 ms.
- GIN-индекс `IX_departments_name_trgm` ускоряет избирательный поиск:
  6.514 ms → 0.784 ms для `%aab aaj%`.
- Широкий шаблон `%bulk level three%` совпадает с 10 000 строк из 11 121.
  Индекс здесь не помогает: 21.821 ms против 21.057 ms.
- EF-список по пути сканирует `departments` повторно внутри `EXISTS`.
  Ключевой оператор — `Seq Scan departments d3` по 11 121 строке.
- Dapper-список по пути рекурсивно собирает 1 111 потомков и сортирует их.
  Это заметно на `count`: 6.582 ms.

## Время API (прогретый кэш)

| Endpoint | Первый запрос | Второй запрос |
|---|---:|---:|
| `GET /departments/tree` | 162 ms | 8 ms |
| `GET /departments/tree/search?q=Bulk` | 71 ms | 28 ms |
| `GET /departments/tree/search?q=aab aaj` | 34 ms | 11 ms |
| `GET /departments/tree/search?q=отд` | 10 ms | 7 ms |
| `GET /departments?search=path=aaa` (до индекса) | 2001 ms | 87 ms |
| `GET /departments/dapper?search=path=aaa` (до индекса) | 115 ms | 23 ms |

Первый запрос EF включает компиляцию запроса. SQL-время EF-запроса — 4.435 ms.
Разница между 2001 ms и 87 ms — накладные расходы первого выполнения EF.
