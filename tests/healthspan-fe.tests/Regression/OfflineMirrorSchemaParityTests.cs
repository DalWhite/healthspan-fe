using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// The C# local store and the SQLite Web Worker are two halves of one mirror:
/// the worker owns the schema, the C# side writes against it. When a column is
/// added to one half and not the other, every mutation for that entity fails
/// with "no column named X" and the page silently loses the action.
/// This test keeps the two halves in lockstep.
/// </summary>
public class OfflineMirrorSchemaParityTests
{
    private static readonly string[] MirrorTables = ["workouts", "exercises", "blocks"];

    [Fact]
    public void Every_Column_The_Local_Store_Writes_Exists_In_The_Worker_Schema()
    {
        var schema = ReadWorkerSchemaColumns();
        var written = ReadLocalStoreWrittenColumns();

        var offenders = new List<string>();
        foreach (var (table, columns) in written)
        {
            if (!schema.TryGetValue(table, out var known))
            {
                offenders.Add($"{table}: the worker creates no such table");
                continue;
            }

            foreach (var column in columns.OrderBy(c => c))
            {
                if (!known.Contains(column))
                {
                    offenders.Add($"{table}.{column}");
                }
            }
        }

        offenders.Should().BeEmpty(
            "SqlLocalStoreBase writes columns the SQLite worker never created; "
            + "add them to SCHEMA (and MIGRATIONS for existing databases) in "
            + "wwwroot/js/sqlite/offline-db-worker.js");
    }

    [Fact]
    public void The_Blocks_Mirror_Keeps_The_Round_Count()
    {
        var schema = ReadWorkerSchemaColumns();
        schema["blocks"].Should().Contain("rounds",
            "a Round/EMOM/AMRAP/Tabata block is defined by its round count");
    }

    // ── readers ───────────────────────────────────────────────────────────────

    private static Dictionary<string, HashSet<string>> ReadWorkerSchemaColumns()
    {
        var js = ReadRepoFile("wwwroot", "js", "sqlite", "offline-db-worker.js");

        var columns = new Dictionary<string, HashSet<string>>();
        foreach (Match table in Regex.Matches(
                     js, @"CREATE TABLE IF NOT EXISTS (\w+)\s*\((.*?)\);",
                     RegexOptions.Singleline))
        {
            var name = table.Groups[1].Value;
            var body = table.Groups[2].Value;

            var names = new HashSet<string>();
            foreach (var rawLine in body.Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("--"))
                {
                    continue;
                }

                var column = Regex.Match(line, @"^(\w+)\s+").Groups[1].Value;
                if (column is "PRIMARY" or "UNIQUE" or "FOREIGN" or "CONSTRAINT")
                {
                    continue;
                }

                names.Add(column);
            }

            columns[name] = names;
        }

        // A column may arrive only through a migration on databases created
        // before it existed; that still counts as known.
        foreach (Match migration in Regex.Matches(
                     js, @"ALTER TABLE (\w+) ADD COLUMN (\w+)"))
        {
            var table = migration.Groups[1].Value;
            if (columns.TryGetValue(table, out var names))
            {
                names.Add(migration.Groups[2].Value);
            }
        }

        return columns;
    }

    private static Dictionary<string, HashSet<string>> ReadLocalStoreWrittenColumns()
    {
        var cs = ReadRepoFile("Offline", "SqlLocalStoreBase.cs");

        var columns = new Dictionary<string, HashSet<string>>();
        void Add(string table, string column)
        {
            if (!MirrorTables.Contains(table))
            {
                return;
            }

            if (!columns.TryGetValue(table, out var set))
            {
                set = new HashSet<string>();
                columns[table] = set;
            }

            set.Add(column);
        }

        foreach (Match insert in Regex.Matches(
                     cs, @"INSERT(?: OR \w+)? INTO (\w+)\s*\((.*?)\)",
                     RegexOptions.Singleline))
        {
            foreach (var column in insert.Groups[2].Value.Split(','))
            {
                Add(insert.Groups[1].Value, column.Trim());
            }
        }

        foreach (Match update in Regex.Matches(
                     cs, @"UPDATE (\w+) SET (.*?) WHERE", RegexOptions.Singleline))
        {
            foreach (var assignment in update.Groups[2].Value.Split(','))
            {
                var column = Regex.Match(assignment, @"^\s*(\w+)\s*=").Groups[1].Value;
                if (column.Length > 0)
                {
                    Add(update.Groups[1].Value, column);
                }
            }
        }

        return columns;
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"{Path.Combine(parts)} was not found above the test assembly.");
    }
}
