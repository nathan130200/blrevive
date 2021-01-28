﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Configuration
{ 
    /// <summary>
    /// Provides read/write access to JSON configuration.
    /// </summary>
    public class Config
    {
        public static AppConfigProvider App;
        [JsonPropertyName("App")]
        public AppConfigProvider _AppShallow { get { return Config.App; } }

        public static UserConfigProvider User;
        [JsonPropertyName("User")]
        public UserConfigProvider _UserShallow { get { return Config.User; } }

        public static GameConfigProvider Game;
        [JsonPropertyName("Game")]
        public GameConfigProvider _GameShallow { get { return Config.Game; } }

        public static ServerConfigProvider Server;
        [JsonPropertyName("Server")]
        public ServerConfigProvider _ServerShallow { get { return Config.Server; } }

        public static ServerListConfigProvider ServerList;
        [JsonPropertyName("ServerList")]
        public ServerListConfigProvider _ServerListShallow { get { return Config.ServerList; } }

        public static HostsConfigProvider Hosts;
        public static DefaultConfigProvider Defaults = new DefaultConfigProvider();

        private const string LauncherConfigFileName = "LauncherConfig.json";

        public static void Load()
        {
            // get all config providers declared in this class
            var confprop = typeof(Config).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            List<FieldInfo> providers = confprop.Where(prop => prop.FieldType.IsSubclassOf(typeof(IConfigProvider))).ToList();

            // read config
            var config = JsonDocument.Parse(File.ReadAllText(LauncherConfigFileName)).RootElement;

            // initialize providers
            foreach (var providerProp in providers)
            {
                var staticProvider = providerProp.FieldType;
                var customFileProp = staticProvider.GetField("FileName", BindingFlags.Public | BindingFlags.Static);

                if ( customFileProp != null)
                {
                    var ownConfig = JsonSerializer.Deserialize(File.ReadAllText((string)customFileProp.GetValue(null)), providerProp.FieldType);
                    providerProp.SetValue(null, ownConfig);
                } else
                {
                    var json = config.GetProperty(providerProp.Name).ToString();
                    IConfigProvider provider = (IConfigProvider)JsonSerializer.Deserialize(json, providerProp.FieldType);
                    providerProp.SetValue(null, provider);
                }
            }
        }

        public static bool Save(IConfigProvider filter = null)
        {
            try
            {
                // get all config providers declared in this class
                var confprop = typeof(Config).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                List<FieldInfo> providers = confprop.Where(prop => prop.FieldType.IsSubclassOf(typeof(IConfigProvider))).ToList();

                var sectionJson = new Dictionary<FieldInfo, string>();

                if(filter != null && filter.GetType().GetProperty("FileName") != null)
                {
                    FieldInfo provider = providers.Where(prov => prov.FieldType == filter.GetType()).FirstOrDefault();
                    if (provider == null)
                        return false;

                    var json = JsonSerializer.Serialize(provider.GetValue(null), provider.FieldType);
                    File.WriteAllText((string)filter.GetType().GetProperty("FileName", BindingFlags.Public | BindingFlags.Static).GetValue(null), json);
                    return true;
                }

                foreach(var providerProp in providers)
                {
                    var staticProvider = providerProp.FieldType;
                    var customFileProp = staticProvider.GetProperty("FileName", BindingFlags.Public | BindingFlags.Static);

                    if (customFileProp != null && customFileProp.GetValue(null) != null)
                    {
                        var json = JsonSerializer.Serialize(providerProp.GetValue(null), providerProp.FieldType);
                        File.WriteAllText((string)customFileProp.GetValue(null), json);
                    } else
                    {
                        var json = JsonSerializer.Serialize(providerProp.GetValue(null), providerProp.FieldType);
                        sectionJson.Add(providerProp, json);
                    }
                }


                string fulljson = JsonSerializer.Serialize<Config>(new Config(), new JsonSerializerOptions() { WriteIndented = true});
                File.WriteAllText(LauncherConfigFileName, fulljson);
            }
            catch (JsonException ex)
            {
                // TODO: error handling
                return false;
            }
            catch (IOException ex)
            {
                // TODO: error handling
                return false;
            }

            return true;
        }
    }
}