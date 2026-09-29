using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace ReklamHesap;

public record ProductRow(long Id, string Category, string Name, string? SubOption, string Unit, decimal Price);
public record ServiceRow(long Id, string Name, string Unit, decimal Price);

public static class Database
{
    public static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ReklamHesap");
    public static string DbPath => Path.Combine(Folder, "reklam_hesap.db");
    public static string ConnectionString => $"Data Source={DbPath}";

    public static void Initialize()
    {
        Directory.CreateDirectory(Folder);
        using var c = new SqliteConnection(ConnectionString);
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = """
        CREATE TABLE IF NOT EXISTS Products (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Category TEXT NOT NULL,
            Name TEXT NOT NULL,
            SubOption TEXT,
            Unit TEXT NOT NULL DEFAULT 'm2',
            Price REAL NOT NULL DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS CompositeMaterials (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Brand TEXT NOT NULL,
            ThicknessMm REAL NOT NULL,
            SheetWidthCm REAL NOT NULL,
            SheetLengthCm REAL NOT NULL,
            Price REAL NOT NULL DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS ServicePrices (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Unit TEXT NOT NULL,
            Price REAL NOT NULL DEFAULT 0
        );
        CREATE TABLE IF NOT EXISTS Jobs (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CustomerName TEXT,
            JobName TEXT,
            CreatedAt TEXT NOT NULL,
            TotalCost REAL NOT NULL DEFAULT 0,
            SalePrice REAL NOT NULL DEFAULT 0
        );
        """;
        cmd.ExecuteNonQuery();
        Seed();
    }

    static void Seed()
    {
        using var c = new SqliteConnection(ConnectionString);
        c.Open();

        using var countCmd = c.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM Products";
        if (Convert.ToInt32(countCmd.ExecuteScalar()) == 0)
        {
            var products = new (string name, string? sub)[]
            {
                ("Laminasyon", "Laminasyonlu"),
                ("Laminasyon", "Laminasyonsuz"),
                ("Kumlama Folyo", null),
                ("Kumlama Baskılı Folyo", null),
                ("Gold Folyo", null),
                ("Gümüş Folyo", null),
                ("Şeffaf Folyo", null),
                ("Simli Folyo", null),
                ("Desenli Folyo", null),
                ("Baskılı Folyo", null),
                ("Kesim Folyosu", null),
                ("Plotter Kesim", null),
                ("Bas-Kes", null)
            };
            foreach (var p in products)
            {
                using var cmd = c.CreateCommand();
                cmd.CommandText = """
                INSERT INTO Products(Category,Name,SubOption,Unit,Price)
                VALUES ('Folyo',$name,$sub,'m2',0)
                """;
                cmd.Parameters.AddWithValue("$name", p.name);
                cmd.Parameters.AddWithValue("$sub", (object?)p.sub ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        using var sc = c.CreateCommand();
        sc.CommandText = "SELECT COUNT(*) FROM ServicePrices";
        if (Convert.ToInt32(sc.ExecuteScalar()) == 0)
        {
            var services = new (string name, string unit)[]
            {
                ("Yemek","kişi"),
                ("Servis KM Gidiş-Dönüş","km"),
                ("İskele","iş"),
                ("Merdiven","iş"),
                ("Vinç","iş"),
                ("Cam Kirli","m2")
            };
            foreach (var s in services)
            {
                using var cmd = c.CreateCommand();
                cmd.CommandText = "INSERT INTO ServicePrices(Name,Unit,Price) VALUES ($n,$u,0)";
                cmd.Parameters.AddWithValue("$n", s.name);
                cmd.Parameters.AddWithValue("$u", s.unit);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static List<ProductRow> Products()
    {
        var list = new List<ProductRow>();
        using var c = new SqliteConnection(ConnectionString); c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,Category,Name,SubOption,Unit,Price FROM Products ORDER BY Id";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new ProductRow(r.GetInt64(0),r.GetString(1),r.GetString(2),
                r.IsDBNull(3)?null:r.GetString(3),r.GetString(4),r.GetDecimal(5)));
        return list;
    }

    public static List<ServiceRow> Services()
    {
        var list = new List<ServiceRow>();
        using var c = new SqliteConnection(ConnectionString); c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT Id,Name,Unit,Price FROM ServicePrices ORDER BY Id";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new ServiceRow(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetDecimal(3)));
        return list;
    }

    public static void SetProductPrice(long id, decimal price)
    {
        using var c = new SqliteConnection(ConnectionString); c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE Products SET Price=$p WHERE Id=$id";
        cmd.Parameters.AddWithValue("$p", price);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public static void SetServicePrice(long id, decimal price)
    {
        using var c = new SqliteConnection(ConnectionString); c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "UPDATE ServicePrices SET Price=$p WHERE Id=$id";
        cmd.Parameters.AddWithValue("$p", price);
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }
}
