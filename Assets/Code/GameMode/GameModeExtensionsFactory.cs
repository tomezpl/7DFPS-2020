using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class GameModeExtensionsFactory
{
    /// <summary>
    /// Sets up the gamemode for the client.
    /// </summary>
    /// <param name="gameManager"></param>
    public static void CreateExtensionsForPlayer(GameManager target, string extensionsTypeName)
    {
        GameModeGameManagerExtension.Create(Type.GetType(extensionsTypeName), target);
    }
}
