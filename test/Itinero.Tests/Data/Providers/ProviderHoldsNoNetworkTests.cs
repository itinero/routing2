using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Itinero.Data.Providers;
using Itinero.Data.Usage;
using Itinero.Network;
using Xunit;

namespace Itinero.Tests.Data.Providers;

/// <summary>
/// The precondition for having no leases: when a network is replaced, nothing on the provider side
/// keeps it alive, so it and its provider are collected and the files close themselves.
/// </summary>
/// <remarks>
/// A field holding a network is invisible until a heap dump, which is why this is a test and not a
/// review note. Shipped providers only — a test double is free to hold whatever it likes.
/// </remarks>
public class ProviderHoldsNoNetworkTests
{
    [Fact]
    public void NoProviderSideField_HoldsARoutingNetwork()
    {
        var types = ProviderTypes().ToList();
        Assert.Contains(typeof(DefaultDataProvider), types);
        Assert.Contains(typeof(Itinero.IO.Osm.OsmDataProvider), types);

        var offenders = new List<string>();
        foreach (var type in types)
        {
            Walk(type, type.Name, new HashSet<Type>(), offenders);
        }

        Assert.True(offenders.Count == 0,
            "a provider-side field holds a network, which pins it past a rotation:" +
            Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void TheTestItself_FindsAFieldThatHoldsOne()
    {
        var offenders = new List<string>();

        Walk(typeof(Offender), nameof(Offender), new HashSet<Type>(), offenders);

        Assert.Single(offenders);
    }

    private sealed class Offender
    {
        private readonly RoutingNetwork? _network = null;
    }

    private static IEnumerable<Type> ProviderTypes()
    {
        var assemblies = new[] { typeof(DataProviderBase).Assembly, typeof(Itinero.IO.Osm.OsmDataProvider).Assembly };

        return assemblies.SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => typeof(DataProviderBase).IsAssignableFrom(t) ||
                        typeof(IDataUseListener).IsAssignableFrom(t));
    }

    // follows fields into Itinero's own types, so a provider holding something that holds a network
    // is caught too. Stops at RouterDb: reaching the newest network through it is the design.
    private static void Walk(Type type, string path, HashSet<Type> visited, List<string> offenders)
    {
        if (!visited.Add(type)) return;

        foreach (var field in Fields(type))
        {
            var fieldType = field.FieldType;
            var where = $"{path}.{field.Name} ({fieldType.Name})";

            if (HoldsANetwork(fieldType))
            {
                offenders.Add(where);
                continue;
            }

            foreach (var argument in fieldType.GetGenericArguments())
            {
                // a weak reference is the sanctioned fallback, not a leak.
                if (fieldType.GetGenericTypeDefinition() == typeof(WeakReference<>)) break;
                if (HoldsANetwork(argument)) offenders.Add($"{where}<{argument.Name}>");
            }

            if (fieldType == typeof(RouterDb)) continue;
            if (fieldType.Assembly != typeof(RoutingNetwork).Assembly) continue;

            Walk(fieldType, where, visited, offenders);
        }
    }

    private static IEnumerable<FieldInfo> Fields(Type type)
    {
        for (var t = type; t != null && t != typeof(object); t = t.BaseType)
        {
            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                              BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                yield return field;
            }
        }
    }

    // the type itself, or an interface a network can be assigned to.
    private static bool HoldsANetwork(Type type)
    {
        if (type == typeof(RoutingNetwork)) return true;

        return type.IsInterface && type.IsAssignableFrom(typeof(RoutingNetwork));
    }
}
