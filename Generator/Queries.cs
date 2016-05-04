using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Odbc;
using System.Diagnostics;

using Generator.Models;

namespace Generator
{
    class Queries
    {
        OdbcConnection con;
        private Stopwatch sw;

        public Queries(OdbcConnection con)
        {
            this.con = con;
        }

        public List<ProductItemAggModel> AggregateCache()
        {
            var sql = @"SELECT pri.id,
                            LIST(p.barcode || ', ' || p.name) as product_info,
                            pri.quantity,
                            pri.shelflife,
                            pri.price,
                            pri.description,
                            LIST(cp.tin || ', ' || cp.name || ', ' || cp.postcode || ', ' || cp.address) as company_info,
                            LIST(ct.name) as location_info
                        FROM exchange.productitem pri
                        LEFT JOIN exchange.city ct
                            ON pri.location = ct.id
                        LEFT JOIN exchange.company cp
                            ON pri.company = cp.id
                        LEFT JOIN exchange.product p
                            ON pri.product = p.id
                        GROUP BY pri.id";

            Console.WriteLine("Cache");
            return Aggregate(sql);
        }

        public List<ProductItemAggModel> AggregatePostgres()
        {
            var sql = @"SELECT pri.id,
	                        STRING_AGG(p.barcode || ', ' || p.name, ';') as product_info, 
	                        pri.quantity, 
	                        pri.shelflife, 
	                        pri.price, 
	                        pri.description, 
	                        STRING_AGG(cp.tin || ', ' || cp.name || ', ' || cp.postcode || ', ' || cp.address, ';') as company_info,
	                        STRING_AGG(ct.name, ';') as location_info
                        FROM productitem pri
                        LEFT JOIN city ct
	                        ON pri.location = ct.id
                        LEFT JOIN company cp
	                        ON pri.company = cp.id
                        LEFT JOIN product p
	                        ON pri.product = p.id
                        GROUP BY pri.id";

            Console.WriteLine("Postgres");
            return Aggregate(sql);
        }

        public List<ProductItemModel> SearchCache(string line, bool insCase)
        {
            string sql;
            if(insCase)
                sql = @"SELECT id, quantity, shelflife, price, description FROM exchange.productitem WHERE description LIKE '%{0}%'";
            else
                sql = @"SELECT id, quantity, shelflife, price, description FROM exchange.productitem WHERE description [ '{0}'";

            sql = string.Format(sql, line);
            OdbcCommand command = new OdbcCommand(sql, con);

            Console.WriteLine("Cache");
            return Search(command);
        }

        public List<ProductItemModel> SearchPostgres(string line, bool insCase)
        {
            string sql;
            if (insCase)
            {
                sql = @"SELECT id, quantity, shelflife, price, description FROM productitem WHERE LOWER(description) LIKE '%{0}%'";
                line = line.ToLower();
            }
            else
                sql = @"SELECT id, quantity, shelflife, price, description FROM productitem WHERE description LIKE '%{0}%'";
            sql = string.Format(sql, line);
            OdbcCommand command = new OdbcCommand(sql, con);

            Console.WriteLine("Postgres");
            return Search(command);
        }
        public List<FastSearchModel> GetFastItemsCache()
        {
            var sql = @"SELECT description 
                        FROM exchange.productitem 
                        GROUP BY %EXACT(description)";

            Console.WriteLine("Cache");
            return GetItemsForFastSearch(sql);
        }

        public List<FastSearchModel> GetFastItemsPostgres()
        {
            var sql = @"SELECT description 
                        FROM productitem 
                        GROUP BY description";

            Console.WriteLine("Postgres");
            return GetItemsForFastSearch(sql);
        }

        private List<FastSearchModel> GetItemsForFastSearch(string sql)
        {
            OdbcCommand command = new OdbcCommand(sql, con);

            sw = Stopwatch.StartNew();
            OdbcDataReader reader = command.ExecuteReader();

            var lines = new List<FastSearchModel>();
            while (reader.Read())
            {
                var prod = new FastSearchModel();
                prod.Description = reader.GetString(0);
                lines.Add(prod);
            }
            sw.Stop();
            Console.WriteLine(" Rows selected: {0} Query elapsed time: {1:N4} ms", lines.Count, sw.Elapsed.TotalMilliseconds);

            return lines;
        }

        private List<ProductItemAggModel> Aggregate(string sql)
        {
            OdbcCommand command = new OdbcCommand(sql, con);

            sw = Stopwatch.StartNew();
            OdbcDataReader reader = command.ExecuteReader();

            var products = new List<ProductItemAggModel>();
            while (reader.Read())
            {
                var prod = new ProductItemAggModel();
                prod.Id = reader.GetInt32(0);
                prod.ProductInfo = reader.GetString(1);
                prod.Quantity = reader.GetInt32(2);
                prod.Date = reader.GetDateTime(3);
                prod.Price = reader.GetDouble(4);
                prod.Description = reader.GetString(5);
                prod.CompanyInfo = reader.GetString(6);
                prod.LocationInfo = reader.GetString(7);
                products.Add(prod);
            }
            sw.Stop();
            Console.WriteLine(" Rows selected: {0} Query elapsed time: {1:N4} ms", products.Count, sw.Elapsed.TotalMilliseconds);

            return products;
        }

        private List<ProductItemModel> Search(OdbcCommand command)
        {
            sw = Stopwatch.StartNew();
            OdbcDataReader reader = command.ExecuteReader();

            var lines = new List<ProductItemModel>();
            while (reader.Read())
            {
                var prod = new ProductItemModel();
                prod.Id = reader.GetInt32(0);
                prod.Quantity = reader.GetInt32(1);
                prod.Date = reader.GetDateTime(2);
                prod.Price = reader.GetDouble(3);
                prod.Description = reader.GetString(4);
                lines.Add(prod);
            }
            sw.Stop();
            Console.WriteLine(" Rows selected: {0} Query elapsed time: {1:N4} ms", lines.Count, sw.Elapsed.TotalMilliseconds);

            return lines;
        }
    }
}
