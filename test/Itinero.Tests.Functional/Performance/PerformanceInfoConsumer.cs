using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Itinero.Tests.Functional.Performance
{
    /// <summary>
    /// A class that consumes performance information.
    /// </summary>
    public class PerformanceInfoConsumer
    {
        private readonly string _name; // Holds the name of this consumer.
        private readonly Timer _memoryUsageTimer; // Holds the memory usage timer.
        private readonly List<double> _memoryUsageLog = new(); // Holds the memory usage log.
        private long _memoryUsageLoggingDuration; // Holds the time spent on logging memory usage.
        private readonly int _iterations;

        /// <summary>
        /// Creates the a new performance info consumer.
        /// </summary>
        public PerformanceInfoConsumer(string name, int iterations = 1)
        {
            _name = name;
            _iterations = iterations;
        }

        /// <summary>
        /// Creates the a new performance info consumer.
        /// </summary>
        public PerformanceInfoConsumer(string name, int memUseLoggingInterval, int iterations = 1)
        {
            _name = name;
            _memoryUsageTimer =
                new Timer(LogMemoryUsage, null, memUseLoggingInterval, memUseLoggingInterval);
            _iterations = iterations;
        }

        /// <summary>
        /// Called when it's time to log memory usage.
        /// </summary>
        private void LogMemoryUsage(object state)
        {
            var ticksBefore = DateTime.Now.Ticks;
            lock (_memoryUsageLog) {
                GC.Collect();
                var p = Process.GetCurrentProcess();
                // ReSharper disable once PossibleInvalidOperationException
                _memoryUsageLog.Add(Math.Round((p.PrivateMemorySize64 - _memory.Value) / 1024.0 / 1024.0, 4));

                _memoryUsageLoggingDuration = _memoryUsageLoggingDuration + (DateTime.Now.Ticks - ticksBefore);
            }
        }

        /// <summary>
        /// Creates a new performance consumer.
        /// </summary>
        /// <param name="key"></param>
        public static PerformanceInfoConsumer Create(string key)
        {
            return new(key);
        }

        /// <summary>
        /// Holds the ticks when started.
        /// </summary>
        private long? _ticks;

        /// <summary>
        /// Holds the amount of memory before start.
        /// </summary>
        private long? _memory;

        /// <summary>
        /// Reports the start of the process/time period to measure.
        /// </summary>
        public void Start()
        {
            GC.Collect();

            var p = Process.GetCurrentProcess();
            lock (_memoryUsageLog) {
                _memory = p.PrivateMemorySize64;
            }

            _ticks = DateTime.Now.Ticks;
        }

        /// <summary>
        /// Reports a message in the middle of progress.
        /// </summary>
        public void Report(string message)
        {
            Log.Information(_name + ":" + message);
        }

        /// <summary>
        /// Reports a message in the middle of progress.
        /// </summary>
        public void Report(string message, params object[] args)
        {
            Log.Information(_name + ":" + message, args);
        }

        private int _previousPercentage;

        /// <summary>
        /// Reports a message about progress.
        /// </summary>
        public void Report(string message, long i, long max)
        {
            var currentPercentage = (int) Math.Round(i / (double) max * 10, 0);
            if (_previousPercentage == currentPercentage) {
                return;
            }

            Log.Information(_name + ":" + message, currentPercentage * 10);
            _previousPercentage = currentPercentage;
        }

        /// <summary>
        /// Reports the end of the process/time period to measure.
        /// </summary>
        public void Stop(string message)
        {
            if (_memoryUsageTimer != null) {
                // only dispose and stop when there IS a timer.
                _memoryUsageTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _memoryUsageTimer.Dispose();
            }

            if (!_ticks.HasValue) {
                return;
            }

            lock (_memoryUsageLog) {
                var seconds = new TimeSpan(DateTime.Now.Ticks - _ticks.Value - _memoryUsageLoggingDuration)
                    .TotalMilliseconds / 1000.0;
                var secondsPerIteration = seconds / _iterations;

                GC.Collect();
                var p = Process.GetCurrentProcess();

                Debug.Assert(_memory != null, nameof(_memory) + " != null");
                var memoryDiff = Math.Round((p.PrivateMemorySize64 - _memory.Value) / 1024.0 / 1024.0, 4);

                if (!string.IsNullOrWhiteSpace(message)) {
                    message = ": " + message;
                }

                var memUsage = $"Memory: {memoryDiff}";
                if (_memoryUsageLog.Count > 0) {
                    // there was memory usage logging.
                    var max = _memoryUsageLog.Max();
                    memUsage = $" mem usage: {max}";
                }

                var iterationMessage = "";
                if (_iterations > 1) {
                    iterationMessage = $"(* {_iterations} = {seconds:F3}s total) ";
                }

                Log.Information($"{_name}: Spent {secondsPerIteration:F3}s {iterationMessage}{memUsage} {message}");
            }
        }
    }

    /// <summary>
    /// Extension methods for the performance info class.
    /// </summary>
    public static class PerformanceInfoConsumerExtensions
    {
        /// <summary>
        /// Tests performance for the given action.
        /// </summary>
        public static void TestPerf(this Action action, string name)
        {
            var info = new PerformanceInfoConsumer(name);
            info.Start();
            action();
            info.Stop(string.Empty);
        }

        /// <summary>
        /// Tests performance for the given action.
        /// </summary>
        public static void TestPerf(this Action action, string name, int count)
        {
            var info = new PerformanceInfoConsumer(name + " x " + count.ToString(), 10000, count);
            info.Start();
            var message = string.Empty;
            while (count > 0) {
                action();
                count--;
            }

            info.Stop(message);
        }

        /// <summary>
        /// Tests performance for the given action.
        /// </summary>
        public static void TestPerf(this Func<string> action, string name)
        {
            var info = new PerformanceInfoConsumer(name);
            info.Start();
            var message = action();
            info.Stop(message);
        }

        /// <summary>
        /// Tests performance for the given action.
        /// </summary>
        public static void TestPerf(this Func<string> action, string name, int count)
        {
            var info = new PerformanceInfoConsumer(name + " x " + count.ToString(), 10000);
            info.Start();
            var message = string.Empty;
            while (count > 0) {
                message = action();
                count--;
            }

            info.Stop(message);
        }

        /// <summary>
        /// Tests performance for the given function.
        /// </summary>
        public static T TestPerf<T>(this Func<PerformanceTestResult<T>> func, string name)
        {
            var info = new PerformanceInfoConsumer(name);
            info.Start();
            var res = func();
            info.Stop(res.Message);
            return res.Result;
        }

        /// <summary>
        /// Tests performance for the given function.
        /// </summary>
        public static T TestPerf<T>(this Func<PerformanceTestResult<T>> func, string name, int count)
        {
            var info = new PerformanceInfoConsumer(name + " x " + count, 10000);
            info.Start();
            PerformanceTestResult<T> res = null;
            while (count > 0) {
                res = func();
                count--;
            }

            if (res == null) {
                throw new ArgumentNullException(nameof(res));
            }

            info.Stop(res.Message);
            return res.Result;
        }

        /// <summary>
        /// Tests performance for the given function.
        /// </summary>
        public static TResult TestPerf<T, TResult>(this Func<T, PerformanceTestResult<TResult>> func, string name,
            T a)
        {
            var info = new PerformanceInfoConsumer(name);
            info.Start();
            var res = func(a);
            info.Stop(res.Message);
            return res.Result;
        }

        /// <summary>
        /// Tests performance for the given function.
        /// </summary>
        public static async Task<TResult> TestPerfAsync<T, TResult>(this Func<T, Task<PerformanceTestResult<TResult>>> func, string name, T a,
            int count)
        {
            var info = new PerformanceInfoConsumer(name, count);
            info.Start();
            var res = await func(a);
            count--;
            while (count > 0) {
                res = await func(a);
                count--;
            }

            info.Stop(res.Message);
            return res.Result;
        }

        /// <summary>
        /// Tests performance for the given function.
        /// </summary>
        public static TResult TestPerf<T, TResult>(this Func<T, PerformanceTestResult<TResult>> func, string name, T a,
            int count)
        {
            var info = new PerformanceInfoConsumer(name, count);
            info.Start();
            var res = func(a);
            count--;
            while (count > 0) {
                res = func(a);
                count--;
            }

            info.Stop(res.Message);
            return res.Result;
        }
    }

    /// <summary>
    /// An object containing feedback from a tested function.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PerformanceTestResult<T>
    {
        /// <summary>
        /// Creates a new peformance test result.
        /// </summary>
        /// <param name="result"></param>
        public PerformanceTestResult(T result)
        {
            Result = result;
        }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        public T Result { get; set; }
    }
}