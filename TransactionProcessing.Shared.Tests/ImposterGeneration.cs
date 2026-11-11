using Imposter.Abstractions;
using Microsoft.Extensions.Configuration;

[assembly: GenerateImposter(typeof(IConfiguration))]
[assembly: GenerateImposter(typeof(IConfigurationSection))]
