using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Odbc;



namespace Generator
{
    class Program
    {
        private static Gen gen;
        static void Main(string[] args)
        {
            //OdbcConnection DbConnection = new OdbcConnection("DSN=CACHE");
            OdbcConnection DbConnection = new OdbcConnection("DSN=PostgreSQL35W");

            try
            {
                DbConnection.Open();
                gen = new Gen(DbConnection, 2);
                gen.CreateUnits();
                gen.CreateProductTypes();
               //gen.CreateProducts(20850, "C:\\CacheGen\\products");
                gen.CreateProducts(1, @"C:\CacheGen\products");
                gen.CreateCompanies(10000);
                gen.CreateCities(@"C:\CacheGen\cities");
                gen.CreateStaff(10000, @"C:\CacheGen\names");
                gen.CreateProductItems(1);
                gen.CreateBatches(1);
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                DbConnection.Close();
            }

            Console.WriteLine("Finish");
            Console.ReadLine();
        }
    }
}
