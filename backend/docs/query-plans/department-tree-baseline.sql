\set ON_ERROR_STOP on
\timing off

\echo '=== 1. tree roots ==='
EXPLAIN (ANALYZE, BUFFERS)
SELECT
    d.id,
    d.name,
    d.path::text AS path,
    nlevel(d.path) - 1 AS depth,
    EXISTS (
        SELECT 1 FROM departments child
        WHERE child.parentid = d.id AND child.deleted_at IS NULL
    ) AS haschildren,
    (
        SELECT count(*)::int FROM departments child
        WHERE child.parentid = d.id AND child.deleted_at IS NULL
    ) AS childrencount
FROM departments d
WHERE d.deleted_at IS NULL AND d.parentid IS NULL
ORDER BY d.name, d.id;

\echo '=== 2. direct children ==='
EXPLAIN (ANALYZE, BUFFERS)
SELECT
    d.id,
    d.name,
    d.path::text AS path,
    nlevel(d.path) - 1 AS depth,
    EXISTS (
        SELECT 1 FROM departments child
        WHERE child.parentid = d.id AND child.deleted_at IS NULL
    ) AS haschildren,
    (
        SELECT count(*)::int FROM departments child
        WHERE child.parentid = d.id AND child.deleted_at IS NULL
    ) AS childrencount
FROM departments d
WHERE d.deleted_at IS NULL AND d.parentid = '4ac0c7e5-0a1e-4967-a413-d4cfcc27c635'
ORDER BY d.name, d.id;

\echo '=== 3. ancestors ==='
EXPLAIN (ANALYZE, BUFFERS)
SELECT
    a.id,
    a.name,
    a.path::text AS path,
    nlevel(a.path) - 1 AS depth
FROM departments a
WHERE a.deleted_at IS NULL
  AND a.path @> (
      SELECT path FROM departments
      WHERE id = '77f357b6-34b3-4fa0-8ce0-f27e561b6fe4' AND deleted_at IS NULL
  )
  AND a.id <> '77f357b6-34b3-4fa0-8ce0-f27e561b6fe4'
ORDER BY nlevel(a.path), a.id;

\echo '=== 4. tree search with ancestors ==='
EXPLAIN (ANALYZE, BUFFERS)
SELECT
    m.id,
    m.name,
    m.path::text AS path,
    nlevel(m.path) - 1 AS depth,
    EXISTS (
        SELECT 1 FROM departments child
        WHERE child.parentid = m.id AND child.deleted_at IS NULL
    ) AS haschildren,
    (
        SELECT count(*)::int FROM departments child
        WHERE child.parentid = m.id AND child.deleted_at IS NULL
    ) AS childrencount,
    COALESCE((
        SELECT jsonb_agg(
            jsonb_build_object(
                'id', a.id,
                'name', a.name,
                'path', a.path::text,
                'depth', nlevel(a.path) - 1
            )
            ORDER BY nlevel(a.path)
        )
        FROM departments a
        WHERE a.deleted_at IS NULL
          AND a.path @> m.path
          AND a.id <> m.id
    ), '[]'::jsonb)::text AS ancestorsjson
FROM departments m
WHERE m.deleted_at IS NULL
  AND lower(m.name) LIKE '%bulk level three%'
ORDER BY nlevel(m.path), m.name, m.id
LIMIT 50;

\echo '=== 5. default list page ==='
EXPLAIN (ANALYZE, BUFFERS)
SELECT d.id, d.name, d.parentid, d.path
FROM departments d
WHERE d.deleted_at IS NULL
ORDER BY d.id
LIMIT 20 OFFSET 0;

\echo '=== 6. list by path, EF handler ==='
EXPLAIN (ANALYZE, BUFFERS)
SELECT
    d2.id,
    d1.positionmatrix_id,
    d2.path,
    d2.name,
    d2.parentid,
    d1.department_id,
    d6.id,
    d4.location_id,
    d4.id,
    d5.position_id,
    d5.id
FROM (
    SELECT d.id, d.name, d.parentid, d.path
    FROM departments AS d
    WHERE d.deleted_at IS NULL AND EXISTS (
        SELECT 1
        FROM (
            SELECT * FROM departments WHERE path <@ 'aaa'::ltree
        ) AS d0
        WHERE d0.deleted_at IS NULL AND d0.id = d.id
    )
    ORDER BY d.id
    LIMIT 20 OFFSET 0
) AS d2
LEFT JOIN department_chiefpositions AS d1 ON d2.id = d1.department_id
LEFT JOIN (
    SELECT d3.id, d3.parentid FROM departments AS d3 WHERE d3.deleted_at IS NULL
) AS d6 ON d2.id = d6.parentid
LEFT JOIN department_locations AS d4 ON d2.id = d4.department_id
LEFT JOIN department_positions AS d5 ON d2.id = d5.department_id
ORDER BY d2.id, d1.department_id, d6.id, d4.id;

\echo '=== 7. list by path, Dapper page ==='
EXPLAIN (ANALYZE, BUFFERS)
WITH RECURSIVE subtree AS (
    SELECT anchor.id
    FROM departments anchor
    WHERE anchor.path = 'aaa'::ltree AND anchor.deleted_at IS NULL
    UNION ALL
    SELECT child.id
    FROM departments child
    JOIN subtree parent ON parent.id = child.parentid
    WHERE child.deleted_at IS NULL
)
SELECT
    d.id,
    d.name,
    d.path::text AS path,
    d.parentid,
    dcp.positionmatrix_id AS chiefpositionid,
    ARRAY(SELECT c.id FROM departments c WHERE c.parentid = d.id AND c.deleted_at IS NULL) AS childsids,
    ARRAY(SELECT dl.location_id FROM department_locations dl WHERE dl.department_id = d.id) AS locationsids,
    ARRAY(SELECT dp.position_id FROM department_positions dp WHERE dp.department_id = d.id) AS positionsids
FROM departments d
LEFT JOIN department_chiefpositions dcp ON dcp.department_id = d.id
WHERE d.deleted_at IS NULL AND d.id IN (SELECT id FROM subtree)
ORDER BY d.id
OFFSET 0 LIMIT 20;

\echo '=== 8. list by path, Dapper count ==='
EXPLAIN (ANALYZE, BUFFERS)
WITH RECURSIVE subtree AS (
    SELECT anchor.id
    FROM departments anchor
    WHERE anchor.path = 'aaa'::ltree AND anchor.deleted_at IS NULL
    UNION ALL
    SELECT child.id
    FROM departments child
    JOIN subtree parent ON parent.id = child.parentid
    WHERE child.deleted_at IS NULL
)
SELECT count(*)
FROM departments d
WHERE d.deleted_at IS NULL AND d.id IN (SELECT id FROM subtree);

\echo '=== 9. name filter count ==='
EXPLAIN (ANALYZE, BUFFERS)
SELECT count(*)
FROM departments d
WHERE d.deleted_at IS NULL AND lower(d.name) LIKE '%bulk level three%';
