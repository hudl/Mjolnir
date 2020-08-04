// using Hudl.Mjolnir.Bulkhead;
// using Hudl.Mjolnir.External;
// using System;
// using System.Diagnostics;
// using System.Threading;
// using System.Threading.Tasks;
// using Hudl.Mjolnir.Config;
// using Amazon.XRay.Recorder.Core;

// namespace Hudl.Mjolnir.Command
// {
//     /// <summary>
//     /// Executes a command within a bulkhead.
//     /// </summary>
//     internal interface IBulkheadInvoker
//     {
//         Task<TResult> ExecuteWithBulkheadAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct);
//         TResult ExecuteWithBulkhead<TResult>(SyncCommand<TResult> command, CancellationToken ct);
//     }

//     internal class BulkheadInvoker : IBulkheadInvoker
//     {
//         private readonly IBreakerInvoker _breakerInvoker;
//         private readonly IBulkheadFactory _bulkheadFactory;
//         private readonly IMetricEvents _metricEvents;
//         private readonly MjolnirConfiguration _config;

//         public BulkheadInvoker(IBreakerInvoker breakerInvoker, IBulkheadFactory bulkheadFactory, IMetricEvents metricEvents, MjolnirConfiguration config)
//         {
//             _breakerInvoker = breakerInvoker ?? throw new ArgumentNullException(nameof(breakerInvoker));
//             _bulkheadFactory = bulkheadFactory ?? throw new ArgumentNullException(nameof(bulkheadFactory));
//             _metricEvents = metricEvents ?? throw new ArgumentNullException(nameof(metricEvents));
//             _config = config ?? throw new ArgumentNullException(nameof(config));
//         }

//         // Note: Bulkhead rejections shouldn't count as failures to the breaker. If a downstream
//         // dependency is slow, the bulkhead will fill up, but the breaker + timeouts will already be
//         // providing protection against that. If the bulkhead is filling up because of a surge of
//         // requests, the rejections will just be a way of shedding load - the breaker and
//         // downstream dependency may be just fine, and we want to keep them that way.

//         // We'll neither mark these as success *nor* failure, since they really didn't even execute
//         // as far as the breaker and downstream dependencies are concerned. That happens naturally
//         // here since the bulkhead won't call the breaker invoker if the bulkhead rejects first.

//         public async Task<TResult> ExecuteWithBulkheadAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct)
//         {
//             try
//             {
//                 if (AWSXRayRecorder.Instance.IsEntityPresent()) AWSXRayRecorder.Instance.BeginSubsegment("ExecuteWithBulkheadAsync");
//                 var bulkhead = _bulkheadFactory.GetBulkhead(command.BulkheadKey);
//                 bool onlyMetrics = false;
//                 if (!bulkhead.TryEnter())
//                 {
//                     onlyMetrics = _config.BulkheadMetricsOnly || _config.GetBulkheadConfiguration(command.BulkheadKey.Name).MetricsOnly;
//                     _metricEvents.RejectedByBulkhead(bulkhead.Name, command.Name);
//                     if (!onlyMetrics) throw new BulkheadRejectedException();
//                 }

//                 if (!onlyMetrics) _metricEvents.EnterBulkhead(bulkhead.Name, command.Name);

//                 // This stopwatch should begin stopped (hence the constructor instead of the usual
//                 // Stopwatch.StartNew(). We'll only use it if we aren't using circuit breakers.
//                 var stopwatch = new Stopwatch();

//                 // If circuit breakers are enabled, the execution will happen down in the circuit
//                 // breaker invoker. If they're disabled, it'll happen here. Since we want an accurate
//                 // timing of the method execution, we'll use this to track where the execution happens
//                 // and then set ExecutionTimeMillis in the finally block conditionally.
//                 var executedHere = false;
//                 try
//                 {
//                     if (_config.UseCircuitBreakers)
//                     {
//                         return await _breakerInvoker.ExecuteWithBreakerAsync(command, ct).ConfigureAwait(false);
//                     }

//                     executedHere = true;
//                     stopwatch.Start();
//                     return await command.ExecuteAsync(ct).ConfigureAwait(false);
//                 }
//                 finally
//                 {
//                     if (!onlyMetrics)
//                     {
//                         bulkhead.Release();
//                         _metricEvents.LeaveBulkhead(bulkhead.Name, command.Name);
//                     }
//                     // If not executed here, the circuit breaker invoker will set the execution time.
//                     if (executedHere)
//                     {
//                         stopwatch.Stop();
//                         command.ExecutionTimeMillis = stopwatch.Elapsed.TotalMilliseconds;
//                     }
//                 }
//             }
//             catch (Exception e)
//             {
//                 if (AWSXRayRecorder.Instance.IsEntityPresent()) AWSXRayRecorder.Instance.AddException(e);
//                 throw;
//             }
//             finally
//             {
//                 if (AWSXRayRecorder.Instance.IsEntityPresent()) AWSXRayRecorder.Instance.EndSubsegment();
//             }
//         }

//         public TResult ExecuteWithBulkhead<TResult>(SyncCommand<TResult> command, CancellationToken ct)
//         {
//             try
//             {
//                 if (AWSXRayRecorder.Instance.IsEntityPresent()) AWSXRayRecorder.Instance.BeginSubsegment("ExecuteWithBulkhead");
//                 var bulkhead = _bulkheadFactory.GetBulkhead(command.BulkheadKey);
//                 bool onlyMetrics = false;
//                 if (!bulkhead.TryEnter())
//                 {
//                     onlyMetrics = _config.BulkheadMetricsOnly || _config.GetBulkheadConfiguration(command.BulkheadKey.Name).MetricsOnly;
//                     _metricEvents.RejectedByBulkhead(bulkhead.Name, command.Name);
//                     if (!onlyMetrics) throw new BulkheadRejectedException();
//                 }

//                 if (!onlyMetrics) _metricEvents.EnterBulkhead(bulkhead.Name, command.Name);

//                 // This stopwatch should begin stopped (hence the constructor instead of the usual
//                 // Stopwatch.StartNew(). We'll only use it if we aren't using circuit breakers.
//                 var stopwatch = new Stopwatch();

//                 // If circuit breakers are enabled, the execution will happen down in the circuit
//                 // breaker invoker. If they're disabled, it'll happen here. Since we want an accurate
//                 // timing of the method execution, we'll use this to track where the execution happens
//                 // and then set ExecutionTimeMillis in the finally block conditionally.
//                 var executedHere = false;
//                 try
//                 {
//                     if (_config.UseCircuitBreakers)
//                     {
//                         return _breakerInvoker.ExecuteWithBreaker(command, ct);
//                     }

//                     executedHere = true;
//                     stopwatch.Start();
//                     return command.Execute(ct);
//                 }
//                 finally
//                 {
//                     if (!onlyMetrics)
//                     {
//                         bulkhead.Release();
//                         _metricEvents.LeaveBulkhead(bulkhead.Name, command.Name);
//                     }
//                     // If not executed here, the circuit breaker invoker will set the execution time.
//                     if (executedHere)
//                     {
//                         stopwatch.Stop();
//                         command.ExecutionTimeMillis = stopwatch.Elapsed.TotalMilliseconds;
//                     }
//                 }
//             }
//             catch (Exception e)
//             {
//                 if (AWSXRayRecorder.Instance.IsEntityPresent()) AWSXRayRecorder.Instance.AddException(e);
//                 throw;
//             }
//             finally
//             {
//                 if (AWSXRayRecorder.Instance.IsEntityPresent()) AWSXRayRecorder.Instance.EndSubsegment();
//             }
//         }
//     }
// }
