using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Itinero.Tests.Functional.Performance;

namespace Itinero.Tests.Functional.Tests;

/// <summary>
/// Abstract definition of a functional test.
/// </summary>
/// <typeparam name="TOut"></typeparam>
/// <typeparam name="TIn"></typeparam>
public abstract class FunctionalTest<TOut, TIn>
{
    /// <summary>
    /// Gets the name of this test.
    /// </summary>
    private string Name => this.GetType().Name;

    /// <summary>
    /// Gets or sets the track performance track.
    /// </summary>
    public bool TrackPerformance { get; set; } = true;

    /// <summary>
    /// Gets or sets the logging flag.
    /// </summary>
    public bool Log { get; set; } = true;

    /// <summary>
    /// Executes this test for the given input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="name">The test name.</param>
    /// <param name="count">The count.</param>
    /// <returns>The output.</returns>
    public Task<TOut> RunAsync(TIn input = default, string? name = null, int count = 1)
    {
        try
        {
            return this.TrackPerformance ? this.RunPerformanceAsync(input, name: name, count: count) : this.ExecuteAsync(input);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, $"Running {this.Name} with inputs {input} failed");

            throw;
        }
    }

    /// <summary>
    /// Executes this test for the given input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="count">The # of times to repeat the test.</param>
    /// <param name="name">The test name.</param>
    /// <returns>The output.</returns>
    public async Task<TOut> RunPerformanceAsync(TIn input, int count = 1, string? name = null)
    {
        name ??= this.Name;

        Func<TIn, Task<PerformanceTestResult<TOut>>>
            executeFunc = async (i) => new PerformanceTestResult<TOut>(await this.ExecuteAsync(i));
        return await executeFunc.TestPerfAsync(name, input, count);
    }

    /// <summary>
    /// Executes this test for the given input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>The output.</returns>
    protected abstract Task<TOut> ExecuteAsync(TIn input);

    /// <summary>
    /// Asserts that the given value is true.
    /// </summary>
    /// <param name="value">The value to verify.</param>
    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
    protected void True(bool value)
    {
        if (!value)
        {
            throw new Exception("Assertion failed, expected true");
        }
    }

    public void NotNull(object o)
    {
        if (o == null)
        {
            throw new ArgumentNullException(nameof(o));
        }
    }

    public void NotNull(object o, string message)
    {
        if (o == null)
        {
            throw new ArgumentException("Null detected: " + message);
        }
    }

    public void AssertContains(object o, IEnumerable xs)
    {
        foreach (var x in xs)
        {
            if (x.Equals(o))
            {
                return;
            }
        }

        throw new Exception($"Element {o} was not found");
    }

    /// <summary>
    /// Write a log event with the Informational level.
    /// </summary>
    /// <param name="message">The log message.</param>
    protected void Information(string message)
    {
        if (!this.Log)
        {
            return;
        }

        Serilog.Log.Information(message);
    }
}
