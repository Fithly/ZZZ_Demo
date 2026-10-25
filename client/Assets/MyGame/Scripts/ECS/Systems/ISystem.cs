using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISystem
{
    void Update(World world, float deltaTime);
}