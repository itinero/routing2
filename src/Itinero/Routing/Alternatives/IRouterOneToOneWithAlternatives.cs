namespace Itinero.Routing.Alternatives;

public interface IRouterOneToOneWithAlternatives : IRouterOneToOne
{
    AlternativeRouteSettings AlternativeRouteSettings
    {
        get;
    }
}
