using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Odbc;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Diagnostics;

using Generator.Models;

namespace Generator
{
    class Gen
    {
        OdbcConnection con;
        Stopwatch sw;
        string Schema;
        public Gen(OdbcConnection con, int dbType)
        {
            this.con = con;
            if (dbType == 1)
                Schema = "";
            if (dbType == 2)
                Schema = "public.";
        }

        public void CreateUnits()
        {
            sw = Stopwatch.StartNew();
            InsertUnit("килограмм", "кг");
            InsertUnit("тонн", "т");
            InsertUnit("грамм", "г");
            InsertUnit("литр", "л");

            sw.Stop();
            Console.WriteLine("Units were added to the database: {0:N4} s", sw.Elapsed.TotalSeconds);
        }

        public void CreateProductTypes()
        {
            sw = Stopwatch.StartNew();
            var unit = SelectUnitId("тонн");

            InsertProductType("хлебные культуры", unit);
            InsertProductType("зернобобовые культуры", unit);
            InsertProductType("кормовые зерновые культуры", unit);
            InsertProductType("миниральные удобрения", unit);
            InsertProductType("органические удобрения", unit);

            sw.Stop();
            Console.WriteLine("Product types were added to the database: {0:N4} s", sw.Elapsed.TotalSeconds);
        }

        public void CreateProducts(int n, string filename)
        {
            sw = Stopwatch.StartNew();

            StreamReader file = new StreamReader(filename);
            char[] delimiterChars = { '_', '\t' };
            string line;
            int typeId = 0;
            Random rand = new Random();

            while ((line = file.ReadLine()) != null)
            {
                string[] words = line.Split(delimiterChars);
                switch (words[0])
                {
                    case "*":
                        typeId = SelectProductTypeId(words[1]);
                        break;
                    case "**":
                        for (int i = 0; i < n; i++)
                        {
                            long barcode = GetRandom(1000000000000, 9999999999999, rand);
                            InsertProduct(barcode, words[1], typeId);
                        }
                        break;
                }
            }
            sw.Stop();
            Console.WriteLine("Products were added to the database: {0:N4} s", sw.Elapsed.TotalSeconds);
        }

        public void CreateCompanies(int n)
        {
            sw = Stopwatch.StartNew();

            Random rand = new Random();
            for (var i = 0; i < n; i++)
            {
                var name = RandomString(15, rand);
                var address = RandomString(150, rand);
                var tin = GetRandom(1000000000, 9999999999, rand);
                var postcode = GetRandom(10000000, 99999999, rand);
                InsertCompany(name, address, tin, postcode);
            }

            sw.Stop();
            Console.WriteLine("Companies were added to the database: {0:N4} s", sw.Elapsed.TotalSeconds);
        }

        public void CreateCities(string filename)
        {
            sw = Stopwatch.StartNew();

            StreamReader file = new StreamReader(filename);
            string line;

            while ((line = file.ReadLine()) != null)
            {
                InsertCity(line);
            }

            sw.Stop();
            Console.WriteLine("Cities were added to the database: {0:N4} s", sw.Elapsed.TotalSeconds);

            file.Close();
        }

        public void CreateStaff(int n, string filename)
        {
            sw = Stopwatch.StartNew();

            char[] delimiterChars = { ',' };
            string line;
            string[] names, surnames;

            using (StreamReader file = new StreamReader(filename))
            {
                line = file.ReadLine();
                names = line.Split(delimiterChars);
                line = file.ReadLine();
                surnames = line.Split(delimiterChars);
            }

            var rand = new Random();
            for (var i = 0; i < n; i++)
            {
                var name = names[rand.Next(0, names.Length-1)];
                var surname = surnames[rand.Next(0, surnames.Length-1)];
                var snils = GetRandom(10000000000, 99999999999, rand);
                InsertStaff(name, surname, snils);
            }

            sw.Stop();
            Console.WriteLine("Staff were added to the database: {0:N4} s", sw.Elapsed.TotalSeconds);
        }

        public void CreateProductItems(int n)
        {
            sw = Stopwatch.StartNew();

            var companies = SelectCompanies();
            var products = SelectProducts();
            var cities = SelectCities();
            var date = DateTime.Now;

            var rand = new Random();
            for (var i = 0; i < companies.Count; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    var company = companies[i];
                    var product = products[rand.Next(0, products.Count - 1)];
                    var city = cities[rand.Next(0, cities.Count - 1)];
                    var newDate = DateTime.Now.AddDays(rand.Next(183, 730));

                    var quantity = rand.Next(100, 10000);
                    var price = RandomDouble(300.0, 1500.0, rand);

                    InsertProductItem(company, product, city, quantity, price, newDate);
                }
            }

            sw.Stop();
            Console.WriteLine("Product items were added to the database: {0:N4} s", sw.Elapsed.TotalSeconds);
        }

