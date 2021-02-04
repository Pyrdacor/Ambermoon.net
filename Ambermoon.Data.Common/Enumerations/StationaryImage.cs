using System.ComponentModel;

namespace Ambermoon.Data.Enumerations
{
    public enum StationaryImage
    {
        Horse = 0x01,
        Raft = 0x02,
        Boat = 0x04,
        SandLizard = 0x08,
        SandShip = 0x10
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class StationaryImageExtensions
    {
        public static StationaryImage ToStationaryImage(this TravelType travelType)
        {
            return travelType switch
            {
                TravelType.Horse => StationaryImage.Horse,
                TravelType.Raft => StationaryImage.Raft,
                TravelType.Ship => StationaryImage.Boat,
                TravelType.SandLizard => StationaryImage.SandLizard,
                TravelType.SandShip => StationaryImage.SandShip,
                _ => throw new AmbermoonException(ExceptionScope.Application, $"Travel type {travelType} does not use a stationary image.")
            };
        }
    }
}
