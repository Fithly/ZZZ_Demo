using System.Collections.Generic;

public class SystemScheduler
{
    List<ISystem> systems = new List<ISystem>();

    public void AddSystem(ISystem system)
    {
        systems.Add(system);
    }

    public void Update(World world, float deltaTime)
    {
        foreach (var system in systems)
        {
            system.Update(world, deltaTime);
        }
    }
}