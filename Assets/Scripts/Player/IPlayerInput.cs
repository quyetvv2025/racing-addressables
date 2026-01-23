

public interface IPlayerInput
{
    float Throttle { get; }   // 0..1
    float Brake { get; }      // 0..1
    float Steer { get; }      // -1..1
}