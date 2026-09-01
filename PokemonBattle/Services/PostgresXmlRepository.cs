using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Npgsql;
using NpgsqlTypes;

namespace PokemonBattle.Services;

public sealed class PostgresXmlRepository : IXmlRepository
{
    private readonly string _connectionString;

    public PostgresXmlRepository(string connectionString)
    {
        _connectionString = connectionString;
        EnsureTable();
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "Xml"
            FROM "DataProtectionKeys"
            ORDER BY "Id";
            """;

        using var reader = command.ExecuteReader();
        var elements = new List<XElement>();
        while (reader.Read())
        {
            elements.Add(XElement.Parse(reader.GetString(0), LoadOptions.PreserveWhitespace));
        }

        return elements;
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "DataProtectionKeys" ("Xml")
            VALUES (@xml);
            """;
        command.Parameters.Add("xml", NpgsqlDbType.Text).Value =
            element.ToString(SaveOptions.DisableFormatting);
        command.ExecuteNonQuery();
    }

    private void EnsureTable()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "DataProtectionKeys" (
                "Id" SERIAL PRIMARY KEY,
                "Xml" TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }
}