        public void CreateBatches(int n)
        {
            sw = Stopwatch.StartNew();

            var staff = SelectStaff();
            var items = SelectProductItems();

            var rand = new Random();
            ProductItemModel item;
            int batchType = -1;
            for (var i = 0; i < staff.Count; i++)
            {
                item = items.First(it => it.Quantity > 0);
                batchType = item.Type;

                var pers = staff[i];
                var date = DateTime.Now;
                var batchId = InsertBatch(pers, item, date);

                for (var j = 0; j < n; j++)
                {
                    if (j != 0)
                    {
                        item = items.First(it => it.Quantity > 0 && it.Type == batchType);
                    }

                    var quantity = rand.Next(1, item.Quantity);
                    item.ReduceQuantity(quantity);
                    InsertBatchItem(item, batchId, quantity);
                }
            }

            items.Where(x => x.Changed).ToList().ForEach(e => UpdateProductItem(e));

            sw.Stop();
            Console.WriteLine("Batches were added to the database: {0:N4} s", sw.Elapsed.TotalSeconds);
        }

        private long GetRandom(long min, long max, Random rand)
        {
            byte[] buf = new byte[8];
            rand.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return (Math.Abs(longRand % (max - min)) + min);
        }

        string RandomString(int length, Random rand)
        {
            const string chars = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[rand.Next(s.Length)]).ToArray());
        }

        private double RandomDouble(double min, double max, Random rand)
        {
            var num = rand.NextDouble() * (max - min) + min;
            return num;
        }

        private void InsertUnit(string name, string shortName)
        {
            var sql = string.Format("insert into Exchange.{0}Unit (name, shortname) values (?,?)", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            command.Parameters.Add("Name", OdbcType.VarChar).Value = name;
            command.Parameters.Add("ShortName", OdbcType.VarChar).Value = shortName;

            InsertRow(command);
        }

        private void InsertProductType(string name, int unit)
        {
            var sql = string.Format("insert into Exchange.{0}ProductType (name, unit) values (?,?)", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            command.Parameters.Add("Name", OdbcType.VarChar).Value = name;
            command.Parameters.Add("Unit", OdbcType.Int).Value = unit;

            InsertRow(command);
        }

        private void InsertProduct(long barcode, string name, int type)
        {
            var sql = string.Format("insert into Exchange.{0}Product (barcode, name, type) values (?,?,?)", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            command.Parameters.Add("Barcode", OdbcType.BigInt).Value = barcode;
            command.Parameters.Add("Name", OdbcType.VarChar).Value = name;
            command.Parameters.Add("type", OdbcType.Int).Value = type;

            InsertRow(command);
        }

        private void InsertCompany(string name, string address, long tin, long postcode)
        {
            var sql = string.Format("insert into Exchange.{0}Company (address, name, postcode, tin) values (?,?,?,?)", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            command.Parameters.Add("Address", OdbcType.VarChar).Value = address;
            command.Parameters.Add("Name", OdbcType.VarChar).Value = name;
            command.Parameters.Add("Postcode", OdbcType.BigInt).Value = postcode;
            command.Parameters.Add("Tin", OdbcType.BigInt).Value = tin;

            InsertRow(command);
        }

        private void InsertCity(string name)
        {
            var sql = string.Format("insert into Exchange.{0}City (name) values (?)", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            command.Parameters.Add("Name", OdbcType.VarChar).Value = name;

            InsertRow(command);
        }

        private void InsertStaff(string name, string surname, long snils)
        {
            var sql = string.Format("insert into Exchange.{0}Staff (name, surname, snils) values (?,?,?)", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            command.Parameters.Add("Name", OdbcType.VarChar).Value = name;
            command.Parameters.Add("Surname", OdbcType.VarChar).Value = surname;
            command.Parameters.Add("Snils", OdbcType.BigInt).Value = snils;

            InsertRow(command);
        }

        private void InsertProductItem(CompanyModel company, ProductModel product, CityModel city, int quantity, double price, DateTime date)
        {
            var sql = string.Format("insert into Exchange.{0}ProductItem (company, product, location, quantity, shelfLife, price) values (?,?,?,?,?,?)", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            command.Parameters.Add("Company", OdbcType.Int).Value = company.Id;
            command.Parameters.Add("Product", OdbcType.Int).Value = product.Id;
            command.Parameters.Add("City", OdbcType.Int).Value = city.Id;
            command.Parameters.Add("Quantity", OdbcType.Int).Value = quantity;
            command.Parameters.Add("ShelfLife", OdbcType.DateTime).Value = date;
            command.Parameters.Add("Price", OdbcType.Double).Value = price;            

            InsertRow(command);
        }

        private int InsertBatch(StaffModel pers, ProductItemModel productItem, DateTime date)
        {
            var sql = string.Format("insert into Exchange.{0}Batch (manager, type, buildDate) values (?,?,?)", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            command.Parameters.Add("Manager", OdbcType.Int).Value = pers.Id;
            command.Parameters.Add("Type", OdbcType.Int).Value = productItem.Type;
            command.Parameters.Add("BuildDate", OdbcType.DateTime).Value = date;

            InsertRow(command);

            sql = string.Format("SELECT max(id) FROM Exchange.{0}Batch", Schema);
            command = new OdbcCommand(sql, con);
            OdbcDataReader reader = command.ExecuteReader();
            reader.Read();

            var id = reader.GetInt32(0);

            return id;
        }

        private void InsertBatchItem(ProductItemModel item, int BatchId, int quantity)
        {
            var sql = string.Format("insert into Exchange.{0}ItemBatch (batch, item, quantity) values (?,?,?)", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            command.Parameters.Add("Batch", OdbcType.Int).Value = BatchId;
            command.Parameters.Add("Item", OdbcType.Int).Value = item.Id;
            command.Parameters.Add("Quantity", OdbcType.Int).Value = quantity;

            InsertRow(command);
        }

        private void InsertRow(OdbcCommand command)
        {
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private int SelectUnitId(string name)
        {
            var sql = string.Format("SELECT * FROM Exchange.{0}Unit WHERE name = ?", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            command.Parameters.Add("Name", OdbcType.VarChar).Value = name;
            OdbcDataReader reader = command.ExecuteReader();
            int fCount = reader.FieldCount;
            reader.Read();

            var id = reader.GetInt32(0);

            return id;
        }

        private int SelectProductTypeId(string name)
        {
            var sql = string.Format("SELECT * FROM Exchange.{0}ProductType WHERE name = ?", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            command.Parameters.Add("Name", OdbcType.VarChar).Value = name;
            OdbcDataReader reader = command.ExecuteReader();
            int fCount = reader.FieldCount;
            reader.Read();

            var id = reader.GetInt32(0);

            return id;
        }

        private List<CompanyModel> SelectCompanies()
        {
            var sql = string.Format("SELECT * FROM Exchange.{0}Company", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            OdbcDataReader reader = command.ExecuteReader();
            var companies = new List<CompanyModel>();
            while (reader.Read())
            {
                var company = new CompanyModel();
                company.Id = reader.GetInt32(0);
                companies.Add(company);
            }

            return companies;
        }

        private List<ProductModel> SelectProducts()
        {
            var sql = string.Format("SELECT * FROM Exchange.{0}Product", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            OdbcDataReader reader = command.ExecuteReader();
            var products = new List<ProductModel>();
            while (reader.Read())
            {
                var product = new ProductModel();
                product.Id = reader.GetInt32(0);
                products.Add(product);
            }

            return products;
        }

        private List<CityModel> SelectCities()
        {
            var sql = string.Format("SELECT * FROM Exchange.{0}City", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            OdbcDataReader reader = command.ExecuteReader();
            var cities = new List<CityModel>();
            while (reader.Read())
            {
                var city = new CityModel();
                city.Id = reader.GetInt32(0);
                cities.Add(city);
            }

            return cities;
        }

        private List<StaffModel> SelectStaff()
        {
            var sql = string.Format("SELECT * FROM Exchange.{0}Staff", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            OdbcDataReader reader = command.ExecuteReader();
            var staff = new List<StaffModel>();
            while (reader.Read())
            {
                var pers = new StaffModel();
                pers.Id = reader.GetInt32(0);
                staff.Add(pers);
            }

            return staff;
        }

        private List<ProductItemModel> SelectProductItems()
        {
            var sql = string.Format("SELECT ProductItem.id, ProductType.id as type,  ProductItem.quantity  FROM Exchange.{0}ProductItem, Exchange.{0}product, Exchange.{0}ProductType " +
                "WHERE ProductItem.Product = Product.id AND Product.type = ProductType.id", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            OdbcDataReader reader = command.ExecuteReader();
            var items = new List<ProductItemModel>();
            while (reader.Read())
            {
                var item = new ProductItemModel();
                item.Id = reader.GetInt32(0);
                item.Type = reader.GetInt32(1);
                item.Quantity = reader.GetInt32(2);
                items.Add(item);
            }

            return items;
        }

        private void UpdateProductItem(ProductItemModel item)
        {
            var sql = string.Format("update Exchange.{0}ProductItem set Quantity = ? where id = ?", Schema);
            OdbcCommand command = new OdbcCommand(sql, con);
            command.Parameters.Add("Quantity", OdbcType.Int).Value = item.Quantity;
            command.Parameters.Add("Id", OdbcType.Int).Value = item.Id;

            InsertRow(command);
        }
    }
}
