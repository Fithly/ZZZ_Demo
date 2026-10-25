using System.Collections.Generic;

// 管理和创建Entity实体
public class World
{
    public List<Entity> entities = new List<Entity>();

    int nextID = 1;

    public Entity CreateEntity()
    {
        Entity e = new Entity();
        e.ID = nextID++;

        entities.Add(e);

        return e;
    }

    public Entity GetEntityById(int id)
    {
        foreach (var entity in entities)
        {
            if(entity.ID == id) 
                return entity;
        }
        return null;
    }

    public void RemoveEntity(int id)
    {
        for (int i = entities.Count - 1; i >= 0; i--)
        {
            if (entities[i].ID == id)
            {
                entities.RemoveAt(i);
                return;
            }
        }
    }
}