using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace MyClassLibrary
{
    class ConfigurationSectionDemo
    {
        public static void Run(string[] args)
        {
            // Get current configuration file.
            System.Configuration.Configuration config =
                ConfigurationManager.OpenExeConfiguration(
                ConfigurationUserLevel.None);

            // Get the MyUrls section.
            UrlsSection myUrlsSection =
                config.GetSection("MyUrls") as UrlsSection;

            if (myUrlsSection == null)
                Console.WriteLine("Failed to load UrlsSection.");
            else
            {
                Console.WriteLine("The 'simple' element of app.config:");
                Console.WriteLine("  Name={0} URL={1} Port={2}",
                    myUrlsSection.Simple.Name,
                    myUrlsSection.Simple.Url,
                    myUrlsSection.Simple.Port);

                Console.WriteLine("The urls collection of app.config:");
                for (int i = 0; i < myUrlsSection.Urls.Count; i++)
                {
                    Console.WriteLine("  Name={0} URL={1} Port={2}",
                        myUrlsSection.Urls[i].Name,
                        myUrlsSection.Urls[i].Url,
                        myUrlsSection.Urls[i].Port);
                }
            }
            Console.ReadLine();
        }
    }

    public class UrlsSection : ConfigurationSection
    {
        [ConfigurationProperty("name",
            DefaultValue = "MyFavorites",
            IsRequired = true,
            IsKey = false)]
        [StringValidator(InvalidCharacters =
            " ~!@#$%^&*()[]{}/;'\"|\\",
            MinLength = 1, MaxLength = 60)]
        public string Name
        {

            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }

        }

        // Declare an element (not in a collection) of the type
        // UrlConfigElement. In the configuration
        // file it corresponds to <simple .... />.
        [ConfigurationProperty("simple")]
        public UrlConfigElement Simple
        {
            get
            {
                UrlConfigElement url =
                (UrlConfigElement)base["simple"];
                return url;
            }
        }

        // Declare a collection element represented 
        // in the configuration file by the sub-section
        // <urls> <add .../> </urls> 
        // Note: the "IsDefaultCollection = false" 
        // instructs the .NET Framework to build a nested 
        // section like <urls> ...</urls>.
        [ConfigurationProperty("urls",
            IsDefaultCollection = false)]
        public UrlsCollection Urls
        {
            get
            {
                UrlsCollection urlsCollection =
                (UrlsCollection)base["urls"];
                return urlsCollection;
            }
        }


        protected override void DeserializeSection(
            System.Xml.XmlReader reader)
        {
            base.DeserializeSection(reader);
            // You can add custom processing code here.
        }

        protected override string SerializeSection(
            ConfigurationElement parentElement,
            string name, ConfigurationSaveMode saveMode)
        {
            string s =
                base.SerializeSection(parentElement,
                name, saveMode);
            // You can add custom processing code here.
            return s;
        }

    }

    public class UrlsCollection : ConfigurationElementCollection
    {
        public UrlsCollection()
        {
            // Add one url to the collection.  This is
            // not necessary; could leave the collection 
            // empty until items are added to it outside
            // the constructor.
            UrlConfigElement url =
                (UrlConfigElement)CreateNewElement();
            Add(url);
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return

                    ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new UrlConfigElement();
        }


        protected override
            ConfigurationElement CreateNewElement(
            string elementName)
        {
            return new UrlConfigElement(elementName);
        }


        protected override Object
            GetElementKey(ConfigurationElement element)
        {
            return ((UrlConfigElement)element).Name;
        }


        public new string AddElementName
        {
            get
            { return base.AddElementName; }

            set
            { base.AddElementName = value; }

        }

        public new string ClearElementName
        {
            get
            { return base.ClearElementName; }

            set
            { base.ClearElementName = value; }

        }

        public new string RemoveElementName
        {
            get
            { return base.RemoveElementName; }
        }

        public new int Count
        {
            get { return base.Count; }
        }


        public UrlConfigElement this[int index]
        {
            get
            {
                return (UrlConfigElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        new public UrlConfigElement this[string Name]
        {
            get
            {
                return (UrlConfigElement)BaseGet(Name);
            }
        }

        public int IndexOf(UrlConfigElement url)
        {
            return BaseIndexOf(url);
        }

        public void Add(UrlConfigElement url)
        {
            BaseAdd(url);
            // Add custom code here.
        }

        protected override void
            BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, false);
            // Add custom code here.
        }

        public void Remove(UrlConfigElement url)
        {
            if (BaseIndexOf(url) >= 0)
                BaseRemove(url.Name);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        public void Clear()
        {
            BaseClear();
            // Add custom code here.
        }
    }

    public class UrlConfigElement : ConfigurationElement
    {
        // Constructor allowing name, url, and port to be specified.
        public UrlConfigElement(String newName,
            String newUrl, int newPort)
        {
            Name = newName;
            Url = newUrl;
            Port = newPort;
        }

        // Default constructor, will use default values as defined
        // below.
        public UrlConfigElement()
        {
        }

        // Constructor allowing name to be specified, will take the
        // default values for url and port.
        public UrlConfigElement(string elementName)
        {
            Name = elementName;
        }

        [ConfigurationProperty("name",
            DefaultValue = "Microsoft",
            IsRequired = true,
            IsKey = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }

        [ConfigurationProperty("url",
            DefaultValue = "http://www.microsoft.com",
            IsRequired = true)]
        [RegexStringValidator(@"\w+:\/\/[\w.]+\S*")]
        public string Url
        {
            get
            {
                return (string)this["url"];
            }
            set
            {
                this["url"] = value;
            }
        }

        [ConfigurationProperty("port",
            DefaultValue = (int)0,
            IsRequired = false)]
        [IntegerValidator(MinValue = 0,
            MaxValue = 8080, ExcludeRange = false)]
        public int Port
        {
            get
            {
                return (int)this["port"];
            }
            set
            {
                this["port"] = value;
            }
        }

        protected override void DeserializeElement(
           System.Xml.XmlReader reader,
            bool serializeCollectionKey)
        {
            base.DeserializeElement(reader,
                serializeCollectionKey);
            // You can your custom processing code here.
        }


        protected override bool SerializeElement(
            System.Xml.XmlWriter writer,
            bool serializeCollectionKey)
        {
            bool ret = base.SerializeElement(writer,
                serializeCollectionKey);
            // You can enter your custom processing code here.
            return ret;

        }


        protected override bool IsModified()
        {
            bool ret = base.IsModified();
            // You can enter your custom processing code here.
            return ret;
        }
    }

}
