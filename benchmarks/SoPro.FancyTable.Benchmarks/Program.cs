using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

var config = DefaultConfig.Instance.AddJob(Job.Default.DontEnforcePowerPlan().AsMutator());
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
