using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Hudl.Mjolnir.Tests.Configuration.Helpers
{    
    public class ExampleJsonConfigProvider
    {
        private const string ConfigurationPath = "Configuration/Helpers/test.json";
        private static TimeSpan _updateTimeInterval = TimeSpan.FromSeconds(30);

        private ExampleMjolnirConfiguration _currentConfig;  
        private IConfigurationRoot _root;

        public ExampleJsonConfigProvider(TimeSpan? updateTime = null)
        {
            _root = LoadConfigFromJsonFile();
            _updateTimeInterval = updateTime ?? _updateTimeInterval;
            
            // Run periodic updates on config
            Task.Run(ReloadConfig);
        }

        private async Task ReloadConfig()
        {
            while (true)
            {
                _root = LoadConfigFromJsonFile();

                // We can add some deep comparision between _currentConfig and new config here so
                // we notify our observers only when config really changes
                _currentConfig?.Notify();
                
                await Task.Delay(_updateTimeInterval);
            }
        }

        public ExampleMjolnirConfiguration GetConfig()
        {
            if(_currentConfig != null)
            {
                return _currentConfig;
            }
            
            const string rootKey = "testconfig";
            var section = _root.GetSection(rootKey);

            if(section == null || section.Value == null && !section.GetChildren().Any())
            {
                _currentConfig = new ExampleMjolnirConfiguration();
                return default(ExampleMjolnirConfiguration);
            }

            var config = new ExampleMjolnirConfiguration();
            section.Bind(config);
            _currentConfig = config;
            return config;
        }
        
        // This function is only to fix the bug in library which should be fixed in 
        // next RTM.
        // See more info http://www.natemcmaster.com/blog/2017/02/01/project-json-to-csproj-part2/#dotnet-test-xunit
        // TODO remove me after updating to newer version of .NET Runtime
        // It will be replaced by Directory.GetCurrentDirectory();
        private static string GetCurrentDirectory()
        {
            var currentDirectory = AppContext.BaseDirectory;
            return !currentDirectory.Contains("bin" + Path.DirectorySeparatorChar) ? 
                currentDirectory : 
                currentDirectory.Substring(0, currentDirectory.LastIndexOf("bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal));
        }   

        private static IConfigurationRoot LoadConfigFromJsonFile()
        {
            var msBuilder = new ConfigurationBuilder();
            msBuilder.SetBasePath(GetCurrentDirectory());
            msBuilder.AddJsonFile(ConfigurationPath, true, true);
            return msBuilder.Build();
        }

    }
}