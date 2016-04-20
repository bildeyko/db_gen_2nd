using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Odbc;
using System.Diagnostics;
using Generator.Models;

using Generator.Extensions;

using CommandLine;
using CommandLine.Text;
using System.Xml.Serialization;
using System.IO;

namespace Generator
{
    class Program
    {
        private static Gen gen;
        private static Settings settings;        

        static void Main(string[] args)
        {
            string path = "settings.xml";

            try {
                XmlSerializer serializer = new XmlSerializer(typeof(Settings));

                using (StreamReader reader = new StreamReader(path))
                {
                    settings = (Settings)serializer.Deserialize(reader);
                }
            } catch 
            {
                Console.WriteLine("The settings.xml not found");
                return;
            }            

            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                Gen.DBType type = Gen.DBType.Cache;
                string dsn = "";
                switch (options.database)
                {
                    case "cache":
                        type = Gen.DBType.Cache;
                        dsn = settings.cacheDSN;
                        break;
                    case "postgres":
                        type = Gen.DBType.Postgres;
                        dsn = settings.postgresDSN;
                        break;
                    default:
                        Console.WriteLine(options.GetUsage());
                        return;
                }
                if (options.generate)
                    Generate(dsn, type);

                if (options.aggregate)
                    Aggregation(dsn, type);

                if (options.searchText != null && options.searchText != "")
                    Search(options.searchText, dsn, type, options.insensitive);
            }
        }

        static void Generate(string dsn, Gen.DBType type)
        {
            OdbcConnection DbConnection = new OdbcConnection(dsn);

            try
            {
                DbConnection.Open();
                gen = new Gen(DbConnection, type);
                gen.CreateUnits();
                gen.CreateProductTypes();
                gen.CreateProducts(settings.perProduct, settings.productsFile);
                gen.CreateCompanies(settings.companiesNumber, settings.companiesFile);
                gen.CreateCities(settings.citiesFile);
                gen.CreateStaff(settings.staffNumber, settings.staffFile);
                gen.CreateProductItems(settings.prodItemsNumber, settings.prodItemsText);
                gen.CreateBatches(settings.batchesNumber);

            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                DbConnection.Close();
            }

        }

        static void Aggregation(string dsn, Gen.DBType type)
        {
            OdbcConnection DbConnection = new OdbcConnection(dsn);
            Queries q;
            try
            {
                DbConnection.Open();

                q = new Queries(DbConnection);

                if(type == Gen.DBType.Postgres)
                    q.AggregatePostgres();

                if (type == Gen.DBType.Cache)
                    q.AggregateCache();


            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                DbConnection.Close();
            }
        }

        static void Search(string line, string dsn, Gen.DBType type, bool insCase)
        {
            var niceLine = CreateNiceString(line);
            OdbcConnection DbConnection = new OdbcConnection(dsn);
            Queries q;

            List<ProductItemModel> results = new List<ProductItemModel>();

            try
            {
                DbConnection.Open();

                q = new Queries(DbConnection);

                if (type == Gen.DBType.Postgres)
                    results = q.SearchPostgres(niceLine, insCase);

                if (type == Gen.DBType.Cache)
                    results = q.SearchCache(niceLine, insCase);
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                DbConnection.Close();
            }

            Console.WriteLine("");
            for (var i=0; i<results.Count;i++)
            {
                if (i == 5)
                {
                    Console.WriteLine("\n  ... {0} rows", results.Count - i);
                    break;
                }

                var outStr = "{0}\t{1}\t{2}\t{3}\t{4}";
                var item = results[i];
                var descr = (item.Description.Length > 70) ? item.Description.Substring(0, 70) + "..." : item.Description;
                Console.WriteLine(outStr, item.Id, item.Quantity, item.Date, item.Price, descr);
            }
        }

