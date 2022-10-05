namespace Itinero.Profiles;

public abstract class RouterDbProfileHandler
{
    public abstract uint FactorForward { get; }

    public abstract uint FactorBackward { get; }

    public abstract uint SpeedForward { get; }

    public abstract uint SpeedBackward { get; }
}
