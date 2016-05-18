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
using System.Text.RegularExpressions;
using Generator.FuzzyString;

namespace Generator
{
    class Program
    {
        private static Gen gen;
        private static Settings settings; 
        private static string dsn;       

        static void Main(string[] args)
        {
            string appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ;
            string path =  appDir + "\\settings.xml";

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
                {
                    Generate(type);
                    return;
                }

                if (options.aggregate)
                {
                    Aggregation(type);
                    return;
                }

                if (options.searchText != null && options.searchText != "")
                {
                    Search(options.searchText, type, options.insensitive);
                    return;
                }

                DialogMode(type);
            }
        }

        static void Generate(Gen.DBType type)
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

        static void Aggregation(Gen.DBType type)
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

        static void DialogMode(Gen.DBType dbtype)
        {
            var dialogStart = new StringBuilder();
            dialogStart.AppendLine("Dialog mode");
            dialogStart.AppendLine("Commands");
            dialogStart.AppendLine("  just text - search");
            dialogStart.AppendLine("  /select N - select item from result");
            dialogStart.AppendLine("  /setfussy [1|2|3] - change search metric");
            dialogStart.AppendLine("  /q or /quit");
            Console.WriteLine(dialogStart.ToString());


            bool exit = true;
            Regex comRegex = new Regex(@"^\s*/(quit|select|q|setfussy)\s*");
            Regex selectRegex = new Regex(@"^\s*/select\s*(\d+)");
            Regex setfussyRegex = new Regex(@"^\s*/setfussy\s*([123])");
            int searchType;

            Console.WriteLine("Set search metric");
            Console.WriteLine("1 - Levenshtein Distance");
            Console.WriteLine("2 - Jaro-Winkler Distance");
            Console.WriteLine("3 - Longest Common Substring");
            searchType = Int32.Parse(Console.ReadLine());

            SortedList<double, FastSearchModel> list = new SortedList<double, FastSearchModel>();

            while (exit)
            {
                var comLine = Console.ReadLine();
                var com = comLine;
                Match match = comRegex.Match(comLine);
                if (match.Success)
                    com = match.Groups[1].Value;

                switch(com)
                {
                    case "select":
                        match = selectRegex.Match(comLine);
                        var index = 0;

                        if (match.Success)
                        {
                            index = Int32.Parse(match.Groups[1].Value);
                            int i = 1;
                            foreach (var item in list)
                            {
                                if (i == index)
                                {
                                    Search(item.Value.Description, dbtype, false);
                                }
                                i++;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Wrong command syntax");
                            break;
                        }
                        Console.WriteLine("Selected index {0}", index);
                        break;
                    case "setfussy":
                        match = setfussyRegex.Match(comLine);
                        if (match.Success)
                        {
                            searchType = Int32.Parse(match.Groups[1].Value);
                            Console.WriteLine("Metric changed");
                        }
                        else
                        {
                            Console.WriteLine("Wrong command syntax");
                            break;
                        }
                        break;
                    case "q":
                    case "quit":
                        Console.WriteLine("Quit");
                        return;
                    default:
                        list = SearchDialog(comLine, dbtype, searchType);
                        if (list.Count == 0)
                            Console.WriteLine("Your search - {0} - did notmatch any sentences", comLine);
                        else
                        {
                            int i = 1;
                            foreach (var item in list)
                            {
                                if (i == 7)
                                {
                                    break;
                                }

                                var outStr = "{0}. {1}";
                                var descr = (item.Value.Description.Length > 76) ? item.Value.Description.Substring(0, 76) + "..." : item.Value.Description;
                                Console.WriteLine(outStr, i, descr);
                                i++;
                            }
                        }
                        break;
                }
            }
        }

        static SortedList<double, FastSearchModel> SearchDialog(string line, Gen.DBType type, int seachType)
        {
            var items = SeacrhDescr(type);

            var list = FussySearch(line, items, seachType);

            if(list.Count == 0)
                line = Replace(line);
            else
                return list;

            var oldCount = list.Count;

            list = FussySearch(line, items, seachType);

            if(list.Count > oldCount)
                Console.WriteLine("Did you mean: {0}", line);

            return list;
        }

        static List<FastSearchModel> SeacrhDescr(Gen.DBType type)
        {
            OdbcConnection DbConnection = new OdbcConnection(dsn);
            Queries q;
            List<FastSearchModel> items = new List<FastSearchModel>();
            try
            {
                DbConnection.Open();

                q = new Queries(DbConnection);

                if (type == Gen.DBType.Postgres)
                    items = q.GetFastItemsPostgres();

                if (type == Gen.DBType.Cache)
                    items = q.GetFastItemsCache();


            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally
            {
                DbConnection.Close();
            }
            return items;
        }

        static SortedList<double, FastSearchModel> FussySearch(string line, List<FastSearchModel> items, int seachType)
        {
            SortedList<double, FastSearchModel> list = new SortedList<double, FastSearchModel>(new DuplicateKeyComparer<double>());

            foreach (var item in items)
            {
                double met = 0.0;
                if (seachType == 1)
                {
                    met = Convert.ToDouble(Metrics.LevenshteinDistance(line, item.Description));
                    if (met < line.Length * 0.5)
                        list.Add(met, item);
                }
                if (seachType == 2)
                {
                    met = Metrics.JaroWinklerDistance(line, item.Description);
                    if (met > 0.7)
                        list.Add(met, item);
                }
                if (seachType == 3)
                {
                    met = Metrics.LongestCommonSubstring(line, item.Description);
                    if (met > line.Length * 0.7)
                        list.Add(met, item);
                }

            }

            return list;
        }

            static void Search(string line, Gen.DBType type, bool insCase)
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

            char[] delimiterChars = { ' ' };

            var lineWords = line.Trim().ToLower().Split(delimiterChars);

            foreach (var word in lineWords)
            {
                if (words.Contains(word))
                    errorCount++;
            }

            return (errorCount >= errorMax) ? Replace(line) : line;
        }

        static string Replace(string line)
        {
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
            return line.ReplaceByAlph(letters);
        }


    }
}