        static string CreateNiceString(string line)
        {
            var errorCount = 0;
            var errorMax = 1;

            line = line.Replace("!", "").Replace("?", "").Replace("&", "").Replace("/", "");

            List<string> words = new List<string> { "b", "d", "yt", "jy", "yf", "z", "xnj", "c", "cj", "njn", ",snm", "f", "dtcm", "nj", "rfr", "jyf",
                                            "gj", "yj", "jyb", "r", "e", "ns", "bp", "pf", "ds", "nfr", ";t", "jn", "crfpfnm", "njn", "rjnjhsq",
                                            "vjxm", "xtkjdtr", "j", "jlby", "tot", ",s", "nfrjq", "njkmrj", "ct,z", "cdjt", "rfrjq", "rjulf",
                                            "e;t", "lkz", "djn", "rnj", "lf", "ujdjhbnm", "ujl", "pyfnm", "vjq", "lj", "bkb", "tckb", "dhtvz",
                                            "herf", "ytn", "cfvsq", "yb", "cnfnm", ",jkmijq", "lf;t", "lheujq", "yfi", "cdjq", "ye", "gjl", "ult",
                                            "ltkj", "tcnm", "cfv", "hfp", "xnj,s", "ldf", "nfv", "xtv", "ukfp", ";bpym", "gthdsq", "ltym", "nenf",
                                            "ybxnj", "gjnjv", "jxtym", "[jntnm", "kb", "ghb", "ujkjdf", "yflj", ",tp", "dbltnm", "blnb", "ntgthm",
                                            "nj;t", "cnjznm", "lheu", "ljv", "ctqxfc", "vj;yj", "gjckt", "ckjdj", "pltcm", "levfnm", "vtcnj", "cghjcbnm",
                                            "xthtp", "kbwj", "njulf", "dtlm", "[jhjibq", "rf;lsq", "yjdsq", ";bnm", "ljk;ys", "cvjnhtnm", "gjxtve", "gjnjve",
                                            "cnjhjyf", "ghjcnj", "yjuf", "cbltnm", "gjyznm", "bvtnm", "rjytxysq", "ltkfnm", "dlheu", "yfl", "dpznm", "ybrnj",
                                            "cltkfnm", "ldthm", "gthtl", "ye;ysq", "gjybvfnm", "rfpfnmcz", "hf,jnf", "nhb", "dfi", "e;", "ptvkz", "rjytw", "ytcrjkmrj",
                                            "xfc", "ujkjc", "ujhjl", "gjcktlybq", "gjrf", "[jhjij", "ghbdtn", "pljhjdj", "pljhjdf", "ntcn", "yjdjq", "jr", "tuj", "rjt",
                                            "kb,j", "xnjkb", "ndj.", "ndjz", "nen", "zcyj", "gjyznyj", "x`", "xt" };

            IDictionary<char, char> letters = new Dictionary<char, char>{ { 'q', 'й'}, { 'w','ц'}, { 'e','у'}, { 'r','к'}, { 't','е'}, { 'y','н'}, { 'u','г'}, { 'i','ш'}, { 'o','щ'}, { 'p','з'}, { '[','х'}, { ']','ъ'}, 
                                                                          { 'a','ф'}, { 's','ы'}, { 'd','в'}, { 'f','а'}, { 'g','п'}, { 'h','р'}, { 'j','о'}, { 'k','л'}, { 'l','д'}, { ';','ж'}, { '\'','э'}, { 'z','я'}, 
                                                                          { 'x','ч'}, { 'c','с'}, { 'v','м'}, { 'b','и'}, { 'n','т'}, { 'm','ь'}, { ',','б'}, { '.','ю'}, { '/','.'}, { '`','ё'}, { 'Q','Й'}, { 'W','Ц'}, 
                                                                          { 'E','У'}, { 'R','К'}, { 'T','Е'}, { 'Y','Н'}, { 'U','Г'}, { 'I','Ш'}, { 'O','Щ'}, { 'P','З'}, { '{','Х'}, { '}','Ъ'}, { 'A','Ф'}, { 'S','Ы'}, 
                                                                          { 'D','В'}, { 'F','А'}, { 'G','П'}, { 'H','Р'}, { 'J','О'}, { 'K','Л'}, { 'L','Д'}, { ':','Ж'}, { '"','Э'}, { '|','/'}, { 'Z','Я'}, { 'X','Ч'}, 
                                                                          { 'C','С'}, { 'V','М'}, { 'B','И'}, { 'N','Т'}, { 'M','Ь'}, { '<','Б'}, { '>','Ю'}, { '?',','}, { '~','Ё'}, { '@','"'}, { '#','№'}, { '$',';'}, 
                                                                          { '^',':' }, { '&','?' }, { ' ', ' '} };

            var dictBuf = letters.FlipDict();

            dictBuf.Remove('.');
            dictBuf.Remove(',');
            dictBuf.Remove(';');
            dictBuf.Remove('"');
            dictBuf.Remove('?');
            dictBuf.Remove('/');

            letters = letters.Merge(dictBuf);

            char[] delimiterChars = { ' ' };

            var lineWords = line.Trim().ToLower().Split(delimiterChars);

            foreach (var word in lineWords)
            {
                if (words.Contains(word))
                    errorCount++;
            }

            return (errorCount >= errorMax) ? line.ReplaceByAlph(letters) : line;
        }


    }
}
