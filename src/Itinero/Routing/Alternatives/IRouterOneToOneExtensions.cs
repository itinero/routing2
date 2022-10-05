namespace Itinero.Routing.Alternatives
{
    public static class IRouterOneToOneExtensions
    {
        public static IRouterOneToOneWithAlternatives WithAlternatives(this IRouterOneToOne oneToOne,
            AlternativeRouteSettings settings)
        {
            var withAlternatives = new Router(oneToOne.Network, oneToOne.Settings)
            {
                AlternativeRouteSettings = settings,
                Source = oneToOne.Source,
                Target = oneToOne.Target
            };
            return withAlternatives;
        }
    }
}
