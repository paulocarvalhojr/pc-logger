
# Kaive.Extensions.Logging

VSTS Status: [![VSTS](https://img.shields.io/vso/build/vertisnet/62009b64-8b3f-4d27-93c2-95512cf9951f/11.svg)](https://vertisnet.visualstudio.com/Product_Development/_build/index?context=allDefinitions&path=\Kaive&definitionId=11&_a=completed)

# Introduction

The _Kaive.Extensions.Logging_ repository includes projects containing the common logging definitions, including types used for multiple logging pipeline configurations.

This repository is part of **Kaive Platform**. You can find samples, documentation and getting started instructions for **Kaive Platform** at the main [Kaive](https://vertisnet.visualstudio.com/Product_Development/_git/Kaive) repo.

# Versioning

We use [SemVer](http://semver.org/) for versioning. For a full list of available versions, see the [tags on this repository](https://vertisnet.visualstudio.com/Product_Development/_git/Kaive.Extensions.Common/tags)

# Target Frameworks

See the table below for a list of supported target frameworks:

| Target Platform Name   | TFM Alias   | Versions |
|:-----------------------|:-----------:|:--------:|
| .NET Platform Standard | netstandard | 1.6, 2.1 |
| .NET Core              | netcoreapp  | 1.0, 2.1 |
| .NET Framework         | net         | 4.5.1    |

# Sample Code

## ASP.Net Core 2.1

When using ASP.Net Core 2.1, all the configuration should be done at the `Program.cs` file, as shown below.

How does it work? The `ConfigureAppConfiguration` delegate defines the host configuration options, which
are then used into the `ConfigureLogging` delegate to setup the required Kaive Log Settings. Finally,
the `UseKaiveLogging` extension method injects its own custom Logger Factory, which will forward all
logger requests to the required sinks, as per configured settings.

```csharp
	/// <summary>
	/// Main application definition
	/// </summary>
	public class Program
    {
	    /// <summary>
	    /// Application's entry point.
	    /// </summary>
	    /// <param name="args">Argumentos de inicialização.</param>
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        /// <summary>
        /// WebHost building process.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IWebHost BuildWebHost(string[] args)
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    config
                        .AddJsonFile("appsettings.json", false, true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);

                    if (env.IsDevelopment())
                    {
                        var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                        if (appAssembly != null)
                            config.AddUserSecrets(appAssembly, true);
                    }

                    config.AddEnvironmentVariables();
                    if (args != null)
                        config.AddCommandLine(args);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    if (!LogManager.GetInstance().IsConfigured)
                        LogManager.Setup(hostingContext.Configuration.GetLogSettings());
                })
                .UseKaiveLogging()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();
        }
    }
```