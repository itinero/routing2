using Itinero.Data.Attributes;

namespace Itinero.Profiles
{
    public class DefaultProfile : Profile
    {
        public override EdgeFactor Factor(IAttributeCollection attributes)
        {
            return new EdgeFactor(1, 1, 1, 1);
        }
    }
}