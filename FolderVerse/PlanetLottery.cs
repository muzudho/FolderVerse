namespace FolderVerse;
using System;

public sealed class PlanetLottery
{
    private readonly PlanetHeadSystem[] _deck=
    {
        PlanetHeadSystem.Fish,PlanetHeadSystem.Fish,PlanetHeadSystem.Fish,
        PlanetHeadSystem.Monkey,PlanetHeadSystem.Monkey,PlanetHeadSystem.Monkey,
        PlanetHeadSystem.Dog,PlanetHeadSystem.Dog,PlanetHeadSystem.Dog
    };
    private int _next=9;

    public void Reset()=>_next=_deck.Length;

    public void Draw(WorldSetup world,Random random)
    {
        while(true)
        {
            if(_next==_deck.Length)
            {
                random.Shuffle(_deck);
                _next=0;
            }
            var range=_deck[_next++] switch
            {
                PlanetHeadSystem.Fish=>(2d/3,1d),
                PlanetHeadSystem.Monkey=>(1d/3,Math.BitDecrement(2d/3)),
                _=>(0d,Math.BitDecrement(1d/3))
            };
            if(world.TrySetWorld(random.Next(),range.Item1,range.Item2))return;
        }
    }
}
