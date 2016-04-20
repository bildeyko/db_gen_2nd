using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;

namespace Generator
{
    [Serializable()]
    [XmlRoot("settings")]
    public class Settings
    {
        [XmlElement("cacheDSN")]
        public string cacheDSN { get; set; }

        [XmlElement("postgresDSN")]
        public string postgresDSN { get; set; }

        [XmlElement("productsFile")]
        public string productsFile { get; set; }

        [XmlElement("perProduct")]
        public int perProduct { get; set; }

        [XmlElement("companiesFile")]
        public string companiesFile { get; set; }

        [XmlElement("companiesNumber")]
        public int companiesNumber { get; set; }

        [XmlElement("citiesFile")]
        public string citiesFile { get; set; }

        [XmlElement("staffFile")]
        public string staffFile { get; set; }

        [XmlElement("staffNumber")]
        public int staffNumber { get; set; }

        [XmlElement("prodItemsText")]
        public string prodItemsText { get; set; }

        [XmlElement("prodItemsNumber")]
        public int prodItemsNumber { get; set; }

        [XmlElement("batchesNumber")]
        public int batchesNumber { get; set; }

    }
}
