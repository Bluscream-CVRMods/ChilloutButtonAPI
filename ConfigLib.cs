using Newtonsoft.Json;
using System;
using System.IO;

namespace Libraries {
    public class ConfigLib<T> where T : class {
        public ConfigLib(string configPath) {
            ConfigPath = configPath;

            string PathToWatch = Path.GetDirectoryName(ConfigPath);

            if (PathToWatch != null) {
                FileSystemWatcher watcher = new(PathToWatch, Path.GetFileName(ConfigPath)) {
                    NotifyFilter = NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };

                watcher.Changed += UpdateConfig;
            }

            if (!File.Exists(ConfigPath)) {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Activator.CreateInstance(typeof(T)), Newtonsoft.Json.Formatting.Indented));
            }

            InternalConfig = JsonConvert.DeserializeObject<T>(File.ReadAllText(ConfigPath));

            System.Timers.Timer timer = new(1000);

            string ConfigCache = JsonConvert.SerializeObject(InternalConfig);

            timer.Elapsed += (sender, args) => {
                if (JsonConvert.SerializeObject(InternalConfig) != ConfigCache) {
                    ConfigCache = JsonConvert.SerializeObject(InternalConfig);

                    SaveConfig();

                    //MessageBox.Show("Saved!");
                }
            };

            timer.Enabled = true;
            timer.Start();
        }

        private string ConfigPath {
            get;
        }

        public T InternalConfig {
            get; private set;
        }

        public event Action OnConfigUpdated;

        private void UpdateConfig(object obj, FileSystemEventArgs args) {
            try {
                T UpdatedConfig = JsonConvert.DeserializeObject<T>(File.ReadAllText(ConfigPath));

                if (UpdatedConfig != null) {
                    foreach (System.Reflection.PropertyInfo newProp in UpdatedConfig.GetType()?.GetProperties()) {
                        System.Reflection.PropertyInfo OldProp = InternalConfig.GetType().GetProperty(newProp?.Name);

                        if (OldProp != null && newProp.GetValue(UpdatedConfig) != OldProp.GetValue(InternalConfig)) // Property Existed Before & Has Changed
                        {
                            InternalConfig = UpdatedConfig;

                            OnConfigUpdated?.Invoke();
                            break;
                        }
                    }
                }
            } catch {
            }
        }

        public void SaveConfig() {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(InternalConfig, Newtonsoft.Json.Formatting.Indented));
        }
    }

    public static class ConfigExt {
        public static bool DoesInstanceMatch<T>(this T instance, T CompareTo) where T : class {
            foreach (System.Reflection.PropertyInfo prop in instance.GetType().GetProperties()) {
                if (JsonConvert.SerializeObject(prop.GetValue(instance)) != JsonConvert.SerializeObject(prop.GetValue(CompareTo))) {
                    return false;
                }
            }

            return true;
        }
    }
}
